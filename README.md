JSON Flat file Datastore
----------------------------------

Dead simple flat file json datastore.

No relations. No indexes. No bells. No whistles. Just basics.

Works with dynamic and typed data.

Has sync and async methods.

## Functionality

Example user collection in json

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

#### Query
Dynamic data

```csharp
var store = new DataStore(pathToJson);

var collection = store.GetCollection("user");

// Find item with name
var userDynamic = collection
                    .AsQueryable()
                    .Single(p => p.name == "Phil");
```

Typed data

```csharp
var store = new DataStore(pathToJson);

var collection = store.GetCollection<User>();

// Find item with name
var userTyped = collection
                    .AsQueryable()
                    .Single(p => p.Name == "Phil");
```

#### Insert
Insert a new item to the collection
```csharp
// Async and dynamic
await collection.InsertOneAsync(new { id = 3, name = "Raymond", age = 32 });
// Sync and typed
collection.InsertOne(new User { Id = 3, Name = "Raymond", Age = 32 });
```

#### Replace
Replace first item that matches the filter
```csharp
// Sync and dynamic
collection.ReplaceOne(e => e.id == 3, new { id = 3, name = "Barry", age = 33 });
// Async and typed
await collection.ReplaceOneAsync(e => e.Id == 3, new User { Id = 3, Name = "Barry", Age = 33 });
```

#### Update
Update first item that matches the filter with passed properties from dynamic object
```csharp
// Dynamic
await collection.UpdateOneAsync(e => e.id == 3, new { age = 42 });
// Typed
await collection.UpdateOneAsync(e => e.Id == 3, new { age = 42 });
```

#### Delete
Delete first object that matches the filter. DeleteOne and DeleteOneAsync.
```csharp
// Dynamic
collection.DeleteOneAsync(e => e.id == 3);
// Typed
collection.DeleteOneAsync(e => e.Id == 3);
```

### Collection naming

If collection name is not defined with typed collection, class name is converted to lowercase. E.g. User is user, UserFamiy is userfamily etc.

```csharp
var store = new DataStore(newFilePath);
// JSON { "movie": [] };
var collection = store.GetCollection<Movie>();
// JSON { "movies": [] };
var collection = store.GetCollection<Movie>("movies");
```

### Writing to file

Changes are committed immediately from collection to DataStore's internal collection. Each update creates a write updates to a blocking collection, which is processed on background. 

On commit, datastore always writes whole collection to the file, even when only one item is changed.

If this is used with e.g. Web API, add it to DI-container as a singleton. This way DataStore's internal state is correct and application does not have to rely on the state on the file.

### API

API is almost identical to MongoDB's C# API, so switching to MongoDB is easy. Use type inference as types are not interchangable.

Links:

[MongoDB-C#-linq](http://mongodb.github.io/mongo-csharp-driver/2.4/reference/driver/crud/linq/#queryable)

[MongoDB-C#-crud](http://mongodb.github.io/mongo-csharp-driver/2.4/reference/driver/crud/writing/)

### License

Licensed under the [MIT](LICENSE) License.

### Changelog

[Changelog](CHANGELOG.md)
