using System;
using System.ComponentModel;
using System.Diagnostics;
using Dashboard.net.DataHandlers;
using NetworkTables;

namespace Dashboard.net.RobotLogging
{
    public class RobotLogInterface : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Command to open the logs folder for the user
        public RelayCommand ExploreLogsCommand { get; private set; }

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
        /// <summary>
        /// Fired when the 
        /// </summary>
        /// <param name="newValue"></param>
        private void OnSaveKeyChanged(Value newValue)
        {
            // Determine if we should be saving based on the value
            bool shouldSave = (NTInterface.IsValidValue(newValue, NtType.Boolean) && IsEnabled) ? newValue.GetBoolean() : false;
            // If we should save, save
            if (shouldSave)
            {
                SaveLogs();

                // Delete the logs in networktables now that they've been saved.
                DeleteNTLogs();

                // Set the save to false after we're done saving
                networktablesInterface.SetBool(NTSAVEKEY, false);
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
            // Convert the logs to a string
            string logsToSave = System.Text.Encoding.UTF8.GetString(rawLogsToSave);

            // Only do stuff it there area actually logs to save.
            if (!string.IsNullOrEmpty(logsToSave))
            {
                // Save the logs to the file.
                RobotLogSaver.SaveLogData(logsToSave, GetFileName());
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
        private string GetFileName()
        {
            // Get the match name to be used as the file name
            string fileName = networktablesInterface.GetString(NTMATCHKEY);

            // If the file name hasn't been set, set it to the data.
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            }

            return fileName;
        }
    }
}
