using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard.net.DataHandlers
{
    /// <summary>
    /// Class to be used to log application data
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// The current instance of the logger class
        /// </summary>
        public static Logger CurrentInstance { get; private set; }
        private static readonly string sSource = "Application";

        public Logger()
        {
            CurrentInstance = this;

            Log("HI", EventLogEntryType.Warning);

            Thread.Sleep(10000);

            Log("HOI", EventLogEntryType.Warning);
        }

        /// <summary>
        /// Appends the given message to the logs with proper formatting and timestamps
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="typeOfMessage">The type of message to display. Must be one of EventLogEntryTypes. Defaults to message if unspecified</param>
        public void Log(string message, EventLogEntryType typeOfMessage = EventLogEntryType.Information)
        {
            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff");

            message = string.Format("{0} --- {1}", timeStamp, message);
            EventLog.WriteEntry(sSource, message, typeOfMessage);
        }
    }
}
