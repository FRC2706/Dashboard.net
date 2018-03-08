using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Dashboard.net.Element_Controllers
{
    /// <summary>
    /// The object in charge of dealing with the master caution button.
    /// </summary>
    public class Cautioner : Controller, INotifyPropertyChanged
    {
        private static readonly string DEFAULTCONTENT = "Master Caution";

        private string _warningMessage = DEFAULTCONTENT;
        public string WarningMessage
        {
            get
            {
                return _warningMessage;
            }
            set
            {
                _warningMessage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("WarningMessage"));
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
                        Interval = new TimeSpan(0, 0, 3)
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

            WarningList.CollectionChanged += Refresh;
        }

        /// <summary>
        /// Called when the main window is set in order to set the storyboard object variable
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnMainWindowSet(object sender, EventArgs e)
        {
            storyboard = (Storyboard)master._MainWindow.FindResource("animate_caution");
        }

        /// <summary>
        /// Begins to display the warnings.
        /// </summary>
        /// <param name="startCaller">Whether or not we should start the DispatcherTimer in order to execute
        /// the execute method over and over again later on.</param>
        private void StartExecuting(bool startCaller = true)
        {
            // Start caller
            if (startCaller) Caller.Start();

            // Start animation
            storyboard.Begin();
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
            // Stop the storyboard 
            storyboard.Stop();
            StopExecuting();
        }

        int counter = -1;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called in order to change the text being displayed on the warning.
        /// </summary>
        private void Execute(object sender, EventArgs e)
        {
            // Increment the counter.
            counter++;

            // Make sure that that that index location exists.
            if (WarningList.Count <= counter) counter = 0;

            WarningMessage = WarningList.ElementAt(counter);
        }

        /// <summary>
        /// Sets a warning with the given text to run on the warning system.
        /// If multiple warnings are occurring at once, the text will slide by.
        /// To stop the warning, use the StopWarning() method
        /// </summary>
        /// <param name="text">The warning message teo display</param>
        public void SetWarning(string text)
        {
            if (WarningList.Contains(text)) return;
            WarningList.Add(text);

            if (WarningList.Count > 1) StartExecuting();
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

        /// <summary>
        /// Refreshes the necessary elements that do not auto-refresh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Refresh(object sender, NotifyCollectionChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsWarning"));
        }
    }
}
