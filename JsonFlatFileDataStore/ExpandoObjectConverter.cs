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
                        // Handle other numeric types as needed
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
                        if (arrayElement.ValueKind == JsonValueKind.Object)
                        {
                            var nestedExpandoInArray = new ExpandoObject();
                            var nestedDictionaryInArray = (IDictionary<string, object>)nestedExpandoInArray;
                            foreach (var nestedPropertyInArray in arrayElement.EnumerateObject())
                            {
                                AddPropertyToExpando(nestedDictionaryInArray, nestedPropertyInArray.Name, nestedPropertyInArray.Value);
                            }
                            arrayValues.Add(nestedExpandoInArray);
                        }
                        else
                        {
                            arrayValues.Add(arrayElement.GetString()); // Adjust this for other value types if needed
                        }
                    }
                    expando[propertyName] = arrayValues;
                    break;
            }
        }
    }
}