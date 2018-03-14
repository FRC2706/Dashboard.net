using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Controls;

namespace Dashboard.net.Element_Controllers
{
    public class ConnectionUI : Controller, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The connection status textblock on the UI>
        /// </summary>
        private TextBlock statusBox;

        /// <summary>
        /// The connect button on the UI
        /// </summary>
        private Button connectButton;

        public RelayCommand OnConnect { get; private set; } = new RelayCommand();
        public bool IsConnecting
        {
            get
            {
                return master._Dashboard_NT.IsConnecting;
            }
        }

        public bool IsConnected
        {
            get
            {
                return master._Dashboard_NT.IsConnected;
            }
        }

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
        /// The background colour for the connect button
        /// </summary>
        public Brush ConnectButtonColour {
            get
            {
                BrushConverter bc = new BrushConverter();
                return (IsConnected) ? Brushes.Red : (Brush)bc.ConvertFrom("#FF663399");
            }
        }

        /// <summary>
        /// The colour of the connecton status box text.
        /// </summary>
        public Brush StatusBoxColour
        {
            get
            {
                return (IsConnected) ? Brushes.Green : Brushes.Red;
            }
        }

        /// <summary>
        /// The label for the connect/disconnect button, depending on the state.
        /// </summary>
        public string ConnectButtonLabel
        {
            get
            {
                // If we're connected, display disconnect. Else, display connect.
                return (IsConnected) ? "Disconnect" : "Connect";
            }
        }

        public ConnectionUI(Master controller) : base(controller)
        {
            // Function to execute is onConnect click
            OnConnect.FunctionToExecute = OnConnectClick;

            // If we're in the middle of connecting, disable the button
            OnConnect.CanExecuteDeterminer = () => !IsConnecting;

            // Subscribe to the connecton event to fix UI elements.
            master._Dashboard_NT.ConnectionEvent += _Dashboard_NT_ConnectionEvent;
        }

        protected override void OnMainWindowSet(object sender, EventArgs e)
        {
            // Set the status box.
            statusBox = master._MainWindow.StatusBox;
            connectButton = master._MainWindow.ConnectButton;
        }

        private void _Dashboard_NT_ConnectionEvent(object sender, bool connected)
        {
            Refresh();
        }

        public void OnConnectClick(object connectAddress)
        {
            // If we aren't connected, connect. Else, disconnect.
            if (!IsConnected) master._Dashboard_NT.Connect(connectAddress.ToString());
            else master._Dashboard_NT.Disconnect();
            Refresh();
        }

        /// <summary>
        /// Refreshes the ui.
        /// </summary>
        private void Refresh()
        {
            PropertyChanged(this, new PropertyChangedEventArgs("StatusMessage"));
            PropertyChanged(this, new PropertyChangedEventArgs("IsConnected"));
            PropertyChanged(this, new PropertyChangedEventArgs("ConnectButtonLabel"));
            PropertyChanged(this, new PropertyChangedEventArgs("ConnectButtonColour"));
            PropertyChanged(this, new PropertyChangedEventArgs("StatusBoxColour"));
            OnConnect.RaiseCanExecuteChanged();
        }
    }
}
