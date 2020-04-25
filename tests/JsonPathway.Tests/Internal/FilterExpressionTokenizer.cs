using JsonPathway.Internal;
using JsonPathway.Internal.BoolExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace JsonPathway.Tests.Internal
{
    public class FilterExpressionTokenizerTests
    {
        [Fact]
        public void ValidExpression_ReturnsValidTokens()
        {
            string input = "@.price >= 0 && (@.name.first.contains('a') || @['name'].contains('b') || !@.f)";

            IReadOnlyList<ExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);

            Assert.Equal(12, tokens.Count);

            Assert.IsType<PropertyExpressionToken>(tokens[0]);
            Assert.IsType<PropertyExpressionToken>(tokens[1]);

        }
    }
}
