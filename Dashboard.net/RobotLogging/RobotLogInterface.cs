using System;
using System.ComponentModel;
using System.Windows.Threading;
using Dashboard.net.DataHandlers;

namespace Dashboard.net.RobotLogging
{
    public class RobotLogInterface : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Command to open the logs folder for the user
        public RelayCommand ExploreLogsCommand { get; private set; }

        // Will call the log save function for auto-logging
        private DispatcherTimer caller;
        private static readonly int SECONDS_FOR_AUTO_SAVE = 15;

        // The networktables interface which will help us get logs from the robot
        NTInterface networktablesInterface;

        #region Networktables constants
        // Networktables constants
        private static readonly string NT_LOGGINGTABLEKEY = "logging-level",
            // The location of the actual log byte array
            NTLOGKEY = NT_LOGGINGTABLEKEY + "/Value", 
            // The match name for the log file name.
            NTMATCHKEY = NT_LOGGINGTABLEKEY + "/match", 
            // The location of the save boolean, which is set to true when we should save.
            NTSAVEKEY = NT_LOGGINGTABLEKEY + "/save";
        #endregion

        private bool _isEnabled;
        /// <summary>
        /// Whether or not logging for the robot is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                _isEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsEnabled"));

                // Save the current enabled state.
                SaveEnabledState();
            }
        }

        public RobotLogInterface()
        {
            networktablesInterface = Master.currentInstance._Dashboard_NT;
            networktablesInterface.AddKeyListener(NTSAVEKEY, OnSaveKeyChanged);
            // Listen for robot connection.
            networktablesInterface.ConnectionEvent += OnRobotConnection;

            _isEnabled = GetEnabledState();

            // Set up the open logs folder command
            ExploreLogsCommand = new RelayCommand
            {
                FunctionToExecute = OpenLogFolder,
                CanExecuteDeterminer = () => true
            };
        }

        #region Enabled Get Set methods for saving
        private static readonly string ENABLED_KEY = "IsRobotLoggingEnabled";
        private bool GetEnabledState()
        {
            object isEnabled = DataDealer.ReadMiscData(ENABLED_KEY);

            bool enabled;
            if (isEnabled == null)
            {
                DataDealer.AppendMiscData(ENABLED_KEY, false);
                enabled = false;
            }
            else
            {
                enabled = (bool)isEnabled;
            }

            // If it's null, we need to write 
            return enabled;
        }
        /// <summary>
        /// Saves the current state of the enabledness of the robot logging system
        /// </summary>
        private void SaveEnabledState()
        {
            // Append the data to the data dealer.
            DataDealer.AppendMiscData(ENABLED_KEY, IsEnabled);
        }
        #endregion

        #region Event Listeners
        private void OnRobotConnection(object sender, bool connected)
        {
            if (connected)
            {
                caller = new DispatcherTimer();
                caller.Tick += AutoSaveLogs;
                caller.Interval = new TimeSpan(0, 0, SECONDS_FOR_AUTO_SAVE);
                caller.Start();
            }
            else
            {
                // Stop the auto logger if the robot disconnects.
                if (caller != null) caller.Stop();
            }
        }

        /// <summary>
        /// Called by the event dispatcher to automatically save the logs every once in a while.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoSaveLogs(object sender, EventArgs e)
        {
            SaveLogs();
        }

        /// <summary>
        /// Fired when the 
        /// </summary>
        /// <param name="newValue"></param>
        private void OnSaveKeyChanged(string key, bool shouldSave)
        {
            // If we should save, save
            if (shouldSave)
            {
                // Make sure the dispatcher for auto-saving logs restarts because now we're going to save them anyway
                caller.Interval = new TimeSpan(0, 0, SECONDS_FOR_AUTO_SAVE);

                SaveLogs();
            }
        }
        #endregion

        #region actions
        /// <summary>
        /// Deletes the log byte array being stored on networktables.
        /// </summary>
        private void DeleteNTLogs()
        {
            // Set the value to a new byte array with nothing in it.
            networktablesInterface.SetByteArray(NTLOGKEY, new byte[0]);
        }
        /// <summary>
        /// Saves the logs in their current state.
        /// </summary>
        private void SaveLogs()
        {
            // Get the logs
            byte[] rawLogsToSave = networktablesInterface.GetByteArray(NTLOGKEY);

            // Only do stuff if the data isn't null
            if (rawLogsToSave != null)
            {
                // Convert the logs to a string
                string logsToSave = System.Text.Encoding.UTF8.GetString(rawLogsToSave);

                // Only save logs if there's anything to save.
                if (!string.IsNullOrWhiteSpace(logsToSave))
                {
                    // Set the name for the logs right away.
                    string matchNum = GetMatchNumber();

                    if (!string.IsNullOrWhiteSpace(matchNum))
                    {
                        RobotLogSaver.LogName = matchNum;
                    }
                    // If the log name is empty and the match number is also empty, use a timestamp
                    else if (string.IsNullOrEmpty(RobotLogSaver.LogName))
                    {
                        // Set it to the date as a last resort.
                        RobotLogSaver.LogName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    }
                    // Save the logs to the file.
                    RobotLogSaver.SaveLogData(logsToSave);
                }

                // Delete the logs in networktables now that they've been saved.
                DeleteNTLogs();

                // Set the save to false after we're done saving
                networktablesInterface.SetBool(NTSAVEKEY, false);
            }
        }
        /// <summary>
        ///  Opens the log folder up in Windows explorer.
        /// </summary>
        /// <param name="sender"></param>
        private void OpenLogFolder(object sender = null)
        {
            RobotLogSaver.OpenLogFolder();
        }
        #endregion

        /// <summary>
        /// Gets the correct file name for the 
        /// </summary>
        /// <returns></returns>
        private string GetMatchNumber()
        {
            // Get the match name to be used as the file name
            return networktablesInterface.GetString(NTMATCHKEY);
        }
    }
}
