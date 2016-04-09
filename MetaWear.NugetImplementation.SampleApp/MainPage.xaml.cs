using System;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MetaWear.NugetImplementation.SampleApp
{
    public class ConnectionStateColorConverter : IValueConverter
    {
        public SolidColorBrush ConnectedColor { get; set; }
        public SolidColorBrush DisconnectedColor { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch ((BluetoothConnectionStatus)value)
            {
                case BluetoothConnectionStatus.Connected:
                    return ConnectedColor;
                case BluetoothConnectionStatus.Disconnected:
                    return DisconnectedColor;
                default:
                    throw new MissingMemberException("Unrecognized connection status: " + value.ToString());
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.

            retrieveDevices();
        }

        private async void retrieveDevices()
        {
            pairedDevicesListView.Items.Clear();
            foreach (DeviceInformation di in await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector()))
            {
                BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(di.Id);
                pairedDevicesListView.Items.Add(bleDevice);
            }
        }
        private void SelectedBtleDevice(object sender, SelectionChangedEventArgs e)
        {
            //Get the data object that represents the current selected item
            BluetoothLEDevice myobject = (sender as ListView).SelectedItem as BluetoothLEDevice;

            //Checks whether that it is not null 
            if (myobject != null)
            {
                this.Frame.Navigate(typeof(DeviceInfo), myobject);
            }
        }

        private void refreshDevices(object sender, RoutedEventArgs e)
        {
            retrieveDevices();
        }
    }
}