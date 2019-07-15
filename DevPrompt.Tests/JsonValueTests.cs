using DevPrompt.Api;
using DevPrompt.Utility.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace DevPrompt.Tests
{
    [TestClass]
    public class JsonValueTests
    {
        [TestMethod]
        [ExpectedException(typeof(JsonException))]
        public void ParseException()
        {
            JsonParser.Parse(@"{ foo: bar }");
        }

        [TestMethod]
        public void DictionaryEnum()
        {
            IJsonValue value = JsonParser.Parse(@"{ ""0"": 0, ""1"": 1, ""2"": 2, ""3"": 3, ""4"": 4 }");

            foreach (KeyValuePair<string, IJsonValue> pair in value.Dictionary)
            {
                Assert.AreEqual(pair.Value.Int, int.Parse(pair.Key));
            }

            foreach (string key in value.Dictionary.Keys)
            {
                Assert.IsTrue(value.Dictionary.ContainsKey(key));
                Assert.AreEqual(value[key].Int, int.Parse(key));
            }
        }

        [TestMethod]
        public void ArrayEnum()
        {
            IJsonValue value = JsonParser.Parse(@"{ ""array"": [ 0, 1, 2, 3, 4 ] }");
            Assert.IsTrue(value.IsDictionary);

            IJsonValue array = value["array"];
            Assert.IsTrue(array.IsArray);

            int i = 0;
            foreach (IJsonValue child in array.Array)
            {
                Assert.AreEqual(child.Int, i++);
            }

            for (i = 0; i < array.Array.Count; i++)
            {
                Assert.AreEqual(array[i].Int, i);
            }

            Assert.IsFalse(array[i].IsValid);
        }

        [TestMethod]
        public void MissingLookup()
        {
            IJsonValue value = JsonParser.Parse(@"{ ""foo"": ""bar"" }");

            IJsonValue bar = value["bar"];
            Assert.IsTrue(!bar.IsValid);

            bar = bar["foo"];
            Assert.IsTrue(!bar.IsValid);

            bar = value[10];
            Assert.IsTrue(!bar.IsValid);

            bar = bar[10];
            Assert.IsTrue(!bar.IsValid);
        }

        [TestMethod]
        public void SimpleLookupTypes()
        {
            IJsonValue value = JsonParser.Parse(
@"{
    ""string"": ""bar"",
    ""int"": 32,
    ""double"": 32.5,
    ""bool"": true,
    ""null"": null,
    ""array"": [ 0, 1, 2 ],
    ""dict"": { ""array"": [ 0, 1, 2 ] },
}");

            IJsonValue stringValue = value["string"];
            IJsonValue intValue = value["int"];
            IJsonValue doubleValue = value["double"];
            IJsonValue boolValue = value["bool"];
            IJsonValue nullValue = value["null"];
            IJsonValue arrayValue = value["array"];
            IJsonValue dictValue = value["dict"];

            Assert.IsTrue(stringValue.IsString);
            Assert.IsTrue(intValue.IsInt);
            Assert.IsTrue(intValue.IsDouble);
            Assert.IsTrue(doubleValue.IsDouble);
            Assert.IsFalse(doubleValue.IsInt);
            Assert.IsTrue(boolValue.IsBool);
            Assert.IsTrue(nullValue.IsNull);
            Assert.IsTrue(arrayValue.IsArray);
            Assert.IsTrue(dictValue.IsDictionary);

            IJsonValue nestedIntValue = value["array"][1];
            IJsonValue nestedArrayValue = value["dict"]["array"];

            Assert.IsTrue(nestedIntValue.IsInt);
            Assert.AreEqual(1, nestedIntValue.Int);
            Assert.IsTrue(nestedArrayValue.IsArray);
            CollectionAssert.AreEqual(arrayValue.Array.ToList(), nestedArrayValue.Array.ToList());
        }
    }
}
