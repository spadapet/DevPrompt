﻿using DevPrompt.Utility.Json;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace DevPrompt.Tests
{
    [TestClass]
    public class JsonDynamicTests
    {
        [TestMethod]
        public void DictionaryEnum()
        {
            dynamic value = JsonParser.ParseAsDynamic(@"{ ""0"": 0, ""1"": 1, ""2"": 2, ""3"": 3, ""4"": 4 }");

            foreach (KeyValuePair<string, dynamic> pair in value)
            {
                int i = pair.Value;
                Assert.AreEqual(i, int.Parse(pair.Key));
                Assert.AreSame(pair.Value, value[pair.Key]);
            }
        }

        [TestMethod]
        public void ArrayEnum()
        {
            dynamic value = JsonParser.ParseAsDynamic(@"{ ""array"": [ 0, 1, 2, 3, 4 ] }");
            dynamic[] array = value.array;

            int i = 0;
            foreach (int child in array)
            {
                Assert.AreEqual(child, i++);
            }

            for (i = 0; i < array.Length; i++)
            {
                int h = array[i];
                Assert.AreEqual(h, i);
            }
        }

        [TestMethod]
        public void ReuseJsonValue()
        {
            dynamic value = JsonParser.ParseAsDynamic(
@"{
    ""a"": [ 32, ""foo"", true, null ],
    ""b"": [ 32, ""foo"", true, null ]
}");

            dynamic[] array1 = value.a;
            dynamic[] array2 = value.b;

            Assert.AreEqual(array1.Length, array2.Length);

            for (int i = 0; i < array1.Length; i++)
            {
                Assert.ReferenceEquals(array1[i], array2[i]);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(RuntimeBinderException))]
        public void MissingLookup()
        {
            dynamic value = JsonParser.ParseAsDynamic(@"{ ""foo"": ""bar"" }");
            _ = value.bar;
        }

        [TestMethod]
        [ExpectedException(typeof(RuntimeBinderException))]
        public void MissingIndex()
        {
            dynamic value = JsonParser.ParseAsDynamic(@"{ ""foo"": [ 0, 1 ] }");
            _ = value.foo[10];
        }

        [TestMethod]
        public void SimpleLookupTypes()
        {
            dynamic value = JsonParser.ParseAsDynamic(
@"{
    ""string"": ""bar"",
    ""int"": 32,
    ""double"": 32.5,
    ""bool"": true,
    ""null"": null,
    ""array"": [ 0, 1, 2 ],
    ""dict"": { ""array"": [ 0, 1, 2 ] },
}");

            string stringValue = value.@string;
            int intValue = value.@int;
            double doubleValue = value.@double;
            bool boolValue = value.@bool;
            object nullValue = value.@null;
            dynamic[] arrayValue = value.array;
            IDictionary<string, dynamic> dictValue = value.dict;

            Assert.AreEqual("bar", stringValue);
            Assert.AreEqual(32, intValue);
            Assert.AreEqual(32.5, doubleValue);
            Assert.AreEqual(true, boolValue);
            Assert.AreEqual(null, nullValue);
            Assert.AreEqual(3, arrayValue.Length);
            Assert.AreEqual(1, dictValue.Count);
        }
    }
}
