using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;



namespace SInnovations.JsonDiff.UnitTests
{

    [TestClass]
    public class UnitTest1
    {

        [TestMethod]
        public void SimpleBooleanPropertyShouldChangeFromFalseToTrue()
        {
           
            var left = JToken.Parse(@"{ ""key"": false }");
            var right = JToken.Parse(@"{ ""key"": true }");

            var operations = JsonDiff.JsonDiffExtensions.Diff(left, right).ToArray();

            operations.Count().Should().Be(1);

            operations.First().Value.ToObject<bool>().Should().Be(true);
            operations.First().From.ToObject<bool>().Should().Be(false);
           
            Console.WriteLine(JToken.FromObject(operations.First()));
        }
    }
}
