using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace MetaWear.NugetImplementation.SampleApp
{
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
}
