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
        private readonly Func<string, Func<List<T>, bool>, bool, Task<bool>> _commit;
        private readonly Func<T, T> _insertConvert;
        private readonly Func<T> _createNewInstance;

        public DocumentCollection(Func<string, Func<List<T>, bool>, bool, Task<bool>> commit, Lazy<List<T>> data, string path, string idField, Func<T, T> insertConvert, Func<T> createNewInstance)
        {
            _path = path;
            _idField = idField;
            _commit = commit;
            _data = data;
            _insertConvert = insertConvert;
            _createNewInstance = createNewInstance;
        }

        public int Count => _data.Value.Count;

        public IEnumerable<T> AsQueryable() => _data.Value.AsQueryable();

        public IEnumerable<T> Find(Predicate<T> query) => _data.Value.Where(t => query(t));

        public IEnumerable<T> Find(string text, bool caseSensitive = false) => _data.Value.Where(t => ObjectExtensions.FullTextSearch(t, text, caseSensitive));

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

        public dynamic GetNextIdValue() => GetNextIdValue(_data.Value);

        private dynamic GetNextIdValue(List<T> data)
        {
            if (!data.Any())
                return 0;

            var lastItem = data.Last();
            var expando = JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(lastItem), new ExpandoObjectConverter());
            // Problem here is if we have typed data with upper camel case properties but lower camel case in JSON, so need to use OrdinalIgnoreCase string comparer
            var expandoAsIgnoreCase = new Dictionary<string, dynamic>(expando, StringComparer.OrdinalIgnoreCase);

            if (!expandoAsIgnoreCase.ContainsKey(_idField))
                return null;

            dynamic keyValue = expandoAsIgnoreCase[_idField];

            if (keyValue is Int64)
                return (int)keyValue + 1;

            return ParseNextIntegertToKeyValue(keyValue.ToString());
        }

        public bool InsertOne(T item)
        {
            var insertOne = new Func<List<T>, bool>(data =>
            {
                TryUpdateId(data, item);
                data.Add(_insertConvert(item));
                return true;
            });

            insertOne(_data.Value);

            return _commit(_path, insertOne, false).Result;
        }

        public async Task<bool> InsertOneAsync(T item)
        {
            var insertOne = new Func<List<T>, bool>(data =>
            {
                TryUpdateId(data, item);
                data.Add(_insertConvert(item));
                return true;
            });

            insertOne(_data.Value);

            return await _commit(_path, insertOne, true).ConfigureAwait(false);
        }

        private void TryUpdateId(List<T> data, T item)
        {
            var insertId = GetNextIdValue(data);

            if (insertId == null)
                return;

            ObjectExtensions.AddDataToField(item, _idField, insertId);
        }

        public bool InsertMany(IEnumerable<T> items)
        {
            var insertMany = new Func<List<T>, bool>(data =>
            {
                foreach (var item in items)
                {
                    TryUpdateId(data, item);
                    data.Add(_insertConvert(item));
                }

                return true;
            });

            insertMany(_data.Value);

            return _commit(_path, insertMany, false).Result;
        }

        public async Task<bool> InsertManyAsync(IEnumerable<T> items)
        {
            var updateAction = new Func<List<T>, bool>(data =>
            {
                foreach (var item in items)
                {
                    TryUpdateId(data, item);
                    data.Add(_insertConvert(item));
                }

                return true;
            });

            updateAction(_data.Value);

            return await _commit(_path, updateAction, true).ConfigureAwait(false);
        }

        public bool ReplaceOne(Predicate<T> filter, T item, bool upsert = false)
        {
            var updateAction = new Func<List<T>, bool>(data =>
            {
                var matches = data.Where(e => filter(e));

                if (!matches.Any())
                {
                    if (!upsert)
                        return false;

                    var newItem = _createNewInstance();
                    ObjectExtensions.CopyProperties(item, newItem);
                    data.Add(_insertConvert(newItem));
                    return true;
                }

                var index = data.IndexOf(matches.First());
                data[index] = item;

                return true;
            });

            if (!updateAction(_data.Value))
                return false;

            return _commit(_path, updateAction, false).Result;
        }

        public bool ReplaceMany(Predicate<T> filter, T item)
        {
            var updateAction = new Func<List<T>, bool>(data =>
            {
                var matches = data.Where(e => filter(e));

                if (!matches.Any())
                    return false;

                foreach (var match in matches.ToList())
                {
                    var index = data.IndexOf(match);
                    data[index] = item;
                }

                return true;
            });

            if (!updateAction(_data.Value))
                return false;

            return _commit(_path, updateAction, false).Result;
        }

        public async Task<bool> ReplaceOneAsync(Predicate<T> filter, T item, bool upsert = false)
        {
            var updateAction = new Func<List<T>, bool>(data =>
            {
                var matches = data.Where(e => filter(e));

                if (!matches.Any())
                {
                    if (!upsert)
                        return false;

                    var newItem = _createNewInstance();
                    ObjectExtensions.CopyProperties(item, newItem);
                    data.Add(_insertConvert(newItem));
                    return true;
                }

                var index = data.IndexOf(matches.First());
                data[index] = item;

                return true;
            });

            if (!updateAction(_data.Value))
                return false;

            return await _commit(_path, updateAction, true).ConfigureAwait(false);
        }

        public async Task<bool> ReplaceManyAsync(Predicate<T> filter, T item)
        {
            var updateAction = new Func<List<T>, bool>(data =>
            {
                var matches = data.Where(e => filter(e));

                if (!matches.Any())
                    return false;

                foreach (var match in matches.ToList())
                {
                    var index = data.IndexOf(match);
                    data[index] = item;
                }

                return true;
            });

            if (!updateAction(_data.Value))
                return false;

            return await _commit(_path, updateAction, true).ConfigureAwait(false);
        }

        public bool UpdateOne(Predicate<T> filter, dynamic item)
        {
            var updateAction = new Func<List<T>, bool>(data =>
            {
                var matches = data.Where(e => filter(e));

                if (!matches.Any())
                    return false;

                var toUpdate = matches.First();
                ObjectExtensions.CopyProperties(item, toUpdate);

                return true;
            });

            if (!updateAction(_data.Value))
                return false;

            return _commit(_path, updateAction, false).Result;
        }

        public async Task<bool> UpdateOneAsync(Predicate<T> filter, dynamic item)
        {
            var updateAction = new Func<List<T>, bool>(data =>
            {
                var matches = data.Where(e => filter(e));

                if (!matches.Any())
                    return false;

                var toUpdate = matches.First();
                ObjectExtensions.CopyProperties(item, toUpdate);

                return true;
            });

            if (!updateAction(_data.Value))
                return false;

            return await _commit(_path, updateAction, true).ConfigureAwait(false);
        }

        public bool UpdateMany(Predicate<T> filter, dynamic item)
        {
            var updateAction = new Func<List<T>, bool>(data =>
            {
                var matches = data.Where(e => filter(e));

                if (!matches.Any())
                    return false;

                foreach (var toUpdate in matches)
                {
                    ObjectExtensions.CopyProperties(item, toUpdate);
                }

                return true;
            });

            if (!updateAction(_data.Value))
                return false;

            return _commit(_path, updateAction, false).Result;
        }

        public async Task<bool> UpdateManyAsync(Predicate<T> filter, dynamic item)
        {
            var updateAction = new Func<List<T>, bool>(data =>
            {
                var matches = data.Where(e => filter(e));

                if (!matches.Any())
                    return false;

                foreach (var toUpdate in matches)
                {
                    ObjectExtensions.CopyProperties(item, toUpdate);
                }

                return true;
            });

            if (!updateAction(_data.Value))
                return false;

            return await _commit(_path, updateAction, true).ConfigureAwait(false);
        }

        public bool DeleteOne(Predicate<T> filter)
        {
            var updateAction = new Func<List<T>, bool>(data =>
            {
                var remove = data.FirstOrDefault(e => filter(e));

                if (remove == null)
                    return false;

                return data.Remove(remove);
            });

            if (!updateAction(_data.Value))
                return false;

            return _commit(_path, updateAction, false).Result;
        }

        public async Task<bool> DeleteOneAsync(Predicate<T> filter)
        {
            var updateAction = new Func<List<T>, bool>(data =>
            {
                var remove = data.FirstOrDefault(e => filter(e));

                if (remove == null)
                    return false;

                return data.Remove(remove);
            });

            if (!updateAction(_data.Value))
                return false;

            return await _commit(_path, updateAction, true).ConfigureAwait(false);
        }

        public bool DeleteMany(Predicate<T> filter)
        {
            var updateAction = new Func<List<T>, bool>(data =>
            {
                int removed = data.RemoveAll(filter);
                return removed > 0;
            });

            if (!updateAction(_data.Value))
                return false;

            return _commit(_path, updateAction, false).Result;
        }

        public async Task<bool> DeleteManyAsync(Predicate<T> filter)
        {
            var updateAction = new Func<List<T>, bool>(data =>
            {
                int removed = data.RemoveAll(filter);
                return removed > 0;
            });

            if (!updateAction(_data.Value))
                return false;

            return await _commit(_path, updateAction, true).ConfigureAwait(false);
        }
    }
}