JSON Flat file Datastore
----------------------------------

Dead simple flat file json datastore.

No relations. No indexes. No bells. No whistles. Just basics.

Works with dynamic and typed data.

Has sync and async methods.

## Example

Example user collection

```json
{
  "user": [
    {
      "id": "1",
      "name": "Phil",
      "age": 40,
      "city": "NY"
    },
	...
  ]
}
```

Dynamic data and using async methods

```csharp
var store = new DataStore(pathToJson);

var dynamicCollection = store.GetCollection("user");

var userDynamic = dynamicCollection
                    .AsQueryable()
                    .Single(p => p.name == "Phil");

await dynamicCollection.InsertOneAsync(new { id = "2", name = "Raymond", age = 32 });
await dynamicCollection.DeleteOneAsync(e => e.name == "Raymond");
```

Typed data and using sync methods

```csharp
var typedCollection = store.GetCollection<User>();

var userTyped = typedCollection
                    .AsQueryable()
                    .Single(p => p.Name == "Phil");

typedCollection.InsertOne(new User { Id = "3", Name = "Jim", Age = 52 });
typedCollection.DeleteOne(e => e.Name == "Jim");
```

### Usage

Changes are committed immediately. Commits are queued to blocking collection and processed on background. 

On commit, datastore always writes whole collection to the file.

If this is used with e.g. Web API, add it to DI-container as a singleton.

### API

API is similiar to MongoDB API so you can try with this and switch to using Mongo, or even better, DocumentDB.

Use type inference as types are not interchangable.

Links:

http://mongodb.github.io/mongo-csharp-driver/2.4/reference/driver/crud/linq/#queryable

http://mongodb.github.io/mongo-csharp-driver/2.4/reference/driver/crud/writing/

https://docs.microsoft.com/en-us/azure/documentdb/documentdb-mongodb-application

### Collection naming

If collection name is not defined with typed collection, class name is converted to lowercase. E.g. User is user, UserFamiy is userfamily etc.

### License

Licensed under the [MIT](LICENSE) License.

### Changelog

[Changelog](CHANGELOG.md)
