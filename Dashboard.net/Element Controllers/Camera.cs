using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Dashboard.net.Camera;
using MjpegProcessor;
using NetworkTables;

namespace Dashboard.net.Element_Controllers
{
    /// <summary>
    /// Class that controls the camera feed and displays it on the dashboard.
    /// Documentation for the module used: https://channel9.msdn.com/coding4fun/articles/MJPEG-Decoder
    /// </summary>
    public class Camera : Controller
    {

        #region Location constants
        private readonly string CAMERAPATH = "CameraPublisher", URLPATH = "streams", PROPERTYPATH = "Property";
        #endregion

        public ObservableCollection<string> AvailableCameras { get; private set; } = new ObservableCollection<string>();

        public RelayCommand OpenSettingsCommand { get; private set; }
        /// <summary>
        /// The relaycommand that shows the feed in a new window.
        /// </summary>
        public RelayCommand OpenNewWindow { get; private set; }

        /// <summary>
        /// The other window with the camera feed in it.
        /// </summary>
        private CameraNewWindow OtherWindow { get; set; }

        #region Display elements
        private MjpegDecoder camera;
        private Image display;
        private ComboBox cameraSelector;
        #endregion

        #region Camera Properties
        private bool isStreaming;
        /// <summary>
        /// Whether or not the camera is currently streaming the image from the robot.
        /// </summary>
        public bool IsStreaming
        {
            get
            {
                return isStreaming;
            }
            set
            {
                isStreaming = value;
                // Tell the command to open settings to refresh itself.
                OpenSettingsCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// The streaming URL of the selected camera
        /// </summary>
        private string CameraURL
        {
            get
            {
                return GetCameraURL(SelectedCamera);
            }
        }

        /// <summary>
        /// The string url of the camera settings
        /// </summary>
        private string CameraSettingsURL
        {
            get
            {
                string url = CameraURL;
                return (!string.IsNullOrEmpty(url)) ? url.Substring(0, url.LastIndexOf('/')) : null;
            }
        }

        /// <summary>
        /// The string name of the selected camera
        /// </summary>
        private string SelectedCamera
        {
            get
            {
                return cameraSelector.SelectedItem?.ToString();
            }
        }

        private string CameraPropertyPath
        {
            get
            {
                return string.Format("{0}/{1}/{2}", CAMERAPATH, SelectedCamera, PROPERTYPATH);
            }
        }
        private bool IsShowingOtherWindow
        {
            get
            {
                return OtherWindow != null && OtherWindow.IsActive; // TODO also check that the window isn't closed.
            }
        }
        #endregion

        public Camera(Master controller) : base(controller)
        {
            // Set up the camera object.
            camera = new MjpegDecoder();
            camera.FrameReady += Camera_FrameReady;
            camera.Error += OnCamera_Error;

            // Subscribe to robot connection event.
            master._Dashboard_NT.ConnectionEvent += OnRobotConnection;

            OpenNewWindow = new RelayCommand()
            {
                CanExecuteDeterminer = () => true,
                FunctionToExecute = ShowInNewWindow
            };

            OpenSettingsCommand = new RelayCommand()
            {
                CanExecuteDeterminer = () => true,
                FunctionToExecute = (object parameter) => OpenCameraSettings()
            };
        }

        #region Event Handlers
        private void OnRobotConnection(object sender, bool connected)
        {
            // When we connect, begin receiving the stream.
            if (connected)
            {
                // Get the available cameras
                AvailableCameras.Clear();
                /* Do not simply want to reassing the AvailableCameras list because that would cause
                 * for the change listener to not work.
                 */
                ObservableCollection<string> cameras =  GetAvailableCameras();
                
                foreach (string camera in cameras)
                {
                    AvailableCameras.Add(camera);
                }
                
                // If we only have one camera, select it.
                if (AvailableCameras.Count == 1) cameraSelector.SelectedIndex = 0;
            }
            // If not connected, disconnect the camera
            else
            {
                AvailableCameras.Clear();
                StopCamera();
            }
        }
        /// <summary>
        /// Handles errors with the camera by restarting the stream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCamera_Error(object sender, ErrorEventArgs e)
        {
            IsStreaming = false;
            // If it's error code 0, don't try restarting. TODO make sure that this makes sense, as maybe it's always 0.
            if (e.ErrorCode != 0) StartCamera();
        }
        protected override void OnMainWindowSet(object sender, MainWindow e)
        {
            display = e.CameraBox;
            cameraSelector = e.CameraSelector;

            cameraSelector.SelectionChanged += CameraSelector_SelectionChanged;
        }

        private void CameraSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartCamera();
        }
        private void Camera_FrameReady(object sender, FrameReadyEventArgs e)
        {
            // Set the camera source.
            display.Source = e.BitmapImage;

            // Also show the image on the other window
            if (IsShowingOtherWindow) OtherWindow.ImageStream = e.BitmapImage;
        }
        #endregion

        #region Get Functions
        /// <summary>
        /// Gets a list of the available cameras.
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<string> GetAvailableCameras()
        {
            ObservableCollection<string> cameras;

            // Get the sub tables in the camera table
            cameras = new ObservableCollection<string>(master._Dashboard_NT.GetSubTables(CAMERAPATH));

            return cameras;
        }
        /// <summary>
        /// Gets the streaming url for the given camera name that is selected.
        /// </summary>
        /// <param name="selection">The selected camera</param>
        /// <returns>The string streaming URL for that camera.</returns>
        private string GetCameraURL(string selection)
        {
            string url;
            if (string.IsNullOrEmpty(selection)) url = string.Empty;
            else
            {
                /* Format the networktables location for camera.
                 * It follows the format of "[camera publisher name]/[camera name]/[path for url for camera stream]"
                 */
                string pathToCameraURL = string.Format("{0}/{1}/{2}", CAMERAPATH, selection, URLPATH);

                // Get the Mjpeg stream URL property from the networktables
                string[] networktablesCameraURL = master._Dashboard_NT.GetStringArray(pathToCameraURL);
                // Validate the value by checking for null and for proper type
                if (networktablesCameraURL == null)
                {
                    // If it's invalid, return empty string.
                    url = string.Empty;
                }
                else
                {
                    // Get the URL (the first element in the string array) and get rid of the "mjpg:" at the start.
                    url = networktablesCameraURL[0].Replace("mjpg:", "");
                }
            }
            // Return
            return url;
        }
        #endregion

        #region Action methods
        /// <summary>
        /// Starts streaming the caemra
        /// </summary>
        /// <param name="url"></param>
        private async void StartCamera()
        {
            // Begin streaming.
            string url = CameraURL;
            if (string.IsNullOrEmpty(url)) return;
            await Task.Run(() => StartCameraAsync(url));
            IsStreaming = true;
        }

        /// <summary>
        /// Stops the camera stream 
        /// </summary>
        private void StopCamera()
        {
            camera.StopStream();
            IsStreaming = false;
        }

        private async Task StartCameraAsync(string url)
        {
            camera.ParseStream(new Uri(url));
            await Task.Delay(0);
        }

        /// <summary>
        /// Shows the camera feed in a new window.
        /// </summary>
        /// <param name="parameter"></param>
        private void ShowInNewWindow(object parameter = null)
        {
            // If we're not streaming, don't do anything and simply return.
            BitmapImage image = (IsStreaming) ? camera.BitmapImage : null;
            if (!IsShowingOtherWindow) OtherWindow = new CameraNewWindow()
            {
                ImageStream = image
            };
            OtherWindow.Show();
            OtherWindow.Focus();
        }

        /// <summary>
        /// Opens the settings page for the current camera.
        /// </summary>
        private void OpenCameraSettings()
        {
            string settingsURL = CameraSettingsURL;
            if (!string.IsNullOrEmpty(settingsURL)) Process.Start(settingsURL);
        }
        #endregion
    }
}