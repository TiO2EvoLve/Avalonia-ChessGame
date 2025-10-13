using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Chess.ViewModels;

public class PieceToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string symbol || string.IsNullOrEmpty(symbol))
            return null;

        // 根据大小写判断颜色
        string color = char.IsLower(symbol[0]) ? "w" : "b";
        string type = char.ToLower(symbol[0]).ToString(); // k, q, r, n, b, p

        string uri = $"avares://Chess/Assets/chess/{color}{type}.png";
        try
        {
            return new Bitmap(AssetLoader.Open(new Uri(uri)));
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}