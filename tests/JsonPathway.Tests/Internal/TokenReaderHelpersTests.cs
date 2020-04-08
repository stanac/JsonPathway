using JsonPathway.Internal;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace JsonPathway.Tests.Internal
{
    public class TokenReaderHelpersTests
    {
        [Fact]
        public void CharsWithNumberReadsNumberTokens()
        {
            string input = " 4  4.44  .44 .4 ";
            PositionedChar[] chars = PositionedChar.GetFromString(input);

            IReadOnlyList<Token> tokens = TokenReaderHelpers.ReadTokens(chars);

            Assert.True(tokens.All(x => x.IsWhiteSpaceToken() || x.IsNumberToken()));

            var numberTokens = tokens.Where(x => x.IsNumberToken()).Cast<NumberToken>().ToList();

            Assert.Equal(4, numberTokens.Count);
            Assert.Equal(4, numberTokens[0].NumberValue, 10);
            Assert.Equal(4.44, numberTokens[1].NumberValue, 10);
            Assert.Equal(0.44, numberTokens[2].NumberValue, 10);
            Assert.Equal(0.4, numberTokens[3].NumberValue, 10);
        }
    }
}
