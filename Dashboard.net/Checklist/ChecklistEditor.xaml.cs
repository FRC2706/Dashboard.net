using System;
using System.Collections;
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
    public partial class ChecklistEditor : Window
    {
        /// <summary>
        /// The list of checkboxes to 
        /// </summary>
        public ObservableCollection<CheckBox> CheckListList { get; private set; }
        private DataDealer dataFileIO;

        private static readonly string DefaultTODO = "TODO: Add stuff to this checklist!";

        public RelayCommand DeleteItemCommand { get; private set; }
        public RelayCommand AddItemCommand { get; private set; }

        #region events
        /// <summary>
        /// Fired when an item is added to the checklist
        /// String that is sent is the text of the item added.
        /// </summary>
        public event EventHandler<string> ItemAdded;

        /// <summary>
        /// Fired when items are removed from the checklist
        /// String list that is sent is the text of the items removed.
        /// </summary>
        public event EventHandler<string[]> ItemsDeleted;

        /// <summary>
        /// Fired when an item is checked or unchecked.
        /// String that is sent is the text of the item checked/unchecked.
        /// </summary>
        public event EventHandler<string> ItemToggled;
        #endregion

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
                    CheckListList.Add(MakeCheckBoxForItem(content));
                }
            }
            get
            {
                return CheckListList.Select(checkbox => checkbox.Content.ToString())
                    .ToList();
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
                FunctionToExecute = DeleteItems
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
        private void DeleteItems(object checkboxes)
        {
            IList boxes = (IList)checkboxes;
            if (boxes == null) return;

            List<CheckBox> convertedBoxes = new List<CheckBox>(boxes.Cast<CheckBox>());

            // Loop around removing the checkboxes selected, but not if they're text is the default text.
            foreach (CheckBox box in convertedBoxes)
            {
                if (box.Content.ToString() == DefaultTODO) continue;
                CheckListList.Remove(box);
            }

            dataFileIO.WriteChecklistData(CheckListItems);

            // Make sure that there's always a default todo
            if (CheckListList.Count <= 0) CheckListList.Add(MakeCheckBoxForItem(DefaultTODO));

            string[] itemsDeleted = convertedBoxes.Select(checkbox => checkbox.Content.ToString()).ToList().ToArray();
            // Fire the event.
            ItemsDeleted?.Invoke(this, itemsDeleted);
        }

        /// <summary>
        /// Adds a checkbox with the given text to the checklist
        /// </summary>
        /// <param name="text">The text on the checkbox</param>
        private void AddItem(object text)
        {
            string itemText = text.ToString();
            // Error validatation. If the default to-do is there still, remove it. If it's empty, return
            if (string.IsNullOrWhiteSpace(text.ToString())) return;
            else if (CheckListItems.Count == 1 && CheckListList.ElementAt(0).Content.ToString() == DefaultTODO)
                CheckListList.RemoveAt(0);

            // Make a checkbox for the item and add it.
            CheckListList.Add(MakeCheckBoxForItem(itemText));
            AddTextBox.Clear();

            // Save the newly created item.
            dataFileIO.WriteChecklistData(CheckListItems);

            // Fire the event.
            ItemAdded?.Invoke(this, itemText);
        }

        /// <summary>
        /// Creates a checkbox for the given checklist item.
        /// </summary>
        /// <param name="text">The text to put on the checkbutton</param>
        /// <returns>A checkbutton with the given text as the content.</returns>
        private CheckBox MakeCheckBoxForItem(string text)
        {
            CheckBox returnValue = new CheckBox()
            {
                Content = text
            };
            // Subscribe to events
            returnValue.Checked += OnToggledCheckbox;
            returnValue.Unchecked += OnToggledCheckbox;

            return returnValue;
        }

        private void OnToggledCheckbox(object sender, RoutedEventArgs e)
        {
            ItemToggled?.Invoke(this, ((CheckBox)(e.Source)).Content.ToString());
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
            List<string> data = dataFileIO.ReadCheckListData();
            if (data == null || data.Count == 0) CheckListItems = new List<string>()
            {
                {DefaultTODO }
            };
            else CheckListItems = data;
        }
    }
}
