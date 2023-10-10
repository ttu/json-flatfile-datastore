using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace JsonFlatFileDataStore
{
    public class DataStore : IDataStore
    {
        private readonly string _filePath;
        private readonly string _keyProperty;
        private readonly bool _reloadBeforeGetCollection;
        private readonly Func<JsonElement, string> _toJsonFunc;
        private readonly Func<string, string> _convertPathToCorrectCamelCase;
        private readonly BlockingCollection<CommitAction> _updates = new BlockingCollection<CommitAction>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            Converters = { new NewtonsoftDateTimeConverter(), new SystemExpandoObjectConverter() },
            PropertyNameCaseInsensitive = true
        };

        private readonly JsonSerializerOptions _serializerOptions;

        private readonly Func<string, string> _encryptJson;
        private readonly Func<string, string> _decryptJson;

        private JsonDocument _jsonData;
        private readonly object _jsonDataLock = new object();
        private bool _executingJsonUpdate;

        public DataStore(string path, bool useLowerCamelCase = true, string keyProperty = null, bool reloadBeforeGetCollection = false,
            string encryptionKey = null, bool minifyJson = false)
        {
            _filePath = path;

            var useEncryption = !string.IsNullOrWhiteSpace(encryptionKey);
            var writeIntended = !minifyJson && !useEncryption;  // Set to `true` if not minifying or encrypting

            _serializerOptions = useLowerCamelCase ? new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = writeIntended
            } : new JsonSerializerOptions
            {
                WriteIndented = writeIntended
            };

            _toJsonFunc = useLowerCamelCase
                ? new Func<JsonElement, string>(data =>
                {
                    // Deserialize to ExpandoObject to allow flexible serialization settings
                    var expandoObject = JsonSerializer.Deserialize<ExpandoObject>(data.GetRawText(), _options);

                    // Serialize back to JSON with camel casing and indentation options applied
                    // Case-insensitive property matching is handled in ObjectExtensions.CopyProperties
                    return JsonSerializer.Serialize(expandoObject, _serializerOptions);
                })
                : (s => JsonSerializer.Serialize(s, _serializerOptions));

            _convertPathToCorrectCamelCase = useLowerCamelCase
                ? new Func<string, string>(s => string.Concat(s.Select((x, i) => i == 0 ? char.ToLower(x).ToString() : x.ToString())))
                : s => s;

            _keyProperty = keyProperty ?? (useLowerCamelCase ? "id" : "Id");

            _reloadBeforeGetCollection = reloadBeforeGetCollection;

            if (useEncryption)
            {
                var aes256 = new Aes256();
                _encryptJson = (json => aes256.Encrypt(json, encryptionKey));
                _decryptJson = (json => aes256.Decrypt(json, encryptionKey));
            }
            else
            {
                _encryptJson = (json => json);
                _decryptJson = (json => json);
            }

            SetJsonData(GetJsonObjectFromFile());

            // Run updates on a background thread and use BlockingCollection to prevent multiple updates to run simultaneously
            Task.Run(() =>
            {
                CommitActionHandler.HandleStoreCommitActions(_cts.Token,
                    _updates,
                    executionState => _executingJsonUpdate = executionState,
                    jsonText =>
                    {
                        lock (_jsonDataLock)
                        {
                            SetJsonData(Parse(jsonText));
                        }

                        return FileAccess.WriteJsonToFile(_filePath, _encryptJson, jsonText);
                    },
                    GetJsonTextFromFile);
            });
        }

        public void Dispose()
        {
            while (IsUpdating)
            {
                Task.Run(async () => await Task.Delay(100)).GetAwaiter().GetResult();
            }

            if (_cts.IsCancellationRequested == false)
            {
                _cts.Cancel();
            }

            // Dispose the JsonDocument to free unmanaged resources
            _jsonData?.Dispose();
        }



        public bool IsUpdating => _updates.Count > 0 || _executingJsonUpdate;

        public void UpdateAll(string jsonData)
        {
            lock (_jsonDataLock)
            {
                SetJsonData(Parse(jsonData));
            }

            FileAccess.WriteJsonToFile(_filePath, _encryptJson, jsonData);
        }

        public void Reload()
        {
            lock (_jsonDataLock)
            {
                SetJsonData(GetJsonObjectFromFile());
            }
        }

        public T GetItem<T>(string key)
        {
            if (_reloadBeforeGetCollection)
            {
                // This might be a bad idea especially if the file is in use, as this can take a long time
                SetJsonData(GetJsonObjectFromFile());
            }

            var convertedKey = _convertPathToCorrectCamelCase(key);

            var token = TryGetElement(_jsonData.RootElement, convertedKey);

            if (token == null)
            {
                if (Nullable.GetUnderlyingType(typeof(T)) != null)
                {
                    return default(T);
                }

                throw new KeyNotFoundException();
            }

            return ConvertJsonElementToObject<T>(token.Value);
        }

        public dynamic GetItem(string key)
        {
            if (_reloadBeforeGetCollection)
            {
                // This might be a bad idea especially if the file is in use, as this can take a long time
                SetJsonData(GetJsonObjectFromFile());
            }

            var convertedKey = _convertPathToCorrectCamelCase(key);

            var token = TryGetElement(_jsonData.RootElement, convertedKey);

            if (token == null)
                return null;

            return SingleDynamicItemReadConverter(token.Value);
        }

        public bool InsertItem<T>(string key, T item) => Insert(key, item).Result;

        public async Task<bool> InsertItemAsync<T>(string key, T item) => await Insert(key, item, true).ConfigureAwait(false);

        private Task<bool> Insert<T>(string key, T item, bool isAsync = false)
        {
            var convertedKey = _convertPathToCorrectCamelCase(key);

            (bool, JsonElement) UpdateAction()
            {
                var data = TryGetElement(_jsonData.RootElement, convertedKey);
                if (data.HasValue)
                    return (false, data.Value);

                var newElement = ConvertToJsonElement(item);
                SetJsonData(SetJsonDataElement(_jsonData.RootElement, convertedKey, newElement));
                return (true, _jsonData.RootElement);
            }

            return CommitItem(UpdateAction, isAsync);
        }

        public bool ReplaceItem<T>(string key, T item, bool upsert = false) => Replace(key, item, upsert).Result;

        public async Task<bool> ReplaceItemAsync<T>(string key, T item, bool upsert = false) => await Replace(key, item, upsert, true).ConfigureAwait(false);

        private Task<bool> Replace<T>(string key, T item, bool upsert = false, bool isAsync = false)
        {
            var convertedKey = _convertPathToCorrectCamelCase(key);

            (bool, JsonElement) UpdateAction()
            {
                var data = TryGetElement(_jsonData.RootElement, convertedKey);
                if (data == null && upsert == false)
                    return (false, _jsonData.RootElement);

                var newElement = ConvertToJsonElement(item);
                SetJsonData(SetJsonDataElement(_jsonData.RootElement, convertedKey, newElement));
                return (true, _jsonData.RootElement);
            }

            return CommitItem(UpdateAction, isAsync);
        }

        public bool UpdateItem(string key, dynamic item) => Update(key, item).Result;

        public async Task<bool> UpdateItemAsync(string key, dynamic item) => await Update(key, item, true).ConfigureAwait(false);

        private Task<bool> Update(string key, dynamic item, bool isAsync = false)
        {
            var convertedKey = _convertPathToCorrectCamelCase(key);

            (bool, JsonElement) UpdateAction()
            {
                var data = TryGetElement(_jsonData.RootElement, convertedKey);
                if (data == null)
                    return (false, _jsonData.RootElement);

                var toUpdate = SingleDynamicItemReadConverter(data.Value);

                if (ObjectExtensions.IsReferenceType(item) && ObjectExtensions.IsReferenceType(toUpdate))
                {
                    ObjectExtensions.CopyProperties(item, toUpdate);
                    var newElement = ConvertToJsonElement(toUpdate);
                    _jsonData = SetJsonDataElement(_jsonData.RootElement, convertedKey, newElement);
                }
                else
                {
                    var newElement = ConvertToJsonElement(item);
                    _jsonData = SetJsonDataElement(_jsonData.RootElement, convertedKey, newElement);
                }

                return (true, _jsonData.RootElement);
            }

            return CommitItem(UpdateAction, isAsync);
        }

        public bool DeleteItem(string key) => Delete(key).Result;

        public async Task<bool> DeleteItemAsync(string key) => await Delete(key).ConfigureAwait(false);

        private Task<bool> Delete(string key, bool isAsync = false)
        {
            var convertedKey = _convertPathToCorrectCamelCase(key);

            (bool, JsonElement) UpdateAction()
            {
                return RemoveJsonDataElement(_jsonData.RootElement, convertedKey);
            }

            return CommitItem(UpdateAction, isAsync);
        }

        public IDocumentCollection<T> GetCollection<T>(string name = null) where T : class
        {
            // Deserialize JsonElement to T
            // Uses NewtonsoftDateTimeConverter for backward compatibility with Newtonsoft.Json DateTime formats
            var readConvert = new Func<JsonDocument, T>(e => e.RootElement.Deserialize<T>(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, true), new NewtonsoftDateTimeConverter() }
                }));
            var insertConvert = new Func<T, T>(e => e);
            var createNewInstance = new Func<T>(() => Activator.CreateInstance<T>());

            return GetCollection(name ?? typeof(T).Name, readConvert, insertConvert, createNewInstance);
        }

        public IDocumentCollection<dynamic> GetCollection(string name)
        {
            // Deserialize JsonElement to ExpandoObject for dynamic handling
            var readConvert = new Func<JsonDocument, dynamic>(e => e.RootElement.Deserialize<ExpandoObject>(_options));
            var insertConvert = new Func<dynamic, dynamic>(e =>
            {
                var json = JsonSerializer.Serialize(e, _serializerOptions);
                return JsonSerializer.Deserialize<ExpandoObject>(json, _options);
            });
            var createNewInstance = new Func<dynamic>(() => new ExpandoObject());

            return GetCollection(name, readConvert, insertConvert, createNewInstance);
        }

        public IDictionary<string, ValueType> GetKeys(ValueType? typeToGet = null)
        {
            bool IsCollection(JsonElement property) =>
   property.ValueKind == JsonValueKind.Array &&
   (!property.EnumerateArray().Any() || property.EnumerateArray().First().ValueKind == JsonValueKind.Object);

            bool IsItem(JsonElement property) =>
               property.ValueKind != JsonValueKind.Array ||
               (property.EnumerateArray().Any() && property.EnumerateArray().First().ValueKind != JsonValueKind.Object);

            lock (_jsonDataLock)
            {
                var result = new Dictionary<string, ValueType>();

                if (_jsonData.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in _jsonData.RootElement.EnumerateObject())
                    {
                        bool isCollection = IsCollection(property.Value);
                        bool isItem = IsItem(property.Value);

                        switch (typeToGet)
                        {
                            case null:
                                result[property.Name] = isCollection ? ValueType.Collection : ValueType.Item;
                                break;
                            case ValueType.Collection when isCollection:
                                result[property.Name] = ValueType.Collection;
                                break;
                            case ValueType.Item when isItem:
                                result[property.Name] = ValueType.Item;
                                break;
                        }
                    }
                }

                return result;
            }
        }

        private IDocumentCollection<T> GetCollection<T>(string path, Func<JsonDocument, T> readConvert, Func<T, T> insertConvert, Func<T> createNewInstance)
        {
            var pathWithConfiguredCase = _convertPathToCorrectCamelCase(path);

            var data = new Lazy<List<T>>(() =>
            {
                lock (_jsonDataLock)
                {
                    if (_reloadBeforeGetCollection)
                    {
                        // This might be a bad idea especially if the file is in use, as this can take a long time
                        SetJsonData(GetJsonObjectFromFile());
                    }

                    var data = TryGetElement(_jsonData.RootElement, pathWithConfiguredCase);

                    if (data.HasValue == false)
                        return new List<T>();

                    return GetChildren(data.Value)
                           .Select(e => readConvert(e))
                           .ToList();
                }
            });

            return new DocumentCollection<T>(
                (sender, dataToUpdate, isOperationAsync) => Commit(sender, dataToUpdate, isOperationAsync, readConvert),
                data,
                pathWithConfiguredCase,
                _keyProperty,
                insertConvert,
                createNewInstance);
        }

        private async Task<bool> CommitItem(Func<(bool, JsonElement)> commitOperation, bool isOperationAsync)
        {
            var commitAction = new CommitAction();

            commitAction.HandleAction = (currentJson =>
            {
                var (success, newJson) = commitOperation();
                return success ? (true, _toJsonFunc(newJson)) : (false, string.Empty);
            });

            return await InnerCommit(isOperationAsync, commitAction);
        }

        private async Task<bool> Commit<T>(string dataPath, Func<List<T>, bool> commitOperation, bool isOperationAsync, Func<JsonDocument, T> readConvert)
        {
            var commitAction = new CommitAction();

            commitAction.HandleAction = (currentJson =>
            {
                var updatedJson = string.Empty;

                var data = TryGetElement(currentJson, dataPath);

                var selectedData = (data.HasValue) ? GetChildren(data.Value)
                                   .Select(e => readConvert(e))
                                   .ToList()
                                : new List<T>();

                var success = commitOperation(selectedData);

                if (success)
                {
                    var newElement = ConvertToJsonElement(selectedData);
                    currentJson = SetJsonDataElement(currentJson, dataPath, newElement).RootElement;
                    updatedJson = _toJsonFunc(currentJson);
                }

                return (success, updatedJson);
            });

            return await InnerCommit(isOperationAsync, commitAction);
        }

        private async Task<bool> InnerCommit(bool isOperationAsync, CommitAction commitAction)
        {
            bool waitFlag = true;
            bool actionSuccess = false;
            Exception actionException = null;

            commitAction.Ready = ((isSuccess, exception) =>
            {
                actionSuccess = isSuccess;
                actionException = exception;
                waitFlag = false;
            });

            _updates.Add(commitAction);

            while (waitFlag)
            {
                if (isOperationAsync)
                    await Task.Delay(5).ConfigureAwait(false);
                else
                    Task.Delay(5).Wait();
            }

            if (actionException != null)
                throw actionException;

            return actionSuccess;
        }

        private dynamic SingleDynamicItemReadConverter(JsonElement e)
        {
            switch (e.ValueKind)
            {
                case JsonValueKind.Object:
                    // Convert JsonElement to ExpandoObject for a dynamic structure
                    var content = e.GetRawText(); // Get the JSON as a raw string
                    return JsonSerializer.Deserialize<ExpandoObject>(content, _options) as dynamic;

                case JsonValueKind.Array:
                    // Convert JsonElement array to a List<object>
                    var list = new List<object>();
                    foreach (var item in e.EnumerateArray())
                    {
                        list.Add(SingleDynamicItemReadConverter(item)); // Recursively handle each item
                    }
                    return list;

                case JsonValueKind.String:
                    // Try to parse as DateTime to maintain Newtonsoft.Json compatibility
                    // Performance Note: DateTime.TryParse() is expensive, so we use a heuristic
                    // to quickly filter out obvious non-date strings before attempting to parse.
                    var strValue = e.GetString();
                    if (!string.IsNullOrEmpty(strValue))
                    {
                        // Quick heuristic: likely a date if it starts with a digit and contains date separators
                        // This filters out most non-date strings very quickly
                        // Common date formats: "2015-11-23T00:00:00", "6/15/2009", "2023-01-15"
                        if (strValue.Length >= 8 && // Minimum reasonable date length (e.g., "1/1/2023")
                            char.IsDigit(strValue[0]) &&
                            (strValue.IndexOf('-') >= 0 || strValue.IndexOf('/') >= 0 || strValue.IndexOf('T') >= 0))
                        {
                            // Try InvariantCulture first (for ISO formats like "2015-11-23T00:00:00")
                            if (DateTime.TryParse(strValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                            {
                                return dateTime;
                            }
                            // Fallback to current culture (for locale-specific formats like "6/15/2009")
                            if (DateTime.TryParse(strValue, CultureInfo.CurrentCulture, DateTimeStyles.None, out dateTime))
                            {
                                return dateTime;
                            }
                        }
                    }
                    return strValue;

                case JsonValueKind.Number:
                    return e.TryGetInt64(out long l) ? l : e.GetDouble();

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return e.GetBoolean();

                case JsonValueKind.Null:
                    return null;

                default:
                    return e.GetRawText(); // Return as string for unknown types
            }
        }


        private void SetJsonData(JsonDocument newData)
        {
            // Safely replaces _jsonData by disposing the old JsonDocument before assigning the new one.
            // This prevents memory leaks by ensuring JsonDocument resources are properly released.
            var oldData = _jsonData;
            _jsonData = newData;
            oldData?.Dispose();
        }
        private string GetJsonTextFromFile() => FileAccess.ReadJsonFromFile(_filePath, _encryptJson, _decryptJson);

        private JsonDocument GetJsonObjectFromFile()
        {
            var jsonText = GetJsonTextFromFile();
            return JsonDocument.Parse(jsonText);
        }

        private JsonElement? TryGetElement(JsonElement element, string key)
        {
            if (element.TryGetProperty(key, out JsonElement childElement))
            {
                return childElement;
            }
            else
            {
                return null;
            }
        }

        private JsonDocument Parse(string json)
        {
            return JsonDocument.Parse(json);
        }

        private T ConvertJsonElementToObject<T>(JsonElement token)
        {
            // Special handling for DateTime to support Newtonsoft.Json format
            if (typeof(T) == typeof(DateTime) && token.ValueKind == JsonValueKind.String)
            {
                var dateString = token.GetString();
                var converter = new NewtonsoftDateTimeConverter();
                var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes($"\"{dateString}\""));
                reader.Read(); // Advance to the string token
                return (T)(object)converter.Read(ref reader, typeof(DateTime), _options);
            }

            return JsonSerializer.Deserialize<T>(token.GetRawText(), _options);
        }

        private JsonElement ConvertToJsonElement(object item)
        {
            // Serialize the object to JSON with proper naming policy and parse it as a JsonDocument
            var json = JsonSerializer.Serialize(item, _serializerOptions);
            using var jsonDocument = JsonDocument.Parse(json);

            // Clone the root element so it doesn't reference the disposed JsonDocument
            // JsonElement is a struct that holds a reference to its parent document's buffer,
            // so we must clone it to create a copy that's independent of the document's lifetime
            return jsonDocument.RootElement.Clone();
        }

        private JsonDocument SetJsonDataElement(JsonElement original, string key, object item)
        {
            // Convert _jsonData to a Dictionary to make modifications
            var jsonDataDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(original.GetRawText());

            // Convert the item to JsonElement
            var newElement = ConvertToJsonElement(item);

            // Set or update the element in the dictionary
            jsonDataDict[key] = newElement;

            // Serialize back to JsonElement
            var modifiedJson = JsonSerializer.Serialize(jsonDataDict);
            return JsonDocument.Parse(modifiedJson);
        }

        public (bool, JsonElement) RemoveJsonDataElement(JsonElement original, string key)
        {
            // Deserialize _jsonData to a dictionary for modification
            var jsonDataDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(original.GetRawText());

            // Remove the specified key
            var removed = jsonDataDict.Remove(key);

            // Serialize the updated dictionary back to a JsonElement
            var modifiedJson = JsonSerializer.Serialize(jsonDataDict);
            using var jsonDocument = JsonDocument.Parse(modifiedJson);

            // Clone the root element so it doesn't reference the disposed JsonDocument
            return (removed, jsonDocument.RootElement.Clone());
        }

        private IEnumerable<JsonDocument> GetChildren(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                // If element is an object, return its properties directly
                // For each property in the JSON object, create a new JsonDocument
                var documents = new List<JsonDocument>();
                foreach (var property in element.EnumerateObject())
                {
                    // Create a JsonDocument from the property's value
                    var jsonDoc = JsonDocument.Parse(property.Value.GetRawText());
                    documents.Add(jsonDoc);
                }
                return documents;
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                // If element is an array, create synthetic JsonProperties using index as the key
                // For each item in the JSON array, create a new JsonDocument
                var documents = new List<JsonDocument>();
                foreach (var arrayItem in element.EnumerateArray())
                {
                    // Create a JsonDocument from the array item
                    var jsonDoc = JsonDocument.Parse(arrayItem.GetRawText());
                    documents.Add(jsonDoc);
                }
                return documents;
            }

            // If it's neither an object nor an array, return an empty sequence
            return Enumerable.Empty<JsonDocument>();

            // IEnumerable<System.Text.Json.JsonProperty> children = element.ValueKind == JsonValueKind.Object
            //    ? _jsonData.EnumerateObject()
            //    : Enumerable.Empty<System.Text.Json.JsonProperty>();  // Assuming _jsonData is an object; adjust for arrays as needed.
            // return children;
        }

        public static string GetJsonPath(JsonElement root, JsonElement target)
        {
            return FindPath(root, target);
        }

        private static string FindPath(JsonElement element, JsonElement target, string currentPath = "")
        {
            if (element.Equals(target))
            {
                return currentPath;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var propertyPath = string.IsNullOrEmpty(currentPath)
                            ? property.Name
                            : $"{currentPath}.{property.Name}";

                        var result = FindPath(property.Value, target, propertyPath);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                    break;

                case JsonValueKind.Array:
                    int index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        var arrayPath = $"{currentPath}[{index}]";

                        var result = FindPath(item, target, arrayPath);
                        if (result != null)
                        {
                            return result;
                        }
                        index++;
                    }
                    break;
            }

            return null; // Target not found in this branch
        }

        internal class CommitAction
        {
            public Action<bool, Exception> Ready { get; set; }

            public Func<JsonElement, (bool success, string json)> HandleAction { get; set; }
        }
    }
}