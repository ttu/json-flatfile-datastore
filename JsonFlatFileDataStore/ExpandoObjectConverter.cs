using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Text.Json.Serialization;

namespace JsonFlatFileDataStore;

public class SystemExpandoObjectConverter : JsonConverter<ExpandoObject>
{
    public override ExpandoObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                var expando = new ExpandoObject();
                var dictionary = (IDictionary<string, object>)expando;

                foreach (var property in root.EnumerateObject())
                {
                    AddPropertyToExpando(dictionary, property.Name, property.Value);
                }

                return expando;
            }

            throw new JsonException("Invalid JSON: Expected JSON object.");
        }
    }

    public override void Write(Utf8JsonWriter writer, ExpandoObject value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (IDictionary<string, object>)value, options);
    }

    private static void AddPropertyToExpando(IDictionary<string, object> expando, string propertyName, JsonElement propertyValue)
    {
        switch (propertyValue.ValueKind)
        {
            case JsonValueKind.Undefined:
            case JsonValueKind.Null:
                expando[propertyName] = null;
                break;

            case JsonValueKind.False:
                expando[propertyName] = false;
                break;

            case JsonValueKind.True:
                expando[propertyName] = true;
                break;

            case JsonValueKind.Number:
                // Newtonsoft.Json deserialized JSON integers as Int64 for dynamic/ExpandoObject mode;
                // preserve that behavior so dynamic arithmetic and type assertions stay stable.
                if (propertyValue.TryGetInt64(out long longValue))
                {
                    expando[propertyName] = longValue;
                }
                else if (propertyValue.TryGetDouble(out double doubleValue))
                {
                    expando[propertyName] = doubleValue;
                }
                else if (propertyValue.TryGetDecimal(out decimal decimalValue))
                {
                    expando[propertyName] = decimalValue;
                }
                else
                {
                    throw new JsonException("Unsupported numeric type");
                }
                break;

            case JsonValueKind.String:
                // Try to parse as DateTime to maintain Newtonsoft.Json compatibility
                // Newtonsoft.Json automatically parsed strings that looked like dates
                expando[propertyName] = TryParseDateTime(propertyValue.GetString());
                break;

            case JsonValueKind.Object:
                var nestedExpando = new ExpandoObject();
                var nestedDictionary = (IDictionary<string, object>)nestedExpando;
                foreach (var nestedProperty in propertyValue.EnumerateObject())
                {
                    AddPropertyToExpando(nestedDictionary, nestedProperty.Name, nestedProperty.Value);
                }
                expando[propertyName] = nestedExpando;
                break;

            case JsonValueKind.Array:
                var arrayValues = new List<object>();
                foreach (var arrayElement in propertyValue.EnumerateArray())
                {
                    switch (arrayElement.ValueKind)
                    {
                        case JsonValueKind.Undefined:
                        case JsonValueKind.Null:
                            arrayValues.Add(null);
                            break;

                        case JsonValueKind.False:
                            arrayValues.Add(false);
                            break;

                        case JsonValueKind.True:
                            arrayValues.Add(true);
                            break;

                        case JsonValueKind.Number:
                            if (arrayElement.TryGetInt64(out long arrLong))
                            {
                                arrayValues.Add(arrLong);
                            }
                            else if (arrayElement.TryGetDouble(out double arrDouble))
                            {
                                arrayValues.Add(arrDouble);
                            }
                            else if (arrayElement.TryGetDecimal(out decimal arrDecimal))
                            {
                                arrayValues.Add(arrDecimal);
                            }
                            else
                            {
                                throw new JsonException("Unsupported numeric type");
                            }
                            break;

                        case JsonValueKind.String:
                            // Try to parse as DateTime to maintain Newtonsoft.Json compatibility
                            arrayValues.Add(TryParseDateTime(arrayElement.GetString()));
                            break;

                        case JsonValueKind.Object:
                            var nestedExpandoInArray = new ExpandoObject();
                            var nestedDictionaryInArray = (IDictionary<string, object>)nestedExpandoInArray;
                            foreach (var nestedPropertyInArray in arrayElement.EnumerateObject())
                            {
                                AddPropertyToExpando(nestedDictionaryInArray, nestedPropertyInArray.Name, nestedPropertyInArray.Value);
                            }
                            arrayValues.Add(nestedExpandoInArray);
                            break;

                        case JsonValueKind.Array:
                            // Recursively handle nested arrays
                            var nestedArray = new List<object>();
                            foreach (var nestedArrayElement in arrayElement.EnumerateArray())
                            {
                                switch (nestedArrayElement.ValueKind)
                                {
                                    case JsonValueKind.Undefined:
                                    case JsonValueKind.Null:
                                        nestedArray.Add(null);
                                        break;

                                    case JsonValueKind.False:
                                        nestedArray.Add(false);
                                        break;

                                    case JsonValueKind.True:
                                        nestedArray.Add(true);
                                        break;

                                    case JsonValueKind.Number:
                                        if (nestedArrayElement.TryGetInt64(out long nestLong))
                                        {
                                            nestedArray.Add(nestLong);
                                        }
                                        else if (nestedArrayElement.TryGetDouble(out double nestDouble))
                                        {
                                            nestedArray.Add(nestDouble);
                                        }
                                        else if (nestedArrayElement.TryGetDecimal(out decimal nestDecimal))
                                        {
                                            nestedArray.Add(nestDecimal);
                                        }
                                        else
                                        {
                                            throw new JsonException("Unsupported numeric type");
                                        }
                                        break;

                                    case JsonValueKind.String:
                                        // Try to parse as DateTime to maintain Newtonsoft.Json compatibility
                                        nestedArray.Add(TryParseDateTime(nestedArrayElement.GetString()));
                                        break;

                                    case JsonValueKind.Object:

                                        var nestedExpandoInNestedArray = new ExpandoObject();
                                        var nestedDictionaryInNestedArray = (IDictionary<string, object>)nestedExpandoInNestedArray;
                                        foreach (var nestedPropertyInNestedArray in nestedArrayElement.EnumerateObject())
                                        {
                                            AddPropertyToExpando(nestedDictionaryInNestedArray,
                                                nestedPropertyInNestedArray.Name,
                                                nestedPropertyInNestedArray.Value);
                                        }
                                        nestedArray.Add(nestedExpandoInNestedArray);
                                        break;

                                    case JsonValueKind.Array:
                                        // Recursively handle deeper nested arrays
                                        nestedArray.Add(ProcessNestedArray(nestedArrayElement));
                                        break;

                                }
                            }
                            arrayValues.Add(nestedArray);
                            break;
                    }
                }
                expando[propertyName] = arrayValues;
                break;
        }
    }

    private static object ProcessNestedArray(JsonElement arrayElement)
    {
        var arrayValues = new List<object>();

        foreach (var element in arrayElement.EnumerateArray())
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var nestedExpando = new ExpandoObject();
                    var nestedDictionary = (IDictionary<string, object>)nestedExpando;
                    foreach (var nestedProperty in element.EnumerateObject())
                    {
                        AddPropertyToExpando(nestedDictionary, nestedProperty.Name, nestedProperty.Value);
                    }
                    arrayValues.Add(nestedExpando);
                    break;

                case JsonValueKind.Array:
                    arrayValues.Add(ProcessNestedArray(element));
                    break;

                case JsonValueKind.Undefined:
                case JsonValueKind.Null:
                    arrayValues.Add(null);
                    break;

                case JsonValueKind.False:
                    arrayValues.Add(false);
                    break;

                case JsonValueKind.True:
                    arrayValues.Add(true);
                    break;

                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long longValue))
                        arrayValues.Add(longValue);
                    else if (element.TryGetDouble(out double doubleValue))
                        arrayValues.Add(doubleValue);
                    else if (element.TryGetDecimal(out decimal decimalValue))
                        arrayValues.Add(decimalValue);
                    else
                        throw new JsonException("Unsupported numeric type");
                    break;

                case JsonValueKind.String:
                    arrayValues.Add(TryParseDateTime(element.GetString()));
                    break;
            }
        }

        return arrayValues;
    }

    /// <summary>
    /// Try to parse a string as DateTime to maintain backward compatibility with Newtonsoft.Json
    /// Newtonsoft.Json automatically parsed strings that looked like dates
    ///
    /// Performance Note: This method is called for every string value when deserializing
    /// dynamic/ExpandoObject data. DateTime.TryParse() has a performance cost, but it's
    /// necessary to maintain backward compatibility. For performance-critical scenarios,
    /// consider using strongly-typed collections (GetCollection&lt;T&gt;()) which don't
    /// require this parsing step.
    /// </summary>
    private static readonly string[] _dateTimeFormats = new[]
    {
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
        "yyyy-MM-ddTHH:mm:ssK",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd"
    };

    private static object TryParseDateTime(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Only parse strings that match an explicit ISO 8601 date(time) format.
        // DateTime.TryParse is too permissive — it would treat "01:30:00" (TimeSpan) or
        // "12.5" (a version-like string) as DateTime by attaching today's date.
        foreach (var format in _dateTimeFormats)
        {
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                return dt;
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt;
        }

        return value;
    }
}