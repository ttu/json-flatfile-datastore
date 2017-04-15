## Changelog

### [Unreleased]
* DataStore will commit only changed item(s) to the JSON file

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