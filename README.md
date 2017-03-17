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
      "id": 1,
      "name": "Phil",
      "age": 40,
      "city": "NY"
    },
	{
      "id": 2,
      "name": "Larry",
      "age": 37,
      "city": "London"
    }
  ]
}
```

Dynamic data and using async methods

```csharp
var store = new DataStore(pathToJson);

var dynamicCollection = store.GetCollection("user");

// Find item with name
var userDynamic = dynamicCollection
                    .AsQueryable()
                    .Single(p => p.name == "Phil");

// Add new item
await dynamicCollection.InsertOneAsync(new { id = 3, name = "Raymond", age = 32 });
// Replace added item
await dynamicCollection.ReplaceOneAsync(e => e.id == 3, new { id = 3, name = "Barry", age = 32 });
// Delete item
await dynamicCollection.DeleteOneAsync(e => e.name == "Barry");
```

Typed data and using sync methods

```csharp
var store = new DataStore(pathToJson);

var typedCollection = store.GetCollection<User>();

// Find item with name
var userTyped = typedCollection
                    .AsQueryable()
                    .Single(p => p.Name == "Phil");

// Add new item
typedCollection.InsertOne(new User { Id = 3, Name = "Jim", Age = 52 });
// Replace added item
typedCollection.ReplaceOne(e => e.Id == 3, new User { Id = 3, Name = "Barry", Age = 52 });
// Delete item
typedCollection.DeleteOne(e => e.Name == "Barry");
```

### Writing to file

Changes are committed immediately from collection to DataStore's internal collection. Each update creates a write updates to a blocking collection, which is processed on background. 

On commit, datastore always writes whole collection to the file, even when only one item is changed.

If this is used with e.g. Web API, add it to DI-container as a singleton. This way DataStore's internal state is correct and application does not have to rely on the state on the file.

### API

API is similiar to MongoDB API so you can try with this and switch to using Mongo, or even better, DocumentDB. Use type inference as types are not interchangable.

Links:

[MongoDB-C#-linq](http://mongodb.github.io/mongo-csharp-driver/2.4/reference/driver/crud/linq/#queryable)

[MongoDB-C#-crud](http://mongodb.github.io/mongo-csharp-driver/2.4/reference/driver/crud/writing/)

[DocumentDB: API for MongoDB](https://docs.microsoft.com/en-us/azure/documentdb/documentdb-mongodb-application)

### Collection naming

If collection name is not defined with typed collection, class name is converted to lowercase. E.g. User is user, UserFamiy is userfamily etc.

```csharp
var store = new DataStore(newFilePath);
// JSON { "movie": [] };
var collection = store.GetCollection<Movie>();
// JSON { "movies": [] };
var collection = store.GetCollection<Movie>("movies");
```

### License

Licensed under the [MIT](LICENSE) License.

### Changelog

[Changelog](CHANGELOG.md)
