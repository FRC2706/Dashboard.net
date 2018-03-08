using Dashboard.net.Checklist;

namespace Dashboard.net.Element_Controllers
{
    /// <summary>
    /// The MainWindow portion of the checlist, meant to display warnings about
    /// incompletion.
    /// </summary>
    public class Checklist : Controller
    {
        private static readonly string WARNINGMESSAGE = "Checklist Incomplete";

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
            _ChecklistEditor.ItemToggled += _ChecklistEditor_ItemToggled;

            master._Dashboard_NT.ConnectionEvent += _Dashboard_NT_ConnectionEvent;
        }

        private void _Dashboard_NT_ConnectionEvent(object sender, bool e)
        {
            CauseAnimation();
        }

        private void _ChecklistEditor_ItemToggled(object sender, string e)
        {
            CauseAnimation();
        }

        private void CauseAnimation()
        {
            if (!ChecklistComplete && master._Dashboard_NT.IsConnected) master._Cautioner.SetWarning(WARNINGMESSAGE);
            else master._Cautioner.StopWarning(WARNINGMESSAGE);
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
