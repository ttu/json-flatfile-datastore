using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JsonFlatFileDataStore
{
    public class DataStore
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

            string json = "{}";

            try
            {
                json = File.ReadAllText(path);
            }
            catch (FileNotFoundException)
            {
                File.WriteAllText(path, json);
            }

            _jsonData = JObject.Parse(json);

            // Run updates on background thread and use BlockingCollection to prevent multiple updates running at the same time
            Task.Run(() =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    var updateAction = _updates.Take();
                    updateAction();
                }
            });
        }

        /// <summary>
        /// Is backgound thread executing writes from queue
        /// </summary>
        public bool IsUpdating => _updates.Count > 0;

        /// <summary>
        /// Update all content from json file
        /// </summary>
        /// <param name="jsonData">New content</param>
        public void UpdateAll(string jsonData)
        {
            _updates.Add(new Action(() =>
            {
                _jsonData.ReplaceAll(JObject.Parse(jsonData));
                File.WriteAllText(_filePath, _jsonData.ToString());
            }));
        }

        /// <summary>
        /// Get collection
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="name">Collection name</param>
        /// <returns>Typed IDocumentCollection</returns>
        public IDocumentCollection<T> GetCollection<T>(string name = null) where T : class
        {
            var convertFunc = new Func<JToken, T>(e => JsonConvert.DeserializeObject<T>(e.ToString()));
            return GetCollection<T>(name ?? _pathToCamelCase(typeof(T).Name), convertFunc);
        }

        /// <summary>
        /// Get dynamic collection
        /// </summary>
        /// <param name="name">Collection name</param>
        /// <returns>Dynamic IDocumentCollection</returns>
        public IDocumentCollection<dynamic> GetCollection(string name)
        {
            // As we don't want to return JObjects when using dynamic, JObjects will be converted to ExpandoObjects
            var convertFunc = new Func<JToken, dynamic>(e => JsonConvert.DeserializeObject<ExpandoObject>(e.ToString(), _converter) as dynamic);
            return GetCollection<dynamic>(name, convertFunc);
        }

        /// <summary>
        /// Get all collections
        /// </summary>
        /// <returns>List of collection names</returns>
        public IEnumerable<string> ListCollections()
        {
            return _jsonData.Children().Select(c => c.Path);
        }

        private IDocumentCollection<T> GetCollection<T>(string path, Func<JToken, T> convertFunc)
        {
            var data = new Lazy<List<T>>(() =>
                               _jsonData[path]?
                                   .Children()
                                   .Select(e => convertFunc(e))
                                   .ToList()
                               ?? new List<T>());

            return new DocumentCollection<T>((sender, dataToUpdate, async) => Commit(sender, dataToUpdate, async), data, path, _keyProperty);
        }

        private async Task Commit<T>(string path, IList<T> data, bool async = false)
        {
            bool waitFlag = true;

            List<T> dataBu = data.ToList();

            _updates.Add(new Action(() =>
            {
                _jsonData[path] = JArray.FromObject(dataBu);
                string json = _toJsonFunc(_jsonData);
                File.WriteAllText(_filePath, json);
                waitFlag = false;
            }));

            while (waitFlag)
            {
                if (async)
                    await Task.Delay(1);
                else
                    Task.Delay(1).Wait();
            }
        }
    }
}