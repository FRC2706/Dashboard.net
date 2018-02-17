﻿using NetworkTables;
using NetworkTables.Tables;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Windows.Controls;
using System.ComponentModel;
using WPF.JoshSmith.ServiceProviders.UI;
using System.Collections.ObjectModel;
using System.Windows;

namespace Dashboard.net
{
    public class AutonomousSelector : INotifyPropertyChanged
    {
        private Master master;


        #region NetworkTables keys
        private static readonly string SELECTED_SIDE_KEY = "selected_position";
        private static readonly string SELECTED_MODES_KEY = "selected_modes";
        private static readonly string POSTED_MODES_KEY = "auto_modes";
        #endregion

        public event EventHandler<ObservableCollection<Tuple<string, string>>> AutoModesChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        #region GUI elements

        private ListView _AutoList;
        
        /// <summary>
        /// The ListBox on the main window which controls autonomous
        /// selection. 
        /// </summary>
        private ListView AutoList
        {
            get
            {
                return _AutoList;
            }
            set
            {
                _AutoList = value;
                new ListViewDragDropManager<Tuple<string, string>>(_AutoList).ProcessDrop += OnDrop;
            }
        }

        private RadioButton _LeftSelect;
        private RadioButton LeftSelect
        {
            get
            {
                return _LeftSelect;
            }
            set
            {
                _LeftSelect = value;
                _LeftSelect.Checked += OnSideSelect;
            }
        }

        private RadioButton _RightSelect;
        private RadioButton RightSelect
        {
            get
            {
                return _RightSelect;
            }
            set
            {
                _RightSelect = value;
                _RightSelect.Checked += OnSideSelect;
            }
        }

        private RadioButton _CentreSelect;
        private RadioButton CentreSelect
        {
            get
            {
                return _CentreSelect;
            }
            set
            {
                _CentreSelect = value;
                _CentreSelect.Checked += OnSideSelect;
            }
        }

        #endregion

        /// <summary>
        /// The autonomous network table, a subtable of SmartDashboard
        /// </summary>
        public ITable AutonomousNT { get; private set; }

        public AutonomousSelector(Master controller)
        {
            master = controller;

            master.MainWindowSet += OnMainWindowSet;

            master._Dashboard_NT.Connected += OnConnect;


            AutoModes.Add(new Tuple<string, string>("hole_hotel_wall", "Hole in hotel wall"));
            AutoModes.Add(new Tuple<string, string>("_1", "1"));
            AutoModes.Add(new Tuple<string, string>("_2", "2"));
            AutoModes.Add(new Tuple<string, string>("_3", "3"));
            AutoModes.Add(new Tuple<string, string>("_4", "4"));
            AutoModesChanged?.Invoke(this, AutoModes);
        }

        #region List Dealers

        private void OnDrop(object sender, ProcessDropEventArgs<Tuple<string, string>> e)
        {
            SendAutoModes();
        }

        #endregion


        #region Event Listeners
        /// <summary>
        /// Called when the dashboard connects to the networktables.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="connected"></param>
        private void OnConnect(object sender, bool connected)
        {
            if (!connected) return;

            AutonomousNT = (master._Dashboard_NT._SmartDashboard != null) ?
                master._Dashboard_NT._SmartDashboard.GetSubTable("autonomous") : null;

            AutonomousNT?.AddSubTableListener(OnAutoKeyChanged);
        }

        private void OnMainWindowSet(object sender, EventArgs e)
        {
            // Get the auto list object from the mainwindow to be able to work with id.
            AutoList = master._MainWindow.AutoList;

            RightSelect = master._MainWindow.Right_Select;
            LeftSelect = master._MainWindow.Left_Select;
            CentreSelect = master._MainWindow.Center_Select;
        }

        private void OnSideSelect(object sender, RoutedEventArgs e)
        {
            string side;

            if (RightSelect.IsChecked == true) side = "r";
            else if (LeftSelect.IsChecked == true) side = "l";
            else side = "c";

            SendSelectedSide(side); 
        }

        #endregion


        #region NetworkTables Handlers

        private void OnAutoKeyChanged(ITable arg1, string key, Value value, NotifyFlags arg4)
        {
            if (key == POSTED_MODES_KEY) OnAutoModesChanged();
        }

        /// <summary>
        /// Function to send the top three auto modes to the NetworktTables
        /// </summary>
        private void SendAutoModes()
        {
            // New list for auto modes
            List<string> topThreeAutos = new List<string>();


            // In case the number of auto modes in the list is less than 3, find the proper loop count
            int loopCount = (AutoModes.Count >= 3) ? 3 : AutoModes.Count;

            // Loop around and add to the top three
            for (int i = 0; i < loopCount; i++)
            {
                topThreeAutos.Add(AutoModes[i].Item1);
            }

            // Encode and send auto modes
            AutonomousNT?.PutString(SELECTED_MODES_KEY, JsonConvert.SerializeObject(topThreeAutos));
        }

        /// <summary>
        /// Function that sends the selected start position to the networktables.
        /// </summary>
        /// <param name="side">The Robot's Starting side</param>
        private void SendSelectedSide(string side)
        {
            AutonomousNT?.PutString(SELECTED_SIDE_KEY, side);
        }

        public ObservableCollection<Tuple<string, string>> AutoModes { get; private set; } 
            = new ObservableCollection<Tuple<string, string>>();

        /// <summary>
        /// Function called by the NetworkTables listener when the autonomous modes key changes.
        /// </summary>
        private void OnAutoModesChanged()
        {
            Dictionary<string, string> tempAutoModes = JsonConvert.DeserializeObject<Dictionary<string, string>>(AutonomousNT.GetString("auto_modes", ""));

            AutoModes.Clear();

            foreach (KeyValuePair<string, string> kv in tempAutoModes)
            {
                AutoModes.Add(new Tuple<string, string>(kv.Key, kv.Value));
            }


            Refresh();
        }

        #endregion

        /// <summary>
        /// Refreshes the autonomous listbox.
        /// </summary>
        private void Refresh()
        {
            AutoModesChanged(this, AutoModes);
            PropertyChanged(this, new PropertyChangedEventArgs("AutoModesObservable"));
            SendAutoModes();
        }
    }
}
