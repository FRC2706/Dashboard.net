using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
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
                master._DataFileIO.WriteCautionerData(DataToSave);
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
            Hashtable cautionerData = master._DataFileIO.ReadCautionerData();
            if (cautionerData == null)
            {
                _isEnabled = true;
                IgnoreList = new ObservableCollection<string>();

                master._DataFileIO.WriteCautionerData(DataToSave);
            }
            else
            {
                _isEnabled = (bool)cautionerData[ENABLEDKEY];
                IgnoreList = (ObservableCollection<string>)cautionerData[IGNOREKEY];
            }
            IgnoreList.CollectionChanged += Refresh;

            // Listen for key changes on the add and remove queues
            master._Dashboard_NT.AddKeyListener(NTADDKEY, OnNTKeyAdded);
            master._Dashboard_NT.AddKeyListener(NTREMOVEKEY, OnNTKeyRemoved);
            // Add a listener to the currrent warnings networktable property in case another program changes it.
            master._Dashboard_NT.AddKeyListener(NTCURRENTWARNINGS, (Value value) => UpdateNTArray());
        }

        #region Networktables stuff
        /// <summary>
        /// Removes the warning from the warnings queue from input from the networktables table
        /// </summary>
        /// <param name="obj"></param>
        private void OnNTKeyRemoved(Value obj)
        {
            // Confirm that the object type is a string
            if (obj == null || obj.Type != NtType.String) return;
            StopWarning(obj.GetString());
        }

        /// <summary>
        /// Adds a warning from the networktables Warnings subtable by listening for the key to change.
        /// </summary>
        /// <param name="obj"></param>
        private void OnNTKeyAdded(Value obj)
        {
            // Confirm that the object type is a string
            if (obj == null || obj.Type != NtType.String) return;
            SetWarning(obj.GetString());
        }
        /// <summary>
        /// Updates the networktables warning array every time a warning is added or removed.
        /// </summary>
        private void UpdateNTArray()
        {
            master._Dashboard_NT.SetStringArray(NTCURRENTWARNINGS, WarningList.ToArray<string>());
        }
        #endregion

        /// <summary>
        /// Called when the main window is set in order to set the storyboard object variable
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnMainWindowSet(object sender, EventArgs e)
        {
            storyboard = (Storyboard)master._MainWindow.FindResource("animate_caution");
            master._MainWindow.Master_Caution.MouseRightButtonDown += IgnoreListViewer_MouseRightButtonDown;
            ignoreListViewer = master._MainWindow.IgnoreListViewer;
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
        public void SetWarning(string text)
        {
            // If it's already being warned of or it's in the ignore list, don't do anything
            if (WarningList.Contains(text) || IgnoreList.Contains(text)) return;
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
        public void StopWarning(string text)
        {
            if (WarningList.Contains(text)) WarningList.Remove(text);
            
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

            master._DataFileIO.WriteCautionerData(DataToSave);
        }
    }
}
