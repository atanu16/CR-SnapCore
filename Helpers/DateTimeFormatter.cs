using System.Globalization;

namespace LumaGallery.Helpers;

/// <summary>
/// Converts raw folder names into human-readable date and time strings.
/// Date format: DDMMYYYY → "DD Month YYYY"
/// Hour format: 0–23 → "H:00 AM/PM"
/// </summary>
public static class DateTimeFormatter
{
    private static readonly string[] MonthNames =
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    };

    /// <summary>
    /// Converts a DDMMYYYY folder name to a formatted date string.
    /// Example: "25022026" → "25 February 2026"
    /// </summary>
    public static string FormatDateFolder(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName) || rawName.Length != 8)
            return rawName;

        if (!int.TryParse(rawName[..2], out int day) ||
            !int.TryParse(rawName[2..4], out int month) ||
            !int.TryParse(rawName[4..], out int year))
            return rawName;

        if (month < 1 || month > 12 || day < 1 || day > 31)
            return rawName;

        return $"{day} {MonthNames[month - 1]} {year}";
    }

    /// <summary>
    /// Parses a DDMMYYYY folder name into a DateTime.
    /// </summary>
    public static DateTime ParseDateFolder(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName) || rawName.Length != 8)
            return DateTime.MinValue;

        if (DateTime.TryParseExact(rawName, "ddMMyyyy",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        return DateTime.MinValue;
    }

    /// <summary>
    /// Converts a 0–23 hour folder name to a formatted time string.
    /// Example: "13" → "1:00 PM", "0" → "12:00 AM"
    /// </summary>
    public static string FormatHourFolder(string rawName)
    {
        if (!int.TryParse(rawName, out int hour) || hour < 0 || hour > 23)
            return rawName;

        return FormatHour(hour);
    }

    /// <summary>
    /// Formats an integer hour (0–23) to 12-hour AM/PM format.
    /// </summary>
    public static string FormatHour(int hour)
    {
        string period = hour < 12 ? "AM" : "PM";
        int displayHour = hour % 12;
        if (displayHour == 0) displayHour = 12;
        return $"{displayHour}:00 {period}";
    }
}
