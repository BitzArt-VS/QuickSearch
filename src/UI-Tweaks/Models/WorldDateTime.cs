using System.Globalization;
using System.Text;

namespace BitzArt.UI.Tweaks.GameStatus;

public readonly record struct WorldDateTime(int Year, int Month, int Day, int Hour, int Minute, int Second = 0) : IFormattable
{
    public override string ToString()
    {
        return ToString(null, CultureInfo.CurrentCulture);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        format = string.IsNullOrEmpty(format) ? "G" : format;

        if (TryCreateDateTime(out var dateTime))
        {
            return dateTime.ToString(format, formatProvider);
        }

        var dateTimeFormat = DateTimeFormatInfo.GetInstance(formatProvider);

        if (format.Length == 1)
        {
            return FormatStandard(format[0], dateTimeFormat);
        }

        return FormatCustom(format, dateTimeFormat);
    }

    private bool TryCreateDateTime(out DateTime dateTime)
    {
        if (Year is < 1 or > 9999)
        {
            dateTime = default;
            return false;
        }

        try
        {
            dateTime = new DateTime(Year, Month, Day, Hour, Minute, Second);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            dateTime = default;
            return false;
        }
    }

    private string FormatStandard(char format, DateTimeFormatInfo dateTimeFormat)
    {
        return format switch
        {
            'd' => FormatCustom(dateTimeFormat.ShortDatePattern, dateTimeFormat),
            'D' => FormatCustom(dateTimeFormat.LongDatePattern, dateTimeFormat),
            't' => FormatCustom(dateTimeFormat.ShortTimePattern, dateTimeFormat),
            'T' => FormatCustom(dateTimeFormat.LongTimePattern, dateTimeFormat),
            'g' => FormatCustom($"{dateTimeFormat.ShortDatePattern} {dateTimeFormat.ShortTimePattern}", dateTimeFormat),
            'G' => FormatCustom($"{dateTimeFormat.ShortDatePattern} {dateTimeFormat.LongTimePattern}", dateTimeFormat),
            'm' or 'M' => FormatCustom(dateTimeFormat.MonthDayPattern, dateTimeFormat),
            'y' or 'Y' => FormatCustom(dateTimeFormat.YearMonthPattern, dateTimeFormat),
            _ => FormatCustom(format.ToString(), dateTimeFormat)
        };
    }

    private string FormatCustom(string format, DateTimeFormatInfo dateTimeFormat)
    {
        var result = new StringBuilder(format.Length + 8);

        for (int index = 0; index < format.Length; index++)
        {
            char current = format[index];

            if (current is '\'' or '"')
            {
                index = AppendQuotedText(result, format, index, current);
                continue;
            }

            if (current == '\\' && index + 1 < format.Length)
            {
                result.Append(format[++index]);
                continue;
            }

            if (current == '%' && index + 1 < format.Length)
            {
                result.Append(FormatToken(format[++index], 1, dateTimeFormat));
                continue;
            }

            int repeatCount = CountRepeat(format, index, current);
            result.Append(IsFormatToken(current)
                ? FormatToken(current, repeatCount, dateTimeFormat)
                : FormatLiteral(current, repeatCount, dateTimeFormat));
            index += repeatCount - 1;
        }

        return result.ToString();
    }

    private static int AppendQuotedText(StringBuilder result, string format, int startIndex, char quote)
    {
        int index = startIndex + 1;

        while (index < format.Length)
        {
            char current = format[index];

            if (current == quote)
            {
                return index;
            }

            if (current == '\\' && index + 1 < format.Length)
            {
                result.Append(format[++index]);
            }
            else
            {
                result.Append(current);
            }

            index++;
        }

        return format.Length - 1;
    }

    private static int CountRepeat(string format, int startIndex, char value)
    {
        int index = startIndex + 1;

        while (index < format.Length && format[index] == value)
        {
            index++;
        }

        return index - startIndex;
    }

    private static bool IsFormatToken(char value)
    {
        return value is 'y' or 'M' or 'd' or 'H' or 'h' or 'm' or 's' or 't';
    }

    private string FormatToken(char token, int count, DateTimeFormatInfo dateTimeFormat)
    {
        return token switch
        {
            'y' => FormatYear(count),
            'M' => FormatMonth(count, dateTimeFormat),
            'd' => FormatNumber(Day, count),
            'H' => FormatNumber(Hour, count),
            'h' => FormatNumber(Hour % 12 == 0 ? 12 : Hour % 12, count),
            'm' => FormatNumber(Minute, count),
            's' => FormatNumber(Second, count),
            't' => FormatAmPm(count, dateTimeFormat),
            _ => new string(token, count)
        };
    }

    private string FormatYear(int count)
    {
        if (count == 2)
        {
            return Math.Abs(Year % 100).ToString("D2", CultureInfo.InvariantCulture);
        }

        return count <= 1
            ? Year.ToString(CultureInfo.InvariantCulture)
            : Year.ToString($"D{count}", CultureInfo.InvariantCulture);
    }

    private string FormatMonth(int count, DateTimeFormatInfo dateTimeFormat)
    {
        return count switch
        {
            1 => Month.ToString(CultureInfo.InvariantCulture),
            2 => Month.ToString("D2", CultureInfo.InvariantCulture),
            3 => GetMonthName(dateTimeFormat.AbbreviatedMonthNames),
            _ => GetMonthName(dateTimeFormat.MonthNames)
        };
    }

    private string GetMonthName(string[] names)
    {
        return Month >= 1 && Month <= names.Length
            ? names[Month - 1]
            : Month.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatNumber(int value, int count)
    {
        return count <= 1
            ? value.ToString(CultureInfo.InvariantCulture)
            : value.ToString($"D{count}", CultureInfo.InvariantCulture);
    }

    private string FormatAmPm(int count, DateTimeFormatInfo dateTimeFormat)
    {
        var designator = Hour < 12 ? dateTimeFormat.AMDesignator : dateTimeFormat.PMDesignator;

        return count <= 1 && designator.Length > 0
            ? designator[..1]
            : designator;
    }

    private static string FormatLiteral(char value, int count, DateTimeFormatInfo dateTimeFormat)
    {
        if (value == ':')
        {
            return dateTimeFormat.TimeSeparator;
        }

        if (value == '/')
        {
            return dateTimeFormat.DateSeparator;
        }

        return new string(value, count);
    }
}
