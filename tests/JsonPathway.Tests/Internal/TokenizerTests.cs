using JsonPathway.Internal;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace JsonPathway.Tests.Internal
{
    public class TokenizerTests
    {
        [Fact]
        public void Tokenize_InputWithProperties_ReturnsValidTokens()
        {
            string input = "abc['efg'][\"hij\"]";
            IReadOnlyList<PropertyToken> tokens = Tokenizer.Tokenize(input)
                .Select(x => x.CastToPropertyToken())
                .ToList();

            Assert.Equal(3, tokens.Count);

            Assert.Equal("abc", tokens[0].StringValue);
            Assert.Equal("efg", tokens[1].StringValue);
            Assert.Equal("hij", tokens[2].StringValue);

            Assert.False(tokens[0].Escaped);
            Assert.True(tokens[1].Escaped);
            Assert.True(tokens[2].Escaped);
        }

        [Fact]
        public void Tokenize_InputWithWildcards_ReturnValidTokens()
        {
            string input = "a..b.*";
            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(input);

            Assert.Equal(5, tokens.Count);

            Assert.True(tokens[0].IsPropertyToken());
            Assert.True(tokens[1].IsRecursivePropertiesToken());
            Assert.True(tokens[2].IsPropertyToken());
            Assert.True(tokens[3].IsSymbolTokenPoint());
            Assert.True(tokens[4].IsChildPropertiesToken());
        }

        [Fact]
        public void Tokenize_InputWithFitler_ReturnsValidTokens()
        {
            string input = "abc['cde'][?(@.length > 2 && @ != 'A')]";

            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(input);

            Assert.Equal(3, tokens.Count);

            Assert.True(tokens[0].IsPropertyToken());
            Assert.Equal("abc", tokens[0].StringValue);

            Assert.True(tokens[1].IsPropertyToken());
            Assert.Equal("cde", tokens[1].StringValue);

            Assert.True(tokens[2].IsFilterToken());
            Assert.Equal("@.length>2&&@!='A'", tokens[2].StringValue);
        }

        [Fact]
        public void Tokenize_InputWithNotValidFilter_DoesNotReturnFilter()
        {
            string input = "abc['cde'][?(@.length > 2 && @ != 'A')";

            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(input);

            Assert.DoesNotContain(tokens, x => x.IsFilterToken());
        }
    }
}
