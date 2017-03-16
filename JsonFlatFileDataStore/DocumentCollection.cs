using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonFlatFileDataStore
{
    public class DocumentCollection<T> : IDocumentCollection<T>
    {
        private readonly string _path;
        private readonly Lazy<List<T>> _data;
        private readonly Func<string, List<T>, bool, Task> _commit;

        public DocumentCollection(Func<string, List<T>, bool, Task> commit, Lazy<List<T>> data, string path)
        {
            _path = path;
            _commit = commit;
            _data = data;
        }

        public int Count => _data.Value.Count;

        public IEnumerable<T> AsQueryable() => _data.Value.AsQueryable();

        public IEnumerable<T> Find(Predicate<T> query) => _data.Value.Where(t => query(t));

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

        public void UpdateOne(Predicate<T> filter, T entity)
        {
            // TODO: Update with reflection
            //var toUpdate = Find(filter).First();
            ReplaceOne(filter, entity);
        }

        public async Task UpdateOneAsync(Predicate<T> filter, T entity)
        {
            // TODO: Update with reflection
            //var toUpdate = Find(filter).First();
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