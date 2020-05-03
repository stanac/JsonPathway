using JsonPathway.Internal.Filters;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace JsonPathway.Tests
{
    public class FilterParserTests
    {
        [Fact]
        public void Test()
        {
            string input = "@.price >= 0 && (@.name.first.contains('a') || @['name'].contains(5) || @.f)";
            IReadOnlyList<FilterExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);
            var expr = FilterParser.Parse(tokens.ToList());

            // todo: assert
        }
    }
}
