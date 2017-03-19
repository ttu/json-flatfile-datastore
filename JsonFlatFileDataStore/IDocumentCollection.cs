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
        /// <summary>
        /// Collection as queryable
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> AsQueryable();

        /// <summary>
        /// Find all items that match the query
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        IEnumerable<T> Find(Predicate<T> query);

        /// <summary>
        /// Inserts a single item
        /// </summary>
        /// <param name="entity"></param>
        void InsertOne(T entity);

        /// <summary>
        /// Inserts a single item
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task InsertOneAsync(T entity);

        /// <summary>
        /// Replace the first item that matches the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="entity"></param>
        void ReplaceOne(Predicate<T> filter, T entity);

        /// <summary>
        /// Replace the first item that matches the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task ReplaceOneAsync(Predicate<T> filter, T entity);

        /// <summary>
        /// Update the first item that matches the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="entity"></param>
        void UpdateOne(Predicate<T> filter, dynamic entity);

        /// <summary>
        /// Update the first item that matches the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task UpdateOneAsync(Predicate<T> filter, dynamic entity);

        /// <summary>
        /// Delete first item that matches the filter
        /// </summary>
        /// <param name="filter"></param>
        void DeleteOne(Predicate<T> filter);

        /// <summary>
        /// Delete first item that matches the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task DeleteOneAsync(Predicate<T> filter);

        /// <summary>
        /// Delete all items that match the filter
        /// </summary>
        /// <param name="filter"></param>
        void DeleteMany(Predicate<T> filter);

        /// <summary>
        /// Delete all that match the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task DeleteManyAsync(Predicate<T> filter);

        /// <summary>
        /// Number of items in the collection
        /// </summary>
        int Count { get; }
    }
}