JSON Flat file Datastore
----------------------------------

Dead simple flat file json datastore.

No relations. No indexes. No bells. No whistles. Just basics.

Works with dynamic and typed data.

### Usage

Commits are made in batches so first come first served.

If this is used with e.g. Web API add to container as singleton.

### API

API is similiar to MongoDB API so you can try with this and switch to using Mongo, or even better, DocumentDB.

Use type inference as types are not interchangable.

[http://mongodb.github.io/mongo-csharp-driver/2.4/reference/driver/crud/linq/#queryable]

[http://mongodb.github.io/mongo-csharp-driver/2.4/reference/driver/crud/writing/]

### Collection naming

Class names are transfered to lower. E.g. User is user nad UserFamiy is userfamily.