using NetworkTables;
using NetworkTables.Tables;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Dashboard.net
{
    public class NTInterface
    {

        /// <summary>
        /// The location of the autonomous table in the smartdashboard table
        /// </summary>
        public static readonly string AutoTableLoc = "autonomous";
        public string ConnectedAddress { get; private set; }

        Master master;

        public event EventHandler<bool> ConnectionEvent;

        private Dispatcher mainDispatcher;

        #region properties
        public bool IsConnected
        {
            get
            {
                return (_SmartDashboard != null && _SmartDashboard.IsConnected);
            }
        }

        public bool IsConnecting { get; private set; } = false;

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
                __SmartDashboard.AddConnectionListener(OnConnectionEvent, false);
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
        #endregion

        public NTInterface(Master controller)
        {
            master = controller;
            master.MainWindowSet += OnMainWindowSet;
        }
        #region static functions
        public static string GetIPV4FromMDNS(string mdsnAddress)
        {
            IPAddress[] addresses;
            try
            {
                // Convert the MDNS address if it is an mdns address to ipv4.
                addresses = Dns.GetHostAddresses(mdsnAddress);
            }
            catch (SocketException)
            {
                return "";
            }

            // Get the proper IPV4 address from the dns address
            string goodAddress = mdsnAddress;
            foreach (IPAddress address in addresses)
            {
                if (IPAddress.Parse(address.ToString()).AddressFamily == AddressFamily.InterNetwork)
                    goodAddress = address.ToString();
            }

            return goodAddress;
        }
        #endregion

        #region connection functions
        async Task<bool> ConnectAsync(CancellationToken ct)
        {
            NetworkTable.SetPort(1735);

            string goodAddress = GetIPV4FromMDNS(ConnectedAddress);
            if (string.IsNullOrEmpty(goodAddress)) return IsConnected;

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


            if (!IsConnected) Disconnect();

            return IsConnected;
        }

        /// <summary>
        /// Connects the dashboard to the robot with the given address.
        /// </summary>
        /// <param name="connectAddress">The address to try connecting to.</param>
        public async void Connect(string connectAddress)
        {
            IsConnecting = true;

            ConnectedAddress = connectAddress.ToString();


            CancellationTokenSource cts = new CancellationTokenSource();
            bool connected = await Task.Run<bool>(() => ConnectAsync(cts.Token));

            IsConnecting = false;

            // Only call the event if we're not connected since it will be called elsewhere if we do succeed in connecting.
            if (!IsConnected) ConnectionEvent?.Invoke(this, IsConnected);
        }

        /// <summary>
        /// Disconnects the dashboard from the robot.
        /// </summary>
        public void Disconnect()
        {
            NetworkTable.Shutdown();
        }
        #endregion

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
            // Don't allow the same key to have two listeners TODO allow this functionality later on?
            if (ListenerFunctions.ContainsKey(key)) return;
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

        /// <summary>
        /// Fired on connect and disconnect events.
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        private void OnConnectionEvent(IRemote arg1, ConnectionInfo arg2, bool arg3)
        {
            // Call the connected event from the main thread.
            mainDispatcher.Invoke(() => ConnectionEvent?.Invoke(this, IsConnected));
        }

        protected void OnMainWindowSet(object sender, EventArgs e)
        {
            mainDispatcher = master._MainWindow.Dispatcher;
        }

        #endregion

        /// <summary>
        /// Gets the table keys from the given path and returns the key location at the first index and the path index following it.
        /// </summary>
        /// <param name="path">The table path including the key, seperated by /.</param>
        /// <returns>Tuple containing the key at index 1, the list of table paths at index 2 and the path string without the value at index 3.
        /// For example, if path is SmartDashboard/autonomous/auto_modes will return [auto_modes, SmartDashboard, autonomous]</returns>
        private Tuple<string, string[], string> ExtractTables(string path)
        {
            int keyLocation = path.LastIndexOf("/");
            // If it's at the end of the string, look for the next one.
            if (keyLocation == path.Length - 1)
            {
                path = path.Remove(path.Length - 1, 1);
                keyLocation = path.LastIndexOf("/");
            }

            // Get the key without the path and remove it from the path string.
            string keyWithoutPath = path.Substring(keyLocation + 1, path.Length - keyLocation + 1);
            path = path.Remove(keyLocation);

            // Split it and loop around it.
            string[] tableKeys = path.Split('/');

            return new Tuple<string, string[], string>(keyWithoutPath, tableKeys, path);
        }

        /// <summary>
        /// Return the table at the given path.
        /// </summary>
        /// <param name="path">The location of the table without any keys.</param>
        /// <returns>The table at that given path.</returns>
        public ITable GetTable(string[] path)
        {
            // The last table that was opened.
            ITable LastTable = null;
            // Loop around opening the tables
            foreach (string table in path)
            {
                // If the LastTable is null, open up the table. It's null when no tables have been opened yet.
                if (LastTable == null) LastTable = NetworkTable.GetTable(table);
                else LastTable = LastTable.GetSubTable(table);
            }

            return LastTable;
        }


        /// <summary>
        /// All the tables and subtables that are opened are stored here for quick access.
        /// </summary>
        private Dictionary<string, ITable> OpenedTables;

        #region Table get set operations
        /// <summary>
        /// Gets the value at the specified networktables path
        /// Example paths are SmartDashboard/autonomous/selected_modes
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Value GetValue(string path) // TODO error handling.
        {
            Tuple<string, string[], string> extracted = ExtractTables(path);

            // Get the table path and the key string.
            string keyWithoutPath = extracted.Item1;

            string[] tableKeys = extracted.Item2;

            // The path without the value key in it.
            path = extracted.Item3;

            if (OpenedTables.ContainsKey(path)) return OpenedTables[path].GetValue(keyWithoutPath);

            ITable table = GetTable(tableKeys);
            
            // Add to the opened tables for quick access
            OpenedTables.Add(path, table);
            // Now that we've looped around, return the value
            return table.GetValue(keyWithoutPath);
        }

        /// <summary>
        /// Sets the value at the given path.
        /// </summary>
        /// <param name="path">The path for the value. Example: SmartDashboard/autonomous/selected_positon</param>
        /// <param name="value">The new value for the path.</param>
        public void SetValue(string path, Value value) // TODO error handling 
        {
            // Get the information for this path.
            Tuple<string, string[], string> extracted = ExtractTables(path);
            path = extracted.Item3;
            string key = extracted.Item1;
            string[] tablePath = extracted.Item2;

            if (OpenedTables.ContainsKey(path))
            {
                OpenedTables[path].PutValue(key, value);
                return;
            }

            // Set the value.
            ITable table = GetTable(tablePath);
            table.PutValue(key, value);

            // Add to the OpenedTables for quick access
            OpenedTables.Add(path, table);
        }
        #endregion 
    }
}