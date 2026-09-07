using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QL_HocVien.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public static readonly StringToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public static readonly BooleanToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }

    public class InvertedBooleanToVisibilityConverter : IValueConverter
    {
        public static readonly InvertedBooleanToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && !b)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v != Visibility.Visible;
        }
    }

    public class IntEqualsToVisibilityConverter : IValueConverter
    {
        public static readonly IntEqualsToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intVal && parameter != null)
            {
                if (int.TryParse(parameter.ToString(), out int targetVal))
                {
                    return intVal == targetVal ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToBrushConverter : IValueConverter
    {
        public static readonly StringToBrushConverter Instance = new();
        private static readonly System.Windows.Media.BrushConverter _brushConverter = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                try
                {
                    var brush = _brushConverter.ConvertFromString(s) as System.Windows.Media.Brush;
                    if (brush != null) return brush;
                }
                catch { }
            }
            return System.Windows.Media.Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToBrushConverter : IValueConverter
    {
        public static readonly BooleanToBrushConverter Instance = new();
        private static readonly System.Windows.Media.SolidColorBrush _greenBrush = new(System.Windows.Media.Color.FromRgb(22, 163, 74));
        private static readonly System.Windows.Media.SolidColorBrush _redBrush = new(System.Windows.Media.Color.FromRgb(220, 38, 38));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b) return _greenBrush;
            return _redBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public object Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }

    public class NullableDoubleConverter : IValueConverter
    {
        public static readonly NullableDoubleConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return d.ToString("0.##", CultureInfo.InvariantCulture);
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return null;
            string str = value.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(str)) return null;

            // Hỗ trợ cả dấu chấm '.' và dấu phẩy ',' (phù hợp thói quen người dùng)
            str = str.Replace(',', '.');
            if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                if (result < 0) return 0.0;
                if (result > 10) return 10.0;
                return Math.Round(result, 2);
            }
            return null;
        }
    }
}
