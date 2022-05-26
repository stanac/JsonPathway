using JsonPathway.Internal;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace JsonPathway.Tests
{
    public class ReadMeExamplesTests
    {
        private readonly string _json;
        private readonly JsonDocument _jsonDoc;
        private readonly JsonElement _jsonDocElement;

        private readonly string _lotr;

        public ReadMeExamplesTests()
        {
            _json = TestDataLoader.Store();
            _jsonDoc = JsonDocument.Parse(_json);
            _jsonDocElement = _jsonDoc.RootElement;

            _lotr = @"{""category"":""fiction"",""author"":""J. R. R. Tolkien"",""title"":""The Lord of the Rings"",""isbn"":""0-395-19395-8"",""price"":22.99}";
        }

        [Fact]
        public void StringLength_ReturnsCorrectResult()
        {
            string path = "$.store.bicycle.color.length";
            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(path, _json);
            Assert.Equal(3, result.Single().GetInt32());
        }

        [Fact]
        public void StringLengthOnJsonDoc_ReturnsCorrectResult()
        {
            string path = "$.store.bicycle.color.length";
            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(path, _jsonDoc);
            Assert.Equal(3, result.Single().GetInt32());
        }

        [Fact]
        public void StringLengthOnElement_ReturnsCorrectResult()
        {
            string path = "$.store.bicycle.color.length";
            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(path, _jsonDocElement);
            Assert.Equal(3, result.Single().GetInt32());
        }

        [Fact]
        public void ArrayLength_ReturnsCorrectResult()
        {
            string path = "$.store.book.length";
            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(path, _json);
            Assert.Equal(4, result.Single().GetInt32());
        }

        [Fact]
        public void StringLengthInFilter_ReturnsCorrectResult()
        {
            string path = "$.store.book[?(@.title.length == 21)]";
            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(path, _json);
            Assert.Single(result);
            string resultString = JsonSerializer.Serialize(result.Single());
            Assert.Equal(_lotr, resultString);
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

        [Fact]
        public void StringMethodToUpper_ReturnsCorrectResult()
        {
            string path = "$.store.book[?(@.author.contains(\"tolkien\", true))]";
            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(path, _json);

            Assert.Single(result);
            string resultString = JsonSerializer.Serialize(result.Single());
            Assert.Equal(_lotr, resultString);
        }

        [Fact]
        public void PropertyAndEscapedProperty_ReturnsSameResult()
        {
            IReadOnlyList<JsonElement> result1 = JsonPath.ExecutePath("$.store.bicycle.price", _json);
            IReadOnlyList<JsonElement> result2 = JsonPath.ExecutePath("$[\"store\"][\"bicycle\"][\"price\"]", _json);
            IReadOnlyList<JsonElement> result3 = JsonPath.ExecutePath("$['store']['bicycle']['price']", _json);

            Assert.Equal(result1.Single(), result2.Single(), JsonElementEqualityComparer.Default);
            Assert.Equal(result2.Single(), result2.Single(), JsonElementEqualityComparer.Default);
        }

        [Fact]
        public void SliceOperator_ReturnsCorrectResult()
        {
            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath("store.book[0:4:2]", _json);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void WildcardOperator_ReturnsCorrectResult()
        {
            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath("$.store.bicycle.*", _json);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.ValueKind == JsonValueKind.String && x.GetString() == "red");
            Assert.Contains(result, x => x.ValueKind == JsonValueKind.Number && x.GetDouble() == 19.95);
        }


        [Fact]
        public void RecursiveOperator_ReturnsCorrectResult()
        {
            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath("$.store.book..", _json);
            Assert.Equal(5, result.Count);

            string expected = "[[{`category`:`reference`,`author`:`Nigel Rees`,`title`:`Sayings of the Century`,`price`:8.95},{`category`:`fiction`,`author`:`Evelyn Waugh`,`title`:`Sword of Honour`,`price`:12.99},{`category`:`fiction`,`author`:`Herman Melville`,`title`:`Moby Dick`,`isbn`:`0-553-21311-3`,`price`:8.99},{`category`:`fiction`,`author`:`J. R. R. Tolkien`,`title`:`The Lord of the Rings`,`isbn`:`0-395-19395-8`,`price`:22.99}],{`category`:`reference`,`author`:`Nigel Rees`,`title`:`Sayings of the Century`,`price`:8.95},{`category`:`fiction`,`author`:`Evelyn Waugh`,`title`:`Sword of Honour`,`price`:12.99},{`category`:`fiction`,`author`:`Herman Melville`,`title`:`Moby Dick`,`isbn`:`0-553-21311-3`,`price`:8.99},{`category`:`fiction`,`author`:`J. R. R. Tolkien`,`title`:`The Lord of the Rings`,`isbn`:`0-395-19395-8`,`price`:22.99}]"
                .Replace("`", "\"");
            string resultJson = JsonSerializer.Serialize(result);
            Assert.Equal(expected, resultJson);
        }

        [Fact]
        public void FitlerOperatorOnPrice_ReturnsCorrectResult()
        {
            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath("$.store.book[?(@.price > 10)]", _json);
            Assert.Equal(2, result.Count);

            string resultJson = JsonSerializer.Serialize(result);
            string expectedJson = "[{`category`:`fiction`,`author`:`Evelyn Waugh`,`title`:`Sword of Honour`,`price`:12.99},{`category`:`fiction`,`author`:`J. R. R. Tolkien`,`title`:`The Lord of the Rings`,`isbn`:`0-395-19395-8`,`price`:22.99}]"
                .Replace("`", "\"");

            Assert.Equal(expectedJson, resultJson);
        }


        [Fact]
        public void FitlerTruthyIsbnOperator_ReturnsCorrectResult()
        {
            IReadOnlyList<JsonElement> result = JsonPath.ExecutePath("$.store.book[?(@.isbn)]", _json);
            Assert.Equal(2, result.Count);

            string resultJson = JsonSerializer.Serialize(result);
            string expectedJson = "[{`category`:`fiction`,`author`:`Herman Melville`,`title`:`Moby Dick`,`isbn`:`0-553-21311-3`,`price`:8.99},{`category`:`fiction`,`author`:`J. R. R. Tolkien`,`title`:`The Lord of the Rings`,`isbn`:`0-395-19395-8`,`price`:22.99}]"
                .Replace("`", "\"");

            Assert.Equal(expectedJson, resultJson);
        }
    }
}
