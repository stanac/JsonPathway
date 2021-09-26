using JsonPathway.Internal;
using System.Collections.Generic;
using Xunit;

namespace JsonPathway.Tests.Internal
{
    public class StringTokenizerHelperTests
    {
        [Theory]
        [InlineData("'abc'", "abc")]
        [InlineData("'abc\"defg\"'", "abc\"defg\"")]
        [InlineData("'abc\\\"defg\\\"'", "abc\"defg\"")]
        [InlineData("'abc' 'efg'  \"hij\" ", "abc", "efg", "hij")]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData(" ''  ''", "", "")]
        [InlineData("'\\''", "'")]
        [InlineData("'\\\"'", "\"")]
        [InlineData("'\\\\'", "\\")]
        [InlineData("\"\\\"\"", "\"")]
        [InlineData("\"\\\'\"", "'")]
        [InlineData("\"\\\\\"", "\\")]
        public void ValidValue_ReturnsString(string input, params string[] expected)
        {
            IReadOnlyList<StringToken> strings = StringTokenizerHelper.GetStringTokens(input);

            Assert.Equal(expected.Length, strings.Count);

            for (int i = 0; i < strings.Count; i++)
            {
                Assert.Equal(expected[i], strings[i].StringValue);
            }
        }

        [Fact]
        public void ValidValue_SetsCorrectIndexes()
        {
            string input = "abc['def'][\"ghi\"]";

            IReadOnlyList<StringToken> tokens = StringTokenizerHelper.GetStringTokens(input);

            Assert.Equal(2, tokens.Count);

            Assert.Equal(4, tokens[0].StartIndex);
            Assert.Equal(8, tokens[0].EndIndex);

            Assert.Equal(11, tokens[1].StartIndex);
            Assert.Equal(15, tokens[1].EndIndex);

        }

        [Fact]
        public void UnclosedString_ThrowsException()
        {
            string[] inputs = new[]
            {
                "'\\'",
                "\"\\\"",
                "'",
                "\""
            };

            foreach (string i in inputs)
            {
                Assert.Throws<UnclosedStringException>(() => StringTokenizerHelper.GetStringTokens(i));
            }
        }

        [Fact]
        public void UnescapedCharacted_ThrowsException()
        {
            string[] inputs = new[]
            {
                "'\\a'",
                "\"\\a\""
            };

            foreach (string i in inputs)
            {
                Assert.Throws<UnescapedCharacterException>(() => StringTokenizerHelper.GetStringTokens(i));
            }
        }
    }
}
