JSON Flat file Datastore
----------------------------------

Dead simple flat file json datastore.

No relations. No indexes. No bells. No whistles. Just basics.

Works with dynamic and typed data.

### Usage

Changes are committed immediately. Commits are queued and processed on background, so first come first served. Datastore always writes whole collection to the file.

If this is used with e.g. Web API, add it to DI-container as a singleton.

### API

API is similiar to MongoDB API so you can try with this and switch to using Mongo, or even better, DocumentDB.

Use type inference as types are not interchangable.

Links:

[http://mongodb.github.io/mongo-csharp-driver/2.4/reference/driver/crud/linq/#queryable]

[http://mongodb.github.io/mongo-csharp-driver/2.4/reference/driver/crud/writing/]

[https://docs.microsoft.com/en-us/azure/documentdb/documentdb-mongodb-application]

### Collection naming

Class names are transfered to lowercase. E.g. User is user, UserFamiy is userfamily etc.

### License

Licensed under the [MIT](LICENSE) License.

### Changelog

[Changelog](CHANGELOG.md)
