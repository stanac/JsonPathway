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
            Assert.True(tokens[3].IsSymbolToken('.'));
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

        [Fact]
        public void Tokenize_InputWithAllArrayElements_ReturnsValidTokens()
        {
            string input = "$[*]";
            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(input);
            Assert.True(tokens.Count == 2 && tokens.Last().IsAllArrayElementsToken());

            input = "[*]";
            tokens = Tokenizer.Tokenize(input);
            Assert.True(tokens.Single().IsAllArrayElementsToken());
        }

        [Fact]
        public void Tokenize_InputWithArrayAccess_ReturnsValidTokens()
        {
            string input = "$[4]";

            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(input);

            Assert.Equal(2, tokens.Count);
            Assert.IsType<ArrayElementsToken>(tokens[1]);

            ArrayElementsToken t = tokens.Last().CastToArrayElementsToken();
            Assert.NotNull(t.ExactElementsAccess);
            Assert.Single(t.ExactElementsAccess);
            Assert.Equal(4, t.ExactElementsAccess[0]);
        }

        [Fact]
        public void Tokenize_InputWithArrayAccessMultipleExact_ReturnsValidTokens()
        {
            string input = "$[4,1,2]";

            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(input);

            Assert.Equal(2, tokens.Count);
            Assert.IsType<ArrayElementsToken>(tokens[1]);

            ArrayElementsToken t = tokens.Last().CastToArrayElementsToken();
            Assert.NotNull(t.ExactElementsAccess);
            Assert.Equal(3, t.ExactElementsAccess.Length);
            Assert.Equal(4, t.ExactElementsAccess[0]);
            Assert.Equal(1, t.ExactElementsAccess[1]);
            Assert.Equal(2, t.ExactElementsAccess[2]);
        }

        [Fact]
        public void Tokenize_InputWithArrayAccessSlice_ReturnsValidTokens()
        {
            string input = "$[-4:-2:-9]";

            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(input);

            Assert.Equal(2, tokens.Count);
            Assert.IsType<ArrayElementsToken>(tokens[1]);

            ArrayElementsToken t = tokens.Last().CastToArrayElementsToken();
            Assert.Null(t.ExactElementsAccess);
            Assert.Equal(-4, t.Start);
            Assert.Equal(-2, t.End);
            Assert.Equal(-9, t.Step);

            input = "$[4:2:9]";

            tokens = Tokenizer.Tokenize(input);

            Assert.Equal(2, tokens.Count);
            Assert.IsType<ArrayElementsToken>(tokens[1]);

            t = tokens.Last().CastToArrayElementsToken();
            Assert.Null(t.ExactElementsAccess);
            Assert.Equal(4, t.Start);
            Assert.Equal(2, t.End);
            Assert.Equal(9, t.Step);

            input = "$[+4:+2:+9]";

            tokens = Tokenizer.Tokenize(input);

            Assert.Equal(2, tokens.Count);
            Assert.IsType<ArrayElementsToken>(tokens[1]);

            t = tokens.Last().CastToArrayElementsToken();
            Assert.Null(t.ExactElementsAccess);
            Assert.Equal(4, t.Start);
            Assert.Equal(2, t.End);
            Assert.Equal(9, t.Step);
        }

        [Fact]
        public void Tokenize_InputWithArrayAccessSliceNotValid_ThrowsException()
        {
            string input = "$[4:2:9:]";

            Assert.Throws<UnrecognizedCharSequence>(() => Tokenizer.Tokenize(input));
        }

        [Fact]
        public void Tokenize_InputWithArrayAccessNegative_ReturnsValidToken()
        {
            string input = "$[-4]";

            var token = Tokenizer.Tokenize(input).Last().CastToArrayElementsToken();

            Assert.Single(token.ExactElementsAccess);
            Assert.Equal(-4, token.ExactElementsAccess.Single());
        }

        [Fact]
        public void Tokenize_InputWithArrayAccessDefaultValues_ReturnsValidToken()
        {
            string input = "[:2]";

            ArrayElementsToken token = Tokenizer.Tokenize(input).Single().CastToArrayElementsToken();

            Assert.Null(token.ExactElementsAccess);
            Assert.Equal(0, token.Start);
            Assert.Equal(2, token.End);
            Assert.Null(token.Step);
        }

        [Fact]
        public void Tokenize_InputWithMultipleProperties_ReturnsValidToken()
        {
            string input = "$['abc', 'efg']";

            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(input);
            Assert.Equal(2, tokens.Count);
            MultiplePropertiesToken mpt = tokens.Last().CastToMultiplePropertiesToken();
            Assert.Equal(2, mpt.Properties.Length);
            Assert.Equal("abc", mpt.Properties[0]);
            Assert.Equal("efg", mpt.Properties[1]);
        }

        [Theory]
        [InlineData("$['abc',]")]
        [InlineData("$['abc','efg',]")]
        [InlineData("$['abc',,'efg']")]
        [InlineData("$[,'abc','efg']")]
        public void Tokenize_InputWithMultiplePropertiesNotValid_ThrowsException(string input)
        {
            Assert.Throws<UnrecognizedCharSequence>(() => Tokenizer.Tokenize(input));
        }
    }
}
