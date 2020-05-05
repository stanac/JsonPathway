using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace JsonPathway.Tests
{
    public class JsonPathArrayTests
    {
        [Theory]
        [InlineData("[0][0]", "[`a`]")]
        [InlineData("[1][0]", "[`b`]")]
        [InlineData("[1]", "[`b`]")]
        [InlineData("[-1]", "[`i`]")]
        [InlineData("[-2]", "[`h`]")]
        [InlineData("$[0][0]", "[`a`]")]
        [InlineData("$[1][0]", "[`b`]")]
        [InlineData("$[1]", "[`b`]")]
        [InlineData("$[-1]", "[`i`]")]
        [InlineData("$[-2]", "[`h`]")]
        public void PathWithIndex_ReturnsValidResult(string path, string expectedJson)
        {
            expectedJson = expectedJson.Replace("`", "\"");
            path = path.Replace("`", "\"");

            string json = TestDataLoader.AbcArray();

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(path, json);
            var resultJson = JsonSerializer.Serialize(result);

            Assert.Equal(expectedJson, resultJson);
        }

        [Fact]
        public void PathWithAllElements_ReturnsExpectedResult()
        {
            string path = "[*]";
            string json = TestDataLoader.AbcArray();

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(path, json);
            var resultJson = JsonSerializer.Serialize(result);

            json = new string(json.Where(x => !char.IsWhiteSpace(x)).ToArray());

            Assert.Equal(json, resultJson);
        }

        [Theory]
        [InlineData("[0:20:3]", "[`a`,`d`,`g`]")]
        [InlineData("[-1:]", "[`i`]")]
        [InlineData("[0:-1]", "[`a`,`b`,`c`,`d`,`e`,`f`,`g`,`h`]")]
        [InlineData("[2:-1]", "[`c`,`d`,`e`,`f`,`g`,`h`]")]
        public void PathWithSlice_ReturnsExpectedResult(string path, string expectedJson)
        {
            expectedJson = expectedJson.Replace("`", "\"");
            string json = TestDataLoader.AbcArray();

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(path, json);
            var resultJson = JsonSerializer.Serialize(result);

            Assert.Equal(expectedJson, resultJson);
        }
    }
}
