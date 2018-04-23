using System;
using System.Collections;

namespace Dashboard.net.DataHandlers
{

    /// <summary>
    /// The class to retrieve and set constants for things like maximum lift height, top robot RPM, etc.
    /// </summary>
    public class ConstantMaster
    {
        public event EventHandler ConstantsUpdated;

        // Key locations for the constants
        private static readonly string LIFTHEIGHT = "maxLiftHeight", RPM = "maxRPM";

        private double maxLiftHeight = 7;
        /// <summary>
        /// The maximum possible lift height.
        /// </summary>
        public double MaxLiftHeight
        {
            get => maxLiftHeight;
            set
            {
                maxLiftHeight = value;
                Save();
            }
        }

        private double maxRPM = 10;

        /// <summary>
        /// The maximum possible RPM
        /// </summary>
        public double MaxRPM
        {
            get => maxRPM;
            set
            {
                maxRPM = value;
                Save();
            }
        }



        public ConstantMaster()
        {
            Hashtable currentData = DataDealer.ReadConstants();
            // If the data for the constants doesn't exist, use the defaults
            if (currentData == null)
            {
                Save();
            }
            else
            {
                maxLiftHeight = (double)currentData[LIFTHEIGHT];
                maxLiftHeight = (double)currentData[RPM];
                CallEvent();
            }
        }

        /// <summary>
        /// Saves the values of the constants the way they currently are.
        /// </summary>
        private void Save()
        {
            // Make new hashtable and save it to the file
            DataDealer.WriteConstants(new Hashtable()
            {
                {LIFTHEIGHT, MaxLiftHeight },
                {RPM, MaxRPM }
            }
            );

            CallEvent();
        }

        /// <summary>
        /// Calls the ConstantsUpdated event
        /// </summary>
        private void CallEvent() => ConstantsUpdated?.Invoke(this, new EventArgs());
    }
}
