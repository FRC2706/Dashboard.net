using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Dashboard.net.DataHandlers;
using NetworkTables;

namespace Dashboard.net.Element_Controllers
{
    /// <summary>
    /// The object in charge of dealing with the master caution button.
    /// </summary>
    public class Cautioner : Controller, INotifyPropertyChanged
    {
        private static readonly string DEFAULTCONTENT = "Master Caution";

        #region NetworkTable constants
        /// <summary>
        /// The networktables location for the addd queue
        /// </summary>
        private static readonly string NTADDKEY = "SmartDashboard/Warnings/AddQueue";
        /// <summary>
        /// The networktables location for the remove queue
        /// </summary>
        private static readonly string NTREMOVEKEY = "SmartDashboard/Warnings/RemoveQueue";
        /// <summary>
        /// The networktables location for the current warnings being displayed.
        /// </summary>
        private static readonly string NTCURRENTWARNINGS = "SmartDashboard/Warnings/CurrentWarnings";

        /// <summary>
        /// The networktables location for the status of the cube being in the robot or not.
        /// </summary>
        private static readonly string CUBESTATUSKEY = "SmartDashboard/CubeIn";
        #endregion

        #region Data file constants
        /// <summary>
        /// The key location for the data hashtable boolean of whether or not the animation should be enabled
        /// </summary>
        public static readonly string ENABLEDKEY = "enabled";
        /// <summary>
        /// The key location for the data hashtable list for the ignore list.
        /// </summary>
        public static readonly string IGNOREKEY = "ignoreList";
        #endregion

        private string _warningMessage = DEFAULTCONTENT;
        /// <summary>
        /// The current warning message being displayed on the warning button
        /// </summary>
        public string WarningMessage
        {
            get
            {
                return _warningMessage;
            }
            private set
            {
                _warningMessage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("WarningMessage"));
            }
        }

        // Brightnesses for the power cube being lit or dimmed
        private static readonly double POWERCUBELIT = 1, POWERCUBEDIMMED = 0.3;
        private double powerCubeBrightess = POWERCUBEDIMMED;
        /// <summary>
        /// The brightness of the power cube
        /// </summary>
        public double PowerCubeBrightness
        {
            get
            {
                return powerCubeBrightess;
            }

            private set
            {
                powerCubeBrightess = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PowerCubeBrightness"));
            }
        }

        private ListView ignoreListViewer;

        /// <summary>
        /// The data that must be saved to the data file
        /// </summary>
        private Hashtable DataToSave
        {
            get
            {
                return new Hashtable()
                {
                    {IGNOREKEY, IgnoreList },
                    {ENABLEDKEY, IsEnabled }
                };
            }
        }

        /// <summary>
        /// RelayCommand for handling what happens if the user clicks the warning button
        /// </summary>
        public RelayCommand CautionerClicked { get; private set; }

