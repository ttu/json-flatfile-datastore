using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace JsonFlatFileDataStore.Test
{
    public class CollectionInsertBatchTests
    {
        [Fact]
        public async Task InsertOne_100SyncThreaded()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collectionPreCheck = store.GetCollection<User>("user");
            Assert.Equal(3, collectionPreCheck.Count);

            var tasks = Enumerable.Range(0, 2).Select(cId =>
            {
                return Task.Run(() =>
                {
                    var collection = store.GetCollection<User>($"user_{cId}");

                    for (int i = 0; i < 50; i++)
                    {
                        collection.InsertOne(new User { Id = i, Name = $"Teddy_{i}" });
                    }

                    return true;
                });
            });

            await Task.WhenAll(tasks);

            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection<User>("user");

            Assert.Equal(3, collection2.Count);

            var collection_0 = store2.GetCollection<User>("user_0");
            Assert.Equal(50, collection_0.Count);

            var distinct_0a = collection_0.AsQueryable().GroupBy(e => e.Id).Select(g => g.First());
            Assert.Equal(50, distinct_0a.Count());

            var distinct_0b = collection_0.AsQueryable().GroupBy(e => e.Name).Select(g => g.First());
            Assert.Equal(50, distinct_0b.Count());

            var collection_1 = store2.GetCollection<User>("user_1");
            Assert.Equal(50, collection_1.Count);

            var distinct_1a = collection_1.AsQueryable().GroupBy(e => e.Id).Select(g => g.First());
            Assert.Equal(50, distinct_1a.Count());

            var distinct_1b = collection_1.AsQueryable().GroupBy(e => e.Name).Select(g => g.First());
            Assert.Equal(50, distinct_1b.Count());

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task InsertOneAsync_100Async()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            var tasks = Enumerable.Range(0, 100)
                .AsParallel()
                .Select(i => collection.InsertOneAsync(new User { Id = i, Name = $"Teddy_{i}" }))
                .ToList();

            await Task.WhenAll(tasks);

            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection<User>("user");

            Assert.Equal(103, collection2.Count);

            var distinct = collection2.AsQueryable().GroupBy(e => e.Id).Select(g => g.First());
            Assert.Equal(103, distinct.Count());

            var distinct2 = collection2.AsQueryable().GroupBy(e => e.Name).Select(g => g.First());
            Assert.Equal(103, distinct2.Count());

            UTHelpers.Down(newFilePath);
        }
    }
}