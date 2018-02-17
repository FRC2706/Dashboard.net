using System;

namespace Dashboard.net.Element_Controllers
{
    public class Controller
    {
        protected Master master;

        public Controller(Master controller)
        {
            master = controller;

            master.MainWindowSet += OnMainWindowSet;
        }

        protected virtual void OnMainWindowSet(object sender, EventArgs e)
        {
            
        }
    }
}