        private bool _isEnabled;
        /// <summary>
        /// Whether or not the animaton is enabled, as determined by the user.
        /// </summary>
        private bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                _isEnabled = value;
                DataDealer.WriteCautionerData(DataToSave);
            }
        }
 
        /// <summary>
        /// True when there is a warning currently being displayed, false otherwise.
        /// </summary>
        public bool IsWarning
        {
            get
            {
                return WarningList.Count > 0;
            }
        }

        /// <summary>
        /// List of warnings that were set by the user to be ignored.
        /// </summary>
        public ObservableCollection<string> IgnoreList { get; private set; }

        public ObservableCollection<string> WarningList { get; private set; }
        DispatcherTimer _caller;
        DispatcherTimer Caller
        {
            get
            {
                // If it doesn't exist, make it.
                if (_caller == null)
                {
                    _caller = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, 0, 1, 500)
                    };
                    _caller.Tick += Execute;
                }

                return _caller;
            }
        }
        Storyboard storyboard;

        public Cautioner(Master controller) : base(controller)
        {
            WarningList = new ObservableCollection<string>();

            // Subscribe to the collection changed event in order to refresh
            WarningList.CollectionChanged += Refresh;

            CautionerClicked = new RelayCommand()
            {
                CanExecuteDeterminer = () => true,
                FunctionToExecute = (object parameter) =>
                {
                    IsEnabled = !IsEnabled;
                    SetAnimation(true);
                }
            };

            // Set the initial state of the enabled boolean from the data file
            Hashtable cautionerData = DataDealer.ReadCautionerData();
            if (cautionerData == null)
            {
                _isEnabled = true;
                IgnoreList = new ObservableCollection<string>();

                DataDealer.WriteCautionerData(DataToSave);
            }
            else
            {
                _isEnabled = (bool)cautionerData[ENABLEDKEY];
                IgnoreList = (ObservableCollection<string>)cautionerData[IGNOREKEY];
            }
            IgnoreList.CollectionChanged += Refresh;

            // Listen for key changes on the add and remove queues
            master._Dashboard_NT.AddKeyListener(NTADDKEY, OnNTKWarningAdded);
            master._Dashboard_NT.AddKeyListener(NTREMOVEKEY, OnNTWarningRemoved);
            // Add a listener to the currrent warnings networktable property in case another program changes it.
            master._Dashboard_NT.AddKeyListener(NTCURRENTWARNINGS, (string key, Value value) => UpdateNTArray());

            // Listen for key changes in the cubeIn boolean
            master._Dashboard_NT.AddKeyListener(CUBESTATUSKEY, CubeStatusChanged);
        }

        private void CubeStatusChanged(string key, bool value)
        {
            PowerCubeBrightness = (value) ? POWERCUBELIT : POWERCUBEDIMMED;
        }

        #region Networktables stuff
        /// <summary>
        /// The warnings that were added from networktables so that the robot can only remove
        /// warnings that it set.
        /// </summary>
        private List<string> NtAddedWarnings = new List<string>();
        /// <summary>
        /// Removes the warning from the warnings queue from input from the networktables table
        /// </summary>
        /// <param name="warningToRemove"></param>
        private void OnNTWarningRemoved(string key, string warningToRemove)
        {
            // Confirm that the object type is a string
            if (!NtAddedWarnings.Contains(warningToRemove)) return;

            // Stop showing the warnings
            StopWarning(warningToRemove, true);
        }

        /// <summary>
        /// Adds a warning from the networktables Warnings subtable by listening for the key to change.
        /// </summary>
        /// <param name="warningToDisplay"></param>
        private void OnNTKWarningAdded(string key, string warningToDisplay)
        {
            // Begin displaying the warning
            SetWarning(warningToDisplay, true);
        }

        /// <summary>
        /// Set to true if the dashboard updates the CurrentWarnings string array in networktables.
        /// </summary>
        private bool justUpdatedWarningArray;
        /// <summary>
        /// Updates the networktables warning array every time a warning is added or removed.
        /// </summary>
        private void UpdateNTArray()
        {
            // If it was the dashboard that did the change, don't keep changing it and calling this method over and over.
            if (justUpdatedWarningArray)
            {
                justUpdatedWarningArray = false;
                return;
            }
            else
            {
                master._Dashboard_NT.SetStringArray(NTCURRENTWARNINGS, WarningList.ToArray<string>());
                justUpdatedWarningArray = true;
            }
        }
        #endregion

        /// <summary>
        /// Called when the main window is set in order to set the storyboard object variable
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnMainWindowSet(object sender, MainWindow e)
        {
            storyboard = (Storyboard)e.FindResource("animate_caution");
            e.Master_Caution.MouseRightButtonDown += IgnoreListViewer_MouseRightButtonDown;
            ignoreListViewer = e.IgnoreListViewer;
            ignoreListViewer.SelectionChanged += IgnoreListViewer_SelectionChanged;
        }

        /// <summary>
        /// Handles a selection change in the ignore list viewer in order to remove the selected item from the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IgnoreListViewer_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ignoreListViewer.SelectedItem == null) return;
            RemoveFromIgnoreList(ignoreListViewer.SelectedItem.ToString());
        }

        private void IgnoreListViewer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Just add the current warning to the ignore list
            AddToIngoreList(WarningMessage);
        }

        #region Basic start stop execute functions
        /// <summary>
        /// Begins to display the warnings.
        /// </summary>
        /// <param name="startCaller">Whether or not we should start the DispatcherTimer in order to execute
        /// the execute method over and over again later on.</param>
        private void StartExecuting(bool startCaller = true)
        {
            if (storyboard == null) return;
            // Start caller
            if (startCaller) Caller.Start();

            // Turn on the animation
            SetAnimation(true);
        }

        /// <summary>
        /// Attempts to turn on the animation, but only if all conditions are met for it.
        /// </summary>
        /// <param name="on">Whether or not to turn the animation on or off</param>
        private void SetAnimation(bool on)
        {
            if (on && IsEnabled) storyboard.Begin();
            else storyboard.Stop();
        }


        /// <summary>
        /// Stops executing the execute method in order to stop showing multiple warnings at once
        /// </summary>
        private void StopExecuting()
        {
            // Stop caller
            Caller.Stop();
        }

        /// <summary>
        /// Stops executing the animation code over and over again and stops the flashing animation
        /// </summary>
        private void StopAnimation()
        {
            WarningMessage = DEFAULTCONTENT;
            // Stop the animation
            SetAnimation(false);
            StopExecuting();
        }

        int counter = -1;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called in order to change the text being displayed on the warning.
        /// </summary>
        private void Execute(object sender = null, EventArgs e = null)
        {
            // Increment the counter.
            counter++;

            // Make sure that that that index location exists.
            if (WarningList.Count <= counter) counter = 0;

            WarningMessage = WarningList.ElementAt(counter);
        }
        #endregion

        #region Start stop methods
        /// <summary>
        /// Sets a warning with the given text to run on the warning system.
        /// If multiple warnings are occurring at once, the text will slide by.
        /// To stop the warning, use the StopWarning() method
        /// </summary>
        /// <param name="text">The warning message teo display</param>
        public void SetWarning(string text, bool fromRobot = false)
        {
            // If it's already being warned of or it's in the ignore list, don't do anything
            if (WarningList.Contains(text) || IgnoreList.Contains(text)) return;
            // If the warning is being sent from the robot, add it to the robot's list of warnings
            if (fromRobot) NtAddedWarnings.Add(text);
            WarningList.Add(text);

            // Set the counter so that this warning is displayed next.
            counter = WarningList.IndexOf(text) - 1;

            if (WarningList.Count > 1)
            {
                // Execute once to start right away
                Execute();

                StartExecuting();
            }
            else
            {
                WarningMessage = text;
                StartExecuting(false);
            }
        }

        /// <summary>
        /// Removes the warning with the given text from the warning queue
        /// </summary>
        /// <param name="text">The texr warning to stop.</param>
        public void StopWarning(string text, bool fromRobot = false)
        {
            if (WarningList.Contains(text)) WarningList.Remove(text);
            // If the warning had been dispatched from the robot, remove it.
            if (fromRobot && NtAddedWarnings.Contains(text)) NtAddedWarnings.Remove(text);
            
            // If we have no text to show, stop executing
            if (WarningList.Count <= 0) StopAnimation();
            else if (WarningList.Count == 1)
            {
                StopExecuting();
                WarningMessage = WarningList.ElementAt(0);
            }
        }
        #endregion

        /// <summary>
        /// Adds the given text to the ignore list so that that warning isn't shown.
        /// </summary>
        /// <param name="text">The text to add to the ignore list</param>
        private void AddToIngoreList(string text)
        {
            // Add it to the ignore list and remove it from being warned right now if it's there.
            IgnoreList.Add(text);
            StopWarning(text);
        }

        /// <summary>
        /// Removes the given text warning from the ignore list so that it isn't ignored anymore
        /// </summary>
        /// <param name="text">The text warning to remove from the ignore list</param>
        private void RemoveFromIgnoreList(string text)
        {
            // Removes the given text from the Ignore List
            if (!IgnoreList.Contains(text)) return;
            IgnoreList.Remove(text);
        }

        /// <summary>
        /// Refreshes the necessary elements that do not auto-refresh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Refresh(object sender, NotifyCollectionChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsWarning"));
            UpdateNTArray();

            DataDealer.WriteCautionerData(DataToSave);
        }
    }
}