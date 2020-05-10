using JsonPathway.Internal;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace JsonPathway.Tests
{
    public class ReadMeExamplesTests
    {
        private string _json = TestDataLoader.Store();

        [Fact]
        public void StringLength_ReturnsCorrectResult()
        {
            string path = "$.store.bicycle.color.length";
            var result = JsonPath.ExecutePath(path, _json);
            Assert.Equal(3, result.Single().GetInt32());
        }

        [Fact]
        public void Expression_UsedMultipleTimes_ReturnsSameResult()
        {
            string path = "$.store.bicycle.color.length";
            ExpressionList expression = JsonPathExpression.Parse(path);
            JsonDocument doc = JsonDocument.Parse(_json);

            JsonElement result1 = JsonPath.ExecutePath(expression, doc).Single();
            JsonElement result2 = JsonPath.ExecutePath(expression, doc).Single();
            Assert.Equal(result1, result2, JsonElementEqualityComparer.Default);
        }
    }
}
