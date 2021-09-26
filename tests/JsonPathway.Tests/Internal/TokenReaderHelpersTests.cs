using JsonPathway.Internal;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace JsonPathway.Tests.Internal
{
    public class TokenReaderHelpersTests
    {
        [Fact]
        public void InputWithNumber_ReadsNumberTokens()
        {
            string input = " 4  4.44  .44 .4 ";
            PositionedChar[] chars = PositionedChar.GetFromString(input);

            IReadOnlyList<Token> tokens = TokenReaderHelpers.ReadTokens(chars);

            Assert.True(tokens.All(x => x.IsWhiteSpaceToken() || x.IsNumberToken()));

            List<NumberToken> numberTokens = tokens.Where(x => x.IsNumberToken()).Cast<NumberToken>().ToList();

            Assert.Equal(4, numberTokens.Count);
            Assert.Equal(4, numberTokens[0].NumberValue, 10);
            Assert.Equal(4.44, numberTokens[1].NumberValue, 10);
            Assert.Equal(0.44, numberTokens[2].NumberValue, 10);
            Assert.Equal(0.4, numberTokens[3].NumberValue, 10);
        }

        [Theory]
        [InlineData(".4 .4.4.")]
        [InlineData("4.4.4")]
        public void InputWithNumberAndExtraPoints_ThrowsException(string input)
        {
            PositionedChar[] chars = PositionedChar.GetFromString(input);

            Assert.Throws<UnrecognizedCharSequence>(() => TokenReaderHelpers.ReadTokens(chars));
        }

        [Theory]
        [InlineData("abc", "abc")]
        [InlineData("qwe asd", "qwe", "asd")]
        [InlineData(" qwe asd   zxc ", "qwe", "asd", "zxc")]
        [InlineData("")]
        [InlineData("$_asd", "$_asd")]
        [InlineData("$_as0132d", "$_as0132d")]
        public void ReadingInputWithPropertyTokens_ReturnPropertyTokens(string input, params string[] expected)
        {
            PositionedChar[] chars = PositionedChar.GetFromString(input);
            IReadOnlyList<Token> tokens = TokenReaderHelpers.ReadTokens(chars);
            tokens = tokens.Where(x => !x.IsWhiteSpaceToken()).ToList();

            Assert.Equal(tokens.Count, expected.Length);
            
            for (int i = 0; i < expected.Length; i++)
            {
                string value = tokens[i].StringValue;
                Assert.Equal(expected[i], value);
            }
        }

        [Theory]
        [InlineData("true false true false", true, false, true, false)]
        [InlineData(" true    false   true   false  ", true, false, true, false)]
        public void ReadingInputWithBooleanTokens_ReturnBooleanTokens(string input, params bool[] expected)
        {
            PositionedChar[] chars = PositionedChar.GetFromString(input);
            IReadOnlyList<Token> tokens = TokenReaderHelpers.ReadTokens(chars);
            tokens = tokens.Where(x => !x.IsWhiteSpaceToken()).ToList();

            Assert.Equal(tokens.Count, expected.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                bool value = tokens[i].CastToBoolToken().BoolValue;
                Assert.Equal(expected[i], value);
            }
        }

        [Theory]
        [InlineData("=-*/", '=', '-', '*', '/')]
        [InlineData("  = - * /   ", '=', '-', '*', '/')]
        public void ReadingInputWithSymbolTokens_ReturnSymbolTokens(string input, params char[] expected)
        {
            PositionedChar[] chars = PositionedChar.GetFromString(input);
            IReadOnlyList<Token> tokens = TokenReaderHelpers.ReadTokens(chars);
            tokens = tokens.Where(x => !x.IsWhiteSpaceToken()).ToList();

            Assert.Equal(tokens.Count, expected.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                char value = tokens[i].CastToSymbolToken().StringValue[0];
                Assert.Equal(expected[i], value);
            }
        }

        [Theory]
        [InlineData("\\")]
        [InlineData("abc %")]
        [InlineData("abc #")]
        public void ReadingInputWithUnsupportedSymbol_ThrowsException(string input)
        {
            PositionedChar[] chars = PositionedChar.GetFromString(input);
            Assert.Throws<UnrecognizedCharSequence>(() => TokenReaderHelpers.ReadTokens(chars));
        }

        [Fact]
        public void ReadingInputWithMixTokens_ReturnsValidTokens()
        {
            PositionedChar[] chars = PositionedChar.GetFromString(
                "123 abc22 .44 true * .test1 test2. 3 false"
                );
            IReadOnlyList<Token> tokens = TokenReaderHelpers.ReadTokens(chars);
            tokens = tokens.Where(x => !x.IsWhiteSpaceToken()).ToList();

            Assert.Equal(11, tokens.Count);

            Assert.Equal(123.0, tokens[0].CastToNumberToken().NumberValue, 10);
            Assert.Equal("abc22", tokens[1].CastToPropertyToken().StringValue);
            Assert.Equal(0.44, tokens[2].CastToNumberToken().NumberValue, 10);
            Assert.True(tokens[3].CastToBoolToken().BoolValue);
            Assert.Equal("*", tokens[4].CastToSymbolToken().StringValue);
            Assert.Equal(".", tokens[5].CastToSymbolToken().StringValue);
            Assert.Equal("test1", tokens[6].CastToPropertyToken().StringValue);
            Assert.Equal("test2", tokens[7].CastToPropertyToken().StringValue);
            Assert.Equal(".", tokens[8].CastToSymbolToken().StringValue);
            Assert.Equal(3.0, tokens[9].CastToNumberToken().NumberValue, 10);
            Assert.False(tokens[10].CastToBoolToken().BoolValue);
        }
    }
}
