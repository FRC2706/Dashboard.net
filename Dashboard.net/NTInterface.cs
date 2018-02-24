using NetworkTables;
using NetworkTables.Tables;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Dashboard.net
{
    public class NTInterface : INotifyPropertyChanged
    {

        /// <summary>
        /// The location of the autonomous table in the smartdashboard table
        /// </summary>
        public static readonly string AutoTableLoc = "autonomous";
        public string ConnectedAddress { get; private set; }

        Master master;


        public RelayCommand OnConnect { get; private set; } = new RelayCommand();

        public event EventHandler<bool> ConnectionEvent;
        public event PropertyChangedEventHandler PropertyChanged;

        private Dispatcher mainDispatcher;

        public bool IsConnected
        {
            get
            {
                return (_SmartDashboard != null && _SmartDashboard.IsConnected);
            }
        }

        public bool IsConnecting { get; private set; } = false;

        public string StatusMessage
        {
            get
            {
                if (IsConnected) return "CONNECTED";
                else if (IsConnecting) return "CONNECTING";
                else return "OFFLINE";
            }
        }

        /// <summary>
        /// The actual SmartDashboard object for getting, setting and dealing with values
        /// </summary>
        private NetworkTable __SmartDashboard;
        public NetworkTable _SmartDashboard
        {
            get
            {
                return __SmartDashboard;
            }
            set
            {
                __SmartDashboard = value;
                __SmartDashboard.AddConnectionListener(OnConnectionEvent, true);
                _SmartDashboard.AddTableListener(OnTableValuesChanged);
            }
        }
        private ITable _AutonomousTable;
        public ITable AutonomousTable
        {
            get
            {
                // If the auto table is null, set it if we're connected
                if (_AutonomousTable == null && IsConnected) _AutonomousTable = _SmartDashboard.GetSubTable(AutoTableLoc);
                return _AutonomousTable;
            }
            set
            {
                _AutonomousTable = value;
                _AutonomousTable.AddSubTableListener(OnTableValuesChanged);
            }
        }


        public NTInterface(Master controller)
        {
            master = controller;
            // Function to execute is onConnect click
            OnConnect.FunctionToExecute = OnConnectClick;

            // If we're in the middle of connecting, disable the button
            OnConnect.CanExecuteDeterminer = () => !IsConnecting;
            master.MainWindowSet += OnMainWindowSet;
        }

        async Task<bool> ConnectAsync(CancellationToken ct)
        {
            NetworkTable.SetPort(1735);

            string goodAddress = GetIPV4FromMDNS(ConnectedAddress);

            NetworkTable.SetIPAddress(goodAddress);
            NetworkTable.SetClientMode();
            NetworkTable.Initialize();

            _SmartDashboard = NetworkTable.GetTable("SmartDashboard");

            /* Wait 10 seconds before we declare that the connection failed
             * If we connect early, exit loop
             */
            for (int loop = 0; loop < 10; loop++)
            {
                await Task.Delay(1000);
                if (IsConnected) break;
            }


            if (!IsConnected) NetworkTable.Shutdown();

            return IsConnected;
        }

        private Dictionary<string, Action<Value>> ListenerFunctions 
            = new Dictionary<string, Action<Value>>();
        /// <summary>
        /// Function that listens for the given key to change and then calls
        /// the given function when it changes within the smart dashboard table.
        /// </summary>
        /// <param name="key">The key location to monitor. If it exists in a sub-table
        /// of smart dashboard, the format should be [sub table key]/[value key]</param>
        /// <param name="functionToExecute">The function to fire when the value changes.</param>
        public void AddSmartDashboardKeyListener(string key, Action<Value> functionToExecute)
        {
            ListenerFunctions.Add(key, functionToExecute);
        }

        /// <summary>
        /// Adds a key listener in the SmartDashboard/autonomous table
        /// </summary>
        /// <param name="key">The key to monitor.</param>
        /// <param name="functionToExecute">The function to fire when the value changes.</param>
        public void AddAutonomousKeyListener(string key, Action<Value> functionToExecute)
        {
            AddSmartDashboardKeyListener(string.Format("{0}/{1}", AutoTableLoc, key), functionToExecute);
        }


        #region Event Listeners
        /// <summary>
        /// Listens for changes in the SmartDashboard table and then calls the function listening.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="arg4"></param>
        private void OnTableValuesChanged(ITable table, string key, Value value, NotifyFlags arg4)
        {
            if (!ListenerFunctions.ContainsKey(key)) return;
            if (table == AutonomousTable) key = string.Format("{0}/{1}", AutoTableLoc, key);
            mainDispatcher.Invoke(() => NTValueChanged(key, value));
        }

        /// <summary>
        /// Fired when a networktables key changes.
        /// </summary>
        /// <param name="key">The key that changed</param>
        /// <param name="value">The new value for that key</param>
        private void NTValueChanged(string key, Value value)
        {
            ListenerFunctions[key](value);
        }


        public async void OnConnectClick(object connectAddress)
        {
            IsConnecting = true;
            PropertyChanged(this, new PropertyChangedEventArgs("StatusMessage"));
            OnConnect.RaiseCanExecuteChanged();

            ConnectedAddress = connectAddress.ToString();


            CancellationTokenSource cts = new CancellationTokenSource();
            bool connected = await Task.Run<bool>(() => ConnectAsync(cts.Token));

            IsConnecting = false;
            OnConnect.RaiseCanExecuteChanged();

            PropertyChanged(this, new PropertyChangedEventArgs("IsConnected"));
            PropertyChanged(this, new PropertyChangedEventArgs("StatusMessage"));
            if (!IsConnected) return;

            ConnectionEvent?.Invoke(this, true);
        }

        /// <summary>
        /// Fired on connect and disconnect events.
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        private void OnConnectionEvent(IRemote arg1, ConnectionInfo arg2, bool arg3)
        {
            //ConnectionEvent?.Invoke(this, IsConnected);
            // TODO make this work
        }

        protected void OnMainWindowSet(object sender, EventArgs e)
        {
            mainDispatcher = master._MainWindow.Dispatcher;
        }

        #endregion


        public static string GetIPV4FromMDNS(string mdsnAddress)
        {
            // Convert the MDNS address if it is an mdns address to ipv4.
            IPAddress[] addresses = Dns.GetHostAddresses(mdsnAddress);

            // Get the proper IPV4 address from the dns address
            string goodAddress = mdsnAddress;
            foreach (IPAddress address in addresses)
            {
                if (IPAddress.Parse(address.ToString()).AddressFamily == AddressFamily.InterNetwork)
                    goodAddress = address.ToString();
            }

            return goodAddress;
        }
    }
}
