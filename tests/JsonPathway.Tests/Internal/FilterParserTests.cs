using JsonPathway.Internal.Filters;
using System.Collections.Generic;
using System.Linq;
using Xunit;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace JsonPathway.Tests.Internal
{
    public class FilterParserTests
    {
        [Fact]
        public void TokensWithMethodsComparisonLogicOperatorsAndTruthy_Parse_ReturnsValidExpression()
        {
            string input = "@.price.count >= 0 && (@.name.first.contains('a') || @['name'].contains(5) || @.f)";
            IReadOnlyList<FilterExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);
            FilterSubExpression expr = FilterParser.Parse(tokens);

            Assert.IsType<LogicalFilterSubExpression>(expr);
            LogicalFilterSubExpression e1 = expr as LogicalFilterSubExpression;

            Assert.True(e1.IsAnd);
            Assert.IsType<ComparisonFilterSubExpression>(e1.LeftSide);

            // @.price.count >= 0
            ComparisonFilterSubExpression comp1 = e1.LeftSide as ComparisonFilterSubExpression;
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
            LogicalFilterSubExpression comp2 = (e1.RightSide as GroupFilterSubExpression).Expression as LogicalFilterSubExpression;
            Assert.NotNull(comp2);

            // @.name.first.contains('a')
            Assert.IsType<MethodCallFilterSubExpression>(comp2.LeftSide);
            MethodCallFilterSubExpression mc1 = comp2.LeftSide as MethodCallFilterSubExpression;
            Assert.Equal("contains", mc1.MethodName);
            Assert.True(mc1.Arguments.Count == 1 && (mc1.Arguments.Single() as StringConstantFilterSubExpression).Value == "a");
            PropertyFilterSubExpression calledOn1 = mc1.CalledOnExpression as PropertyFilterSubExpression;
            Assert.True(calledOn1.PropertyChain.Length == 2 && calledOn1.PropertyChain.First() == "name" && calledOn1.PropertyChain.Last() == "first");

            // @['name'].contains(5) || @.f
            Assert.IsType<LogicalFilterSubExpression>(comp2.RightSide);
            LogicalFilterSubExpression logical3 = comp2.RightSide as LogicalFilterSubExpression;
            Assert.True(logical3.IsOr);

            // @['name'].contains(5)
            Assert.IsType<MethodCallFilterSubExpression>(logical3.LeftSide);
            MethodCallFilterSubExpression mc2 = logical3.LeftSide as MethodCallFilterSubExpression;
            Assert.Equal("contains", mc2.MethodName);
            Assert.Equal(5.0, (mc2.Arguments.Single() as NumberConstantFilterSubExpression).Value);
            Assert.Equal("name", (mc2.CalledOnExpression as PropertyFilterSubExpression).PropertyChain.Single());

            // || @.f
            Assert.IsType<TruthyFilterSubExpression>(logical3.RightSide);
            TruthyFilterSubExpression truthy = logical3.RightSide as TruthyFilterSubExpression;
            Assert.IsType<PropertyFilterSubExpression>(truthy.Expression);
            PropertyFilterSubExpression truthyProp = truthy.Expression as PropertyFilterSubExpression;
            Assert.Equal("f", truthyProp.PropertyChain.Single());
        }

        [Fact]
        public void TokensWithNegation_Parse_ReturnsCorrectExpression()
        {
            string input = "!@.price.count";
            IReadOnlyList<FilterExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);
            FilterSubExpression expr = FilterParser.Parse(tokens);

            Assert.IsType<NegationFilterSubExpression>(expr);
            NegationFilterSubExpression neg = expr as NegationFilterSubExpression;
            Assert.IsType<TruthyFilterSubExpression>(neg.Expression);
            TruthyFilterSubExpression truthy = neg.Expression as TruthyFilterSubExpression;
            PropertyFilterSubExpression prop = truthy.Expression as PropertyFilterSubExpression;

            Assert.True(prop != null && prop.PropertyChain.Length == 2
                        && prop.PropertyChain[0] == "price" && prop.PropertyChain[1] == "count");
        }

        [Fact]
        public void TokensWithDoubleNegation_Parse_ReturnsCorrectExpression()
        {
            string input = "!!@.price.count";
            IReadOnlyList<FilterExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);
            FilterSubExpression expr = FilterParser.Parse(tokens);

            Assert.IsType<NegationFilterSubExpression>(expr);
            NegationFilterSubExpression neg1 = (NegationFilterSubExpression)expr;
            Assert.IsType<NegationFilterSubExpression>(neg1.Expression);
            NegationFilterSubExpression neg2 = (NegationFilterSubExpression)neg1.Expression;
            Assert.IsType<TruthyFilterSubExpression>(neg2.Expression);
            Assert.IsType<PropertyFilterSubExpression>(((TruthyFilterSubExpression)neg2.Expression).Expression);
            PropertyFilterSubExpression prop = ((TruthyFilterSubExpression)neg2.Expression).Expression as PropertyFilterSubExpression;

            Assert.True(prop.PropertyChain.Length == 2 && prop.PropertyChain[0] == "price" && prop.PropertyChain[1] == "count");
        }

        [Fact]
        public void TokensWithMethodCallOnArray_Parse_ReturnsCorrectExpression()
        {
            string input = "@.items.next[-1].contains(2.123)";
            IReadOnlyList<FilterExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);
            FilterSubExpression expr = FilterParser.Parse(tokens);

            Assert.IsType<MethodCallFilterSubExpression>(expr);
            MethodCallFilterSubExpression mc = (MethodCallFilterSubExpression)expr;
            Assert.Single(mc.Arguments);
            Assert.Equal(2.123, (mc.Arguments[0] as NumberConstantFilterSubExpression)?.Value ?? double.MinValue, 8);

            Assert.IsType<ArrayAccessFilterSubExpression>(mc.CalledOnExpression);
            ArrayAccessFilterSubExpression aa = mc.CalledOnExpression as ArrayAccessFilterSubExpression;

            Assert.Equal(-1, aa.ExactElementsAccess.Single());

            Assert.IsType<PropertyFilterSubExpression>(aa.ExecutedOn);
            PropertyFilterSubExpression prop = aa.ExecutedOn as PropertyFilterSubExpression;
            Assert.Equal(2, prop.PropertyChain.Length);
            Assert.Equal("items", prop.PropertyChain[0]);
            Assert.Equal("next", prop.PropertyChain[1]);
        }

        [Fact]
        public void TokensWithArray_Parse_ReturnsCorrectExpression()
        {
            string input = "@.items[2] > 3";
            IReadOnlyList<FilterExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);
            FilterSubExpression expr = FilterParser.Parse(tokens);

            Assert.IsType<ComparisonFilterSubExpression>(expr);
            ComparisonFilterSubExpression comp = expr as ComparisonFilterSubExpression;

            Assert.IsType<ArrayAccessFilterSubExpression>(comp.LeftSide);
            Assert.True(((NumberConstantFilterSubExpression)comp.RightSide)?.Value == 3.0);

            ArrayAccessFilterSubExpression aa = comp.LeftSide as ArrayAccessFilterSubExpression;
            Assert.True(aa.ExactElementsAccess?.Length == 1 && aa.ExactElementsAccess[0] == 2);
            Assert.True(aa.ExecutedOn is PropertyFilterSubExpression p && p.PropertyChain.Length == 1 && p.PropertyChain[0] == "items");
        }

        [Fact]
        public void TokensWithEscapedPropAccess_Parse_ReturnsCorrectExpression()
        {
            string input = "@.items['2'] > 3";
            IReadOnlyList<FilterExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);
            FilterSubExpression expr = FilterParser.Parse(tokens);

            Assert.IsType<ComparisonFilterSubExpression>(expr);
            ComparisonFilterSubExpression comp = expr as ComparisonFilterSubExpression;

            Assert.IsType<PropertyFilterSubExpression>(comp.LeftSide);
            Assert.True((comp.RightSide as NumberConstantFilterSubExpression)?.Value == 3.0);

            PropertyFilterSubExpression aa = comp.LeftSide as PropertyFilterSubExpression;
            Assert.Equal(2, aa.PropertyChain.Length);
            Assert.Equal("items", aa.PropertyChain[0]);
            Assert.Equal("2", aa.PropertyChain[1]);
        }

        [Fact]
        public void TokensWithTruthyElementAccess_Parse_ReturnsCorrectExpression()
        {
            string input = "@.items >= 3 || @.b[123]";
            IReadOnlyList<FilterExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);
            FilterSubExpression expr = FilterParser.Parse(tokens);

            Assert.IsType<LogicalFilterSubExpression>(expr);
            LogicalFilterSubExpression logical = expr as LogicalFilterSubExpression;

            // @.items > 3
            Assert.IsType<ComparisonFilterSubExpression>(logical.LeftSide);
            ComparisonFilterSubExpression left = logical.LeftSide as ComparisonFilterSubExpression;
            Assert.True(left.IsGreaterOrEqual);
            Assert.True(left.LeftSide is PropertyFilterSubExpression p1 && p1.PropertyChain.Length == 1 && p1.PropertyChain[0] == "items");
            Assert.True(left.RightSide is NumberConstantFilterSubExpression n1 && n1.Value == 3.0);

            // @.b[0]
            Assert.IsType<TruthyFilterSubExpression>(logical.RightSide);
            TruthyFilterSubExpression tr = logical.RightSide as TruthyFilterSubExpression;
            Assert.IsType<ArrayAccessFilterSubExpression>(tr.Expression);
            ArrayAccessFilterSubExpression ar = tr.Expression as ArrayAccessFilterSubExpression;
            Assert.True(ar.ExactElementsAccess != null && ar.ExactElementsAccess.Length == 1 && ar.ExactElementsAccess[0] == 123);
        }
    }
}
