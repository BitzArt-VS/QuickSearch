using System;

namespace BitzArt.UI.Tweaks.Gui;

public readonly record struct GuiColor
{
    public double R { get; }
    public double G { get; }
    public double B { get; }
    public double A { get; }

    private GuiColor(double r, double g, double b, double a = 1.0)
    {
        if (r < 0.0 || r > 1.0) throw new ArgumentOutOfRangeException(nameof(r), r, "Must be in [0, 1].");
        if (g < 0.0 || g > 1.0) throw new ArgumentOutOfRangeException(nameof(g), g, "Must be in [0, 1].");
        if (b < 0.0 || b > 1.0) throw new ArgumentOutOfRangeException(nameof(b), b, "Must be in [0, 1].");
        if (a < 0.0 || a > 1.0) throw new ArgumentOutOfRangeException(nameof(a), a, "Must be in [0, 1].");

        R = r; G = g; B = b; A = a;
    }

    // ── Float RGBA (0–1) ───────────────────────────────────────────────────

    public static GuiColor FromRgb(double r, double g, double b) => new(r, g, b);
    public static GuiColor FromRgba(double r, double g, double b, double a) => new(r, g, b, a);

    // ── Byte RGB (0–255) ───────────────────────────────────────────────────

    public static GuiColor FromRgb(byte r, byte g, byte b) =>
        new(r / 255.0, g / 255.0, b / 255.0);

    public static GuiColor FromRgba(byte r, byte g, byte b, byte a) =>
        new(r / 255.0, g / 255.0, b / 255.0, a / 255.0);

    // ── Hex (#rgb #rgba #rrggbb #rrggbbaa) ────────────────────────────────

    public static GuiColor FromHex(string hex)
    {
        ReadOnlySpan<char> s = hex.AsSpan().TrimStart('#');

        return s.Length switch
        {
            3 => new(ExpandNibble(s[0]) / 255.0, ExpandNibble(s[1]) / 255.0, ExpandNibble(s[2]) / 255.0),
            4 => new(ExpandNibble(s[0]) / 255.0, ExpandNibble(s[1]) / 255.0, ExpandNibble(s[2]) / 255.0, ExpandNibble(s[3]) / 255.0),
            6 => new(ParseByte(s, 0) / 255.0, ParseByte(s, 2) / 255.0, ParseByte(s, 4) / 255.0),
            8 => new(ParseByte(s, 0) / 255.0, ParseByte(s, 2) / 255.0, ParseByte(s, 4) / 255.0, ParseByte(s, 6) / 255.0),
            _ => throw new FormatException($"Invalid hex color '{hex}': expected #rgb, #rgba, #rrggbb, or #rrggbbaa.")
        };
    }

    private static int ExpandNibble(char c)
    {
        int v = HexVal(c);
        return (v << 4) | v;
    }

    private static int ParseByte(ReadOnlySpan<char> s, int offset) =>
        (HexVal(s[offset]) << 4) | HexVal(s[offset + 1]);

    private static int HexVal(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => throw new FormatException($"Invalid hex character '{c}'.")
    };

    // ── HSL (h: 0–360, s/l/a: 0–1) ────────────────────────────────────────

    public static GuiColor FromHsl(double h, double s, double l, double a = 1.0)
    {
        if (h < 0.0 || h > 360.0) throw new ArgumentOutOfRangeException(nameof(h), h, "Must be in [0, 360].");
        if (s < 0.0 || s > 1.0)   throw new ArgumentOutOfRangeException(nameof(s), s, "Must be in [0, 1].");
        if (l < 0.0 || l > 1.0)   throw new ArgumentOutOfRangeException(nameof(l), l, "Must be in [0, 1].");
        if (a < 0.0 || a > 1.0)   throw new ArgumentOutOfRangeException(nameof(a), a, "Must be in [0, 1].");

        if (s == 0.0) return new(l, l, l, a);

        double chroma = (1.0 - Math.Abs(2.0 * l - 1.0)) * s;
        double hPrime = h / 60.0;
        double x = chroma * (1.0 - Math.Abs(hPrime % 2.0 - 1.0));
        double m = l - chroma / 2.0;

        (double r1, double g1, double b1) = (int)hPrime switch
        {
            0 => (chroma, x, 0.0),
            1 => (x, chroma, 0.0),
            2 => (0.0, chroma, x),
            3 => (0.0, x, chroma),
            4 => (x, 0.0, chroma),
            _ => (chroma, 0.0, x)
        };

        // clamp to guard against floating-point precision drift
        return new(
            Math.Clamp(r1 + m, 0.0, 1.0),
            Math.Clamp(g1 + m, 0.0, 1.0),
            Math.Clamp(b1 + m, 0.0, 1.0),
            a);
    }

    // ── Named colors ───────────────────────────────────────────────────────

    public static GuiColor Transparent => new(0.0, 0.0, 0.0, 0.0);
    public static GuiColor Black       => new(0.0, 0.0, 0.0);
    public static GuiColor White       => new(1.0, 1.0, 1.0);
    public static GuiColor Red         => new(1.0, 0.0, 0.0);
    public static GuiColor Lime        => new(0.0, 1.0, 0.0);
    public static GuiColor Blue        => new(0.0, 0.0, 1.0);
    public static GuiColor Yellow      => new(1.0, 1.0, 0.0);
    public static GuiColor Cyan        => new(0.0, 1.0, 1.0);
    public static GuiColor Magenta     => new(1.0, 0.0, 1.0);
    public static GuiColor Gray        => new(0.5, 0.5, 0.5);
    public static GuiColor DarkGray    => new(0.25, 0.25, 0.25);
    public static GuiColor LightGray   => new(0.75, 0.75, 0.75);
}
