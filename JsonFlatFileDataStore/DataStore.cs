﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace JsonFlatFileDataStore
{
    public class DataStore : IDataStore
    {
        private readonly string _filePath;
        private readonly string _keyProperty;
        private readonly bool _reloadBeforeGetCollection;
        private readonly Func<JObject, string> _toJsonFunc;
        private readonly Func<string, string> _convertPathToCorrectCamelCase;
        private readonly BlockingCollection<CommitAction> _updates = new BlockingCollection<CommitAction>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ExpandoObjectConverter _converter = new ExpandoObjectConverter();

        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings()
            { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        private readonly Func<string, string> _encryptJson;
        private readonly Func<string, string> _decryptJson;

        private JObject _jsonData;
        private bool _executingJsonUpdate;

        public DataStore(string path, bool useLowerCamelCase = true, string keyProperty = null, bool reloadBeforeGetCollection = false,
            string encryptionKey = null, bool minifyJson = false)
        {
            _filePath = path;

            var useEncryption = !string.IsNullOrWhiteSpace(encryptionKey);
            var usedFormatting = minifyJson || useEncryption ? Formatting.None : Formatting.Indented;

            _toJsonFunc = useLowerCamelCase
                ? new Func<JObject, string>(data =>
                {
                    // Serializing JObject ignores SerializerSettings, so we have to first deserialize to ExpandoObject and then serialize
                    // http://json.codeplex.com/workitem/23853
                    var jObject = JsonConvert.DeserializeObject<ExpandoObject>(data.ToString());
                    return JsonConvert.SerializeObject(jObject, usedFormatting, _serializerSettings);
                })
                : (s => s.ToString(usedFormatting));

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

            _jsonData = GetJsonObjectFromFile();

            // Run updates on a background thread and use BlockingCollection to prevent multiple updates to run simultaneously
            Task.Run(() =>
            {
                CommitActionHandler.HandleStoreCommitActions(_cts.Token,
                    _updates,
                    executionState => _executingJsonUpdate = executionState,
                    jsonText =>
                    {
                        lock (_jsonData)
                        {
                            _jsonData = JObject.Parse(jsonText);
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
        }

        public bool IsUpdating => _updates.Count > 0 || _executingJsonUpdate;

        public void UpdateAll(string jsonData)
        {
            lock (_jsonData)
            {
                _jsonData = JObject.Parse(jsonData);
            }

            FileAccess.WriteJsonToFile(_filePath, _encryptJson, jsonData);
        }

        public void Reload()
        {
            lock (_jsonData)
            {
                _jsonData = GetJsonObjectFromFile();
            }
        }

        public T GetItem<T>(string key)
        {
            if (_reloadBeforeGetCollection)
            {
                // This might be a bad idea especially if the file is in use, as this can take a long time
                _jsonData = GetJsonObjectFromFile();
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
                _jsonData = GetJsonObjectFromFile();
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
            var pathInCorrectCase = _convertPathToCorrectCamelCase(path);
            
            var data = new Lazy<List<T>>(() =>
            {
                lock (_jsonData)
                {
                    if (_reloadBeforeGetCollection)
                    {
                        // This might be a bad idea especially if the file is in use, as this can take a long time
                        _jsonData = GetJsonObjectFromFile();
                    }

                    return _jsonData[pathInCorrectCase]?
                           .Children()
                           .Select(e => readConvert(e))
                           .ToList()
                        ?? new List<T>();
                }
            });

            return new DocumentCollection<T>(
                (sender, dataToUpdate, isOperationAsync) => Commit(sender, dataToUpdate, isOperationAsync, readConvert),
                data,
                pathInCorrectCase,
                _keyProperty,
                insertConvert,
                createNewInstance);
        }

        private async Task<bool> CommitItem(Func<(bool, JObject)> commitOperation, bool isOperationAsync)
        {
            var commitAction = new CommitAction();

            commitAction.HandleAction = (currentJson =>
            {
                var (success, newJson) = commitOperation();
                return success ? (true, _toJsonFunc(newJson)) : (false, string.Empty);
            });

            return await InnerCommit(isOperationAsync, commitAction);
        }

        private async Task<bool> Commit<T>(string dataPath, Func<List<T>, bool> commitOperation, bool isOperationAsync, Func<JToken, T> readConvert)
        {
            var commitAction = new CommitAction();

            commitAction.HandleAction = (currentJson =>
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

        private string GetJsonTextFromFile() => FileAccess.ReadJsonFromFile(_filePath, _encryptJson, _decryptJson);

        private JObject GetJsonObjectFromFile() => JObject.Parse(GetJsonTextFromFile());


        internal class CommitAction
        {
            public Action<bool, Exception> Ready { get; set; }

            public Func<JObject, (bool success, string json)> HandleAction { get; set; }
        }
    }
}