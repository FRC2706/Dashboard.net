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
        private static readonly string LIFTHEIGHTPATH = "SmartDashboard/lift_height";


        private Slider heightSlider;

        /// <summary>
        /// The highest height that the lift goes to.
        /// </summary>
        private static readonly double MAXHEIGHT = 10.0;

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
        public double SliderValue
        {
            get
            {
                return heightSlider != null ? LiftHeight / MAXHEIGHT * heightSlider.Maximum : 0.0;
            }
        }

        public Lift(Master controller) : base(controller)
        {
            master._Dashboard_NT.AddKeyListener(LIFTHEIGHTPATH, OnLiftHeightChanged);
        }

        #region Event listeners
        protected override void OnMainWindowSet(object sender, EventArgs e)
        {
            heightSlider = master._MainWindow.LiftDiagram;
        }

        /// <summary>
        /// Just updates the GUI whenever the height of the lift is changed
        /// </summary>
        /// <param name="obj"></param>
        private void OnLiftHeightChanged(Value obj)
        {
            PropertyChanged(this, new PropertyChangedEventArgs("SliderValue"));
        }

        #endregion
    }
}
