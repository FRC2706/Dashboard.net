using System;
using System.Diagnostics;
using System.IO;

namespace Dashboard.net.DataHandlers
{
    public static class RobotLogSaver
    {

        public enum TypeOfSave
        {
            OverwriteOrCreate = 1, AppendOrCreate
        }

        // We're saving these as log files. Folder path is the folder location for the logs.
        private static readonly string EXTENSION = ".log";

        private static string logName;
        /// <summary>
        /// Sets the file name for the logs that will be saved by the logger. Should only be set once every time the program is run.
        /// </summary>
        /// <param name="name"></param>
        public static string LogName
        {
            get
            {
                return logName;
            }
            set
            {
                // Get rid of bad characters and leading and trailing spaces.
                logName = value.Replace("/", "-").Trim();
            }
        }

        /// <summary>
        /// Whether or not the log name has been set
        /// </summary>
        public static bool IsLogNameSet
        {
            get
            {
                return !string.IsNullOrEmpty(LogName);
            }
        }

        /// <summary>
        /// The full directory path for the logs
        /// </summary>
        private static string DirectoryPath
        {
            get
            {
                return Path.Combine(DataDealer.DataLocation, "RobotLogs");
            }
        }

        /// <summary>
        /// Saves the given log data to the file name given
        /// </summary>
        /// <param name="dataToSave"></param>
        /// <param name="typeOfSave">The type of saving to be doing, either overwriting the file, appending
        /// to it or appending and creating a new file if it doesn't exist.</param>
        public static void SaveLogData(string dataToSave, TypeOfSave typeOfSave = TypeOfSave.AppendOrCreate)
        {
            // Only do stuff if there's actually stuff to save.
            if (!string.IsNullOrEmpty(dataToSave))
            {
                // Seperate the data into a string array based on the newline characters
                dataToSave = dataToSave.Replace("\n", Environment.NewLine);

                // If the directory doesn't exist, create it.
                if (!Directory.Exists(DirectoryPath)) Directory.CreateDirectory(DirectoryPath);

                // If the log name hasn't been set, set it to the date
                if (string.IsNullOrEmpty(LogName))
                {
                    LogName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                }

                // Add the directory to the file name.
                string fileName = Path.Combine(DirectoryPath, FormatFileName(LogName));


                FileStream logFile;
                // If the file doesn't exist, create it or overwrite it if that's the selected option.
                if (!File.Exists(fileName) || typeOfSave == TypeOfSave.OverwriteOrCreate) logFile = File.Create(fileName);
                else logFile = new FileStream(fileName, FileMode.Append);

                // Use stream writer to write the data.
                using (StreamWriter writer = new StreamWriter(logFile))
                {
                    writer.Write(dataToSave);
                }
            }
        }

        /// <summary>
        /// Formats the file name so that it has the proper extension and name
        /// </summary>
        /// <param name="fileName">The file name to format</param>
        /// <returns>The properly formatted file name (with right extension)</returns>
        private static string FormatFileName(string fileName)
        {
            // Get rid of any extension on the file name.
            if (fileName.Contains("."))
            {
                fileName = fileName.Substring(0, fileName.IndexOf("."));
            }

            // Get rid of spaces
            fileName = fileName.Trim();
            
            // Add the extension to the file name
            fileName += EXTENSION;

            return fileName;

        }

        /// <summary>
        /// Opens the log folder up in the windows explorer
        /// </summary>
        internal static void OpenLogFolder()
        {
            Process.Start("explorer.exe", DirectoryPath);
        }
    }
}
