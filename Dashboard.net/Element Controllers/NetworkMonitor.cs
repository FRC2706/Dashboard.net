using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows.Threading;

namespace Dashboard.net.Element_Controllers
{
    public class NetworkMonitor : Controller, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private PerformanceCounter BytesSentPerfCounter;
        private PerformanceCounter BytesReceivedPerfCounter;

        /// <summary>
        /// The usage of the program in MB/s
        /// </summary>
        public long Usage
        {
            get
            {
                //return BytesSentPerfCounter.RawValue + BytesReceivedPerfCounter.RawValue;
                return 0;
            }
        }

        public string Display
        {
            get
            {
                return string.Format("{0} MB/s", Usage);
            }
        }

        public NetworkMonitor(Master controller) : base(controller)
        {
            BytesSentPerfCounter = new PerformanceCounter
            {
                CounterName = "Bytes Sent",
                CategoryName = ".NET CLR Networking",
                InstanceName = AppDomain.CurrentDomain.FriendlyName,
                ReadOnly = true
            };

            BytesReceivedPerfCounter = new PerformanceCounter
            {
                CounterName = "Bytes Received",
                CategoryName = ".NET CLR Networking",
                InstanceName = AppDomain.CurrentDomain.FriendlyName,
                ReadOnly = true
            };

            // Set a timer to update the UI every so often
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 1)
            };
            timer.Tick += UpdateUsage;
            timer.Start();
        }


        /// <summary>
        /// Just tells the GUI to update
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UpdateUsage(object sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Display"));
        }
    }
}
