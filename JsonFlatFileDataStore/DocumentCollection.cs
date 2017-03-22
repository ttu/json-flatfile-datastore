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

        public void InsertOne(T entity)
        {
            _data.Value.Add(entity);
            _commit(_path, _data.Value, false);
        }

        public async Task InsertOneAsync(T entity)
        {
            _data.Value.Add(entity);
            await _commit(_path, _data.Value, true);
        }

        public void ReplaceOne(Predicate<T> filter, T entity)
        {
            var index = _data.Value.IndexOf(Find(filter).First());
            _data.Value[index] = entity;
            _commit(_path, _data.Value, false);
        }

        public async Task ReplaceOneAsync(Predicate<T> filter, T entity)
        {
            var index = _data.Value.IndexOf(Find(filter).First());
            _data.Value[index] = entity;
            await _commit(_path, _data.Value, true);
        }

        public void UpdateOne(Predicate<T> filter, dynamic entity)
        {
            var toUpdate = Find(filter).First();
            ObjectExtensions.CopyProperties(entity, toUpdate);
            ReplaceOne(filter, toUpdate);
        }

        public async Task UpdateOneAsync(Predicate<T> filter, dynamic entity)
        {
            var toUpdate = Find(filter).First();
            ObjectExtensions.CopyProperties(entity, toUpdate);
            await ReplaceOneAsync(filter, entity);
        }

        public void DeleteOne(Predicate<T> filter)
        {
            _data.Value.Remove(Find(filter).First());
            _commit(_path, _data.Value, false);
        }

        public async Task DeleteOneAsync(Predicate<T> filter)
        {
            _data.Value.Remove(Find(filter).First());
            await _commit(_path, _data.Value, true);
        }

        public void DeleteMany(Predicate<T> filter)
        {
            _data.Value.RemoveAll(filter);
            _commit(_path, _data.Value, false);
        }

        public async Task DeleteManyAsync(Predicate<T> filter)
        {
            _data.Value.RemoveAll(filter);
            await _commit(_path, _data.Value, true);
        }
    }
}