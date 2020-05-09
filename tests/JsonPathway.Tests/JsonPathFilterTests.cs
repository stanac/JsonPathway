using Xunit;

namespace JsonPathway.Tests
{
    public class JsonPathFilterTests
    {
        [Fact]
        public void Test()
        {
            string input = TestDataLoader.Store();
            string path = "$.store.book[?(@.price > 10)]";

            JsonPath.ExecutePath(path, input);
        }
    }
}
