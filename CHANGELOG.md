## Changelog

### [Unreleased]
*

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