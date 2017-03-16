using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
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
        private readonly ExpandoObjectConverter _converter = new ExpandoObjectConverter();
        private readonly BlockingCollection<Action> _updates = new BlockingCollection<Action>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public DataStore(string path)
        {
            _filePath = path;
            var json = File.ReadAllText(path);
            _jsonData = JObject.Parse(json);

            // Run updates on background thread and use BlockingCollection to prevent
            // multiple updates running at the same time
            Task.Run(() =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    var updateAction = _updates.Take();
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
                File.WriteAllText(_filePath, _jsonData.ToString());
            }));
        }

        public IDocumentCollection<T> GetCollection<T>(string path = null) where T : class
        {
            var convertFunc = new Func<JToken, T>(e => JsonConvert.DeserializeObject<T>(e.ToString()));
            return GetCollection<T>(path ?? typeof(T).Name.ToLower(), convertFunc);
        }

        public IDocumentCollection<dynamic> GetCollection(string path)
        {
            // As we don't want to return JObjects when using dynamic, JObjects will be converted to ExpandoObjects
            var convertFunc = new Func<JToken, dynamic>(e => JsonConvert.DeserializeObject<ExpandoObject>(e.ToString(), new ExpandoObjectConverter()) as dynamic);
            return GetCollection<dynamic>(path, convertFunc);
        }

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

            return new DocumentCollection<T>((sender, dataToUpdate, async) => Commit(sender, dataToUpdate, async), data, path);
        }

        private async Task Commit<T>(string path, IList<T> data, bool async = false)
        {
            bool waitFlag = true;

            // string wouldn't actually need a local copy, but still better to be safe ;)
            string pathBu = path;
            List<T> dataBu = data.ToList();

            _updates.Add(new Action(() =>
            {
                _jsonData[pathBu] = JArray.FromObject(dataBu);
                File.WriteAllText(_filePath, _jsonData.ToString());
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