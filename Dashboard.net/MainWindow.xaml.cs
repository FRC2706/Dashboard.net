using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
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

            ((Master)Grid.DataContext)._Dashboard_NT.Connected += OnRobotConnected;
            ((Master)Grid.DataContext)._MainWindow = this;

            StatusBox.Foreground = Brushes.Red;
        }

        // TODO get this working in XAML only
        private void OnRobotConnected(object sender, bool connected)
        {
            if (connected) StatusBox.Foreground = Brushes.Green;
            else StatusBox.Foreground = Brushes.Red;
        }
    }

    public class ValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (null != value)
            {
                if (value.ToString() == "1")
                    return true;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
