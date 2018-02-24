using System;
using System.Threading;
using System.Windows.Controls;
using CSCore;
using MjpegProcessor;

namespace Dashboard.net.Element_Controllers
{
    public class Camera : Controller
    {

        private MjpegDecoder camera;
        private Image display;

        public Camera(Master controller) : base(controller)
        {
            master.MainWindowSet += Master_MainWindowSet;

            //HttpCamera camera = new HttpCamera("cam1", "http://10.27.6.11/video/stream.mjpg");
            camera = new MjpegDecoder();
            camera.FrameReady += Camera_FrameReady;

            master._Dashboard_NT.ConnectionEvent += _Dashboard_NT_ConnectionEvent;

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
    }
}