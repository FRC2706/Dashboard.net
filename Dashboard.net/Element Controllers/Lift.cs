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


        private Slider heightSlider;

        /// <summary>
        /// The highest height that the lift goes to.
        /// </summary>
        private double MaxHeight { get; set; } = 7.0;

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
                return heightSlider != null ? LiftHeight / MaxHeight * heightSlider.Maximum : 0.0;
            }
        }

        public Lift(Master controller) : base(controller)
        {
            master._Dashboard_NT.AddKeyListener(LIFTHEIGHTPATH, OnLiftHeightChanged);


            master.Constants.ConstantsUpdated += (sender, e) => MaxHeight = master.Constants.MaxLiftHeight;
        }

        #region Event listeners
        protected override void OnMainWindowSet(object sender, MainWindow e)
        {
            heightSlider = e.LiftDiagram;
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
