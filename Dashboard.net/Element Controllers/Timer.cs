using NetworkTables;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

namespace Dashboard.net.Element_Controllers
{
    public class Timer : Controller, INotifyPropertyChanged
    {
        public string Time
        {
            get
            {
                string minutes = ((Seconds / 60) > 0) ? (Seconds / 60).ToString() : "0";
                string seconds = (Seconds % 60).ToString();

                if (seconds.Length == 1) seconds = "0" + seconds;

                return string.Format("{0}:{1}", minutes, seconds);
            }
        }

        private int Seconds = 135;
        private DispatcherTimer caller;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsRunning { get; private set; }

        public Timer(Master controller) : base(controller)
        {
            master._Dashboard_NT.AddSmartDashboardKeyListener("time_running", OnNTKeyChanged);
        }

        private void OnNTKeyChanged(Value startValue)
        {
            bool shouldStart;
            if (startValue.Type == NtType.String) shouldStart = (startValue.ToString() == "true");
            else shouldStart = startValue.GetBoolean();

            if (!shouldStart && IsRunning)
            {
                StopAndReset();
                return;
            }
            else if (!shouldStart) return;

            Start();
        }

        #region Control Methods
        public void Start()
        { 
            IsRunning = true;

            caller = new DispatcherTimer();
            caller.Tick += new EventHandler(Run);
            caller.Interval = new TimeSpan(0, 0, 1);
            caller.Start();

            Console.WriteLine(caller.IsEnabled);
        }

        public void Stop()
        {
            caller?.Stop();
            IsRunning = false;
        }

        public void StopAndReset()
        {
            Stop();
            Reset();
        }

        /// <summary>
        /// Resets the timer to the start.
        /// </summary>
        private void Reset()
        {
            Seconds = 135;
            Refresh();
        }
        #endregion


        public void Run(object sender, EventArgs e)
        {
            Seconds--;
            Refresh();
        }

        private void Refresh()
        {
            PropertyChanged(this, new PropertyChangedEventArgs("Time"));
        }
    }
}
