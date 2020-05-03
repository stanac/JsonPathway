using JsonPathway.Internal.Filters;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace JsonPathway.Tests
{
    public class FilterParserTests
    {
        [Fact]
        public void TokensWithMethodsComparisonLogicOperatorsAndTruthy_Parse_ReturnsValidExpression()
        {
            string input = "@.price.count >= 0 && (@.name.first.contains('a') || @['name'].contains(5) || @.f)";
            IReadOnlyList<FilterExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);
            var expr = FilterParser.Parse(tokens.ToList());

            Assert.IsType<LogicalFilterSubExpression>(expr);
            var e1 = expr as LogicalFilterSubExpression;

            Assert.True(e1.IsAnd);
            Assert.IsType<ComparisonFilterSubExpression>(e1.LeftSide);

            // @.price.count >= 0
            var comp1 = e1.LeftSide as ComparisonFilterSubExpression;
            Assert.True(comp1.IsGreaterOrEqual);
            Assert.IsType<PropertyFilterSubExpression>(comp1.LeftSide);
            Assert.True(comp1.LeftSide is PropertyFilterSubExpression p1
                        && p1.PropertyChain.Length == 2
                        && p1.PropertyChain.First() == "price"
                        && p1.PropertyChain.Last() == "count"
                        );
            Assert.True(comp1.RightSide is NumberConstantFilterSubExpression c1
                        && c1.Value == 0.0
                        );

            // @.name.first.contains('a') || @['name'].contains(5) || @.f
            var comp2 = (e1.RightSide as GroupFilterSubExpression).Expression as LogicalFilterSubExpression;
            Assert.NotNull(comp2);

            // @.name.first.contains('a')
            Assert.IsType<MethodCallFilterSubExpression>(comp2.LeftSide);
            var mc1 = comp2.LeftSide as MethodCallFilterSubExpression;
            Assert.Equal("contains", mc1.MethodName);
            Assert.True(mc1.Arguments.Count == 1 && (mc1.Arguments.Single() as StringConstantFilterSubExpression).Value == "a");
            var calledOn1 = mc1.CalledOnExpression as PropertyFilterSubExpression;
            Assert.True(calledOn1.PropertyChain.Length == 2 && calledOn1.PropertyChain.First() == "name" && calledOn1.PropertyChain.Last() == "first");

            // @['name'].contains(5) || @.f
            Assert.IsType<LogicalFilterSubExpression>(comp2.RightSide);
            var logical3 = comp2.RightSide as LogicalFilterSubExpression;
            Assert.True(logical3.IsOr);

            // @['name'].contains(5)
            Assert.IsType<MethodCallFilterSubExpression>(logical3.LeftSide);
            var mc2 = logical3.LeftSide as MethodCallFilterSubExpression;
            Assert.Equal("contains", mc2.MethodName);
            Assert.Equal(5.0, (mc2.Arguments.Single() as NumberConstantFilterSubExpression).Value);
            Assert.Equal("name", (mc2.CalledOnExpression as PropertyFilterSubExpression).PropertyChain.Single());

            // || @.f
            Assert.IsType<TruthyFilterSubExpression>(logical3.RightSide);
            var truthy = logical3.RightSide as TruthyFilterSubExpression;
            Assert.Equal("f", truthy.PropertyChain.Single());
        }
    }
}
