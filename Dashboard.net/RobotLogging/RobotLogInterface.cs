using System;
using Dashboard.net.DataHandlers;
using NetworkTables;

namespace Dashboard.net.RobotLogging
{
    public class RobotLogInterface
    {
        // The networktables interface which will help us get logs from the robot
        NTInterface networktablesInterface;

        #region Networktables constants
        // Networktables constants
        private static readonly string NTLOGGINGTABLEKEYLOGPATH = "logging-level",
            // The location of the actual log byte array
            NTLOGKEY = NTLOGGINGTABLEKEYLOGPATH + "/Value", 
            // The match name for the log file name.
            NTMATCHKEY = NTLOGGINGTABLEKEYLOGPATH + "/match", 
            // The location of the save boolean, which is set to true when we should save.
            NTSAVEKEY = NTLOGGINGTABLEKEYLOGPATH + "/logging";
        #endregion

        public RobotLogInterface()
        {
            networktablesInterface = Master.currentInstance._Dashboard_NT;
            networktablesInterface.AddKeyListener(NTSAVEKEY, OnSaveKeyChanged);
        }

        /// <summary>
        /// Fired when the 
        /// </summary>
        /// <param name="newValue"></param>
        private void OnSaveKeyChanged(Value newValue)
        {
            // Determine if we should be saving based on the value
            bool shouldSave = (NTInterface.IsValidValue(newValue, NtType.Boolean)) ? newValue.GetBoolean() : false;
            // If we should save, save
            if (shouldSave) SaveLogs();
        }

        /// <summary>
        /// Saves the logs in their current state.
        /// </summary>
        private void SaveLogs()
        {
            // Get the logs
            byte[] rawLogsToSave = networktablesInterface.GetByteArray(NTLOGKEY);
            // Convert the logs to a string
            string logsToSave = rawLogsToSave.ToString();

            // Save the logs to the file.
            RobotLogSaver.SaveLogData(logsToSave, GetFileName());
        }

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
