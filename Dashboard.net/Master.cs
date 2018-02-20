using Dashboard.net.Element_Controllers;
using System;

namespace Dashboard.net
{
    public class Master
    {
        public event EventHandler MainWindowSet;

        private MainWindow masterWindow;
        public MainWindow _MainWindow
        {
            get
            {
                return masterWindow;
            }
            set
            {
                masterWindow = value;
                MainWindowSet?.Invoke(this, new EventArgs());
            }
        }

        public SmartDashboard _Dashboard_NT { get; private set; }
        public AutonomousSelector _AutoSelector { get; private set; }
        public Timer _Timer { get; private set; }
        public Accelerometer _Accelerometer { get; private set; }
        public Lift _Lift { get; private set; }
        public NetworkMonitor _Monitor { get; private set; }

        public Master()
        {
            _Dashboard_NT = new SmartDashboard(this);
            _AutoSelector = new AutonomousSelector(this);
            _Timer = new Timer(this);
            _Accelerometer = new Accelerometer(this);
            _Lift = new Lift(this);
            _Monitor = new NetworkMonitor(this);
        }
    }
}
