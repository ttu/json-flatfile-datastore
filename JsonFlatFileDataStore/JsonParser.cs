using System;
using System.Dynamic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonFlatFileDataStore;

/// <summary>
/// JSON parsing that keeps the precision of non-integral numbers.
///
/// Newtonsoft materializes them as double by default, which silently corrupts any value with more
/// significant digits than a double can hold — and a value such as decimal.MaxValue is then written
/// back in exponent notation, after which it no longer deserializes into a decimal property at all.
///
/// Reading numbers as decimal avoids both, but decimal covers a much narrower range than double.
/// A document holding a value outside that range is parsed the old way instead: too large throws
/// and is caught, too small would round to zero without any error, so those documents are
/// recognized from the text before parsing.
/// </summary>
internal static class JsonParser
{
    // Decimal keeps at most 28 digits after the point
    private const int MaxDecimalScale = 28;

    private static readonly string _unsafeSmallLiteral = "0." + new string('0', MaxDecimalScale);

    private static readonly JsonSerializerSettings _decimalSettings = new JsonSerializerSettings
    { FloatParseHandling = FloatParseHandling.Decimal };

    internal static JObject Parse(string jsonText)
    {
        if (!MayLoseDecimalPrecision(jsonText))
        {
            try
            {
                return LoadWithDecimalNumbers(jsonText);
            }
            catch (JsonReaderException)
            {
                // Either a number too large for decimal or genuinely invalid JSON. Parsing again
                // with the default handling either succeeds or throws the error the caller expects.
            }
        }

        return JObject.Parse(jsonText);
    }

    internal static ExpandoObject ToExpandoObject(string jsonText)
    {
        if (!MayLoseDecimalPrecision(jsonText))
        {
            try
            {
                return JsonConvert.DeserializeObject<ExpandoObject>(jsonText, _decimalSettings);
            }
            catch (JsonReaderException)
            {
            }
        }

        return JsonConvert.DeserializeObject<ExpandoObject>(jsonText);
    }

    private static JObject LoadWithDecimalNumbers(string jsonText)
    {
        using (var stringReader = new StringReader(jsonText))
        using (var jsonReader = new JsonTextReader(stringReader) { FloatParseHandling = FloatParseHandling.Decimal })
        {
            var jObject = JObject.Load(jsonReader);

            // JObject.Parse rejects trailing content after the object, so do the same
            while (jsonReader.Read())
            {
                if (jsonReader.TokenType != JsonToken.Comment)
                    throw new JsonReaderException("Additional text found in JSON string after parsing content.");
            }

            return jObject;
        }
    }

    /// <summary>
    /// True when the text contains a number so small that reading it as a decimal would round it
    /// towards zero. Text inside strings is not skipped: a false positive only means the document
    /// is read the way it always was, whereas a missed number would be silently destroyed.
    /// </summary>
    private static bool MayLoseDecimalPrecision(string jsonText)
    {
        for (var i = 0; i < jsonText.Length - 2; i++)
        {
            var current = jsonText[i];

            if ((current != 'e' && current != 'E') || jsonText[i + 1] != '-')
                continue;

            var exponent = 0;
            var digitIndex = i + 2;

            for (; digitIndex < jsonText.Length && char.IsDigit(jsonText[digitIndex]); digitIndex++)
            {
                exponent = exponent * 10 + (jsonText[digitIndex] - '0');

                if (exponent > MaxDecimalScale)
                    return true;
            }

            // The exponent alone does not decide it: the digits the mantissa already has after the
            // point shift the value further down. 1e-28 is exact, 1.5e-28 is not.
            if (digitIndex > i + 2 && exponent + CountFractionDigits(jsonText, i) > MaxDecimalScale)
                return true;
        }

        // Same magnitude written without an exponent, which only a hand-authored file contains
        return jsonText.IndexOf(_unsafeSmallLiteral, StringComparison.Ordinal) >= 0;
    }

    /// <summary>
    /// Number of digits the mantissa ending at <paramref name="exponentIndex"/> has after the point.
    /// </summary>
    private static int CountFractionDigits(string jsonText, int exponentIndex)
    {
        var index = exponentIndex - 1;

        while (index >= 0 && char.IsDigit(jsonText[index]))
            index--;

        return index >= 0 && jsonText[index] == '.' ? exponentIndex - 1 - index : 0;
    }
}