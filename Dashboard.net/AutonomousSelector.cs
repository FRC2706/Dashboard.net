using NetworkTables;
using NetworkTables.Tables;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Windows.Controls;
using System.ComponentModel;
using WPF.JoshSmith.ServiceProviders.UI;
using System.Collections.ObjectModel;

namespace Dashboard.net
{
    public class AutonomousSelector : INotifyPropertyChanged
    {
        private Master master;

        public event EventHandler<ObservableCollection<Tuple<string, string>>> AutoModesChanged;
        public event PropertyChangedEventHandler PropertyChanged;


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
                //_AutoList.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
                //_AutoList.Drop += OnDrop;

                new ListViewDragDropManager<Tuple<string, string>>(_AutoList).ProcessDrop += OnDrop;
            }
        }

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


        #region EventListeners For Setup
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
        }

        #endregion

        ///// <summary>
        ///// Method fired when the autonomous mode is actually dropped.
        ///// This method is in charge of actually moving the autonomous mode.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void OnDrop(object sender, DragEventArgs e)
        //{
        //    Refresh();
        //}

        ///// <summary>
        ///// Function fired on the preview down button of the drag and drop mechanism.
        ///// Once the left button is released, the drag and drop event is manually called to perform the action.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    // Make sure the sender is the right type of object.
        //    if (sender is ListBox)
        //    {
        //        // Get the item being dragged
        //        Selector draggedItem = ((ListBox)sender).SelectedItem;

        //        // Do the drag and drop on that item.
        //        DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);

        //        // Maked the newly dropped item selected
        //        draggedItem.IsSelected = true;
        //    }
        //}


        #region NetworkTables Handlers

        private void OnAutoKeyChanged(ITable arg1, string key, Value value, NotifyFlags arg4)
        {
            if (key == "auto_modes") OnAutoModesChanged();
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
            AutonomousNT.PutString("selected_modes", JsonConvert.SerializeObject(topThreeAutos));
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
