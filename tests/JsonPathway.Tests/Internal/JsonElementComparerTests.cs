using JsonPathway.Internal;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace JsonPathway.Tests.Internal
{
    public class JsonElementComparerTests
    {
        [Fact]
        public void SameNumbers_ReturnsTrue()
        {
            JsonElement n1 = JsonElementFactory.CreateNumber(1.123);
            JsonElement n2 = JsonElementFactory.CreateNumber(1.123);
            bool result = JsonElementEqualityComparer.Default.Equals(n1, n2);
            Assert.True(result);
        }

        [Fact]
        public void DifferentNumbers_ReturnsFalse()
        {
            JsonElement n1 = JsonElementFactory.CreateNumber(1.123);
            JsonElement n2 = JsonElementFactory.CreateNumber(1.121);
            bool result = JsonElementEqualityComparer.Default.Equals(n1, n2);
            Assert.False(result);
        }

        [Fact]
        public void SameStrings_ReturnsTrue()
        {
            JsonElement n1 = JsonElementFactory.CreateString("abc");
            JsonElement n2 = JsonElementFactory.CreateString("abc");
            bool result = JsonElementEqualityComparer.Default.Equals(n1, n2);
            Assert.True(result);
        }

        [Fact]
        public void DifferentStrings_ReturnsFalse()
        {
            JsonElement n1 = JsonElementFactory.CreateString("a");
            JsonElement n2 = JsonElementFactory.CreateString(" a");
            bool result = JsonElementEqualityComparer.Default.Equals(n1, n2);
            Assert.False(result);
        }

        [Fact]
        public void SameArrays_ReturnsTrue()
        {
            JsonElement n1 = JsonElementFactory.CreateArray(new List<object> { 1, "abc", false });
            JsonElement n2 = JsonElementFactory.CreateArray(new List<object> { 1, "abc", false });
            bool result = JsonElementEqualityComparer.Default.Equals(n1, n2);
            Assert.True(result);
        }

        [Fact]
        public void DifferentArrays_ReturnsFalse()
        {
            JsonElement n1 = JsonElementFactory.CreateArray(new List<object> { 1, "abc", false });
            JsonElement n2 = JsonElementFactory.CreateArray(new List<object> { 1, "abc", 3 });
            bool result = JsonElementEqualityComparer.Default.Equals(n1, n2);
            Assert.False(result);
        }

        [Fact]
        public void DifferentTypes_ReturnsFalse()
        {
            JsonElement n1 = JsonElementFactory.CreateBool(true);
            JsonElement n2 = JsonElementFactory.CreateArray(new List<object> { 1, "abc", 3 });
            bool result = JsonElementEqualityComparer.Default.Equals(n1, n2);
            Assert.False(result);
        }

        [Fact]
        public void SameObjects_ReturnsTrue()
        {
            JsonElement n1 = JsonDocument.Parse("{ \"a\": 3 }").RootElement;
            JsonElement n2 = JsonDocument.Parse("{ \"a\": 3 }").RootElement;
            bool result = JsonElementEqualityComparer.Default.Equals(n1, n2);
            Assert.True(result);
        }

        [Fact]
        public void DifferentObjects_ReturnsTrue()
        {
            JsonElement n1 = JsonDocument.Parse("{ \"a\": 3 }").RootElement;
            JsonElement n2 = JsonDocument.Parse("{ \"a\": 1 }").RootElement;
            bool result = JsonElementEqualityComparer.Default.Equals(n1, n2);
            Assert.False(result);
        }

        [Fact]
        public void SameBools_ReturnsTrue()
        {
            JsonElement n1 = JsonElementFactory.CreateBool(true);
            JsonElement n2 = JsonElementFactory.CreateBool(true);
            bool result = JsonElementEqualityComparer.Default.Equals(n1, n2);
            Assert.True(result);
        }

        [Fact]
        public void DifferentBools_ReturnsTrue()
        {
            JsonElement n1 = JsonElementFactory.CreateBool(true);
            JsonElement n2 = JsonElementFactory.CreateBool(false);
            bool result = JsonElementEqualityComparer.Default.Equals(n1, n2);
            Assert.False(result);
        }
    }
}
