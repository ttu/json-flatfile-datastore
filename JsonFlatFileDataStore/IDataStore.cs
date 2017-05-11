using System.Collections.Generic;

namespace JsonFlatFileDataStore
{
    public interface IDataStore
    {
        /// <summary>
        /// Is backgound thread executing writes from queue
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
        /// List collections
        /// </summary>
        /// <returns>List of collection names</returns>
        IEnumerable<string> ListCollections();

        /// <summary>
        /// Update all content from json file
        /// </summary>
        /// <param name="jsonData">New content</param>
        void UpdateAll(string jsonData);

        /// <summary>
        /// Reload data from the file
        /// </summary>
        void Reload();
    }
}