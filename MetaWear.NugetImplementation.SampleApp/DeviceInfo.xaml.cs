using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Sensor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace MetaWear.NugetImplementation.SampleApp
{
    public sealed partial class DeviceInfo : Page
    {
        private static readonly Guid
            GUID_METAWEAR_SERVICE = new Guid("326A9000-85CB-9195-D9DD-464CFBBAE75A"),
            GUID_DEVICE_INFO_SERVICE = new Guid("0000180a-0000-1000-8000-00805f9b34fb"),
            CHARACTERISTIC_MANUFACTURER = new Guid("00002a29-0000-1000-8000-00805f9b34fb"),
            CHARACTERISTIC_MODEL_NUMBER = new Guid("00002a24-0000-1000-8000-00805f9b34fb"),
            CHARACTERISTIC_SERIAL_NUMBER = new Guid("00002a25-0000-1000-8000-00805f9b34fb"),
            CHARACTERISTIC_FIRMWARE_REVISION = new Guid("00002a26-0000-1000-8000-00805f9b34fb"),
            CHARACTERISTIC_HARDWARE_REVISION = new Guid("00002a27-0000-1000-8000-00805f9b34fb"),
            METAWEAR_COMMAND_CHARACTERISTIC = new Guid("326A9001-85CB-9195-D9DD-464CFBBAE75A"),
            METAWEAR_NOTIFY_CHARACTERISTIC = new Guid("326A9006-85CB-9195-D9DD-464CFBBAE75A");

        private static readonly Dictionary<Guid, String> DEVICE_INFO_NAMES = new Dictionary<Guid, String>();

        static DeviceInfo()
        {
            DEVICE_INFO_NAMES.Add(CHARACTERISTIC_MANUFACTURER, "Manufacturer");
            DEVICE_INFO_NAMES.Add(CHARACTERISTIC_MODEL_NUMBER, "Model Number");
            DEVICE_INFO_NAMES.Add(CHARACTERISTIC_SERIAL_NUMBER, "Serial Number");
            DEVICE_INFO_NAMES.Add(CHARACTERISTIC_FIRMWARE_REVISION, "Firmware Revision");
            DEVICE_INFO_NAMES.Add(CHARACTERISTIC_HARDWARE_REVISION, "Hardware Revision");
        }

        private enum Signal
        {
            SWITCH,
            ACCELEROMETER,
            BMP280_PRESSURE,
            BMP280_ALTITUDE,
            AMBIENT_LIGHT,
            GYRO
        }

        DeviceInfo page;

        private BluetoothLEDevice selectedBtleDevice;

        private Dictionary<Guid, string> mwDeviceInfoChars = new Dictionary<Guid, string>();
        private Dictionary<Signal, IntPtr> signals = new Dictionary<Signal, IntPtr>();

        private BtleConnection conn;
        private IntPtr mwBoard;

        private GattDeviceService mwGattService;
        private Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic mwNotifyChar;

        public DeviceInfo()
        {
            this.InitializeComponent();

            conn = new BtleConnection()
            {
                readGattChar = ReadGattChar,
                writeGattChar = WriteGattChar
            };
            mwBoard = Functions.mbl_mw_metawearboard_create(ref conn);
            Functions.mbl_mw_metawearboard_initialize(mwBoard, Initialized);
        }

        private void ReadGattChar(IntPtr characteristic)
        {
            // todo... read gatt char
        }
        
        private string byteArrayToHex(byte[] array)
        {
            var builder = new StringBuilder();

            builder.Append(string.Format("[0x{0:X2}", array[0]));
            for (int i = 1; i < array.Length; i++)
            {
                builder.Append(string.Format(", 0x{0:X2}", array[i]));
            }
            builder.Append("]");
            return builder.ToString();
        }

        private async void WriteGattChar(IntPtr characteristic, IntPtr bytes, byte length)
        {
            byte[] managedArray = new byte[length];
            Marshal.Copy(bytes, managedArray, 0, length);
            outputListView.Items.Add(new ConsoleLine(ConsoleEntryType.COMMAND, "Command: " + byteArrayToHex(managedArray)));

            try
            {
                Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic mwCommandChar = mwGattService.GetCharacteristics(METAWEAR_COMMAND_CHARACTERISTIC).FirstOrDefault();
                GattCommunicationStatus status = await mwCommandChar.WriteValueAsync(managedArray.AsBuffer(), GattWriteOption.WriteWithoutResponse);

                if (status != GattCommunicationStatus.Success)
                {
                    outputListView.Items.Add(new ConsoleLine(ConsoleEntryType.SEVERE, "Error writing command, GattCommunicationStatus= " + status));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private void Initialized()
        {
            if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
            {
                page.IsEnabled = true;
            }
            else {
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => page.IsEnabled = true);
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            selectedBtleDevice = (BluetoothLEDevice)e.Parameter;
            mwGattService = selectedBtleDevice.GetGattService(GUID_METAWEAR_SERVICE);

            foreach (var characteristic in selectedBtleDevice.GetGattService(GUID_DEVICE_INFO_SERVICE).GetAllCharacteristics())
            {
                var result = await characteristic.ReadValueAsync();
                string value = result.Status == GattCommunicationStatus.Success ?
                    System.Text.Encoding.UTF8.GetString(result.Value.ToArray(), 0, (int)result.Value.Length) :
                    "N/A";
                mwDeviceInfoChars.Add(characteristic.Uuid, value);
                outputListView.Items.Add(new ConsoleLine(ConsoleEntryType.INFO, DEVICE_INFO_NAMES[characteristic.Uuid] + ": " + value));
            }

            mwNotifyChar = mwGattService.GetCharacteristics(METAWEAR_NOTIFY_CHARACTERISTIC).First();
            await mwNotifyChar.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            mwNotifyChar.ValueChanged += new TypedEventHandler<Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic, GattValueChangedEventArgs>((Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic sender, GattValueChangedEventArgs obj) =>
            {
                byte[] response = obj.CharacteristicValue.ToArray();
                Functions.mbl_mw_connection_notify_char_changed(mwBoard, response, (byte)response.Length);
            });
        }

        public void acc_handler(IntPtr data) { receivedSensorData(data, Signal.ACCELEROMETER); }
        public void baroAlt_handler(IntPtr data) { receivedSensorData(data, Signal.BMP280_ALTITUDE); }
        public void baroPre_handler(IntPtr data) { receivedSensorData(data, Signal.BMP280_PRESSURE); }
        public void amb_handler(IntPtr data) { receivedSensorData(data, Signal.AMBIENT_LIGHT); }
        public void gyro_handler(IntPtr data) { receivedSensorData(data, Signal.GYRO); }
        public void switch_handler(IntPtr data) { receivedSensorData(data, Signal.SWITCH); }

        private void receivedSensorData(IntPtr data, Signal signal)
        {
            object managedValue = null;

            Data _data = Marshal.PtrToStructure<Data>(data);

            switch (_data.typeId)
            {
                case DataTypeId.UINT32:
                    managedValue = Marshal.PtrToStructure<uint>(_data.value);
                    break;
                case DataTypeId.FLOAT:
                    managedValue = Marshal.PtrToStructure<float>(_data.value);
                    break;
                case DataTypeId.CARTESIAN_FLOAT:
                    managedValue = Marshal.PtrToStructure<CartesianFloat>(_data.value);
                    break;
            }

            if (managedValue == null) return;

            ConsoleLine newLine = new ConsoleLine(ConsoleEntryType.SENSOR);

            switch (signal)
            {
                case Signal.ACCELEROMETER:
                    newLine.Value = "Acceleration: " + managedValue.ToString();
                    break;
                case Signal.BMP280_ALTITUDE:
                    newLine.Value = string.Format("Altitude: {0:F3}m", (float)managedValue);
                    break;
                case Signal.BMP280_PRESSURE:
                    newLine.Value = string.Format("Pressure: {0:F3}pa", (float)managedValue);
                    break;
                case Signal.AMBIENT_LIGHT:
                    newLine.Value = string.Format("Illuminance: {0:D}mlx", (uint)managedValue);
                    break;
                case Signal.GYRO:
                    newLine.Value = string.Format("Rotation: {0:S} \u00B0/s", managedValue.ToString());
                    break;
                case Signal.SWITCH:
                    newLine.Value = "Switch " + (((uint)managedValue) == 0 ? "Released" : "Pressed");
                    break;
                default:
                    newLine.Value = "Unexpected signal data";
                    break;
            }

            if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
            {
                outputListView.Items.Add(newLine);
            }
            else {
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () => outputListView.Items.Add(newLine)
                );
            }
        }

        private void startMotor(object sender, RoutedEventArgs e)
        {
            Functions.mbl_mw_haptic_start_motor(mwBoard, (float)100, 5000);
        }

        private void startBuzzer(object sender, RoutedEventArgs e)
        {
            Functions.mbl_mw_haptic_start_buzzer(mwBoard, 5000);
        }

        private void toggleAccelerationSampling(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;

            if (mwDeviceInfoChars[CHARACTERISTIC_MODEL_NUMBER].Equals("0"))
            {
                if (!signals.ContainsKey(Signal.ACCELEROMETER))
                {
                    signals[Signal.ACCELEROMETER] = Functions.mbl_mw_acc_get_acceleration_data_signal(mwBoard);
                }

                if (toggleSwitch != null)
                {
                    if (toggleSwitch.IsOn)
                    {
                        Functions.mbl_mw_acc_mma8452q_set_odr(mwBoard, AccelerometerMma8452q.OutputDataRate.ODR_12_5HZ);
                        Functions.mbl_mw_acc_mma8452q_set_range(mwBoard, AccelerometerMma8452q.FullScaleRange.FSR_8G);
                        Functions.mbl_mw_acc_mma8452q_write_acceleration_config(mwBoard);

                        Functions.mbl_mw_datasignal_subscribe(signals[Signal.ACCELEROMETER], acc_handler);
                        Functions.mbl_mw_acc_mma8452q_enable_acceleration_sampling(mwBoard);
                        Functions.mbl_mw_acc_mma8452q_start(mwBoard);
                    }
                    else {
                        Functions.mbl_mw_acc_mma8452q_stop(mwBoard);
                        Functions.mbl_mw_acc_mma8452q_disable_acceleration_sampling(mwBoard);
                        Functions.mbl_mw_datasignal_unsubscribe(signals[Signal.ACCELEROMETER]);
                    }
                }
            }
            else {
                //some of these functions are missing

                //if (!signals.ContainsKey(Signal.ACCELEROMETER))
                //{
                //    signals[Signal.ACCELEROMETER] = Functions.mbl_mw_acc_bmi160_get_acceleration_data_signal(mwBoard);
                //}

                //if (toggleSwitch != null)
                //{
                //    if (toggleSwitch.IsOn)
                //    {
                //        Functions.mbl_mw_acc_bmi160_set_odr(mwBoard, AccelerometerBmi160.OutputDataRate.ODR_12_5HZ);
                //        Functions.mbl_mw_acc_bmi160_set_range(mwBoard, AccelerometerBmi160.FullScaleRange.FSR_8G);
                //        Functions.mbl_mw_acc_bmi160_write_acceleration_config(mwBoard);

                //        Functions.mbl_mw_datasignal_subscribe(signals[Signal.ACCELEROMETER], acc_handler);
                //        Functions.mbl_mw_acc_bmi160_enable_acceleration_sampling(mwBoard);
                //        Functions.mbl_mw_acc_bmi160_start(mwBoard);
                //    }
                //    else {
                //        Functions.mbl_mw_acc_bmi160_stop(mwBoard);
                //        Functions.mbl_mw_acc_bmi160_disable_acceleration_sampling(mwBoard);
                //        Functions.mbl_mw_datasignal_unsubscribe(signals[Signal.ACCELEROMETER]);
                //    }
                //}
            }
        }

        private void toggleBarometerSampling(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;

            if (!signals.ContainsKey(Signal.BMP280_PRESSURE))
            {
                signals[Signal.BMP280_PRESSURE] = Functions.mbl_mw_baro_bosch_get_pressure_data_signal(mwBoard);
            }
            if (!signals.ContainsKey(Signal.BMP280_ALTITUDE))
            {
                signals[Signal.BMP280_ALTITUDE] = Functions.mbl_mw_baro_bosch_get_altitude_data_signal(mwBoard);
            }

            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn)
                {
                    Functions.mbl_mw_datasignal_subscribe(signals[Signal.BMP280_ALTITUDE], baroAlt_handler);
                    Functions.mbl_mw_datasignal_subscribe(signals[Signal.BMP280_PRESSURE], baroPre_handler);
                    Functions.mbl_mw_baro_bosch_start(mwBoard);
                }
                else {
                    Functions.mbl_mw_baro_bosch_stop(mwBoard);
                    Functions.mbl_mw_datasignal_unsubscribe(signals[Signal.BMP280_ALTITUDE]);
                    Functions.mbl_mw_datasignal_unsubscribe(signals[Signal.BMP280_PRESSURE]);
                }
            }
        }

        private void toggleAmbientLightSampling(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (!signals.ContainsKey(Signal.AMBIENT_LIGHT))
            {
                signals[Signal.AMBIENT_LIGHT] = Functions.mbl_mw_als_ltr329_get_illuminance_data_signal(mwBoard);
            }

            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn)
                {
                    Functions.mbl_mw_datasignal_subscribe(signals[Signal.AMBIENT_LIGHT], amb_handler);
                    Functions.mbl_mw_als_ltr329_start(mwBoard);
                }
                else {
                    Functions.mbl_mw_als_ltr329_stop(mwBoard);
                    Functions.mbl_mw_datasignal_unsubscribe(signals[Signal.AMBIENT_LIGHT]);
                }
            }
        }

        private void toggleGyroSampling(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (!signals.ContainsKey(Signal.GYRO))
            {
                signals[Signal.GYRO] = Functions.mbl_mw_gyro_bmi160_get_rotation_data_signal(mwBoard);
            }

            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn)
                {
                    Functions.mbl_mw_datasignal_subscribe(signals[Signal.GYRO], gyro_handler);
                    Functions.mbl_mw_gyro_bmi160_set_odr(mwBoard, GyroBmi160.OutputDataRate.ODR_25HZ);
                    Functions.mbl_mw_gyro_bmi160_set_range(mwBoard, GyroBmi160.FullScaleRange.FSR_500DPS);
                    Functions.mbl_mw_gyro_bmi160_write_config(mwBoard);
                    Functions.mbl_mw_gyro_bmi160_enable_rotation_sampling(mwBoard);
                    Functions.mbl_mw_gyro_bmi160_start(mwBoard);
                }
                else {
                    Functions.mbl_mw_gyro_bmi160_stop(mwBoard);
                    Functions.mbl_mw_gyro_bmi160_disable_rotation_sampling(mwBoard);
                    Functions.mbl_mw_datasignal_unsubscribe(signals[Signal.GYRO]);
                }
            }
        }

        private void toggleSwitchSampling(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;

            if (!signals.ContainsKey(Signal.SWITCH))
            {
                signals[Signal.SWITCH] = Functions.mbl_mw_switch_get_state_data_signal(mwBoard);
            }

            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn)
                {
                    Functions.mbl_mw_datasignal_subscribe(signals[Signal.SWITCH], switch_handler);
                }
                else {
                    Functions.mbl_mw_datasignal_unsubscribe(signals[Signal.SWITCH]);
                }
            }
        }

        private void ledRedOn(object sender, RoutedEventArgs e)
        {
            Led.Pattern pattern = new Led.Pattern();
            Functions.mbl_mw_led_load_preset_pattern(ref pattern, Led.PatternPreset.SOLID);
            Functions.mbl_mw_led_write_pattern(mwBoard, ref pattern, Led.Color.RED);
            Functions.mbl_mw_led_play(mwBoard);
        }

        private void ledGreenOn(object sender, RoutedEventArgs e)
        {
            Led.Pattern pattern = new Led.Pattern();
            Functions.mbl_mw_led_load_preset_pattern(ref pattern, Led.PatternPreset.BLINK);
            Functions.mbl_mw_led_write_pattern(mwBoard, ref pattern, Led.Color.GREEN);
            Functions.mbl_mw_led_play(mwBoard);
        }

        private void ledBlueOn(object sender, RoutedEventArgs e)
        {
            Led.Pattern pattern = new Led.Pattern();
            Functions.mbl_mw_led_load_preset_pattern(ref pattern, Led.PatternPreset.PULSE);
            Functions.mbl_mw_led_write_pattern(mwBoard, ref pattern, Led.Color.BLUE);
            Functions.mbl_mw_led_play(mwBoard);
        }

        private void ledOff(object sender, RoutedEventArgs e)
        {
            Functions.mbl_mw_led_stop_and_clear(mwBoard);
        }
    }
}