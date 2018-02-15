using NetworkTables;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Dashboard.net
{
    public class SmartDashboard : INotifyPropertyChanged
    {
        public RelayCommand OnConnect { get; private set; } = new RelayCommand
        {
            CanExecuteDeterminer = () => true
        };

        public event EventHandler<bool> Connected;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsConnected
        {
            get
            {
                return (_SmartDashboard != null && _SmartDashboard.IsConnected);
            }
        }

        public string StatusMessage
        {
            get
            {
                if (IsConnected) return "CONNECTED";
                else return "OFFLINE";
            }
        }

        public NetworkTable _SmartDashboard { get; private set; }
        private Master master;

        public SmartDashboard(Master controller)
        {
            master = controller;

            OnConnect.FunctionToExecute = OnConnectClick;
        }

        public async void OnConnectClick(object connectAddress)
        {
            bool connected = await OnConnectClick(connectAddress.ToString());

            if (!connected) return;

            Connected?.Invoke(this, true);
            PropertyChanged(this, new PropertyChangedEventArgs("IsConnected"));
            PropertyChanged(this, new PropertyChangedEventArgs("StatusMessage"));
        }

        public async Task<bool> OnConnectClick(string connectAddress)
        {
            NetworkTable.SetPort(1735);
            NetworkTable.SetIPAddress(connectAddress.ToString());
            NetworkTable.SetClientMode();
            NetworkTable.Initialize();

            _SmartDashboard = NetworkTable.GetTable("SmartDashboard");

            // TODO do the connection stuff in a different thread.
            while (!_SmartDashboard.IsConnected) { }

            await Task.Delay(0);

            return true;

        }
    }
}
