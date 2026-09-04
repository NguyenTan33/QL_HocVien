using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace QL_HocVien.Converters
{
    public class GradeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string grade)
            {
                return grade switch
                {
                    "Xuất sắc" => new SolidColorBrush(Color.FromRgb(16, 185, 129)),  // Emerald
                    "Giỏi" => new SolidColorBrush(Color.FromRgb(37, 99, 235)),       // Blue
                    "Khá" => new SolidColorBrush(Color.FromRgb(13, 148, 136)),       // Teal
                    "Đạt" => new SolidColorBrush(Color.FromRgb(217, 119, 6)),        // Amber
                    "Không đạt" => new SolidColorBrush(Color.FromRgb(220, 38, 38)),   // Red
                    _ => new SolidColorBrush(Color.FromRgb(100, 116, 139))           // Slate
                };
            }
            return new SolidColorBrush(Color.FromRgb(100, 116, 139));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
