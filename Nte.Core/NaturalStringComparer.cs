using System.Globalization;

namespace NET_Thing_Encryptor;

public sealed class NaturalStringComparer : IComparer<string>
{
    private readonly CompareInfo _compareInfo;

    public NaturalStringComparer(CultureInfo? culture = null)
    {
        _compareInfo = (culture ?? CultureInfo.CurrentCulture).CompareInfo;
    }

    public int Compare(string? x, string? y)
    {
        x ??= string.Empty;
        y ??= string.Empty;

        int xIndex = 0;
        int yIndex = 0;
        while (xIndex < x.Length && yIndex < y.Length)
        {
            bool xIsDigit = char.IsAsciiDigit(x[xIndex]);
            bool yIsDigit = char.IsAsciiDigit(y[yIndex]);
            if (xIsDigit && yIsDigit)
            {
                int numericComparison = CompareNumericRun(x, ref xIndex, y, ref yIndex);
                if (numericComparison != 0)
                    return numericComparison;
                continue;
            }

            if (xIsDigit != yIsDigit)
                return xIsDigit ? -1 : 1;

            int xStart = xIndex;
            int yStart = yIndex;
            while (xIndex < x.Length && !char.IsAsciiDigit(x[xIndex]))
                xIndex++;
            while (yIndex < y.Length && !char.IsAsciiDigit(y[yIndex]))
                yIndex++;

            int textComparison = _compareInfo.Compare(
                x,
                xStart,
                xIndex - xStart,
                y,
                yStart,
                yIndex - yStart,
                CompareOptions.IgnoreCase);
            if (textComparison != 0)
                return textComparison;
        }

        return (x.Length - xIndex).CompareTo(y.Length - yIndex);
    }

    private static int CompareNumericRun(string x, ref int xIndex, string y, ref int yIndex)
    {
        int xRunStart = xIndex;
        int yRunStart = yIndex;
        while (xIndex < x.Length && char.IsAsciiDigit(x[xIndex]))
            xIndex++;
        while (yIndex < y.Length && char.IsAsciiDigit(y[yIndex]))
            yIndex++;

        int xSignificant = xRunStart;
        int ySignificant = yRunStart;
        while (xSignificant < xIndex - 1 && x[xSignificant] == '0')
            xSignificant++;
        while (ySignificant < yIndex - 1 && y[ySignificant] == '0')
            ySignificant++;

        int xDigits = xIndex - xSignificant;
        int yDigits = yIndex - ySignificant;
        int lengthComparison = xDigits.CompareTo(yDigits);
        if (lengthComparison != 0)
            return lengthComparison;

        int digitComparison = string.CompareOrdinal(x, xSignificant, y, ySignificant, xDigits);
        if (digitComparison != 0)
            return digitComparison;

        return (xIndex - xRunStart).CompareTo(yIndex - yRunStart);
    }
}
