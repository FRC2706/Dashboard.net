using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public static Logger currentInstance;

        public Logger()
        {
            currentInstance = this;
        }
    }
}
