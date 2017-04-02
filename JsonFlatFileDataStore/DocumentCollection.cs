using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonFlatFileDataStore
{
    public class DocumentCollection<T> : IDocumentCollection<T>
    {
        private readonly string _path;
        private readonly string _idField;
        private readonly Lazy<List<T>> _data;
        private readonly Func<string, List<T>, bool, Task> _commit;

        public DocumentCollection(Func<string, List<T>, bool, Task> commit, Lazy<List<T>> data, string path, string idField)
        {
            _path = path;
            _idField = idField;
            _commit = commit;
            _data = data;
        }

        public int Count => _data.Value.Count;

        public IEnumerable<T> AsQueryable() => _data.Value.AsQueryable();

        public IEnumerable<T> Find(Predicate<T> query) => _data.Value.Where(t => query(t));

        private string ParseNextIntegertToKeyValue(string input)
        {
            int nextInt = 0;

            if (input == null)
                return $"{nextInt}";

            var chars = input.ToArray().Reverse().TakeWhile(char.IsNumber).Reverse().ToArray();

            if (chars.Count() == 0)
                return $"{input}{nextInt}";

            input = input.Substring(0, input.Length - chars.Count());

            if (int.TryParse(new string(chars), out nextInt))
                nextInt += 1;

            return $"{input}{nextInt}";
        }

        public dynamic GetNextIdValue()
        {
            if (!_data.Value.Any())
                return 0;

            var lastItem = _data.Value.Last();
            var expando = JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(lastItem), new ExpandoObjectConverter());
            // Problem here is if we have typed data with upper camel case properties but lower camel case in JSON, so need to use OrdinalIgnoreCase string comparer
            var expandoAsIgnoreCase = new Dictionary<string, dynamic>(expando, StringComparer.OrdinalIgnoreCase);
            dynamic keyValue = expandoAsIgnoreCase[_idField];

            if (keyValue is Int64)
                return (int)keyValue + 1;

            return ParseNextIntegertToKeyValue(keyValue.ToString());
        }

        public bool InsertOne(T item)
        {
            _data.Value.Add(item);
            _commit(_path, _data.Value, false);
            return true;
        }

        public async Task<bool> InsertOneAsync(T item)
        {
            _data.Value.Add(item);
            await _commit(_path, _data.Value, true);
            return true;
        }

        public bool ReplaceOne(Predicate<T> filter, T item)
        {
            var matches = Find(filter);

            if (!matches.Any())
                return false;

            var index = _data.Value.IndexOf(matches.First());
            _data.Value[index] = item;
            _commit(_path, _data.Value, false);
            return true;
        }

        public async Task<bool> ReplaceOneAsync(Predicate<T> filter, T item)
        {
            var matches = Find(filter);

            if (!matches.Any())
                return false;

            var index = _data.Value.IndexOf(matches.First());
            _data.Value[index] = item;
            await _commit(_path, _data.Value, true);
            return true;
        }

        public bool UpdateOne(Predicate<T> filter, dynamic item)
        {
            var matches = Find(filter);

            if (!matches.Any())
                return false;

            var toUpdate = matches.First();
            ObjectExtensions.CopyProperties(item, toUpdate);
            ReplaceOne(filter, toUpdate);
            return true;
        }

        public async Task<bool> UpdateOneAsync(Predicate<T> filter, dynamic item)
        {
            var matches = Find(filter);

            if (!matches.Any())
                return false;

            var toUpdate = matches.First();
            ObjectExtensions.CopyProperties(item, toUpdate);
            await ReplaceOneAsync(filter, toUpdate);
            return true;
        }

        public bool DeleteOne(Predicate<T> filter)
        {
            var matches = Find(filter);

            if (!matches.Any())
                return false;

            _data.Value.Remove(matches.First());
            _commit(_path, _data.Value, false);
            return true;
        }

        public async Task<bool> DeleteOneAsync(Predicate<T> filter)
        {
            var matches = Find(filter);

            if (!matches.Any())
                return false;

            _data.Value.Remove(Find(filter).First());
            await _commit(_path, _data.Value, true);
            return true;
        }

        public bool DeleteMany(Predicate<T> filter)
        {
            var removed = _data.Value.RemoveAll(filter);

            if (removed > 0)
            {
                _commit(_path, _data.Value, false);
                return true;
            }

            return false;
        }

        public async Task<bool> DeleteManyAsync(Predicate<T> filter)
        {
            var removed = _data.Value.RemoveAll(filter);

            if (removed > 0)
            {
                await _commit(_path, _data.Value, true);
                return true;
            }

            return false;
        }
    }
}