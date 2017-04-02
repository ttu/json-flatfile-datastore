using System;
using System.Collections.Generic;
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
        /// Get next value for id field
        /// </summary>
        /// <returns></returns>
        dynamic GetNextIdValue();

        /// <summary>
        /// Inserts a single item
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>true if operation succesful</returns>
        bool InsertOne(T entity);

        /// <summary>
        /// Inserts a single item
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>true if operation succesful</returns>
        Task<bool> InsertOneAsync(T entity);

        /// <summary>
        /// Replace the first item that matches the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="entity"></param>
        /// <returns>true if items found for replacement</returns>
        bool ReplaceOne(Predicate<T> filter, T entity);

        /// <summary>
        /// Replace the first item that matches the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="entity"></param>
        /// <returns>true if items found for replacement</returns>
        Task<bool> ReplaceOneAsync(Predicate<T> filter, T entity);

        /// <summary>
        /// Update the first item that matches the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="entity"></param>
        /// <returns>true if items found for update</returns>
        bool UpdateOne(Predicate<T> filter, dynamic entity);

        /// <summary>
        /// Update the first item that matches the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="entity"></param>
        /// <returns>true if items found for update</returns>
        Task<bool> UpdateOneAsync(Predicate<T> filter, dynamic entity);

        /// <summary>
        /// Delete first item that matches the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>true if items found for deletion</returns>
        bool DeleteOne(Predicate<T> filter);

        /// <summary>
        /// Delete first item that matches the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>true if items found for deletion</returns>
        Task<bool> DeleteOneAsync(Predicate<T> filter);

        /// <summary>
        /// Delete all items that match the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>true if items found for deletion</returns>
        bool DeleteMany(Predicate<T> filter);

        /// <summary>
        /// Delete all that match the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>true if items found for deletion</returns>
        Task<bool> DeleteManyAsync(Predicate<T> filter);

        /// <summary>
        /// Number of items in the collection
        /// </summary>
        int Count { get; }
    }
}