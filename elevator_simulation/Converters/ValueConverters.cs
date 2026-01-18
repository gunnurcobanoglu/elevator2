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
                if (floor <= 9)
                {
                    return -(floor * 55);
                }
                else
                {
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

    public class FloorListPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int floor)
            {
                int offset = Math.Max(0, floor - 9);
                return (offset * 55);
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

    /// <summary>
    /// Kapý açýklýk miktarýný (0.0-1.0) TranslateX deðerine dönüþtürür
    /// Sol kapý için: Negatif yönde kayar (sola)
    /// Sað kapý için: Pozitif yönde kayar (saða)
    /// </summary>
    public class DoorPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double openAmount && parameter is string direction)
            {
                // Maksimum kayma mesafesi (100 piksel - kapýnýn geniþliðinin yarýsý)
                const double maxOffset = 100;

                if (direction == "Left")
                {
                    // Sol kapý: Sola kay (negatif)
                    return -(openAmount * maxOffset);
                }
                else if (direction == "Right")
                {
                    // Sað kapý: Saða kay (pozitif)
                    return (openAmount * maxOffset);
                }
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
