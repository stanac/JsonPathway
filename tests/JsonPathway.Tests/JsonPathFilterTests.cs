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

        [Theory]
        [InlineData("$..author", "Nigel Rees", "Evelyn Waugh", "Herman Melville", "J. R. R. Tolkien")]
        [InlineData("$.store.book[*].author", "Nigel Rees", "Evelyn Waugh", "Herman Melville", "J. R. R. Tolkien")]
        public void RecursiveFilterAndWildcardArrayFilter_ReturnsCorrectResult(string path, params string[] expected)
        {
            string input = TestDataLoader.Store();
            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(path, input);

            Assert.Equal(expected.Length, result.Count);

            foreach (var r in result)
            {
                Assert.Equal(JsonValueKind.String, r.ValueKind);
                var rString = r.GetString();
                Assert.Contains(rString, expected);
            }
        }

        [Fact]
        public void FilterWithWildcardPropertyFilter_ReturnsCorrectResult()
        {
            string path = "$.store.bicycle.*";
            string input = TestDataLoader.Store();
            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(path, input);

            Assert.Equal(2, result.Count);

            Assert.Contains(result, x => x.ValueKind == JsonValueKind.String && x.GetString() == "red");
            Assert.Contains(result, x => x.ValueKind == JsonValueKind.Number && x.GetDouble() == 19.95);

            path = "store.*";
            result = JsonPath.ExecutePath(path, input);

            string resultJson = JsonSerializer.Serialize(result).RemoveWhiteSpace();

            string expected = @"
                [
                  [
                    {
                      `category`: `reference`,
                      `author`: `Nigel Rees`,
                      `title`: `Sayings of the Century`,
                      `price`: 8.95
                    },
                    {
                      `category`: `fiction`,
                      `author`: `Evelyn Waugh`,
                      `title`: `Sword of Honour`,
                      `price`: 12.99
                    },
                    {
                      `category`: `fiction`,
                      `author`: `Herman Melville`,
                      `title`: `Moby Dick`,
                      `isbn`: `0-553-21311-3`,
                      `price`: 8.99
                    },
                    {
                      `category`: `fiction`,
                      `author`: `J. R. R. Tolkien`,
                      `title`: `The Lord of the Rings`,
                      `isbn`: `0-395-19395-8`,
                      `price`: 22.99
                    }
                  ],
                  {
                    `color`: `red`,
                    `price`: 19.95
                  }
                ]"
                .Replace("`", "\"").RemoveWhiteSpace();

            Assert.Equal(expected, resultJson);
        }

        [Theory]
        [InlineData("$.store.books[?(@.price > 10)]", "soh", "lotr")]
        [InlineData("$.store.books[?(@.price >= 8.99)]", "soh", "lotr", "md")]
        [InlineData("$.store.books[?(@.price > 8.99)]", "soh", "lotr")]
        [InlineData("$.store.books[?(@.price > 22.99)]")]
        [InlineData("$.store.books[?(@.price >= 22.99)]", "lotr")]
        [InlineData("$.store.books[?(@.price == 8.95)]", "sotc")]
        [InlineData("$.store.books[?(@.price != 8.95)]", "lotr", "soh", "md")]
        [InlineData("$.store.books[?(@.price < 9)]", "sotc", "md")]
        [InlineData("$.store.books[?(@.price <= 8.99)]", "sotc", "md")]
        [InlineData("$.store.books[?(@.price < 8.99)]", "sotc")]
        [InlineData("$.store.books[?(@.isbn)]", "lotr", "md")]
        public void FilterOnBooksObject_ReturnsCorrectResult(string path, params string[] expected)
        {
            string input = TestDataLoader.BooksObject();
            var expression = ExpressionList.TokenizeAndParse(path);
            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(expression, input);

            Assert.Equal(expected.Length, result.Count);
            string[] resultJsons = result.Select(x => JsonSerializer.Serialize(x)).Select(x => x.RemoveWhiteSpace()).ToArray();

            foreach (string e in expected)
            {
                Assert.Contains(_bookJsons[e], resultJsons);
            }
        }

        [Fact]
        public void UnrecognizedMethodName_ThrowsException()
        {
            string path = "$.store.book[?(@.price.nonExistingMethodName() > 10)]";
            string input = TestDataLoader.Store();

            Assert.Throws<UnrecognizedMethodNameException>(() => JsonPath.ExecutePath(path, input));
        }
    }
}
