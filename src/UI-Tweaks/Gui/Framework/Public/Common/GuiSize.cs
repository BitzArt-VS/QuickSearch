using System.Globalization;

namespace BitzArt.UI.Tweaks.Gui;

public readonly struct GuiSize
{
    private readonly GuiSizeKind _kind;

    public double Value { get; }
    public double? Minimum { get; }
    public double? Maximum { get; }

    public bool IsAuto => _kind == GuiSizeKind.Auto;
    public bool IsFixed => _kind == GuiSizeKind.Fixed;
    public bool IsFraction => _kind == GuiSizeKind.Fraction;

    private GuiSize(GuiSizeKind kind, double value, double? minimum = null, double? maximum = null)
    {
        _kind = kind;
        Value = value;
        Minimum = minimum;
        Maximum = maximum;
    }

    public static GuiSize Auto => default;
    public static GuiSize Fixed(double value) => new(GuiSizeKind.Fixed, value);

    public static GuiSize Fraction(double value, double? minimum = null, double? maximum = null)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Fractional size must be non-negative.");
        }

        return new GuiSize(GuiSizeKind.Fraction, value, minimum, maximum);
    }

    public static GuiSize Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        string trimmed = value.Trim();
        if (trimmed.EndsWith('%'))
        {
            string percent = trimmed[..^1].Trim();
            return Fraction(double.Parse(percent, CultureInfo.InvariantCulture) / 100.0);
        }

        return Fixed(double.Parse(trimmed, CultureInfo.InvariantCulture));
    }

    public double Resolve(double availableSize)
    {
        double resolved = _kind switch
        {
            GuiSizeKind.Fixed => Value,
            GuiSizeKind.Fraction => availableSize * Value,
            _ => throw new InvalidOperationException("Auto sizes cannot be resolved directly."),
        };

        if (Minimum is not null)
        {
            resolved = Math.Max(Minimum.Value, resolved);
        }

        if (Maximum is not null)
        {
            resolved = Math.Min(Maximum.Value, resolved);
        }

        return resolved;
    }

    public double FixedOrDefault(double defaultValue)
        => IsFixed ? Value : defaultValue;

    internal bool CanResolve(double availableSize)
        => !IsAuto && (!IsFraction || !double.IsPositiveInfinity(availableSize));

    public static implicit operator GuiSize(int value) => Fixed(value);
    public static implicit operator GuiSize(double value) => Fixed(value);
    public static implicit operator GuiSize(string value) => Parse(value);
}

internal enum GuiSizeKind
{
    Auto,
    Fixed,
    Fraction,
}
