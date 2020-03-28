using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace JsonPath.Net.Tests
{
    public class JsonValuePathParserTests
    {
        [Fact]
        public void SingleValueReturnsPath()
        {
            string path = "person";

            IReadOnlyList<string> values = JsonValuePathParser.GetPathParts(path);

            Assert.NotNull(values);
            Assert.NotEmpty(values);
            Assert.Equal(1, values.Count);
            Assert.Equal("person", values.First());
        }

        [Fact]
        public void ValueStartingWithIllegarCharacterThrowsException()
        {
            string[] paths = new[]
            {
                "!person",
                "@person",
                ".person",
                ",person",
                "\\person",
                "-person",
                "+person",
                ":person",
                ";person",
                "&person"
            };

            foreach (var path in paths)
            {
                Assert.Throws<ArgumentException>(() => JsonValuePathParser.GetPathParts(path));
            }
        }

        [Fact]
        public void ValueStartingWithLegarCharacterDoesNotThrowsException()
        {
            string[] paths = new[]
            {
                "$person",
                "_person",
            };

            foreach (var path in paths)
            {
                var pathParts = JsonValuePathParser.GetPathParts(path);
                Assert.Equal(path, pathParts.Single());
            }
        }

        [Fact]
        public void EscapedValueWithLegarCharacterDoesNotThrowsException()
        {
            string[] paths = new[]
            {
                 "[\"person\"]",
                 "['person']"
            };

            foreach (var path in paths)
            {
                var pathParts = JsonValuePathParser.GetPathParts(path);
                Assert.Equal("person", pathParts.Single());
            }
        }

        [Fact]
        public void TwoValuesUnEscapedWorks()
        {
            string path = "a.b";

            var pathParts = JsonValuePathParser.GetPathParts(path);

            Assert.Equal(2, pathParts.Count);

            Assert.Equal("a", pathParts[0]);
            Assert.Equal("b", pathParts[1]);
        }

        [Fact]
        public void MoreThanTwoValuesUnEscapedWorks()
        {
            string path = "a.b.c.d";

            var pathParts = JsonValuePathParser.GetPathParts(path);

            Assert.Equal(4, pathParts.Count);

            Assert.Equal("a", pathParts[0]);
            Assert.Equal("b", pathParts[1]);
            Assert.Equal("c", pathParts[2]);
            Assert.Equal("d", pathParts[3]);
        }

        [Fact]
        public void TwoValuesEscapedWorks()
        {
            string path = "[\"abvd\"]['z1wer']";

            var pathParts = JsonValuePathParser.GetPathParts(path);

            Assert.Equal(2, pathParts.Count);

            Assert.Equal("abvd", pathParts[0]);
            Assert.Equal("z1wer", pathParts[1]);
        }

        [Fact]
        public void MoreThanTwoValuesEscapedWorks()
        {
            string path = "[\"abvd\"]['z1wer']['123'][\"444\"]";

            var pathParts = JsonValuePathParser.GetPathParts(path);

            Assert.Equal(4, pathParts.Count);

            Assert.Equal("abvd", pathParts[0]);
            Assert.Equal("z1wer", pathParts[1]);
            Assert.Equal("123", pathParts[2]);
            Assert.Equal("444", pathParts[3]);
        }

        [Fact]
        public void TwoMixedValuesWork()
        {
            string path = "[\"a\"].z";

            var pathParts = JsonValuePathParser.GetPathParts(path);

            Assert.Equal(2, pathParts.Count);

            Assert.Equal("a", pathParts[0]);
            Assert.Equal("z", pathParts[1]);
        }

        [Fact]
        public void MoreThanTwoMixedValuesWork()
        {
            string path = "[\"a\"].z['123']['456'].abc";

            var pathParts = JsonValuePathParser.GetPathParts(path);

            Assert.Equal(5, pathParts.Count);

            Assert.Equal("a", pathParts[0]);
            Assert.Equal("z", pathParts[1]);
            Assert.Equal("123", pathParts[2]);
            Assert.Equal("456", pathParts[3]);
            Assert.Equal("abc", pathParts[4]);
        }

        [Fact]
        public void EscapedBackslashWorks()
        {
            var path = "['\\\\']";
            var pathParts = JsonValuePathParser.GetPathParts(path);
            Assert.Equal(1, pathParts.Count);
            Assert.Equal("\\", pathParts[0]);

            path = "['\\\\\\\\']";
            pathParts = JsonValuePathParser.GetPathParts(path);
            Assert.Equal(1, pathParts.Count);
            Assert.Equal("\\\\", pathParts[0]);
        }

        [Fact]
        public void EscapedDoubleQuotesWorks()
        {
            var path = "[\"\\\"a\\\"\"]";
            var pathParts = JsonValuePathParser.GetPathParts(path);
            Assert.Equal(1, pathParts.Count);
            Assert.Equal("\"a\"", pathParts[0]);
        }

        [Theory]
        [InlineData(".a")]
        [InlineData(";a")]
        [InlineData("a;")]
        [InlineData("''")]
        [InlineData("''[")]
        [InlineData("'']")]
        [InlineData("[''")]
        [InlineData("a.")]
        [InlineData(".a.")]
        [InlineData("[")]
        [InlineData("]")]
        [InlineData("a..b")]
        [InlineData("a[[]")]
        [InlineData("a[[]]")]
        [InlineData("abc def")]
        [InlineData("['abc'].['abc']")]
        [InlineData("['abc'].]'abc']")]
        [InlineData("[]")]
        [InlineData("['\"]")]
        [InlineData("[\"']")]
        [InlineData("[abc]")]
        [InlineData("abc.[]")]
        [InlineData("[a'bc']")]
        [InlineData("a.v.['abc']")]
        public void NotValidPath_ThrowsException(string path)
        {
            Assert.Throws<ArgumentException>(() => JsonValuePathParser.GetPathParts(path));
        }

        [Theory]
        [InlineData("a")]
        [InlineData("_a")]
        [InlineData("$a")]
        [InlineData("a.a")]
        [InlineData("a.v['abc']")]
        [InlineData("['']")]
        [InlineData(@"[""""]")]
        [InlineData("a['']")]
        [InlineData("a[ 'a' ]")]
        [InlineData(" [ ' ' ] ")]
        [InlineData("[ 'a' ]")]
        [InlineData("a[ 'abc' ]")]
        [InlineData(" abc . def  ")]
        public void ValidPath_DoesNotThrowException(string path)
        {
            JsonValuePathParser.GetPathParts(path);
            // nothing thrown, assertion passed
        }

        [Theory]
        [InlineData("a", "a")]
        [InlineData("_a", "_a")]
        [InlineData("$a", "$a")]
        [InlineData("a.a", "a;a")]
        [InlineData("a.v['abc']", "a;v;abc")]
        [InlineData("['']", "")]
        [InlineData(@"[""""]", "")]
        [InlineData("a['']", "a;")]
        [InlineData("a[ 'a' ]", "a;a")]
        [InlineData(" [ ' ' ] ", " ")]
        [InlineData("[ 'a' ]", "a")]
        [InlineData("a[ 'abc' ]", "a;abc")]
        [InlineData("a[ ' abc ' ]", "a; abc ")]
        [InlineData("a[ ' 123 ' ]", "a; 123 ")]
        [InlineData(" abc . def  ", "abc;def")]
        public void ValidPath_ReturnsValidValues(string path, string expectedCsv)
        {
            var parts = JsonValuePathParser.GetPathParts(path);
            var expectedParts = expectedCsv.Split(";");

            Assert.Equal(expectedParts.Length, parts.Count);

            for (int i = 0; i < parts.Count; i++)
            {
                Assert.Equal(expectedParts[i], parts[i]);
            }
        }

        [Fact]
        public void ValidPath_IsValid_ReturnTrue()
        {
            bool valid = JsonValuePathParser.IsValid("abc.def", out _);
            Assert.True(valid);
        }

        [Fact]
        public void InvalidPath_IsValid_ReturnFalse()
        {
            bool valid = JsonValuePathParser.IsValid("abc.1def", out _);
            Assert.False(valid);
        }

        [Fact]
        public void ArrayAccessPath_WithIndex_ReturnsCorrectValue()
        {
            string path = "a.b[ 33   ].c";
            IReadOnlyList<string> pathParts = JsonValuePathParser.GetPathParts(path);

            Assert.Equal(4, pathParts.Count);
            Assert.Equal("a", pathParts[0]);
            Assert.Equal("b", pathParts[1]);
            Assert.Equal(JsonValuePathParser.ArrayAccessPrefix + 33, pathParts[2]);
            Assert.Equal("c", pathParts[3]);
        }

        [Fact]
        public void ArrayAccessPath_WithIndexWithLeadingZeros_ReturnsCorrectValue()
        {
            string path = "['a'].b[ 007 ].c";
            IReadOnlyList<string> pathParts = JsonValuePathParser.GetPathParts(path);

            Assert.Equal(4, pathParts.Count);
            Assert.Equal("a", pathParts[0]);
            Assert.Equal("b", pathParts[1]);
            Assert.Equal(JsonValuePathParser.ArrayAccessPrefix + 7, pathParts[2]);
            Assert.Equal("c", pathParts[3]);
        }

        [Fact]
        public void ArrayAccessPath_WithAny_ReturnsCorrectValue()
        {
            string path = "a.b[ :any   ].c";
            IReadOnlyList<string> pathParts = JsonValuePathParser.GetPathParts(path);

            Assert.Equal(4, pathParts.Count);
            Assert.Equal("a", pathParts[0]);
            Assert.Equal("b", pathParts[1]);
            Assert.Equal(JsonValuePathParser.ArrayAccessAny, pathParts[2]);
            Assert.Equal("c", pathParts[3]);
        }

        [Fact]
        public void ArrayAccessPath_WithNone_ReturnsCorrectValue()
        {
            string path = "a.b[ :none   ].c";
            IReadOnlyList<string> pathParts = JsonValuePathParser.GetPathParts(path);

            Assert.Equal(4, pathParts.Count);
            Assert.Equal("a", pathParts[0]);
            Assert.Equal("b", pathParts[1]);
            Assert.Equal(JsonValuePathParser.ArrayAccessNone, pathParts[2]);
            Assert.Equal("c", pathParts[3]);
        }

        [Fact]
        public void ArrayAccessPath_WithLast_ReturnsCorrectValue()
        {
            string path = "a.b[ :last  ].c";
            IReadOnlyList<string> pathParts = JsonValuePathParser.GetPathParts(path);

            Assert.Equal(4, pathParts.Count);
            Assert.Equal("a", pathParts[0]);
            Assert.Equal("b", pathParts[1]);
            Assert.Equal(JsonValuePathParser.ArrayAccessLast, pathParts[2]);
            Assert.Equal("c", pathParts[3]);
        }

        [Fact]
        public void ArrayAccessPath_WithInvalidAccessor_ThrowException()
        {
            string path = "a.b[ :first   ].c";
            Assert.Throws<ArgumentException>(() => JsonValuePathParser.GetPathParts(path));
        }

        [Fact]
        public void ArrayAccessPath_WithOnlyArrayAccess_ReturnsCorrectValue()
        {
            string path = "[ 0303  ]";
            IReadOnlyList<string> pathParts = JsonValuePathParser.GetPathParts(path);

            Assert.Equal(1, pathParts.Count);
            Assert.Equal(JsonValuePathParser.ArrayAccessPrefix + 303, pathParts[0]);
        }
    }
}
