using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JsonFlatFileDataStore
{
    public class DataStore : IDataStore
    {
        private const int CommitBatchMaxSize = 50;

        private readonly string _filePath;
        private readonly string _keyProperty;
        private readonly bool _reloadBeforeGetCollection;
        private readonly Func<JObject, string> _toJsonFunc;
        private readonly Func<string, string> _convertPathToCorrectCamelCase;
        private readonly BlockingCollection<CommitAction> _updates = new BlockingCollection<CommitAction>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ExpandoObjectConverter _converter = new ExpandoObjectConverter();
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        private JObject _jsonData;
        private bool _executingJsonUpdate;

        public DataStore(string path, bool useLowerCamelCase = true, string keyProperty = null, bool reloadBeforeGetCollection = false)
        {
            _filePath = path;

            _toJsonFunc = useLowerCamelCase
                        ? new Func<JObject, string>(data =>
                        {
                            // Serializing JObject ignores SerializerSettings, so we have to first deserialize to ExpandoObject and then serialize
                            // http://json.codeplex.com/workitem/23853
                            var jObject = JsonConvert.DeserializeObject<ExpandoObject>(data.ToString());
                            return JsonConvert.SerializeObject(jObject, Formatting.Indented, _serializerSettings);
                        })
                        : new Func<JObject, string>(s => s.ToString());

            _convertPathToCorrectCamelCase = useLowerCamelCase
                                ? new Func<string, string>(s => string.Concat(s.Select((x, i) => i == 0 ? char.ToLower(x).ToString() : x.ToString())))
                                : new Func<string, string>(s => s);

            _keyProperty = keyProperty ?? (useLowerCamelCase ? "id" : "Id");

            _reloadBeforeGetCollection = reloadBeforeGetCollection;

            _jsonData = JObject.Parse(ReadJsonFromFile(path));

            // Run updates on a background thread and use BlockingCollection to prevent multiple updates to run simultaneously
            Task.Run(() =>
            {
                var token = _cts.Token;

                var batch = new Queue<CommitAction>();
                var callbacks = new Queue<(CommitAction action, bool success)>();

                while (!token.IsCancellationRequested)
                {
                    batch.Clear();
                    callbacks.Clear();

                    var updateAction = _updates.Take(token);

                    _executingJsonUpdate = true;

                    batch.Enqueue(updateAction);

                    while (_updates.Count > 0 && batch.Count < CommitBatchMaxSize)
                    {
                        batch.Enqueue(_updates.Take(token));
                    }

                    var jsonText = ReadJsonFromFile(_filePath);

                    foreach (var action in batch)
                    {
                        var (actionSuccess, updatedJson) = action.HandleAction(JObject.Parse(jsonText));

                        callbacks.Enqueue((action, actionSuccess));

                        if (actionSuccess)
                            jsonText = updatedJson;
                    }

                    var writeSuccess = false;
                    Exception actionException = null;

                    try
                    {
                        writeSuccess = WriteJsonToFile(_filePath, jsonText);

                        lock (_jsonData)
                        {
                            _jsonData = JObject.Parse(jsonText);
                        }
                    }
                    catch (Exception e)
                    {
                        actionException = e;
                    }

                    foreach (var (cbAction, cbSuccess) in callbacks)
                    {
                        cbAction.Ready(writeSuccess ? cbSuccess : false, actionException);
                    }

                    _executingJsonUpdate = false;
                }
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
        }

        public bool IsUpdating => _updates.Count > 0 || _executingJsonUpdate;

        public void UpdateAll(string jsonData)
        {
            lock (_jsonData)
            {
                _jsonData = JObject.Parse(jsonData);
            }

            WriteJsonToFile(_filePath, jsonData);
        }

        public void Reload()
        {
            lock (_jsonData)
            {
                _jsonData = JObject.Parse(ReadJsonFromFile(_filePath));
            }
        }

        public T GetItem<T>(string key)
        {
            if (_reloadBeforeGetCollection)
            {
                // This might be a bad idea especially if the file is in use, as this can take a long time
                _jsonData = JObject.Parse(ReadJsonFromFile(_filePath));
            }

            var token = _jsonData[key];

            if (token == null)
            {
                if (Nullable.GetUnderlyingType(typeof(T)) != null)
                {
                    return default(T);
                }

                throw new KeyNotFoundException();
            }

            return token.ToObject<T>();
        }

        public dynamic GetItem(string key)
        {
            if (_reloadBeforeGetCollection)
            {
                // This might be a bad idea especially if the file is in use, as this can take a long time
                _jsonData = JObject.Parse(ReadJsonFromFile(_filePath));
            }

            var token = _jsonData[key];

            if (token == null)
                return null;

            return SingleDynamicItemReadConverter(token);
        }

        public bool InsertItem<T>(string key, T item) => Insert(key, item).Result;

        public async Task<bool> InsertItemAsync<T>(string key, T item) => await Insert(key, item, true).ConfigureAwait(false);

        private Task<bool> Insert<T>(string key, T item, bool isAsync = false)
        {
            (bool, JObject) UpdateAction()
            {
                if (_jsonData[key] != null)
                    return (false, _jsonData);

                _jsonData[key] = JToken.FromObject(item);
                return (true, _jsonData);
            }

            return CommitItem(UpdateAction, isAsync);
        }

        public bool ReplaceItem<T>(string key, T item, bool upsert = false) => Replace(key, item, upsert).Result;

        public async Task<bool> ReplaceItemAsync<T>(string key, T item, bool upsert = false) => await Replace(key, item, upsert, true).ConfigureAwait(false);

        private Task<bool> Replace<T>(string key, T item, bool upsert = false, bool isAsync = false)
        {
            (bool, JObject) UpdateAction()
            {
                if (_jsonData[key] == null && upsert == false)
                    return (false, _jsonData);

                _jsonData[key] = JToken.FromObject(item);
                return (true, _jsonData);
            }

            return CommitItem(UpdateAction, isAsync);
        }

        public bool UpdateItem(string key, dynamic item) => Update(key, item).Result;

        public async Task<bool> UpdateItemAsync(string key, dynamic item) => await Update(key, item, true).ConfigureAwait(false);

        private Task<bool> Update(string key, dynamic item, bool isAsync = false)
        {
            (bool, JObject) UpdateAction()
            {
                if (_jsonData[key] == null)
                    return (false, _jsonData);

                var toUpdate = SingleDynamicItemReadConverter(_jsonData[key]);

                if (ObjectExtensions.IsReferenceType(item) && ObjectExtensions.IsReferenceType(toUpdate))
                {
                    ObjectExtensions.CopyProperties(item, toUpdate);
                    _jsonData[key] = JToken.FromObject(toUpdate);
                }
                else
                {
                    _jsonData[key] = JToken.FromObject(item);
                }

                return (true, _jsonData);
            }

            return CommitItem(UpdateAction, isAsync);
        }

        public bool DeleteItem(string key) => Delete(key).Result;

        public async Task<bool> DeleteItemAsync(string key) => await Delete(key).ConfigureAwait(false);

        private Task<bool> Delete(string key, bool isAsync = false)
        {
            (bool, JObject) UpdateAction()
            {
                var result = _jsonData.Remove(key);
                return (result, _jsonData);
            }

            return CommitItem(UpdateAction, isAsync);
        }

        public IDocumentCollection<T> GetCollection<T>(string name = null) where T : class
        {
            // NOTE 27.6.2017: Should this be new Func<JToken, T>(e => e.ToObject<T>())?
            var readConvert = new Func<JToken, T>(e => JsonConvert.DeserializeObject<T>(e.ToString()));
            var insertConvert = new Func<T, T>(e => e);
            var createNewInstance = new Func<T>(() => Activator.CreateInstance<T>());

            return GetCollection(name ?? _convertPathToCorrectCamelCase(typeof(T).Name), readConvert, insertConvert, createNewInstance);
        }

        public IDocumentCollection<dynamic> GetCollection(string name)
        {
            // As we don't want to return JObject when using dynamic, JObject will be converted to ExpandoObject
            var readConvert = new Func<JToken, dynamic>(e => JsonConvert.DeserializeObject<ExpandoObject>(e.ToString(), _converter) as dynamic);
            var insertConvert = new Func<dynamic, dynamic>(e => JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(e), _converter));
            var createNewInstance = new Func<dynamic>(() => new ExpandoObject());

            return GetCollection(name, readConvert, insertConvert, createNewInstance);
        }

        public IDictionary<string, ValueType> GetKeys(ValueType? typeToGet = null)
        {
            bool IsCollection(JToken c) => c.Children().FirstOrDefault() is JArray && c.Children().FirstOrDefault().Any() == false
                                        || c.Children().FirstOrDefault()?.FirstOrDefault()?.Type == JTokenType.Object;

            bool IsItem(JToken c) => c.Children().FirstOrDefault().GetType() != typeof(JArray)
                                  || (c.Children().FirstOrDefault() is JArray
                                   && c.Children().FirstOrDefault().Any() // Empty array is considered as a collection
                                   && c.Children().FirstOrDefault()?.FirstOrDefault()?.Type != JTokenType.Object);

            lock (_jsonData)
            {
                switch (typeToGet)
                {
                    case null:
                        return _jsonData.Children()
                                        .ToDictionary(c => c.Path, c => IsCollection(c) ? ValueType.Collection : ValueType.Item);

                    case ValueType.Collection:
                        return _jsonData.Children()
                                        .Where(IsCollection)
                                        .ToDictionary(c => c.Path, c => ValueType.Collection);

                    case ValueType.Item:
                        return _jsonData.Children()
                                        .Where(IsItem)
                                        .ToDictionary(c => c.Path, c => ValueType.Item);

                    default:
                        throw new NotSupportedException();
                }
            }
        }

        private IDocumentCollection<T> GetCollection<T>(string path, Func<JToken, T> readConvert, Func<T, T> insertConvert, Func<T> createNewInstance)
        {
            var data = new Lazy<List<T>>(() =>
            {
                lock (_jsonData)
                {
                    if (_reloadBeforeGetCollection)
                    {
                        // This might be a bad idea especially if the file is in use, as this can take a long time
                        _jsonData = JObject.Parse(ReadJsonFromFile(_filePath));
                    }

                    return _jsonData[path]?
                                .Children()
                                .Select(e => readConvert(e))
                                .ToList()
                                ?? new List<T>();
                }
            });

            return new DocumentCollection<T>(
                (sender, dataToUpdate, isOperationAsync) => Commit(sender, dataToUpdate, isOperationAsync, readConvert),
                data,
                path,
                _keyProperty,
                insertConvert,
                createNewInstance);
        }

        private async Task<bool> CommitItem(Func<(bool, JObject)> commitOperation, bool isOperationAsync)
        {
            var commitAction = new CommitAction();

            commitAction.HandleAction = new Func<JObject, (bool success, string json)>(currentJson =>
            {
                var (success, newJson) = commitOperation();
                return success ? (true, _toJsonFunc(newJson)) : (false, string.Empty);
            });

            return await InnerCommit(isOperationAsync, commitAction);
        }

        private async Task<bool> Commit<T>(string dataPath, Func<List<T>, bool> commitOperation, bool isOperationAsync, Func<JToken, T> readConvert)
        {
            var commitAction = new CommitAction();

            commitAction.HandleAction = new Func<JObject, (bool success, string json)>(currentJson =>
            {
                var updatedJson = string.Empty;

                var selectedData = currentJson[dataPath]?
                                        .Children()
                                        .Select(e => readConvert(e))
                                        .ToList()
                                        ?? new List<T>();

                var success = commitOperation(selectedData);

                if (success)
                {
                    currentJson[dataPath] = JArray.FromObject(selectedData);
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

            commitAction.Ready = new Action<bool, Exception>((isSuccess, exception) =>
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

        private dynamic SingleDynamicItemReadConverter(JToken e)
        {
            switch (e)
            {
                case var objToken when e.Type == JTokenType.Object:
                    //As we don't want to return JObject when using dynamic, JObject will be converted to ExpandoObject
                    // JToken.ToString() is not culture invariant, so need to use string.Format
                    var content = string.Format(CultureInfo.InvariantCulture, "{0}", objToken);
                    return JsonConvert.DeserializeObject<ExpandoObject>(content, _converter) as dynamic;

                case var arrayToken when e.Type == JTokenType.Array:
                    return e.ToObject<List<object>>();

                case JValue jv when e is JValue:
                    return jv.Value;

                default:
                    return e.ToObject<object>();
            }
        }

        private string ReadJsonFromFile(string path)
        {
            Stopwatch sw = null;
            string json = "{}";

            while (true)
            {
                try
                {
                    json = File.ReadAllText(path);
                    break;
                }
                catch (FileNotFoundException)
                {
                    File.WriteAllText(path, json);
                    break;
                }
                catch (IOException e) when (e.Message.Contains("because it is being used by another process"))
                {
                    // If some other process is using this file, retry operation unless elapsed times is greater than 10sec
                    sw = sw ?? Stopwatch.StartNew();
                    if (sw.ElapsedMilliseconds > 10000)
                        throw;
                }
            }

            return json;
        }

        private bool WriteJsonToFile(string path, string content)
        {
            Stopwatch sw = null;

            while (true)
            {
                try
                {
                    File.WriteAllText(path, content);
                    return true;
                }
                catch (IOException e) when (e.Message.Contains("because it is being used by another process"))
                {
                    // If some other process is using this file, retry operation unless elapsed times is greater than 10sec
                    sw = sw ?? Stopwatch.StartNew();
                    if (sw.ElapsedMilliseconds > 10000)
                        return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private class CommitAction
        {
            public Action<bool, Exception> Ready { get; set; }

            public Func<JObject, (bool success, string json)> HandleAction { get; set; }
        }
    }
}