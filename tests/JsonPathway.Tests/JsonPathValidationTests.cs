using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace JsonPathway.Tests
{
    public class JsonPathValidationTests
    {
        [Theory]
        [InlineData("$")]
        [InlineData("$.a.b")]
        [InlineData("$['a'][\"b\"]")]
        [InlineData("$[?(@.a)]")]
        [InlineData("$[-11:-3:44]")]
        public void ValidPath_ReturnsTrue(string path)
        {
            bool valid = JsonPath.IsPathValid(path, out string error);
            Assert.True(valid);
            Assert.Null(error);
        }

        [Theory]
        [InlineData("$.")]
        [InlineData("$...")]
        [InlineData("$.a[]")]
        [InlineData("$['a'],[\"b\"]")]
        [InlineData("$.a/")]
        [InlineData("$.a+")]
        public void NotValidPath_ReturnsFalse(string path)
        {
            bool valid = JsonPath.IsPathValid(path, out string error);
            Assert.False(valid);
            Assert.NotNull(error);
        }
    }
}
