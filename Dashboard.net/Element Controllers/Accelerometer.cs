using NetworkTables;
using System;
using System.ComponentModel;

namespace Dashboard.net.Element_Controllers
{
    public class Accelerometer : Controller, INotifyPropertyChanged
    {
        public static readonly string VELOCITYKEY = "velocity";
        public static readonly double MAXVELOCITY = 100;
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The degrees for the acelerometer to display
        /// </summary>
        public int Degrees
        {
            get
            {
                return (int)Math.Round(Velocity / MAXVELOCITY * 240) - 120;
            }
        }

        /// <summary>
        /// The robot's velocity, as recorded by the networktables
        /// </summary>
        public int Velocity
        {
            get
            {
                return master._Dashboard_NT._SmartDashboard != null ?
                    (int)master._Dashboard_NT._SmartDashboard.GetNumber(VELOCITYKEY, 0) : 0;
            }
        }

        public Accelerometer(Master controller) : base(controller)
        {
            master._Dashboard_NT.AddKeyListener(VELOCITYKEY, OnKeyChange);
        }

        /// <summary>
        /// Called when the velocity key's value is changed
        /// </summary>
        /// <param name="newValue">The new value for </param>
        private void OnKeyChange(Value newValue)
        {
            PropertyChanged(this, new PropertyChangedEventArgs("Degrees"));
            PropertyChanged(this, new PropertyChangedEventArgs("Velocity"));
        }
    }
}
