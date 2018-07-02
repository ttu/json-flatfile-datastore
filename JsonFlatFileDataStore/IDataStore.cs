using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JsonFlatFileDataStore
{
    /// <summary>
    /// JSON data store
    /// </summary>
    public interface IDataStore : IDisposable
    {
        /// <summary>
        /// Has the background thread update actions in the queue or is the update excuting
        /// </summary>
        bool IsUpdating { get; }

        /// <summary>
        /// Get dynamic collection
        /// </summary>
        /// <param name="name">Collection name</param>
        /// <returns>Dynamic IDocumentCollection</returns>
        IDocumentCollection<dynamic> GetCollection(string name);

        /// <summary>
        /// Get collection
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="name">Collection name</param>
        /// <returns>Typed IDocumentCollection</returns>
        IDocumentCollection<T> GetCollection<T>(string name = null) where T : class;

        /// <summary>
        /// List keys in JSON
        /// </summary>
        /// <param name="typeToGet">Item type to get</param>
        /// <returns>Dictionary of keys and item value type</returns>
        IDictionary<string, ValueType> GetKeys(ValueType? typeToGet = null);

        /// <summary>
        /// Update the content of the json file
        /// </summary>
        /// <param name="jsonData">New content</param>
        void UpdateAll(string jsonData);

        /// <summary>
        /// Reload data from the file
        /// </summary>
        void Reload();

        /// <summary>
        /// Get single item
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="key">Item key</param>
        /// <returns>Typed item</returns>
        T GetItem<T>(string key);

        /// <summary>
        /// Get single item
        /// </summary>
        /// <param name="key">Item key</param>
        /// <returns>Dynamic item</returns>
        dynamic GetItem(string key);

        /// <summary>
        /// Insert single item
        /// </summary>
        /// <typeparam name="T">Type of item</typeparam>
        /// <param name="key">Item key</param>
        /// <param name="item">New item to be inserted</param>
        /// <returns>true if operation is successful</returns>
        bool InsertItem<T>(string key, T item);

        /// <summary>
        /// Insert single item
        /// </summary>
        /// <typeparam name="T">Type of item</typeparam>
        /// <param name="key">Item key</param>
        /// <param name="item">New item to be inserted</param>
        /// <returns>true if operation is successful</returns>
        Task<bool> InsertItemAsync<T>(string key, T item);

        /// <summary>
        /// Replace the item matching the key
        /// </summary>
        /// <typeparam name="T">Type of item</typeparam>
        /// <param name="key">Item key</param>
        /// <param name="item">New content</param>
        /// <param name="upsert">Will item be inserted if not found</param>
        /// <returns>true if item found for replacement</returns>
        bool ReplaceItem<T>(string key, T item, bool upsert = false);

        /// <summary>
        /// Replace the item matching the key
        /// </summary>
        /// <typeparam name="T">Type of item</typeparam>
        /// <param name="key">Item key</param>
        /// <param name="item">New content</param>
        /// <param name="upsert">Will item be inserted if not found</param>
        /// <returns>true if item found for replacement</returns>
        Task<bool> ReplaceItemAsync<T>(string key, T item, bool upsert = false);

        /// <summary>
        /// Update the item matching the key
        /// </summary>
        /// <param name="key">Item key</param>
        /// <param name="item">New content</param>
        /// <returns>true if item found for update</returns>
        bool UpdateItem(string key, dynamic item);

        /// <summary>
        /// Update the item matching the key
        /// </summary>
        /// <param name="key">Item key</param>
        /// <param name="item">New content</param>
        /// <returns>true if item found for update</returns>
        Task<bool> UpdateItemAsync(string key, dynamic item);

        /// <summary>
        /// Delete the item matching the filter
        /// </summary>
        /// <param name="key">Item key</param>
        /// <returns>true if items found for deletion</returns>
        bool DeleteItem(string key);

        /// <summary>
        /// Delete the item matching the filter
        /// </summary>
        /// <param name="key">Item key</param>
        /// <returns>true if items found for deletion</returns>
        Task<bool> DeleteItemAsync(string key);
    }

    public enum ValueType
    {
        Collection,
        Item
    }
}