using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace OthelloConsole.Converters;

public class GameModeConverter : IValueConverter
{
    public object? Convert(object? value, Type _, object? __, CultureInfo ___)
        => value is GameMode m ? m switch
        {
            GameMode.PlayervsAI     => "AI 対戦",
            GameMode.PlayervsPlayer => "2人対戦",
            GameMode.AIvsAI         => "AI vs AI",
            _                       => value.ToString()
        } : null;

    public object? ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

public class AiStrengthConverter : IValueConverter
{
    public object? Convert(object? value, Type _, object? __, CultureInfo ___)
        => value is AIStrength s ? s switch
        {
            AIStrength.Beginner     => "初心者",
            AIStrength.Normal       => "普通",
            AIStrength.Expert       => "上級者",
            AIStrength.Professional => "プロ",
            _                       => value.ToString()
        } : null;

    public object? ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

public class PieceColorConverter : IValueConverter
{
    public object? Convert(object? value, Type _, object? __, CultureInfo ___)
        => value is int v ? v switch
        {
            1 => Brushes.Black,
            2 => Brushes.White,
            _ => Brushes.Transparent
        } : Brushes.Transparent;

    public object? ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

public class HasPieceConverter : IValueConverter
{
    public object? Convert(object? value, Type _, object? __, CultureInfo ___)
        => value is int v && v != 0;

    public object? ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

public class LastMoveBgConverter : IValueConverter
{
    private static readonly SolidColorBrush Normal   = new(Color.Parse("#1a6b3c"));
    private static readonly SolidColorBrush LastMove = new(Color.Parse("#2a9b5c"));

    public object? Convert(object? value, Type _, object? __, CultureInfo ___)
        => value is true ? LastMove : Normal;

    public object? ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

public class IsPvAiConverter : IValueConverter
{
    public object? Convert(object? value, Type _, object? __, CultureInfo ___)
        => value is GameMode.PlayervsAI;

    public object? ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

public class HasAiConverter : IValueConverter
{
    public object? Convert(object? value, Type _, object? __, CultureInfo ___)
        => value is GameMode.PlayervsAI or GameMode.AIvsAI;

    public object? ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}
