using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

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

            ((Master)Grid.DataContext)._MainWindow = this;

            // Set width and height based on screen size
            Height = SystemParameters.FullPrimaryScreenHeight * 0.7;
            Width = SystemParameters.FullPrimaryScreenWidth;
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
