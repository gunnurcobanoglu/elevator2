using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace elevator_simulation.Converters
{
    public class FloorToPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int floor)
            {
                // 0-9 katlar arasi: Normal hareket (floor * 55 yukari)
                // 10 ve uzeri katlarda: Asansor 9. katta sabitlenir, katlar kayar
                if (floor <= 9)
                {
                    // 0-9 arasi normal hareket
                    return -(floor * 55);
                }
                else
                {
                    // 10 ve uzeri: Asansor 9. katta sabit kalir
                    return -(9 * 55);
                }
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Kat numaralari icin converter - asansor yukari ciktikca ust katlar kaybolur, alt katlar gorunur
    public class FloorListPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int floor)
            {
                // 0-9 katlari arasi: Canvas sabit kalir (0-9 gorunur)
                // 10. kattan itibaren: Canvas asagi kayar, ust katlar kaybolur
                // 10. kat ve uzeri: (floor - 9) * 55 kadar asagi kay
                int offset = Math.Max(0, floor - 9);
                return (offset * 55); // Pozitif deger - asagi kayar
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PassengerStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasPassenger)
            {
                return hasPassenger ? "Iceride" : "Yok";
            }
            return "Yok";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FloorEqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is int buttonFloor && values[1] is int currentFloor)
            {
                return buttonFloor == currentFloor;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
