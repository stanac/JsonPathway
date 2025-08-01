﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace JsonPathway.Tests
{
    public class JsonPathFilterTests
    {
        private readonly IReadOnlyDictionary<string, string> _bookJsons;

        public JsonPathFilterTests()
        {
            string bookSayingsOfTheCentury = "{ `category`: `reference`, `author`: `Nigel Rees`, `title`: `Sayings of the Century`, `price`: 8.95 }"
                .Replace("`", "\"").RemoveWhiteSpace();

            string bookSwordOfHonour = "{ `category`: `fiction`, `author`: `Evelyn Waugh`, `title`: `Sword of Honour`, `price`: 12.99 }"
                .Replace("`", "\"").RemoveWhiteSpace();

            string bookMobiDick = "{ `category`: `fiction`, `author`: `Herman Melville`, `title`: `Moby Dick`, `isbn`: `0-553-21311-3`, `price`: 8.99 }"
                .Replace("`", "\"").RemoveWhiteSpace();
                
            string bookTheLordOfTheRings = "{ `category`: `fiction`, `author`: `J. R. R. Tolkien`, `title`: `The Lord of the Rings`, `isbn`: `0-395-19395-8`, `price`: 22.99 }"
                .Replace("`", "\"").RemoveWhiteSpace();

            _bookJsons = new Dictionary<string, string>
            {
                { "sotc", bookSayingsOfTheCentury },
                { "soh", bookSwordOfHonour },
                { "md", bookMobiDick },
                { "lotr", bookTheLordOfTheRings }
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

            foreach (JsonElement r in result)
            {
                Assert.Equal(JsonValueKind.String, r.ValueKind);
                string rString = r.GetString();
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
            // ReSharper disable once CompareOfFloatsByEqualityOperator
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
            ExpressionList expression = ExpressionList.TokenizeAndParse(path);
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

        [Fact]
        public void EqualFilterOnDecimalWithInt_ReturnsCorrectResult()
        {
            string input = @"
                {
                    ""object"": {
                        ""arrays"": [ { ""amount"": 10.0 } ]
                    }
                }
            ";

            ExpressionList expression = ExpressionList.TokenizeAndParse("$.object.arrays[?(@.amount == 10)]");

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(expression, input);
            Assert.Single(result);
        }

        [Fact]
        public void EqualFilterOnIntWithDecimal_ReturnsCorrectResult()
        {
            string input = @"
                {
                    ""object"": {
                        ""arrays"": [ { ""amount"": 10 } ]
                    }
                }
            ";

            ExpressionList expression = ExpressionList.TokenizeAndParse("$.object.arrays[?(@.amount == 10.0)]");

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(expression, input);
            Assert.Single(result);
        }

        [Fact]
        public void GreaterThanOrEqualFilterOnDecimalWithInt_ReturnsCorrectResult()
        {
            string input = @"
                {
                    ""object"": {
                        ""arrays"": [ { ""amount"": 10.0 } ]
                    }
                }
            ";

            ExpressionList expression = ExpressionList.TokenizeAndParse("$.object.arrays[?(@.amount >= 10)]");

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(expression, input);
            Assert.Single(result);
        }

        [Fact]
        public void GreaterThanOrEqualFilterOnIntWithDecimal_ReturnsCorrectResult()
        {
            string input = @"
                {
                    ""object"": {
                        ""arrays"": [ { ""amount"": 10 } ]
                    }
                }
            ";

            ExpressionList expression = ExpressionList.TokenizeAndParse("$.object.arrays[?(@.amount >= 10.0)]");

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(expression, input);
            Assert.Single(result);
        }

        [Fact]
        public void LessThanOrEqualFilterOnDecimalWithInt_ReturnsCorrectResult()
        {
            string input = @"
                {
                    ""object"": {
                        ""arrays"": [ { ""amount"": 10.0 } ]
                    }
                }
            ";

            ExpressionList expression = ExpressionList.TokenizeAndParse("$.object.arrays[?(@.amount >= 10)]");

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(expression, input);
            Assert.Single(result);
        }

        [Fact]
        public void LessThanOrEqualFilterOnIntWithDecimal_ReturnsCorrectResult()
        {
            string input = @"
                {
                    ""object"": {
                        ""arrays"": [ { ""amount"": 10 } ]
                    }
                }
            ";

            ExpressionList expression = ExpressionList.TokenizeAndParse("$.object.arrays[?(@.amount >= 10.0)]");

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(expression, input);
            Assert.Single(result);
        }

        [Fact]
        public void FilterOnStringEqualNull_ReturnsCorrectResult()
        {
            string input = TestDataLoader.BooksWithNulls();

            ExpressionList expression = ExpressionList.TokenizeAndParse("$.books[?(@.category == null)]");

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(expression, input);

            Assert.Single(result);
            Assert.Equal(JsonValueKind.Object, result[0].ValueKind);
            Assert.Equal("Sword of Honour", result[0].EnumerateObject().FirstOrDefault(x => x.Name == "title").Value.GetString());
        }

        [Fact]
        public void FilterOnStringNotEqualNull_ReturnsCorrectResult()
        {
            string input = TestDataLoader.BooksWithNulls();

            ExpressionList expression = ExpressionList.TokenizeAndParse("$.books[?(@.category != null)]");

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(expression, input);

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void FilterOnNumberEqualNull_ReturnsCorrectResult()
        {
            string input = TestDataLoader.BooksWithNulls();

            ExpressionList expression = ExpressionList.TokenizeAndParse("$.books[?(@.price== null)]");

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(expression, input);

            Assert.Single(result);
            Assert.Equal(JsonValueKind.Object, result[0].ValueKind);
            Assert.Equal("Sayings of the Century", result[0].EnumerateObject().FirstOrDefault(x => x.Name == "title").Value.GetString());
        }

        [Fact]
        public void FilterOnNumberNotEqualNull_ReturnsCorrectResult()
        {
            string input = TestDataLoader.BooksWithNulls();

            ExpressionList expression = ExpressionList.TokenizeAndParse("$.books[?(@.price !=null)]");

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(expression, input);

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void FilterOnStringEqualNull_WhereNoPropertyIsNull_ReturnsCorrectResult()
        {
            string input = TestDataLoader.BooksWithNulls();

            ExpressionList expression = ExpressionList.TokenizeAndParse("$.books[?(@.title == null)]");

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(expression, input);

            Assert.Empty(result);
        }

        [Fact]
        public void FilterOnStringNotEqualNull_WhereNoPropertyIsNull_ReturnsCorrectResult()
        {
            string input = TestDataLoader.BooksWithNulls();

            ExpressionList expression = ExpressionList.TokenizeAndParse("$.books[?(@.title != null)]");

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(expression, input);

            Assert.Equal(4, result.Count);
        }

        [Fact]
        public void FilterOnNonExistingPropertyEqualNull_ReturnsCorrectResult()
        {
            string input = TestDataLoader.BooksWithNulls();

            ExpressionList expression = ExpressionList.TokenizeAndParse("$.books[?(@.plumbus == null)]");

            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(expression, input);

            Assert.Equal(4, result.Count);
        }
    }
}
