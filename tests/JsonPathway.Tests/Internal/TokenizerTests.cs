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
            Assert.True(tokens.Count == 1 && tokens[0].IsAllArrayElementsToken());

            input = "[*]";
            tokens = Tokenizer.Tokenize(input);
            Assert.True(tokens.Single().IsAllArrayElementsToken());
        }

        [Fact]
        public void Tokenize_InputWithArrayAccess_ReturnsValidTokens()
        {
            string input = "$[4]";

            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(input);

            Assert.Equal(1, tokens.Count);
            Assert.IsType<ArrayElementsToken>(tokens[0]);

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

            Assert.Equal(1, tokens.Count);
            Assert.IsType<ArrayElementsToken>(tokens[0]);

            ArrayElementsToken t = tokens[0].CastToArrayElementsToken();
            Assert.NotNull(t.ExactElementsAccess);
            Assert.Equal(3, t.ExactElementsAccess.Length);
            Assert.Equal(4, t.ExactElementsAccess[0]);
            Assert.Equal(1, t.ExactElementsAccess[1]);
            Assert.Equal(2, t.ExactElementsAccess[2]);
        }

        [Fact]
        public void Tokenize_InputWithArrayAccessSlice_ReturnsValidTokens()
        {
            string input = "$[-4:-2:9]";

            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(input);

            Assert.Equal(1, tokens.Count);
            Assert.IsType<ArrayElementsToken>(tokens[0]);

            ArrayElementsToken t = tokens[0].CastToArrayElementsToken();
            Assert.Null(t.ExactElementsAccess);
            Assert.Equal(-4, t.SliceStart);
            Assert.Equal(-2, t.SliceEnd);
            Assert.Equal(9, t.SliceStep);

            input = "$[4:2:9]";

            tokens = Tokenizer.Tokenize(input);

            Assert.Equal(1, tokens.Count);
            Assert.IsType<ArrayElementsToken>(tokens[0]);

            t = tokens[0].CastToArrayElementsToken();
            Assert.Null(t.ExactElementsAccess);
            Assert.Equal(4, t.SliceStart);
            Assert.Equal(2, t.SliceEnd);
            Assert.Equal(9, t.SliceStep);

            input = "$[+4:+2:+9]";

            tokens = Tokenizer.Tokenize(input);

            Assert.Equal(1, tokens.Count);
            Assert.IsType<ArrayElementsToken>(tokens[0]);

            t = tokens[0].CastToArrayElementsToken();
            Assert.Null(t.ExactElementsAccess);
            Assert.Equal(4, t.SliceStart);
            Assert.Equal(2, t.SliceEnd);
            Assert.Equal(9, t.SliceStep);
        }

        [Fact]
        public void Tokenize_InputWithSliceStepNegative_ThrowsException()
        {
            string input = "$[4:2:-1]";
            Assert.Throws<UnexpectedTokenException>(() => Tokenizer.Tokenize(input));
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

        [Theory]
        [InlineData("[:2]", null, 2, null)]
        [InlineData("[2:]", 2, null, null)]
        [InlineData("[::2]", null, null, 2)]
        [InlineData("[1:3:2]", 1, 3, 2)]
        [InlineData("[1:3:]", 1, 3, null)]
        [InlineData("[1::3]", 1, null, 3)]
        [InlineData("[:1:3]", null, 1, 3)]
        public void Tokenize_InputWithArraySliceDefaultValues_ReturnsValidToken(string input, int? start, int? end, int? step)
        {
            ArrayElementsToken token = Tokenizer.Tokenize(input).Single().CastToArrayElementsToken();

            Assert.Null(token.ExactElementsAccess);

            if (!start.HasValue) Assert.False(token.SliceStart.HasValue);
            else Assert.Equal(start.Value, token.SliceStart);

            if (!end.HasValue) Assert.False(token.SliceEnd.HasValue);
            else Assert.Equal(end.Value, token.SliceEnd);

            if (!step.HasValue) Assert.False(token.SliceStep.HasValue);
            else Assert.Equal(step.Value, token.SliceStep);
        }

        [Fact]
        public void Tokenize_InputWithMultipleProperties_ReturnsValidToken()
        {
            string input = "$['abc', 'efg']";

            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(input);
            Assert.Equal(1, tokens.Count);
            MultiplePropertiesToken mpt = tokens.First().CastToMultiplePropertiesToken();
            Assert.Equal(2, mpt.Properties.Length);
            Assert.Equal("abc", mpt.Properties[0]);
            Assert.Equal("efg", mpt.Properties[1]);
        }

        [Fact]
        public void Tokenize_InputWithFilter_ReturnsValidToken()
        {
            string input = "$.a[?(@.b > 0)]";

            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(input);
            Assert.IsType<FilterToken>(tokens.Last());

            input = "$.a[?(@.b > 0 && (@.c == 'a' || @.c == ''))]";
            tokens = Tokenizer.Tokenize(input);
            Assert.IsType<FilterToken>(tokens.Last());
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
