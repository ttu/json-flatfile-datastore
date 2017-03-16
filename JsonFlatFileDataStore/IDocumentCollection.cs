using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonFlatFileDataStore
{
    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDocumentCollection<T>
    {
        IEnumerable<T> AsQueryable();

        IEnumerable<T> Find(Predicate<T> query);

        void InsertOne(T entity);

        Task InsertOneAsync(T entity);

        /// <summary>
        /// Replace the first document that matches the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="entity"></param>
        void ReplaceOne(Predicate<T> filter, T entity);

        Task ReplaceOneAsync(Predicate<T> filter, T entity);

        /// <summary>
        /// Update the first document that matches the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="entity"></param>
        void UpdateOne(Predicate<T> filter, T entity);

        Task UpdateOneAsync(Predicate<T> filter, T entity);

        /// <summary>
        /// Delete first that matches the filter
        /// </summary>
        /// <param name="filter"></param>
        void DeleteOne(Predicate<T> filter);

        Task DeleteOneAsync(Predicate<T> filter);

        /// <summary>
        /// Delete all that match the filter
        /// </summary>
        /// <param name="filter"></param>
        void DeleteMany(Predicate<T> filter);

        Task DeleteManyAsync(Predicate<T> filter);

        int Count { get; }
    }
}