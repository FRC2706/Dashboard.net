namespace Dashboard.net
{
    public class Master
    {
        public MainWindow _MainWindow;

        public SmartDashboard _Dashboard_NT { get; private set; }
        public AutonomousSelector _AutoSelector { get; private set; }

        public Master()
        {
            _Dashboard_NT = new SmartDashboard(this);
            _AutoSelector = new AutonomousSelector(this);
        }
    }
}
