using System;
using System.Windows;
using NetworkTables;

namespace Dashboard.net
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            NetworkTable.SetPort(1735);
            NetworkTable.SetIPAddress("10.27.6.2");
            NetworkTable.SetClientMode();
            NetworkTable.Initialize();
            NetworkTable smartDashboard = NetworkTable.GetTable("SmartDashboard");

            //while (!smartDashboard.IsConnected)
            //{
            //    Console.WriteLine("Robot Not Connected");
            //}


            while (!smartDashboard.IsConnected)
            {

            }

            Console.WriteLine(smartDashboard.GetString("test", "Null"));
            smartDashboard.PutString("test2", "hi");
        }

        private void OnConnectButtonClicked(object sender, RoutedEventArgs e)
        {

        }
    }
}
