using System.Globalization;
using System.Text.Json.Serialization;

namespace JsonFlatFileDataStore;
/// <summary>
/// Custom DateTime converter for System.Text.Json that provides compatibility with Newtonsoft.Json DateTime formats.
///
/// This converter was created during the migration from Newtonsoft.Json to System.Text.Json to handle
/// DateTime strings that were serialized by Newtonsoft.Json, particularly the format "yyyy-MM-ddTHH:mm:ss"
/// without timezone designators, which System.Text.Json's default converter doesn't support.
///
/// Supported formats:
/// - "yyyy-MM-ddTHH:mm:ss.FFFFFFFK" (full precision with timezone)
/// - "yyyy-MM-ddTHH:mm:ss.FFFFFFF" (full precision without timezone)
/// - "yyyy-MM-ddTHH:mm:ssK" (seconds with timezone)
/// - "yyyy-MM-ddTHH:mm:ss" (seconds without timezone - Newtonsoft.Json default)
/// - "yyyy-MM-dd" (date only)
/// </summary>
public class NewtonsoftDateTimeConverter : JsonConverter<DateTime>
{
    private readonly string _defaultFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK"; // ISO 8601 format

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string dateString = reader.GetString();

            // Common formats to try
            string[] formats = new[]
            {
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",  // Full precision with timezone
            "yyyy-MM-ddTHH:mm:ss.FFFFFFF",   // Full precision without timezone
            "yyyy-MM-ddTHH:mm:ssK",          // Seconds with timezone
            "yyyy-MM-ddTHH:mm:ss",           // Seconds without timezone (Newtonsoft default)
            "yyyy-MM-dd"                     // Date only
        };

            // Try parsing with various formats
            foreach (var format in formats)
            {
                // Try with RoundtripKind first (for formats with timezone)
                if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime date))
                {
                    return date;
                }
                // Try without RoundtripKind for formats without timezone
                if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
            }

            // Last resort: try general parsing
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate;
            }
        }
        throw new JsonException($"Invalid date format: {reader.GetString()}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Write DateTime in ISO 8601 format (default for Newtonsoft.Json)
        writer.WriteStringValue(value.ToString(_defaultFormat, CultureInfo.InvariantCulture));
    }
}