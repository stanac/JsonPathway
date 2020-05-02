using JsonPathway.Internal.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            FilterParser.Parse(tokens.ToList());
        }
    }
}
