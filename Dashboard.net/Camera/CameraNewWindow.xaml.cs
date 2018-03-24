using System.Windows;
using System.Windows.Media.Imaging;
using MjpegProcessor;

namespace Dashboard.net.Camera
{
    /// <summary>
    /// Interaction logic for CameraNewWindow.xaml
    /// </summary>
    public partial class CameraNewWindow : Window
    {
        private MjpegDecoder Camera;

        public BitmapImage ImageStream
        {
            get
            {
                return (BitmapImage)CameraDisplay.Source;
            }
            set
            {
                if (value != null) CameraDisplay.Source = value;
            }
        }

        public CameraNewWindow(BitmapImage imageStream)
        {
            InitializeComponent();
            // Set width and height based on screen size
            Height = SystemParameters.FullPrimaryScreenHeight * 0.5;
            Width = SystemParameters.FullPrimaryScreenWidth * 0.5;

            ImageStream = imageStream;
        }
    }
}
