using JsonPathway.Internal;
using JsonPathway.Internal.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace JsonPathway.Tests.Internal
{
    public class FilterSubExpressionExecutionTests
    {
        private static JsonElement _null = JsonElementFactory.CreateNull();
        private static JsonElement _true = JsonElementFactory.CreateBool(true);
        private static JsonElement _false = JsonElementFactory.CreateBool(false);
        private static JsonElement _number1_22 = JsonElementFactory.CreateNumber(1.22);
        private static JsonElement _number3344 = JsonElementFactory.CreateNumber(3344);
        private static JsonElement _numberNegative44 = JsonElementFactory.CreateNumber(-44);
        private static JsonElement _stringAbc = JsonElementFactory.CreateString("Abc");
        private static JsonElement _stringX123 = JsonElementFactory.CreateString("x123");

        [Fact]
        public void BooleanConstantFilterSubExpression_ReturnsCorrectValue()
        {
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("true"));
            Assert.IsType<BooleanConstantFilterSubExpression>(exp);

            JsonElement result = exp.Execute(_null);
            Assert.Equal(_true, result, JsonElementEqualityComparer.Default);

            exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("false"));
            Assert.IsType<BooleanConstantFilterSubExpression>(exp);

            result = exp.Execute(_null);
            Assert.Equal(_false, result, JsonElementEqualityComparer.Default);
        }

        [Fact]
        public void NumberConstantFilterSubExpression_ReturnsCorrectValue()
        {
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("1.22"));
            Assert.IsType<NumberConstantFilterSubExpression>(exp);

            JsonElement result = exp.Execute(_null);
            Assert.Equal(_number1_22, result, JsonElementEqualityComparer.Default);

            exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("3344"));
            Assert.IsType<NumberConstantFilterSubExpression>(exp);
            
            result = exp.Execute(_null);
            Assert.Equal(_number3344, result, JsonElementEqualityComparer.Default);

            exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("-44"));
            Assert.IsType<NumberConstantFilterSubExpression>(exp);
            
            result = exp.Execute(_null);
            Assert.Equal(_numberNegative44, result, JsonElementEqualityComparer.Default);
        }

        [Fact]
        public void StringConstantFilterSubExpression_ReturnsCorrectValue()
        {
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("'Abc'"));
            Assert.IsType<StringConstantFilterSubExpression>(exp);

            JsonElement result = exp.Execute(_null);
            Assert.Equal(_stringAbc, result, JsonElementEqualityComparer.Default);
            Assert.NotEqual(_stringX123, result, JsonElementEqualityComparer.Default);

            exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("\"x123\""));
            Assert.IsType<StringConstantFilterSubExpression>(exp);
            
            result = exp.Execute(_null);
            Assert.Equal(_stringX123, result, JsonElementEqualityComparer.Default);
            Assert.NotEqual(_stringAbc, result, JsonElementEqualityComparer.Default);
        }

        [Fact]
        public void GroupFilterSubExpression_ReturnsCorrectValue()
        {
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("('Abc')"));
            Assert.IsType<GroupFilterSubExpression>(exp);

            JsonElement result = exp.Execute(_null);
            Assert.Equal(_stringAbc, result, JsonElementEqualityComparer.Default);
        }

        continue;
    }
}
