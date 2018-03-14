using System.Windows;
using MjpegProcessor;

namespace Dashboard.net.Camera
{
    /// <summary>
    /// Interaction logic for CameraNewWindow.xaml
    /// </summary>
    public partial class CameraNewWindow : Window
    {
        private MjpegDecoder Camera;

        public CameraNewWindow(MjpegDecoder camera)
        {
            InitializeComponent();
            // Set width and height based on screen size
            Height = SystemParameters.FullPrimaryScreenHeight * 0.5;
            Width = SystemParameters.FullPrimaryScreenWidth * 0.5;

            Camera = camera;
            Camera.FrameReady += Camera_FrameReady;
        }

        /// <summary>
        /// Called when the camera feed is ready to be displayed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The MjpegDecoder camera</param>
        private void Camera_FrameReady(object sender, FrameReadyEventArgs e)
        {
            CameraDisplay.Source = e.BitmapImage;
        }
    }
}
