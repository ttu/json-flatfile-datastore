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
        /// <returns>All items in queryable collection</returns>
        IEnumerable<T> AsQueryable();

        /// <summary>
        /// Find all items matching the query
        /// </summary>
        /// <param name="query">Filter predicate</param>
        /// <returns>Items matching the query</returns>
        IEnumerable<T> Find(Predicate<T> query);

        /// <summary>
        /// Full-text search
        /// </summary>
        /// <param name="text">Search text</param>
        /// <param name="caseSensitive">Is the search case sensitive</param>
        /// <returns>Items matching the search text</returns>
        IEnumerable<T> Find(string text, bool caseSensitive = false);

        /// <summary>
        /// Get next value for id field
        /// </summary>
        /// <returns>Integer or string identifier</returns>
        dynamic GetNextIdValue();

        /// <summary>
        /// Insert single item
        /// </summary>
        /// <param name="item">New item to be inserted</param>
        /// <returns>true if operation is successful</returns>
        bool InsertOne(T item);

        /// <summary>
        /// Insert single item
        /// </summary>
        /// <param name="item">New item to be inserted</param>
        /// <returns>true if operation is successful</returns>
        Task<bool> InsertOneAsync(T item);

        /// <summary>
        /// Insert items
        /// </summary>
        /// <param name="items">New items to be inserted</param>
        /// <returns>true if operation is successful</returns>
        bool InsertMany(IEnumerable<T> items);

        /// <summary>
        /// Insert items
        /// </summary>
        /// <param name="items">New items to be inserted</param>
        /// <returns>true if operation is successful</returns>
        Task<bool> InsertManyAsync(IEnumerable<T> items);

        /// <summary>
        /// Replace the first item matching the filter
        /// </summary>
        /// <param name="filter">First item matching the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <param name="upsert">Will item be inserted if not found</param>
        /// <returns>true if item found for replacement</returns>
        bool ReplaceOne(Predicate<T> filter, T item, bool upsert = false);

        /// <summary>
        /// Replace the item matching the id
        /// </summary>
        /// <param name="id">The item matching the id-value will be replaced</param>
        /// <param name="item">New content</param>
        /// <param name="upsert">Will item be inserted if not found</param>
        /// <returns>true if item found for replacement</returns>
        bool ReplaceOne(dynamic id, T item, bool upsert = false);

        /// <summary>
        /// Replace the first item matching the filter
        /// </summary>
        /// <param name="filter">First item matching the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <param name="upsert">Will item be inserted if not found</param>
        /// <returns>true if item found for replacement</returns>
        Task<bool> ReplaceOneAsync(Predicate<T> filter, T item, bool upsert = false);

        /// <summary>
        /// Replace the item matching the id
        /// </summary>
        /// <param name="id">The item matching the id-value will be replaced</param>
        /// <param name="item">New content</param>
        /// <param name="upsert">Will item be inserted if not found</param>
        /// <returns>true if item found for replacement</returns>
        Task<bool> ReplaceOneAsync(dynamic id, T item, bool upsert = false);

        /// <summary>
        /// Replace all items matching the filter
        /// </summary>
        /// <param name="filter">All items matching the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <returns>true if items found for replacement</returns>
        bool ReplaceMany(Predicate<T> filter, T item);

        /// <summary>
        /// Replace all items matching the filter
        /// </summary>
        /// <param name="filter">All items matching the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <returns>true if items found for replacement</returns>
        Task<bool> ReplaceManyAsync(Predicate<T> filter, T item);

        /// <summary>
        /// Update the first item matching the filter
        /// </summary>
        /// <param name="filter">First item matching the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <returns>true if item found for update</returns>
        bool UpdateOne(Predicate<T> filter, dynamic item);

        /// <summary>
        /// Update the item matching the id
        /// </summary>
        /// <param name="id">The item matching the id-value will be replaced</param>
        /// <param name="item">New content</param>
        /// <returns>true if item found for update</returns>
        bool UpdateOne(dynamic id, dynamic item);

        /// <summary>
        /// Update the first item matching the filter
        /// </summary>
        /// <param name="filter">First item matching the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <returns>true if item found for update</returns>
        Task<bool> UpdateOneAsync(Predicate<T> filter, dynamic item);

        /// <summary>
        /// Update the item matching the id
        /// </summary>
        /// <param name="id">The item matching the id-value will be replaced</param>
        /// <param name="item">New content</param>
        /// <returns>true if item found for update</returns>
        Task<bool> UpdateOneAsync(dynamic id, dynamic item);

        /// <summary>
        /// Update all items matching the filter
        /// </summary>
        /// <param name="filter">All items matching the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <returns>true if items found for update</returns>
        bool UpdateMany(Predicate<T> filter, dynamic item);

        /// <summary>
        /// Update all items matching the filter
        /// </summary>
        /// <param name="filter">All items matching the predicate will be replaced</param>
        /// <param name="item">New content</param>
        /// <returns>true if items found for update</returns>
        Task<bool> UpdateManyAsync(Predicate<T> filter, dynamic item);

        /// <summary>
        /// Delete first item matching the filter
        /// </summary>
        /// <param name="filter">First item matching the predicate will be deleted</param>
        /// <returns>true if item found for deletion</returns>
        bool DeleteOne(Predicate<T> filter);

        /// <summary>
        /// Delete the item matching the id
        /// </summary>
        /// <param name="id">The item matching the id-value will be deleted</param>
        /// <returns>true if item found for deletion</returns>
        bool DeleteOne(dynamic id);

        /// <summary>
        /// Delete first item matching the filter
        /// </summary>
        /// <param name="filter">First item matching the predicate will be deleted</param>
        /// <returns>true if item found for deletion</returns>
        Task<bool> DeleteOneAsync(Predicate<T> filter);

        /// <summary>
        /// Delete the item matching the id
        /// </summary>
        /// <param name="id">The item matching the id-value will be deleted</param>
        /// <returns>true if item found for deletion</returns>
        Task<bool> DeleteOneAsync(dynamic id);

        /// <summary>
        /// Delete all items matching the filter
        /// </summary>
        /// <param name="filter">All items matching the predicate will be deleted</param>
        /// <returns>true if items found for deletion</returns>
        bool DeleteMany(Predicate<T> filter);

        /// <summary>
        /// Delete all items matching the filter
        /// </summary>
        /// <param name="filter">All items matching the predicate will be deleted</param>
        /// <returns>true if items found for deletion</returns>
        Task<bool> DeleteManyAsync(Predicate<T> filter);

        /// <summary>
        /// Number of items in the collection
        /// </summary>
        int Count { get; }
    }
}