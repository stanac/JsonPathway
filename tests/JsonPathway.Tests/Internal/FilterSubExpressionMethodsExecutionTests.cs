using JsonPathway.Internal.Filters;
using System.Text.Json;
using Xunit;

namespace JsonPathway.Tests.Internal
{
    public class FilterSubExpressionMethodsExecutionTests
    {
        private readonly JsonElement _stringArrayJson;
        private readonly JsonElement _numberArrayJson;
        private readonly JsonElement _stringJson;
        private readonly JsonElement _stringNameJson;

        public FilterSubExpressionMethodsExecutionTests()
        {
            _stringArrayJson = JsonDocument.Parse(@"
                {
                    ""array"": [""abc"", ""XYZ""]
                }
                ").RootElement;

            _numberArrayJson = JsonDocument.Parse(@"
                {
                    ""array"": [123, -44, 1.48]
                }
                ").RootElement;

            _stringJson = JsonDocument.Parse(
                "{ \"test\": \"abc\" }"
                ).RootElement;

            _stringNameJson = JsonDocument.Parse(
                "{ \"name\": \"Kovalski\" }"
                ).RootElement;
        }

        [Theory]
        [InlineData("abc", true)]
        [InlineData("ABC", false)]
        [InlineData("abC", false)]
        [InlineData("abc ", false)]
        [InlineData("xyz", false)]
        [InlineData("XYZ", true)]
        public void StringArrayContains_ReturnsCorrectResult(string value, bool expected)
        {
            string path = $"@.array.contains('{value}')";
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize(path));
            Assert.IsType<MethodCallFilterSubExpression>(exp);

            JsonElement result = exp.Execute(_stringArrayJson);

            JsonValueKind expectedResult = expected ? JsonValueKind.True : JsonValueKind.False;
            Assert.Equal(expectedResult, result.ValueKind);
        }

        [Theory]
        [InlineData("123", true)]
        [InlineData("-123", false)]
        [InlineData("44", false)]
        [InlineData("-44", true)]
        [InlineData("0", false)]
        [InlineData("1.48", true)]
        [InlineData("1.488", false)]
        public void NumberArrayContains_ReturnsCorrectResult(string value, bool expected)
        {
            string path = $"@.array.contains({value})";
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize(path));
            Assert.IsType<MethodCallFilterSubExpression>(exp);

            JsonElement result = exp.Execute(_numberArrayJson);

            JsonValueKind expectedResult = expected ? JsonValueKind.True : JsonValueKind.False;
            Assert.Equal(expectedResult, result.ValueKind);
        }

        [Fact]
        public void NumberArrayContainsString_ReturnsFalse()
        {
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.array.contains('123')"));
            Assert.IsType<MethodCallFilterSubExpression>(exp);

            JsonElement result = exp.Execute(_numberArrayJson);
            Assert.Equal(JsonValueKind.False, result.ValueKind);
        }


        [Fact]
        public void StringArrayContainsNumber_ReturnsFalse()
        {
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.array.contains(1)"));
            Assert.IsType<MethodCallFilterSubExpression>(exp);

            JsonElement result = exp.Execute(_stringArrayJson);
            Assert.Equal(JsonValueKind.False, result.ValueKind);
        }

        [Theory]
        [InlineData("abc", true)]
        [InlineData("ab", true)]
        [InlineData("a", true)]
        [InlineData("b", true)]
        [InlineData("bc", true)]
        [InlineData("c", true)]
        [InlineData("z", false)]
        [InlineData("ABC", false)]
        [InlineData("A", false)]
        [InlineData("az", false)]
        public void StringContains_ReturnsCorrectResult(string value, bool expected)
        {
            string path = $"@.test.contains('{value}')";
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize(path));
            Assert.IsType<MethodCallFilterSubExpression>(exp);

            JsonElement result = exp.Execute(_stringJson);
            JsonValueKind expectedResult = expected ? JsonValueKind.True : JsonValueKind.False;
            Assert.Equal(expectedResult, result.ValueKind);
        }

        [Theory]
        [InlineData("abc", false, true)]
        [InlineData("abc", true, true)]
        [InlineData("ABC", true, true)]
        [InlineData("ABC", false, false)]
        [InlineData("ab", false, true)]
        [InlineData("ab", true, true)]
        [InlineData("AB", true, true)]
        [InlineData("AB", false, false)]
        [InlineData("AA", false, false)]
        [InlineData("AA", true, false)]
        public void StringContainsOverride_ReturnsCorrectResult(string value, bool caseSensitive, bool expected)
        {
            string path = $"@.test.contains('{value}', {caseSensitive})";
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize(path));
            Assert.IsType<MethodCallFilterSubExpression>(exp);

            JsonElement result = exp.Execute(_stringJson);
            JsonValueKind expectedResult = expected ? JsonValueKind.True : JsonValueKind.False;
            Assert.Equal(expectedResult, result.ValueKind);
        }

        [Theory]
        [InlineData("toUpper")]
        [InlineData("toUpperCase")]
        [InlineData("toLower")]
        [InlineData("toLowerCase")]
        public void StringToUpperOrToLower_ReturnsCorrectValue(string methodName)
        {
            string path = $"@.name.{methodName}()";
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize(path));
            Assert.IsType<MethodCallFilterSubExpression>(exp);

            JsonElement result = exp.Execute(_stringNameJson);
            string actual = result.GetString();

            string expected = methodName.Contains("pper") ? "KOVALSKI" : "kovalski";

            Assert.Equal(expected, actual);
        }
    }
}
