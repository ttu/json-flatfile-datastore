using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using Xunit;

namespace JsonFlatFileDataStore.Test
{
    public class CopyPropertiesTests
    {
        [Fact]
        public void CopyProperties_TypedAndDynamicAddressCity()
        {
            var family = new Family { Address = new Address { City = "Helsinki" } };

            ObjectExtensions.CopyProperties(new { City = "Espoo" }, family.Address);
            Assert.Equal("Espoo", family.Address.City);

            ObjectExtensions.CopyProperties(new { Address = new { City = "Turku" } }, family);
            Assert.Equal("Turku", family.Address.City);
        }

        [Fact]
        public void CopyProperties_TypedFamily()
        {
            var family = new Family
            {
                Parents = new List<Parent>
                {
                    new Parent {  Name = "Jim", Age = 52 },
                    new Parent {  Name = "Theodor", Age = 14 }
                },
                Address = new Address { City = "Helsinki" }
            };

            ObjectExtensions.CopyProperties(new { Parents = new[] { new { Name = "Ray", Age = 49 } } }, family);
            Assert.Equal(49, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].Name);
            Assert.Equal(14, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].Name);

            ObjectExtensions.CopyProperties(new { Parents = new[] { new { Age = 39 } } }, family);
            Assert.Equal(39, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].Name);
            Assert.Equal(14, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].Name);

            ObjectExtensions.CopyProperties(new { Parents = new[] { null, new { Age = 21 } } }, family);
            Assert.Equal(39, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].Name);
            Assert.Equal(21, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].Name);

            ObjectExtensions.CopyProperties(new { Parents = new[] { null, null, new { Name = "Bill", Age = 28 } } }, family);
            Assert.Equal(39, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].Name);
            Assert.Equal(21, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].Name);
            Assert.Equal(28, family.Parents[2].Age);
            Assert.Equal("Bill", family.Parents[2].Name);

            Assert.Equal("Helsinki", family.Address.City);
        }

        [Fact]
        public void CopyProperties_DynamicFamily()
        {
            dynamic family = new ExpandoObject();

            dynamic fParent = new ExpandoObject();
            fParent.FirstName = "Jim";
            fParent.Age = 52;

            dynamic sParent = new ExpandoObject();
            sParent.FirstName = "Theodor";
            sParent.Age = 14;

            family.Parents = new List<ExpandoObject>
            {
                fParent,
                sParent,
            };

            family.Address = new ExpandoObject();
            family.Address.City = "Helsinki";

            ObjectExtensions.CopyProperties(new { Parents = new[] { new { FirstName = "Ray", Age = 49 } } }, family);
            Assert.Equal(49, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].FirstName);
            Assert.Equal(14, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].FirstName);

            ObjectExtensions.CopyProperties(new { Parents = new[] { new { Age = 39 } } }, family);
            Assert.Equal(39, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].FirstName);
            Assert.Equal(14, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].FirstName);

            ObjectExtensions.CopyProperties(new { Parents = new[] { null, new { Age = 21 } } }, family);
            Assert.Equal(39, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].FirstName);
            Assert.Equal(21, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].FirstName);

            ObjectExtensions.CopyProperties(new { Parents = new[] { null, null, new { FirstName = "Bill", Age = 28 } } }, family);
            Assert.Equal(39, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].FirstName);
            Assert.Equal(21, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].FirstName);
            Assert.Equal(28, family.Parents[2].Age);
            Assert.Equal("Bill", family.Parents[2].FirstName);

            Assert.Equal("Helsinki", family.Address.City);
        }

        [Fact]
        public void CopyProperties_TypedArrayValueTypes()
        {
            var item = new PrivateOwner();
            item.FirstName = "Theodor";
            item.MyValues = new List<int> { 1, 2, 3 };

            ObjectExtensions.CopyProperties(new { MyValues = new List<int> { 4, 5, 6, 7 } }, item);
            Assert.Equal(4, item.MyValues[0]);
            Assert.Equal(7, item.MyValues[3]);
        }

        [Fact]
        public void CopyProperties_TypedExpandoSource()
        {
            var item = new PrivateOwner();
            item.FirstName = "Theodor";
            item.MyValues = new List<int> { };

            var source = new ExpandoObject();
            var items = source as IDictionary<string, object>;
            items.Add("FirstName", "Alter");
            items.Add("MyValues", new List<int> { 1, 2 });

            ObjectExtensions.CopyProperties(source, item);
            Assert.Equal(1, item.MyValues[0]);
            Assert.Equal(2, item.MyValues[1]);
        }

        [Fact]
        public void CopyProperties_DynamicExpandoSource()
        {
            dynamic item = new ExpandoObject();
            item.FirstName = "Theodor";
            item.MyValues = new List<int> { };

            var source = new ExpandoObject();
            var items = source as IDictionary<string, object>;
            items.Add("FirstName", "Alter");
            items.Add("MyValues", new List<int> { 1, 2 });

            ObjectExtensions.CopyProperties(source, item);
            Assert.Equal("Alter", item.FirstName);
            Assert.Equal(1, item.MyValues[0]);
            Assert.Equal(2, item.MyValues[1]);
        }

        [Fact]
        public void CopyProperties_DynamicArrayValueTypes()
        {
            dynamic item = new ExpandoObject();
            item.FirstName = "Theodor";
            item.MyValues = new List<int> { 1, 2, 3 };
            item.MyCollection = new Collection<int> { 1, 2, 3 };

            ObjectExtensions.CopyProperties(new { MyValues = new List<int> { 4, 5, 6, 7 }, MyCollection = new Collection<int> { 4 } }, item);
            Assert.Equal(4, item.MyValues[0]);
            Assert.Equal(7, item.MyValues[3]);
            Assert.Equal(4, item.MyCollection[0]);
        }

        [Fact]
        public void CopyProperties_TypedFamilyParents()
        {
            var family = new Family
            {
                Parents = new List<Parent>
                {
                    new Parent {  Name = "Jim", Age = 52 }
                },
                Address = new Address { City = "Helsinki" }
            };

            ObjectExtensions.CopyProperties(new { Age = 49 }, family.Parents[0]);
            Assert.Equal(49, family.Parents[0].Age);
            Assert.Equal("Helsinki", family.Address.City);
        }

        [Fact]
        public void CopyProperties_DynamicWithInnerExpandos()
        {
            dynamic work = new ExpandoObject();
            work.name = "EMACS";

            dynamic user = new ExpandoObject();
            user.name = "Timmy";
            user.age = 30;
            user.work = work;

            var patchData = new Dictionary<string, object>();
            patchData.Add("age", 41);
            patchData.Add("name", "James");
            patchData.Add("work", new Dictionary<string, object> { { "name", "ACME" } });

            var jobject = JObject.FromObject(patchData);
            dynamic patchExpando = JsonConvert.DeserializeObject<ExpandoObject>(jobject.ToString());

            ObjectExtensions.CopyProperties(patchExpando, user);
            Assert.Equal("James", user.name);
            Assert.Equal("ACME", user.work.name);
        }

        [Fact]
        public void CopyProperties_TypedWithInnerExpandos()
        {
            var user = new User
            {
                Name = "Timmy",
                Age = 30,
                Work = new WorkPlace { Name = "EMACS" }
            };

            var patchData = new Dictionary<string, object>();
            patchData.Add("Age", 41);
            patchData.Add("Name", "James");
            patchData.Add("Work", new Dictionary<string, object> { { "Name", "ACME" } });

            var jobject = JObject.FromObject(patchData);
            dynamic patchExpando = JsonConvert.DeserializeObject<ExpandoObject>(jobject.ToString());

            ObjectExtensions.CopyProperties(patchExpando, user);
            Assert.Equal("James", user.Name);
            Assert.Equal("ACME", user.Work.Name);
        }

        [Fact]
        public void CopyProperties_TypedWithDictionary()
        {
            var user = new PrivateOwner
            {
                FirstName = "Timmy",
                MyStrings = new Dictionary<string, string> { { "A", "ABBA" }, { "B", "ACDC" } },
                MyIntegers = new Dictionary<int, int> { { 1, 111 }, { 2, 222 } }
            };

            var patchData = new ExpandoObject();
            var items = patchData as IDictionary<string, object>;
            items.Add("MyStrings", new Dictionary<string, string> { { "C", "CEEC" }, });
            items.Add("MyIntegers", new Dictionary<int, int> { { 3, 333 } });

            ObjectExtensions.CopyProperties(patchData, user);
            Assert.Equal("CEEC", user.MyStrings["C"]);
            Assert.Equal(333, user.MyIntegers[3]);
        }

        [Fact]
        public void CopyProperties_DynamicithDictionary()
        {
            dynamic user = new ExpandoObject();
            user.FirstName = "Timmy";
            user.MyStrings = new Dictionary<string, string> { { "A", "ABBA" }, { "B", "ACDC" } };
            user.MyIntegers = new Dictionary<int, int> { { 1, 111 }, { 2, 222 } };

            var patchData = new ExpandoObject();
            var items = patchData as IDictionary<string, object>;
            items.Add("MyStrings", new Dictionary<string, string> { { "C", "CEEC" }, });
            items.Add("MyIntegers", new Dictionary<int, int> { { 3, 333 } });

            ObjectExtensions.CopyProperties(patchData, user);
            Assert.Equal("CEEC", user.MyStrings["C"]);
            Assert.Equal(333, user.MyIntegers[3]);
        }
    }
}