using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace JsonPathway.Tests
{
    public class JsonPathFilterTests
    {
        private readonly IReadOnlyDictionary<string, string> _bookJsons;

        private readonly string _bookSayingsOfTheCentury;
        private readonly string _bookSwordOfHonour;
        private readonly string _bookMobiDick;
        private readonly string _bookTheLordOfTheRings;

        public JsonPathFilterTests()
        {
            _bookSayingsOfTheCentury = "{ `category`: `reference`, `author`: `Nigel Rees`, `title`: `Sayings of the Century`, `price`: 8.95 }"
                .Replace("`", "\"").RemoveWhiteSpace();

            _bookSwordOfHonour = "{ `category`: `fiction`, `author`: `Evelyn Waugh`, `title`: `Sword of Honour`, `price`: 12.99 }"
                .Replace("`", "\"").RemoveWhiteSpace();

            _bookMobiDick = "{ `category`: `fiction`, `author`: `Herman Melville`, `title`: `Moby Dick`, `isbn`: `0-553-21311-3`, `price`: 8.99 }"
                .Replace("`", "\"").RemoveWhiteSpace();
                
            _bookTheLordOfTheRings = "{ `category`: `fiction`, `author`: `J. R. R. Tolkien`, `title`: `The Lord of the Rings`, `isbn`: `0-395-19395-8`, `price`: 22.99 }"
                .Replace("`", "\"").RemoveWhiteSpace();

            _bookJsons = new Dictionary<string, string>
            {
                { "sotc", _bookSayingsOfTheCentury },
                { "soh", _bookSwordOfHonour },
                { "md", _bookMobiDick },
                { "lotr", _bookTheLordOfTheRings }
            };
        }

        [Theory]
        [InlineData("$.store.book[?(@.price > 10)]", "soh", "lotr")]
        [InlineData("$.store.book[?(@.price >= 8.99)]", "soh", "lotr", "md")]
        [InlineData("$.store.book[?(@.price > 8.99)]", "soh", "lotr")]
        [InlineData("$.store.book[?(@.price > 22.99)]")]
        [InlineData("$.store.book[?(@.price >= 22.99)]", "lotr")]
        [InlineData("$.store.book[?(@.price == 8.95)]", "sotc")]
        [InlineData("$.store.book[?(@.price != 8.95)]", "lotr", "soh", "md")]
        [InlineData("$.store.book[?(@.price < 9)]", "sotc", "md")]
        [InlineData("$.store.book[?(@.price <= 8.99)]", "sotc", "md")]
        [InlineData("$.store.book[?(@.price < 8.99)]", "sotc")]
        [InlineData("$.store.book[?(@.isbn)]", "lotr", "md")]
        public void FilterOnBooks_ReturnsCorrectResult(string path, params string[] expected)
        {
            string input = TestDataLoader.Store();
            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(path, input);

            Assert.Equal(expected.Length, result.Count);
            string[] resultJsons = result.Select(x => JsonSerializer.Serialize(x)).Select(x => x.RemoveWhiteSpace()).ToArray();

            foreach (string e in expected)
            {
                Assert.Contains(_bookJsons[e], resultJsons);
            }
        }
    }
}
