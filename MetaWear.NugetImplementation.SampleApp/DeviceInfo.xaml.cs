﻿using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Sensor;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MetaWear.NugetImplementation.SampleApp
{
    public enum ConsoleEntryType
    {
        SEVERE,
        INFO,
        COMMAND,
        SENSOR
    }

    public class ConsoleLineColorConverter : IValueConverter
    {
        public SolidColorBrush SevereColor { get; set; }
        public SolidColorBrush InfoColor { get; set; }
        public SolidColorBrush CommandColor { get; set; }
        public SolidColorBrush SensorColor { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch ((ConsoleEntryType)value)
            {
                case ConsoleEntryType.SEVERE:
                    return SevereColor;
                case ConsoleEntryType.INFO:
                    return InfoColor;
                case ConsoleEntryType.COMMAND:
                    return CommandColor;
                case ConsoleEntryType.SENSOR:
                    return SensorColor;
                default:
                    throw new MissingMemberException("Unrecognized console entry type: " + value.ToString());
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public class ConsoleLine
    {
        public ConsoleLine(ConsoleEntryType type)
        {
            this.Type = type;
        }

        public ConsoleLine(ConsoleEntryType type, string value)
        {
            this.Type = type;
            this.Value = value;
        }

        public ConsoleEntryType Type { get; }
        public string Value { get; set; }
    }

    public sealed partial class DeviceInfo : Page
    {
        private static readonly Guid DEVICE_INFO_SERVICE = new Guid("0000180a-0000-1000-8000-00805f9b34fb"),
            CHARACTERISTIC_MANUFACTURER = new Guid("00002a29-0000-1000-8000-00805f9b34fb"),
            CHARACTERISTIC_MODEL_NUMBER = new Guid("00002a24-0000-1000-8000-00805f9b34fb"),
            CHARACTERISTIC_SERIAL_NUMBER = new Guid("00002a25-0000-1000-8000-00805f9b34fb"),
            CHARACTERISTIC_FIRMWARE_REVISION = new Guid("00002a26-0000-1000-8000-00805f9b34fb"),
            CHARACTERISTIC_HARDWARE_REVISION = new Guid("00002a27-0000-1000-8000-00805f9b34fb"),
            GUID_METAWEAR_SERVICE = new Guid("326A9000-85CB-9195-D9DD-464CFBBAE75A"),
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

        private Dictionary<Signal, IntPtr> signals = new Dictionary<Signal, IntPtr>();
        private Dictionary<Guid, string> mwDeviceInfoChars = new Dictionary<Guid, string>();
        private BluetoothLEDevice selectedBtleDevice;
        private IntPtr mwBoard;

        private BtleConnection conn;

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

        private void WriteGattChar(IntPtr characteristic, IntPtr bytes, byte length)
        {
            // todo... figure out how to write gatt char 
        }

        private void Initialized() { }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            selectedBtleDevice = (BluetoothLEDevice)e.Parameter;
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
                // I could only find bmi160 specific funtions for some of these, but not all

                //if (!signals.ContainsKey(Signal.ACCELEROMETER))
                //{
                //    signals[Signal.ACCELEROMETER] = AccelerometerBmi160.GetAccelerationDataSignal(mwBoard);
                //}

                //if (toggleSwitch != null)
                //{
                //    if (toggleSwitch.IsOn)
                //    {
                //        AccelerometerBmi160.SetOutputDataRate(mwBoard, AccelerometerBmi160.OutputDataRate.ODR_12_5HZ);
                //        AccelerometerBmi160.SetFullScaleRange(mwBoard, AccelerometerBmi160.FullScaleRange.FSR_8G);
                //        AccelerometerBmi160.WriteAccelerationConfig(mwBoard);

                //        DataSignal.Subscribe(signals[Signal.ACCELEROMETER]);
                //        AccelerometerBmi160.EnableAccelerationSampling(mwBoard);
                //        AccelerometerBmi160.Start(mwBoard);
                //    }
                //    else {
                //        AccelerometerBmi160.Stop(mwBoard);
                //        AccelerometerBmi160.DisableAccelerationSampling(mwBoard);
                //        DataSignal.Unsubscribe(signals[Signal.ACCELEROMETER]);
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