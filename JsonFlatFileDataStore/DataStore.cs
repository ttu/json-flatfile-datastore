using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JsonFlatFileDataStore
{
    public class DataStore : IDataStore
    {
        private readonly JObject _jsonData;
        private readonly string _filePath;
        private readonly string _keyProperty;

        private readonly Func<JObject, string> _toJsonFunc;
        private readonly Func<string, string> _pathToCamelCase;

        private readonly BlockingCollection<Action> _updates = new BlockingCollection<Action>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly ExpandoObjectConverter _converter = new ExpandoObjectConverter();

        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public DataStore(string path, bool useLowerCamelCase = true, string keyProperty = null)
        {
            _filePath = path;

            _toJsonFunc = useLowerCamelCase
                        ? new Func<JObject, string>(s =>
                        {
                            // Serializing JObject ignores SerializerSettings, so we have to first deserialize to ExpandoObject and then serialize
                            // http://json.codeplex.com/workitem/23853
                            var jObject = JsonConvert.DeserializeObject<ExpandoObject>(_jsonData.ToString());
                            return JsonConvert.SerializeObject(jObject, Formatting.Indented, _serializerSettings);
                        })
                        : new Func<JObject, string>(s => s.ToString());

            _pathToCamelCase = useLowerCamelCase
                                ? new Func<string, string>(s => string.Concat(s.Select((x, i) => i == 0 ? char.ToLower(x).ToString() : x.ToString())))
                                : new Func<string, string>(s => s);

            // Default key property is id or Id
            _keyProperty = keyProperty ?? (useLowerCamelCase ? "id" : "Id");

            string json = ReadJsonFromFile(path);

            _jsonData = JObject.Parse(json);

            // Run updates on background thread and use BlockingCollection to prevent multiple updates running at the same time
            Task.Run(() =>
            {
                var token = _cts.Token;

                while (token != null && !token.IsCancellationRequested)
                {
                    var updateAction = _updates.Take(token);
                    updateAction();
                }
            });
        }

        public bool IsUpdating => _updates.Count > 0;

        public void UpdateAll(string jsonData)
        {
            _updates.Add(new Action(() =>
            {
                _jsonData.ReplaceAll(JObject.Parse(jsonData));
                WriteJsonToFile(_filePath, _jsonData.ToString());
            }));
        }

        public IDocumentCollection<T> GetCollection<T>(string name = null) where T : class
        {
            var readConvert = new Func<JToken, T>(e => JsonConvert.DeserializeObject<T>(e.ToString()));
            var insertConvert = new Func<T, T>(e => e);
            return GetCollection<T>(name ?? _pathToCamelCase(typeof(T).Name), readConvert, insertConvert);
        }

        public IDocumentCollection<dynamic> GetCollection(string name)
        {
            // As we don't want to return JObjects when using dynamic, JObjects will be converted to ExpandoObjects
            var readConvert = new Func<JToken, dynamic>(e => JsonConvert.DeserializeObject<ExpandoObject>(e.ToString(), _converter) as dynamic);
            var insertConvert = new Func<dynamic, dynamic>(e => JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(e), _converter));

            return GetCollection<dynamic>(name, readConvert, insertConvert);
        }

        /// <summary>
        /// Get all collections
        /// </summary>
        /// <returns>List of collection names</returns>
        public IEnumerable<string> ListCollections()
        {
            return _jsonData.Children().Select(c => c.Path);
        }

        private IDocumentCollection<T> GetCollection<T>(string path, Func<JToken, T> readConvert, Func<T, T> insertConvert)
        {
            var data = new Lazy<List<T>>(() =>
                               _jsonData[path]?
                                   .Children()
                                   .Select(e => readConvert(e))
                                   .ToList()
                               ?? new List<T>());

            return new DocumentCollection<T>(
                (sender, dataToUpdate, isOperationAsync) => Commit(sender, dataToUpdate, isOperationAsync, readConvert), 
                data, 
                path, 
                _keyProperty, 
                insertConvert);
        }

        private async Task<bool> Commit<T>(string dataPath, Func<List<T>, bool> commitOperation, bool isOperationAsync, Func<JToken, T> readConvert)
        {
            bool waitFlag = true;
            bool result = false;
            Exception actionException = null;

            _updates.Add(new Action(() =>
            {
                var selectedData = _jsonData[dataPath]?
                                   .Children()
                                   .Select(e => readConvert(e))
                                   .ToList()
                                   ?? new List<T>();

                if (commitOperation(selectedData))
                {
                    _jsonData[dataPath] = JArray.FromObject(selectedData);
                    string json = _toJsonFunc(_jsonData);

                    try
                    {
                        result = WriteJsonToFile(_filePath, json);
                    }
                    catch (Exception e)
                    {
                        actionException = e;
                    }
                }

                waitFlag = false;
            }));

            while (waitFlag)
            {
                if (isOperationAsync)
                    await Task.Delay(1);
                else
                    Task.Delay(1).Wait();
            }

            if (actionException != null)
                throw actionException;

            return result;
        }

        private string ReadJsonFromFile(string path)
        {
            Stopwatch sw = null;

            while (true)
            {
                try
                {
                    return File.ReadAllText(path);
                }
                catch (FileNotFoundException)
                {
                    File.WriteAllText(path, "{}");
                    return "{}";
                }
                catch (IOException e) when (e.Message.Contains("because it is being used by another process"))
                {
                    // If some other process is using this file, try operation again unless elapsed times is greater than x
                    sw = sw ?? Stopwatch.StartNew();
                    if (sw.ElapsedMilliseconds > 10000)
                        throw;
                }
            }
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
                    // If some other process is using this file, try operation again unless elapsed times is greater than x
                    sw = sw ?? Stopwatch.StartNew();
                    if (sw.ElapsedMilliseconds > 10000)
                        return false;
                }
                catch (Exception)
                {
                    // Bad this now is that there is no logging here
                    return false;
                }
            }
        }
    }
}