using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Dashboard.net.Checklist
{
    /// <summary>
    /// Interaction logic for ChecklistEditor.xaml
    /// </summary>
    public partial class ChecklistEditor : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// The list of checkboxes to 
        /// </summary>
        public ObservableCollection<CheckBox> CheckListList { get; private set; }
        private DataDealer dataFileIO;

        private static readonly string DefaultTODO = "TODO: Add stuff to this checklist!";

        public event PropertyChangedEventHandler PropertyChanged;

        public RelayCommand DeleteItemCommand { get; private set; }
        public RelayCommand AddItemCommand { get; private set; }

        /// <summary>
        /// Automatically sets the observablecollection being displayed on the window.
        /// </summary>
        private List<string> CheckListItems
        {
            set
            {
                CheckListList = new ObservableCollection<CheckBox>();

                // Loop around and make new checkboxes.
                foreach (string content in value)
                {
                    CheckListList.Add(new CheckBox()
                    {
                        Content = content
                    });
                }

                // Tell the GUI To update.
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CheckListList"));
            }
            get
            {
                return (List<string>)CheckListList.Select(checkbox => checkbox.Content.ToString())
                    .ToList<string>();
            }
        }

        /// <summary>
        /// Returns true if all items in the checlist are checked.
        /// </summary>
        public bool CheckListComplete
        {
            get
            {
                return CheckListList.All(checklist => checklist.IsChecked == true);
            }
        }

        /// <summary>
        /// Constructor for the window
        /// </summary>
        /// <param name="_DataFileIO">The data file to get data from.</param>
        public ChecklistEditor(DataDealer _DataFileIO)
        {
            InitializeComponent();

            // Set width and height based on screen size
            Height = SystemParameters.FullPrimaryScreenHeight * 0.8;
            Width = SystemParameters.FullPrimaryScreenWidth * 0.6;

            Focusable = true;

            dataFileIO = _DataFileIO;

            // Load the checklist.
            Reload();

            Closing += ChecklistEditor_Closing;

            DeleteItemCommand = new RelayCommand()
            {
                CanExecuteDeterminer = () => true,
                FunctionToExecute = DeleteItem
            };
            AddItemCommand = new RelayCommand()
            {
                CanExecuteDeterminer = () => true,
                FunctionToExecute = AddItem
            };
        }

        /// <summary>
        /// Deletes the given checkbox item.
        /// </summary>
        /// <param name="checkbox"></param>
        private void DeleteItem(object checkbox)
        {
            CheckBox box = (CheckBox)checkbox;
            if (box == null || box.Content.ToString() == DefaultTODO) return;
            CheckListList.Remove(box);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CheckListList"));
            dataFileIO.WriteChecklistData(CheckListItems);
        }

        /// <summary>
        /// Adds a checkbox with the given text to the checklist
        /// </summary>
        /// <param name="text">The text on the checkbox</param>
        private void AddItem(object text)
        {
            if (CheckListItems.Count == 1 && CheckListList.ElementAt(0).Content.ToString() == DefaultTODO)
                CheckListList.RemoveAt(0);
            List<string> checklist = new List<string>(CheckListItems);
            checklist.Add(text.ToString());
            CheckListItems = checklist;
            AddTextBox.Clear();
            dataFileIO.WriteChecklistData(CheckListItems);
        }

        /// <summary>
        /// Handles the closing event by cancelling it and hiding the screen instead.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChecklistEditor_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        /// <summary>
        /// Reloads the checklist from the file.
        /// </summary>
        private void Reload()
        {
            List<string> data = dataFileIO.GetCheckListData();
            if (data == null || data.Count == 0) CheckListItems = new List<string>()
            {
                {DefaultTODO }
            };
            else CheckListItems = data;
        }
    }
}
