using NetworkTables;
using System;
using System.ComponentModel;

namespace Dashboard.net.Element_Controllers
{
    public class Accelerometer : Controller, INotifyPropertyChanged
    {
        public static readonly string RIGHTSPEEDKEY = "SmartDashboard/Right Speed (RPM)", LEFTSPEEDKEY = "SmartDashboard/Left Speed (RPM)";
        // Max degrees is the largest possible number that the degrees should be allowed to reach. Degrees range is the number of degrees that the accelerometer occupies.
        public static readonly int MAX_DEGREEES = 120, DEGREES_RANGE = 240;

        public double MaxVelocity { get; private set; } = 10;
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The degrees for the acelerometer to display
        /// </summary>
        public int Degrees
        {
            get
            {
                int initialiDegrees = (int)Math.Round(Velocity / MaxVelocity * DEGREES_RANGE) - DEGREES_RANGE / 2;
                // Make sure that we don't return more than the maxmium possible degrees
                return (initialiDegrees <= MAX_DEGREEES) ? initialiDegrees : MAX_DEGREEES;
            }
        }

        /// <summary>
        /// The robot's velocity, as recorded by the networktables in rates per minute
        /// </summary>
        public double Velocity
        {
            get
            {
                return Math.Abs(Math.Round((master._Dashboard_NT.GetDouble(RIGHTSPEEDKEY) +
                    master._Dashboard_NT.GetDouble(LEFTSPEEDKEY)) / 2, 2));
            }
        }

        public Accelerometer(Master controller) : base(controller)
        {
            master._Dashboard_NT.AddKeyListener(LEFTSPEEDKEY, OnKeyChange);
            master._Dashboard_NT.AddKeyListener(RIGHTSPEEDKEY, OnKeyChange);

            // Set the maximum velocity to auto update whenever it's changed.
            MaxVelocity = master.Constants.MaxRPM;
            master.Constants.ConstantsUpdated += (sender, e) => MaxVelocity = master.Constants.MaxRPM;
        }

        /// <summary>
        /// Called when the velocity key's value is changed
        /// </summary>
        /// <param name="newValue">The new value for </param>
        private void OnKeyChange(string key, Value newValue)
        {
            PropertyChanged(this, new PropertyChangedEventArgs("Degrees"));
            PropertyChanged(this, new PropertyChangedEventArgs("Velocity"));
        }
    }
}
