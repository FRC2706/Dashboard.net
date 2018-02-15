using NetworkTables;
using NetworkTables.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows.Controls;
using System.ComponentModel;

namespace Dashboard.net
{
    public class AutonomousSelector : INotifyPropertyChanged
    {
        private Master master;

        public event EventHandler<Dictionary<string, string>> AutoModesChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private ListBox autoList { get; set; }

        public ITable AutonomousTable { get; private set; }

        public AutonomousSelector(Master controller)
        {
            master = controller;

            AutonomousTable = master._Dashboard_NT._SmartDashboard.GetSubTable("autonomous");

            AutonomousTable.AddSubTableListener(OnAutoKeyChanged);

            // Get the auto list object from the mainwindow to be able to work with id.
            autoList = master._MainWindow.AutoList;
        }

        private void OnAutoKeyChanged(ITable arg1, string key, Value value, NotifyFlags arg4)
        {
            if (key == "auto_modes") OnAutoModesChanged();
        }

        public Dictionary<string, string> AutoModes { get; private set; } = new Dictionary<string, string>();
        private void OnAutoModesChanged()
        {
            AutoModes = JsonConvert.DeserializeObject <Dictionary<string, string>>(AutonomousTable.GetString("auto_modes", ""));

            PropertyChanged(this, new PropertyChangedEventArgs("AutoModes"));

            AutoModesChanged(this, AutoModes);
        }
    }
}
