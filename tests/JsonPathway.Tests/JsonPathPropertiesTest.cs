using JsonPathway.Tests.TestData;
using System.Text.Json;
using Xunit;

namespace JsonPathway.Tests
{
    public class JsonPathPropertiesTest
    {
        [Theory]
		[ClassData(typeof(PropertiesDataSource))]
        public void AllProperties_ReturnsValidResult(string path, string expected)
        {
            string json = TestDataLoader.Store();

            var result = JsonPath.ExecutePath(path, json);
            var resultJson = JsonSerializer.Serialize(result).RemoveWhiteSpace();

			Assert.Equal(expected, resultJson);
		}
    }
}
