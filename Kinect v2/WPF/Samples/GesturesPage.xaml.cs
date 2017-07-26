using LightBuzz.Vitruvius;
using Microsoft.Kinect;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.IO.Ports;
using uPLibrary.Networking.M2Mqtt;
using System.Text;

namespace LightBuzz.Vituvius.Samples.WPF
{
    /// <summary>
    /// Interaction logic for GesturesPage.xaml
    /// </summary>
    public partial class GesturesPage : Page
    {
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        GestureController _gestureController;
        SerialPort _port;
        MqttClient _mqttClient;

        public GesturesPage()
        {
            InitializeComponent();

            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                _gestureController = new GestureController();
                _gestureController.GestureRecognized += GestureController_GestureRecognized;
            }

            //_port = new SerialPort("COM4", 9600, Parity.None, 8, StopBits.One);
            //_port.Open();

            _mqttClient = new MqttClient("192.168.86.37");
            byte code = _mqttClient.Connect(System.Guid.NewGuid().ToString());
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            // Color
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (viewer.Visualization == Visualization.Color)
                    {
                        viewer.Image = frame.ToBitmap();
                    }
                }
            }

            // Body
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    Body body = frame.Bodies().Closest();

                    if (body != null)
                    {
                        _gestureController.Update(body);
                    }
                }
            }
        }

        void GestureController_GestureRecognized(object sender, GestureEventArgs e)
        {
            var gesture = e.GestureType;

            switch (gesture)
            {
                case (GestureType.JoinedHands):
                    _mqttClient.Publish("gestures", Encoding.UTF8.GetBytes("joined_hands"));
                    break;
                case (GestureType.Menu):
                    _mqttClient.Publish("gestures", Encoding.UTF8.GetBytes("menu"));
                    break;
                case (GestureType.SwipeDown):
                    _mqttClient.Publish("gestures", Encoding.UTF8.GetBytes("swipe_down"));
                    break;
                case (GestureType.SwipeLeft):
                    _mqttClient.Publish("gestures", Encoding.UTF8.GetBytes("swipe_left"));
                    break;
                case (GestureType.SwipeRight):
                    _mqttClient.Publish("gestures", Encoding.UTF8.GetBytes("swipe_right"));
                    break;
                case (GestureType.SwipeUp):
                    _mqttClient.Publish("gestures", Encoding.UTF8.GetBytes("swipe_up"));
                    break;
                case (GestureType.WaveLeft):
                    _mqttClient.Publish("gestures", Encoding.UTF8.GetBytes("wave_left"));
                    break;
                case (GestureType.WaveRight):
                    _mqttClient.Publish("gestures", Encoding.UTF8.GetBytes("wave_right"));
                    break;
                case (GestureType.ZoomIn):
                    _mqttClient.Publish("gestures", Encoding.UTF8.GetBytes("zoom_in"));
                    break;
                case (GestureType.ZoomOut):
                    _mqttClient.Publish("gestures", Encoding.UTF8.GetBytes("zoom_out"));
                    break;
            }
            tblGestures.Text = e.GestureType.ToString();
        }
    }
}
