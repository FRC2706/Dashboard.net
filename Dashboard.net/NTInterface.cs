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
                return (SmartDashboard != null && SmartDashboard.IsConnected);
            }
        }

        public bool IsConnecting { get; private set; } = false;

        /// <summary>
        /// The actual SmartDashboard object for getting, setting and dealing with values
        /// </summary>
        private NetworkTable _SmartDashboard;
        public NetworkTable SmartDashboard
        {
            get
            {
                return _SmartDashboard;
            }
            set
            {
                _SmartDashboard = value;
                _SmartDashboard.AddConnectionListener(OnConnectionEvent, false);
                _SmartDashboard.AddTableListener(OnTableValuesChanged);

                // Add the smartdashboard to the Tables dictionary
                string tableName = GetTableName(_SmartDashboard);
                if (!Tables.ContainsKey(tableName)) Tables.Add(GetTableName(_SmartDashboard), _SmartDashboard);
                else Tables[tableName] = _SmartDashboard;
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

        public static bool IsValidValue(Value value)
        {
            return (value.Type != NtType.Unassigned);
        }

        /// <summary>
        /// Gets the table name at the beginning of the path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetTableName(string path)
        {
            return path.Split('/')[0];
        }

        /// <summary>
        /// Gets the sub table name from the given path
        /// </summary>
        /// <param name="path">The path where the sub table is located</param>
        /// <returns>The sub table from the given path. If there is no real sub-table, the toplevel table is returned instead.</returns>
        public static string GetSubTablePath(string path)
        {
            int index = path.LastIndexOf('/');
            // Get the correct substring and return it.
            return path.Substring(0, index);
        }

        /// <summary>
        /// Gets the table's name from the table object
        /// </summary>
        /// <param name="table">The table to get the name from</param>
        /// <returns>The string name of the table.</returns>
        public static string GetTableName(ITable table)
        {
            // Get the string value of the table
            string tableString = table.ToString();

            // Get rid of the part we don't want.
            int goodIndex = tableString.IndexOf('/') + 1;
            string goodName = tableString.Substring(goodIndex, tableString.Length - goodIndex);

            return goodName;
        }

        /// <summary>
        /// Removes the table part from the given path
        /// </summary>
        /// <param name="path">The given path</param>
        /// <returns>The path without the table name</returns>
        public static Tuple<string, string> SeperateTableFromPath(string path)
        {
            int goodIndex = path.IndexOf('/') + 1;
            return new Tuple<string, string>(path.Substring(0, goodIndex - 1), path.Substring(goodIndex));
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

            SmartDashboard = NetworkTable.GetTable("SmartDashboard");

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
        /// Populates tables in the Tables dictionary asynchronously so that they're not null
        /// </summary>
        private async Task PopulateTablesAsync()
        {
            List<string> keys = new List<string>(Tables.Keys);
            // Add the connection listeners to the tables in the Tables dictionary and add the table to the Tables Dictionary
            foreach (string key in keys)
            {
                if (Tables[key] != null) continue;
                ITable table = GetTable(key);
                table.AddTableListener(OnTableValuesChanged);
                Tables[key] = table;
            }

            await Task.Delay(0);
        }

        /// <summary>
        /// Calls the changed event for all the subscribed functions
        /// </summary>
        private void CallChangedMethodsForAll()
        {
            // Call all the values changes events in the list so that the change function is called on connect.
            foreach (KeyValuePair<string, Action<Value>> kv in ListenerFunctions)
            {
                kv.Value(GetValue(kv.Key));
            }
        }

        /// <summary>
        /// Connects the dashboard to the robot with the given address.
        /// </summary>
        /// <param name="connectAddress">The address to try connecting to.</param>
        public async void Connect(string connectAddress)
        {
            IsConnecting = true;

            // Make it a string and trim so this works properly
            ConnectedAddress = connectAddress.ToString();
            ConnectedAddress = ConnectedAddress.Trim();


            CancellationTokenSource cts = new CancellationTokenSource();
            bool connected = await Task.Run<bool>(() => ConnectAsync(cts.Token));

            IsConnecting = false;

            ConnectionEvent?.Invoke(this, IsConnected);

            // Populate the Tables dictionary
            if (connected)
            {
                await Task.Run(PopulateTablesAsync);
                // Call changed events
                CallChangedMethodsForAll();
            }
        }

        /// <summary>
        /// Disconnects the dashboard from the robot.
        /// </summary>
        public void Disconnect()
        {
            // Get the previous connection state
            bool previousConnectionState = IsConnected;
            NetworkTable.Shutdown();

            // If we were connected before disconnect, notify elements
            if (previousConnectionState) ConnectionEvent?.Invoke(this, IsConnected);
        }
        #endregion

        private Dictionary<string, Action<Value>> ListenerFunctions 
            = new Dictionary<string, Action<Value>>();
        private Dictionary<string, ITable> Tables = 
            new Dictionary<string, ITable>();
        /// <summary>
        /// Function that listens for the given key to change and then calls
        /// the given function when it changes within the smart dashboard table.
        /// </summary>
        /// <param name="key">The key location to monitor. The format should be [sub table key]/[value key]</param>
        /// <param name="functionToExecute">The function to fire when the value changes.</param>
        public void AddKeyListener(string key, Action<Value> functionToExecute)
        {
            // Don't allow the same key to have two listeners TODO allow this functionality later on?
            if (ListenerFunctions.ContainsKey(key)) return;
            string subtablePath = GetSubTablePath(key);

            // Add it to the listener array
            ListenerFunctions.Add(key, functionToExecute);

            // If we're not connected, we'll add the key but keep the table null. It will be filled in when we connect.
            if (!IsConnected)
            {
                if (!Tables.ContainsKey(subtablePath)) Tables.Add(subtablePath, null);
                return;
            }

            ITable table = GetTable(subtablePath);

            // If the table is not on our list, add it and subscribe to its change event.
            if (!Tables.ContainsKey(subtablePath))
            {
                Tables.Add(subtablePath, table);
                table.AddTableListener(OnTableValuesChanged);
            }
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
            key = string.Format("{0}/{1}", GetTableName(table), key);
            if (!ListenerFunctions.ContainsKey(key)) return;
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

        private bool previousConnectedState = false;
        /// <summary>
        /// Fired on connect and disconnect events by NetworkTables itself
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        private void OnConnectionEvent(IRemote arg1, ConnectionInfo arg2, bool arg3)
        {
            /*Call the connected event from the main thread.
             * Only notify of disconnects, connects are handled elsewhere in the Connect() method.
             */
            if (previousConnectedState == arg3) return;
            else previousConnectedState = arg3;
            if (!arg3) mainDispatcher.Invoke(() => ConnectionEvent?.Invoke(this, IsConnected));
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
        /// For example, if path is SmartDashboard/autonomous/auto_modes will return (key, [auto_modes, SmartDashboard, autonomous], SmartDashboard/autonomous)</returns>
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

        #region Table get set operations

        /// <summary>
        /// Gets the given table from the root directory. Returns null if it doesn't exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The table corresponding to the given path</returns>
        public ITable GetTable(string name)
        {
            if (Tables.ContainsKey(name) && Tables[name] != null) return Tables[name];
            else if (IsConnected) return NetworkTable.GetTable(name);
            else return null;
        }

        /// <summary>
        /// Return the table at the given path.
        /// </summary>
        /// <param name="path">The location of the table without any keys.</param>
        /// <returns>The table at that given path.</returns>
        public ITable GetTable(string[] path)
        {
            // Joing the string together and go.
            return GetTable(string.Join("/", path));
        }
        /// <summary>
        /// Gets the value at the specified networktables path
        /// Example paths are SmartDashboard/autonomous/selected_modes
        /// </summary>
        /// <param name="path">The path to that value. Example: SmartDashboard/autonomous/selected_modes</param>
        /// <returns></returns>
        public Value GetValue(string path)
        {
            if (!path.Contains("/")) throw new ArgumentException("Invalid path provided to GetValue function.");
            // If we're not connected, return
            else if (!IsConnected) return new Value();

            Tuple<string, string> seperatedPath = SeperateTableFromPath(path);
            string table = seperatedPath.Item1, key = seperatedPath.Item2;

            // Open the table and get the value
            Value value = GetTable(table).GetValue(key, new Value());
            if (value.Type == NtType.Unassigned) return null;
            return value;
        }

        /// <summary>
        /// Gets the double value at the specified networktables path
        /// Example paths are SmartDashboard/autonomous/selected_modes
        /// </summary>
        /// <param name="path">The path to that value. Example: SmartDashboard/autonomous/selected_modes</param>
        /// <returns>A double representing the value at that path. Returns 0 if not connected</returns>
        public double GetDouble(string path)
        {
            Value value = GetValue(path);
            double doubleValue = (value != null && value.Type != NtType.Unassigned) ? value.GetDouble() : 0;
            return doubleValue;
        }

        /// <summary>
        /// Gets the string value at the specified networktables path
        /// Example paths are SmartDashboard/autonomous/selected_modes
        /// </summary>
        /// <param name="path">The path to that value. Example: SmartDashboard/autonomous/selected_modes</param>
        /// <returns>A string representing the value at that path. Returns empty string if not connected</returns>
        public string GetString(string path)
        {
            Value value = GetValue(path);
            string stringValue = (value != null && value.Type != NtType.Unassigned) ? value.GetString() : "";
            return stringValue;
        }

        /// <summary>
        /// Gets the boolean value at the specified networktables path
        /// Example paths are SmartDashboard/autonomous/selected_modes
        /// </summary>
        /// <param name="path">The path to that value. Example: SmartDashboard/autonomous/selected_modes</param>
        /// <returns>A boolean  representing the value at that path. Returns false if not connected</returns>
        public bool GetBool(string path)
        {
            Value value = GetValue(path);
            bool boolValue = (value != null && value.Type != NtType.Unassigned) ? value.GetBoolean() : false;
            return boolValue;
        }


        /// <summary>
        /// Sets the value at the given path.
        /// </summary>
        /// <param name="path">The path for the value. Example: SmartDashboard/autonomous/selected_positon</param>
        /// <param name="value">The new value for the path.</param>
        public void SetValue(string path, Value value)
        {
            if (!IsConnected) return;
            // Get the information for this path.


            Tuple<string, string> seperated = SeperateTableFromPath(path);
            
            // Set the value
            GetTable(seperated.Item1).PutValue(seperated.Item2, value);
        }

        /// <summary>
        /// Sets the string value at the given path
        /// </summary>
        /// <param name="path">The path for the value. Example: SmartDashboard/autonomous/selected_positon</param>
        /// <param name="value">The new value for the path.</param>
        public void SetString(string path, string value)
        {
            SetValue(path, Value.MakeString(value));
        }
        /// <summary>
        /// Sets the double value at the given path
        /// </summary>
        /// <param name="path">The path for the value. Example: SmartDashboard/autonomous/selected_positon</param>
        /// <param name="value">The new value for the path.</param>
        public void SetDouble(string path, double value)
        {
            SetValue(path, Value.MakeDouble(value));
        }
        /// <summary>
        /// Sets the boolean value at the given path
        /// </summary>
        /// <param name="path">The path for the value. Example: SmartDashboard/autonomous/selected_positon</param>
        /// <param name="value">The new value for the path.</param>
        public void SetBool(string path, bool value)
        {
            SetValue(path, Value.MakeBoolean(value));
        }

        /// <summary>
        /// Retrieves all the keys and values in the given table. Note that this will not search sub tables.
        /// </summary>
        /// <param name="path">The path to the sub table or table where you want to get all values</param>
        public Dictionary<string, Value> GetAllValuesInTable(string path)
        {
            ITable table = GetTable(path);

            if (table == null) return null;

            // Get all the keys in the table
            HashSet<string> keys = table.GetKeys();

            // Create the dictionary object that will be returned.
            Dictionary<string, Value> valuesInTable = new Dictionary<string, Value>();

            foreach (string key in keys)
            {
                valuesInTable.Add(key, table.GetValue(key));
            }

            return valuesInTable;
        }

        /// <summary>
        /// Gets the subtables in the given table
        /// </summary>
        /// <param name="tablePath">The path to the table to look through</param>
        /// <returns>A string list of the subtables available in that table.</returns>
        public HashSet<string> GetSubTables(string tablePath)
        {
            ITable table = GetTable(tablePath);
            return table.GetSubTables();
        }

        #endregion 
    }
}