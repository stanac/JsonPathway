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

        }
    }
}
