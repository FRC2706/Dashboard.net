﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;

namespace Dashboard.net
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // We do not want this application to run if there are multiple instances of it.
            if (IsApplicationAlreadyRuning()) Application.Current.Shutdown();

            InitializeComponent();

            ((Master)Grid.DataContext)._MainWindow = this;

            // Set width and height based on screen size
            Height = SystemParameters.FullPrimaryScreenHeight * 0.7;
            Width = SystemParameters.FullPrimaryScreenWidth;
            // Set the dashboard to be at the top left of the screen
            Left = 0;
            Top = 0;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Manually shut down application
            Application.Current.Shutdown();
        }

        // Determines if the application is already running
        private bool IsApplicationAlreadyRuning()
        {
            return Process.GetProcessesByName("Dashboard.net").Length > 1;
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
