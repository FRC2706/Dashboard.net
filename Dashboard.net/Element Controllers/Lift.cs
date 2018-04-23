using System;
using System.ComponentModel;
using System.Windows.Controls;
using NetworkTables;

namespace Dashboard.net.Element_Controllers
{
    /// <summary>
    /// Controller for the lift slider on the main window
    /// </summary>
    public class Lift : Controller, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly string LIFTHEIGHTPATH = "SmartDashboard/Lift Distance";

        private double maxHeight;
        /// <summary>
        /// The highest height that the lift goes to.
        /// </summary>
        public double MaxHeight
        {
            get => maxHeight;
            private set
            {
                maxHeight = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MaxHeight"));
            }
        }

        public double LiftHeight
        {
            get
            {
                return master._Dashboard_NT.GetDouble(LIFTHEIGHTPATH);
            }
        }

        /// <summary>
        /// The height that the slider should be at.
        /// </summary>
        public double SliderValue => LiftHeight;

        public Lift(Master controller) : base(controller)
        {
            master._Dashboard_NT.AddKeyListener(LIFTHEIGHTPATH, OnLiftHeightChanged);

            // Set the maxHeight property right away if we can.
            if (master.Constants != null) MaxHeight = master.Constants.MaxLiftHeight;


            master.Constants.ConstantsUpdated += (sender, e) => MaxHeight = master.Constants.MaxLiftHeight;
        }

        #region Event listeners
        /// <summary>
        /// Just updates the GUI whenever the height of the lift is changed
        /// </summary>
        /// <param name="obj"></param>
        private void OnLiftHeightChanged(string key, Value obj)
        {
            PropertyChanged(this, new PropertyChangedEventArgs("SliderValue"));
        }

        #endregion
    }
}
