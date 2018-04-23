using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.net.BlingController
{
    public class BlingControllerBackend
    {
        #region Networktables constants
        public static readonly string BLING_TABLE_KEY = "blingTable", COMMAND_KEY = BLING_TABLE_KEY + "command",
            BRIGHTESS_KEY = BLING_TABLE_KEY + "LED_BRIGHTNESS", DELAY_KEY = BLING_TABLE_KEY + "wait_ms",
            REPEAT_KEY = BLING_TABLE_KEY + "repeat";
        #endregion

        #region UI structures
        // All the available bling commands
        ObservableCollection<string> availablePattern;
        #endregion

    }
}
