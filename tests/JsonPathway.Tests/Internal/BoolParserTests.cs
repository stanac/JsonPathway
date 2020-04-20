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
    public class BoolParserTests
    {
        [Fact]
        public void Test()
        {
            string input = "@.price > 0 && (@.name.contains('a') || @['name'].contains('b') || !@.f)";

            var r = BoolParser.Parse(input);
        }
    }
}
