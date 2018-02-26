using System;
using System.Windows.Controls;
using Dashboard.net.Camera;
using MjpegProcessor;

namespace Dashboard.net.Element_Controllers
{
    /// <summary>
    /// Class that controls the camera feed and displays it on the dashboard.
    /// Documentation for the module used: https://channel9.msdn.com/coding4fun/articles/MJPEG-Decoder
    /// </summary>
    public class Camera : Controller
    {

        private MjpegDecoder camera;
        private Image display;

        /// <summary>
        /// The relaycommand that shows the feed in a new window.
        /// </summary>
        public RelayCommand OpenNewWindow { get; private set; }

        /// <summary>
        /// The other window with the camera feed in it.
        /// </summary>
        private CameraNewWindow OtherWindow { get; set; }

        public Camera(Master controller) : base(controller)
        {
            master.MainWindowSet += Master_MainWindowSet;

            camera = new MjpegDecoder();
            camera.FrameReady += Camera_FrameReady;

            master._Dashboard_NT.ConnectionEvent += _Dashboard_NT_ConnectionEvent;

            OpenNewWindow = new RelayCommand()
            {
                CanExecuteDeterminer = () => true,
                FunctionToExecute = ShowInNewWindow
            };

        }

        private void _Dashboard_NT_ConnectionEvent(object sender, bool connected)
        {
            // When we connect, begin receiving the stream.
            if (connected)
            {
                camera.ParseStream(new Uri("http://roboRIO-2706-FRC.local:1181/?action=stream"));
            }
        }

        private void Master_MainWindowSet(object sender, EventArgs e)
        {
            display = master._MainWindow.CameraBox;
        }

        private void Camera_FrameReady(object sender, FrameReadyEventArgs e)
        {
            // Set the camera source.
            display.Source = e.BitmapImage;
        }

        /// <summary>
        /// Shows the camera feed in a new window.
        /// </summary>
        /// <param name="parameter"></param>
        private void ShowInNewWindow(object parameter = null)
        {
            if (OtherWindow == null) OtherWindow = new CameraNewWindow(camera);
            OtherWindow.Show();
        }
    }
}