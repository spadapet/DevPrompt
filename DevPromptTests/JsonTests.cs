using DevPrompt.Api;
using DevPrompt.Utility.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace DevPromptTests
{
    [TestClass]
    public class JsonTests
    {
        [TestMethod]
        public void ParseException()
        {
            IJsonValue value = JsonParser.Parse(@"{ foo: bar }");
            Assert.IsTrue(value.IsException);
            Assert.IsInstanceOfType(value.Exception, typeof(JsonException));
        }

        [TestMethod]
        public void DictionaryEnum()
        {
            IJsonValue value = JsonParser.Parse(@"{ ""0"": 0, ""1"": 1, ""2"": 2, ""3"": 3, ""4"": 4 }");

            foreach (KeyValuePair<string, IJsonValue> pair in value.Dictionary)
            {
                Assert.IsTrue(int.TryParse(pair.Key, out int key));
                Assert.AreEqual(pair.Value.Int, key);
            }
        }

        [TestMethod]
        public void ArrayEnum()
        {
            IJsonValue value = JsonParser.Parse(@"{ ""array"": [ 0, 1, 2, 3, 4 ] }");

            int i = 0;
            foreach (IJsonValue child in value.Array)
            {
                Assert.AreEqual(child.Int, i++);
            }
        }

        [TestMethod]
        public void MissingLookup()
        {
            IJsonValue value = JsonParser.Parse(@"{ ""foo"": ""bar"" }");

            IJsonValue bar = value["bar"];
            Assert.IsTrue(bar.IsUnset);

            bar = bar["foo"];
            Assert.IsTrue(bar.IsUnset);

            bar = value[10];
            Assert.IsTrue(bar.IsUnset);

            bar = bar[10];
            Assert.IsTrue(bar.IsUnset);
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
    ""null"": null
}");

            IJsonValue stringValue = value["string"];
            IJsonValue intValue = value["int"];
            IJsonValue doubleValue = value["double"];
            IJsonValue boolValue = value["bool"];
            IJsonValue nullValue = value["null"];

            Assert.IsTrue(stringValue.IsString);
            Assert.IsTrue(intValue.IsInt);
            Assert.IsTrue(intValue.IsDouble);
            Assert.IsTrue(doubleValue.IsDouble);
            Assert.IsFalse(doubleValue.IsInt);
            Assert.IsTrue(boolValue.IsBool);
            Assert.IsTrue(nullValue.IsNull);
        }
    }
}
