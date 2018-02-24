using Dashboard.net.Checklist;

namespace Dashboard.net.Element_Controllers
{
    /// <summary>
    /// The MainWindow portion of the checlist, meant to display warnings about
    /// incompletion.
    /// </summary>
    public class Checklist : Controller
    {
        private ChecklistEditor ChecklistWindow;
        public ChecklistEditor _ChecklistEditor
        {
            get
            {
                if (ChecklistWindow == null) ChecklistWindow = new ChecklistEditor(master._DataFileIO);
                return ChecklistWindow;
            }
        }

        public RelayCommand OpenChecklist { get; private set; }

        public Checklist(Master controller) : base (controller)
        {
            OpenChecklist = new RelayCommand()
            {
                FunctionToExecute = (object parameter) => _ChecklistEditor.Show(),
                CanExecuteDeterminer = () => true
            };
        }

        /// <summary>
        /// Returns true if all items in the checkist are complete.
        /// </summary>
        public bool ChecklistComplete
        {
            get
            {
                return _ChecklistEditor.CheckListComplete;
            }
        }
    }
}
