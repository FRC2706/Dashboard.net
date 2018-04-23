using Dashboard.net.DataHandlers;
using System.Windows;


namespace Dashboard.net.Element_Controllers
{
    /// <summary>
    /// A controller form miscellaneous small operations that don't fall into a specific category.
    /// </summary>
    public class MiscOperations : Controller
    {
        public RelayCommand EraseDataCommand { get; private set; }

        public MiscOperations(Master controller) : base(controller)
        {
            EraseDataCommand = new RelayCommand()
            {
                CanExecuteDeterminer = () => true,
                FunctionToExecute = EraseData
            };
        }

        /// <summary>
        /// Prompts the user to confirm that they indeed want to erase the application's data and then proceeds to erase the data if so.
        /// </summary>
        /// <param name="obj"></param>
        private void EraseData(object obj)
        {
            MessageBoxResult dialogResult = MessageBox.Show("Are you sure you wish to PERMANENTLY ERASE " +
                "ALL APPLICATION DATA and restart the application?"
                , "Continue with Application Wipe?", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            // If user continues with yes, restart the application to make the changes work.
            if (dialogResult == MessageBoxResult.Yes)
            {
                DataDealer.EraseDataFile();
                DataDealer.RestartApplication();
            }
        }
    }
}
