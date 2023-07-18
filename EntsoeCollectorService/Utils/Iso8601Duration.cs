namespace EntsoeCollectorService.Utils;

using System.Xml;

public static class Iso8601Duration
{
    #region Public Methods and Operators

    public static bool TryParseIso8601DurationString(this string? value, out TimeSpan timeSpan)
    {
        timeSpan = default;

        if (value == null)
        {
            return false;
        }

        try
        {
            timeSpan = XmlConvert.ToTimeSpan(value);
            return true;
        }

        catch
        {
            return false;
        }
    }

    #endregion
}
