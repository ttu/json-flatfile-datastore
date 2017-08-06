using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JsonFlatFileDataStore
{
    /// <summary>
    /// Collection of items
    /// </summary>
    /// <typeparam name="T">Type of item</typeparam>
    public interface IDocumentCollection<T>
    {
        /// <summary>
        /// Collection as queryable
        /// </summary>
        /// <returns>Collection enumerable</returns>
        IEnumerable<T> AsQueryable();

        /// <summary>
        /// Find all items that match the query
        /// </summary>
        /// <param name="query">Filter predicate</param>
        /// <returns>Enumerable of items</returns>
        IEnumerable<T> Find(Predicate<T> query);

        /// <summary>
        /// Full-text search
        /// </summary>
        /// <param name="text">Search text</param>
        /// <param name="caseSensitive">Is search case sensitive</param>
        /// <returns>Enumerable of items</returns>
        IEnumerable<T> Find(string text, bool caseSensitive = false);

        /// <summary>
        /// Get next value for id field
        /// </summary>
        /// <returns>Integer or string identification value</returns>
        dynamic GetNextIdValue();

        /// <summary>
        /// Insert a single item
        /// </summary>
        /// <param name="item">New item to be inserted</param>
        /// <returns>true if operation successful</returns>
        bool InsertOne(T item);

        /// <summary>
        /// Insert a single item
        /// </summary>
        /// <param name="item">New item to be inserted</param>
        /// <returns>true if operation successful</returns>
        Task<bool> InsertOneAsync(T item);

        /// <summary>
        /// Insert items
        /// </summary>
        /// <param name="items">New items to be inserted</param>
        /// <returns>true if operation successful</returns>
        bool InsertMany(IEnumerable<T> items);

        /// <summary>
        /// Insert items
        /// </summary>
        /// <param name="items">New items to be inserted</param>
        /// <returns>true if operation successful</returns>
        Task<bool> InsertManyAsync(IEnumerable<T> items);

        /// <summary>
        /// Replace the first item that matches the filter
        /// </summary>
        /// <param name="filter">First item that matches the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <param name="upsert">Will item be inserted if not found</param>
        /// <returns>true if items found for replacement</returns>
        bool ReplaceOne(Predicate<T> filter, T item, bool upsert = false);

        /// <summary>
        /// Replace the first item that matches the filter
        /// </summary>
        /// <param name="filter">First item that matches the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <param name="upsert">Will item be inserted if not found</param>
        /// <returns>true if items found for replacement</returns>
        Task<bool> ReplaceOneAsync(Predicate<T> filter, T item, bool upsert = false);

        /// <summary>
        /// Replace all items that match the filter
        /// </summary>
        /// <param name="filter">All items that match the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <returns>true if items found for replacement</returns>
        bool ReplaceMany(Predicate<T> filter, T item);

        /// <summary>
        /// Replace all items that match the filter
        /// </summary>
        /// <param name="filter">All items that match the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <returns>true if items found for replacement</returns>
        Task<bool> ReplaceManyAsync(Predicate<T> filter, T item);

        /// <summary>
        /// Update the first item that matches the filter
        /// </summary>
        /// <param name="filter">First item that matches the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <returns>true if items found for update</returns>
        bool UpdateOne(Predicate<T> filter, dynamic item);

        /// <summary>
        /// Update the first item that matches the filter
        /// </summary>
        /// <param name="filter">First item that matches the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <returns>true if items found for update</returns>
        Task<bool> UpdateOneAsync(Predicate<T> filter, dynamic item);

        /// <summary>
        /// Update all items that match the filter
        /// </summary>
        /// <param name="filter">All items that match the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <returns>true if items found for update</returns>
        bool UpdateMany(Predicate<T> filter, dynamic item);

        /// <summary>
        /// Update all items that match the filter
        /// </summary>
        /// <param name="filter">All items that match the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <returns>true if items found for update</returns>
        Task<bool> UpdateManyAsync(Predicate<T> filter, dynamic item);

        /// <summary>
        /// Delete first item that matches the filter
        /// </summary>
        /// <param name="filter">First item that matches the predicate will be deleted</param>
        /// <returns>true if items found for deletion</returns>
        bool DeleteOne(Predicate<T> filter);

        /// <summary>
        /// Delete first item that matches the filter
        /// </summary>
        /// <param name="filter">First item that matches the predicate will be deleted</param>
        /// <returns>true if items found for deletion</returns>
        Task<bool> DeleteOneAsync(Predicate<T> filter);

        /// <summary>
        /// Delete all items that match the filter
        /// </summary>
        /// <param name="filter">All items that match the predicate will be deleted</param>
        /// <returns>true if items found for deletion</returns>
        bool DeleteMany(Predicate<T> filter);

        /// <summary>
        /// Delete all that match the filter
        /// </summary>
        /// <param name="filter">All items that match the predicate will be deleted</param>
        /// <returns>true if items found for deletion</returns>
        Task<bool> DeleteManyAsync(Predicate<T> filter);

        /// <summary>
        /// Number of items in the collection
        /// </summary>
        int Count { get; }
    }
}