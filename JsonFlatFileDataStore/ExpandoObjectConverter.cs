using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonFlatFileDataStore
{
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
            JsonSerializer.Serialize(writer, (object)value, options);
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
                    if (propertyValue.TryGetInt32(out int intValue))
                    {
                        expando[propertyName] = intValue;
                    }
                    else if (propertyValue.TryGetInt64(out long longValue))
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
                        // TODO: Handle other numeric types as needed
                        throw new JsonException("Unsupported numeric type");
                    }
                    break;

                case JsonValueKind.String:
                    expando[propertyName] = propertyValue.GetString();
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
                                if (arrayElement.TryGetInt32(out int arrayElementIntValue))
                                {
                                    arrayValues.Add(arrayElementIntValue);
                                }
                                else if (arrayElement.TryGetInt64(out long longValue))
                                {
                                    arrayValues.Add(longValue);
                                }
                                else if (arrayElement.TryGetDouble(out double doubleValue))
                                {
                                    arrayValues.Add(doubleValue);
                                }
                                else if (arrayElement.TryGetDecimal(out decimal decimalValue))
                                {
                                    arrayValues.Add(decimalValue);
                                }
                                else
                                {
                                    throw new JsonException("Unsupported numeric type");
                                }
                                break;

                            case JsonValueKind.String:
                                arrayValues.Add(arrayElement.GetString());
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
                                            if (nestedArrayElement.TryGetInt32(out int nestedArrayElementIntValue))
                                            {
                                                nestedArray.Add(nestedArrayElementIntValue);
                                            }
                                            else if (nestedArrayElement.TryGetInt64(out long longValue))
                                            {
                                                nestedArray.Add(longValue);
                                            }
                                            else if (nestedArrayElement.TryGetDouble(out double doubleValue))
                                            {
                                                nestedArray.Add(doubleValue);
                                            }
                                            else if (nestedArrayElement.TryGetDecimal(out decimal decimalValue))
                                            {
                                                nestedArray.Add(decimalValue);
                                            }
                                            else
                                            {
                                                throw new JsonException("Unsupported numeric type");
                                            }
                                            break;

                                        case JsonValueKind.String:
                                            nestedArray.Add(nestedArrayElement.GetString());
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
                if (element.ValueKind == JsonValueKind.Object)
                {
                    var nestedExpando = new ExpandoObject();
                    var nestedDictionary = (IDictionary<string, object>)nestedExpando;
                    foreach (var nestedProperty in element.EnumerateObject())
                    {
                        AddPropertyToExpando(nestedDictionary, nestedProperty.Name, nestedProperty.Value);
                    }
                    arrayValues.Add(nestedExpando);
                }
                else if (element.ValueKind == JsonValueKind.Array)
                {
                    // Recursively handle further nested arrays
                    arrayValues.Add(ProcessNestedArray(element));
                }
                else
                {
                    arrayValues.Add(element.ToString());
                }
            }

            return arrayValues;
        }
    }
}