using DevPrompt.Utility;
using DevPrompt.Utility.Json;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DevPrompt.Tests
{
    [TestClass]
    public class JsonConvertTests
    {
        [TestMethod]
        public void ToDateSuccess()
        {
            DateTime date = new DateTime(1970, 7, 4, 12, 0, 30, 500, DateTimeKind.Local);
            dynamic value = JsonParser.ParseAsDynamic($@"{{ ""date"": ""{date.ToString("O", CultureInfo.InvariantCulture)}"" }}");
            DateTime parsedDate = value.date;

            Assert.AreEqual(date, parsedDate);
        }

        [TestMethod]
        [ExpectedException(typeof(RuntimeBinderException))]
        public void ToDateFailed()
        {
            DateTime date = new DateTime(1970, 7, 4, 12, 0, 30, 500, DateTimeKind.Local);
            dynamic value = JsonParser.ParseAsDynamic($@"{{ ""date"": ""Foo{date.ToString("O", CultureInfo.InvariantCulture)}"" }}");
            DateTime _ = value.date;
        }

        [TestMethod]
        public void ConvertToCult()
        {
            Cult cult = JsonParser.ParseAsType<Cult>(
@"{
    'Name': 'Amazing Test Cult',
    'Active': true,
    'Leader': { 'name': 'Cult Leader', 'born': '7/4/1972' },
    'Followers':
    [
        { 'name': 'Follower 1', 'born': '1/2/1979' },
        { 'name': 'Follower 2', 'born': '3/4/1980' },
        { 'name': 'Follower 3', 'born': '5/6/1981' }
    ]
}".Replace('\'', '\"'));

            Assert.AreEqual("Amazing Test Cult", cult.Name);
            Assert.AreEqual(true, cult.Active);
            Assert.AreEqual(3, cult.Followers.Count);

            Assert.AreEqual(new Person("Cult Leader", DateTime.Parse("7/4/1972")), cult.Leader);
            Assert.AreEqual(new Person("Follower 1", DateTime.Parse("1/2/1979")), cult.Followers[0]);
            Assert.AreEqual(new Person("Follower 2", DateTime.Parse("3/4/1980")), cult.Followers[1]);
            Assert.AreEqual(new Person("Follower 3", DateTime.Parse("5/6/1981")), cult.Followers[2]);
        }

        private struct Person
        {
            public string name;
            public DateTime born;

            public Person(string name, DateTime born)
            {
                this.name = name;
                this.born = born;
            }

            public override bool Equals(object obj)
            {
                return obj is Person other && this.name == other.name && this.born == other.born;
            }

            public override int GetHashCode()
            {
                return HashUtility.CombineHashCodes(this.name.GetHashCode(), this.born.GetHashCode());
            }
        }

        private class Cult
        {
            public Person Leader { get; set; }
            public IList<Person> Followers { get; } = new List<Person>();
            public bool Active { get; set; }
            public string Name { get; set; }
        }
    }
}
