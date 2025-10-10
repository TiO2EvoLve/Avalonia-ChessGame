using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using System.Collections.Generic;

namespace Chess.ViewModels
{
    public class HighlightConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isHighlighted = values[0] is bool b && b;
            var background = values[1] as IBrush ?? Brushes.Transparent;
            return isHighlighted ? Brushes.Moccasin : background;
        }

        public object ConvertBack(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
