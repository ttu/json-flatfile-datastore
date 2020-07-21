# Changelog

### [Unreleased]
* FIXED: Updates and full text search for objects with null collections

### [2.2.2] - 2020-02-26
* FIXED: GetKeys when collection is empty

### [2.2.1] - 2019-10-11
* FIXED: Single item sync methods to use non-async commit

### [2.2.0] - 2019-04-06
* ADDED: DocumentCollection Update, Replace and Delete-methods with id-value parameter
 
### [2.1.0] - 2019-01-30
* FIXED: Single item dynamic converter. Returns lists correctly, regional fixes for dates and number types.
* FIXED: Copy values inside collection of strings correctly
* CHANGED: On add, if collection is empty and item's id is set, that id value is used instead of generated one
* CHANGED: On update, case of the first character of property's name is insensitive. E.g. Name can be updated with Name or name.

### [2.0.1] - 2018-12-14
* CHANGED: GetItem to return null for not found nullable type
* FIXED: Add id-value correctly for fields with string type when collection is empty

### [2.0.0] - 2018-07-04
* CHANGED: Target to netstandard 2.0
* ADDED: Support for single items
* ADDED: GetKeys method, removed old ListCollections method
* CHANGED: ObjectExtensions to internal class

### [1.8.0] - 2017-11-22
* DataStore to implement IDisposable

### [1.7.2] - 2017-10-29
* Include missing xml documentation file

### [1.7.1] - 2017-10-29
* Fix for Insert-methods when using an anonymous type without id
* Fix for DataStore IsUpdating
* Make DocumentCollection Thread-Safe

### [1.7.0] - 2017-08-13
* Full-text search

### [1.6.2] - 2017-07-01
* Fix for ReplaceOne Upsert with dynamic items and null values

### [1.6.1] - 2017-07-01
* Fix for ReplaceOne Upsert with dynamic items and inner Expandos or Dictionaries

### [1.6.0] - 2017-06-10
* Upsert option for IDocumentCollection ReplaceOne-methods

### [1.5.2] - 2017-05-27
* Fix for Update when object has null values

### [1.5.1] - 2017-05-11
* Include xml documentation file and fix IDataStore method summaries

### [1.5.0] - 2017-05-05
* Optional JSON Reload functionality to DataStore

### [1.4.0] - 2017-04-24
* IDocumentCollection Insert-methods automatically add correct value to id-field

### [1.3.1] - 2017-04-22
* IDocumentCollection Update-methods support JSON object and Dictionary as update object

### [1.3.0] - 2017-04-17
* DataStore will commit only changed item(s) to the JSON file
* DataStore will save changes in batches

### [1.2.0] - 2017-04-11
* InsertMany, UpdateMany and ReplaceMany methods to IDocumentCollection

### [1.1.3] - 2017-04-04
* Handle dictionaries in IDocumentCollection UpdateOne
 
### [1.1.2] - 2017-04-03
* Interface for DataStore

### [1.1.1] - 2017-04-02
* Fix for IDocumentCollection UpdateOne when handling different list types
* Fix for IDocumentCollection UpdateOne with patch data that contais inner ExpandoObjects

### [1.1.0] - 2017-03-27
* Use ExpandoObject as a source on IDocumentCollection UpdateOne
* IDocumentCollection methods will return true if operation is succesful

### [1.0.2] - 2017-03-26
* Fix for UpdateOneAsync

### [1.0.1] - 2017-03-22
* Fix for GetNextIdValue when collection is empty

### [1.0.0] - 2017-03-22
* AsQueryable, Find, GetNextIdValue, InsertOne, ReplaceOne, UpdateOne, DeleteOne, DeleteMany
* Synchronous and asynchronous methods
* Handle dynamic and typed collections
* Handle different camel cases
* Write data to JSON on background thread