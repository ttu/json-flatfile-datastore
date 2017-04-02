## Changelog

### [Unreleased]
* Fix for CopyProperties when handling different list types

### [1.1.0] - 2017-03-27
* Use ExpandoObject as a source on CopyProperties / UpdateOne
* Collection methods will return true if operation is succesful

### [1.0.2] - 2017-03-26
* Fix for UpdateOneAsync

### [1.0.1] - 2017-03-22
* Fix for GetNextIdValue when collection is empty
* Newtonsoft.Json to 10.0.1

### [1.0.0] - 2017-03-22
* AsQueryable, Find, GetNextIdValue, InsertOne, ReplaceOne, UpdateOne, DeleteOne, DeleteMany
* Synchronous and asynchronous methods
* Handle dynamic and typed collections
* Handle different camel cases
* Write data to JSON on background thread