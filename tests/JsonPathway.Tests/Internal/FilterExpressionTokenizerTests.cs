using JsonPathway.Internal.FilterExpressionTokens;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace JsonPathway.Tests.Internal
{
    public class FilterExpressionTokenizerTests
    {
        [Fact]
        public void ValidExpression_ReturnsValidTokens()
        {
            string input = "@.price >= 0 && (@.name.first.contains('a') || @['name'].contains(5) || !@.f)";

            IReadOnlyList<ExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);

            Assert.Equal(12, tokens.Count);

            Assert.IsType<PropertyExpressionToken>(tokens[0]);
            Assert.IsType<ComparisonOperatorExpressionToken>(tokens[1]);
            Assert.IsType<ConstantNumberExpressionToken>(tokens[2]);
            Assert.IsType<LogicalBinaryOperatorExpressionToken>(tokens[3]);
            Assert.IsType<OpenGroupToken>(tokens[4]);
            
            Assert.IsType<MethodCallExpressionToken>(tokens[5]);
            var mct = tokens[5] as MethodCallExpressionToken;
            Assert.Single(mct.Arguments);
            Assert.IsType<ConstantStringExpressionToken>(mct.Arguments.Single());
            Assert.Equal("a", (mct.Arguments.Single() as ConstantStringExpressionToken).StringValue);

            Assert.IsType<LogicalBinaryOperatorExpressionToken>(tokens[6]);
            Assert.IsType<MethodCallExpressionToken>(tokens[7]);
            Assert.IsType<LogicalBinaryOperatorExpressionToken>(tokens[8]);
            Assert.IsType<NegationExpressionToken>(tokens[9]);
            Assert.IsType<PropertyExpressionToken>(tokens[10]);
            Assert.IsType<CloseGroupToken>(tokens[11]);
        }

        [Fact]
        public void ExpressionWithMethodCallOnStringConstant_ReturnsValidTokens()
        {
            string input = "'a'.ToUpper() == 'A'";
            IReadOnlyList<ExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);

            Assert.Equal(3, tokens.Count);
            Assert.IsType<MethodCallExpressionToken>(tokens[0]);

            var mct = tokens[0] as MethodCallExpressionToken;

            Assert.Empty(mct.Arguments);
            Assert.IsType<ConstantStringExpressionToken>(mct.CalledOnExpression);
            var constant = mct.CalledOnExpression as ConstantStringExpressionToken;

            Assert.Equal("a", constant.StringValue);
        }

        [Fact]
        public void ExpressionWithMethodCallOnNonRoot_HasNonEmptyPropertyChain()
        {
            var input = "@.a.Something()";
            IReadOnlyList<ExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);

            Assert.Single(tokens);
            Assert.IsType<MethodCallExpressionToken>(tokens.Single());
            var mct = tokens.Single() as MethodCallExpressionToken;

            Assert.Equal("Something", mct.MethodName);
            Assert.Empty(mct.Arguments);
            Assert.IsType<PropertyExpressionToken>(mct.CalledOnExpression);
            PropertyExpressionToken callee = mct.CalledOnExpression as PropertyExpressionToken;
            Assert.Single(callee.PropertyChain);
            Assert.Equal("a", callee.PropertyChain.Single().StringValue);
        }

        [Fact]
        public void ExpressionWithMethodCallOnRoot_HasEmptyPropertyChain()
        {
            var input = "@.Something()";
            IReadOnlyList<ExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);

            Assert.Single(tokens);
            Assert.IsType<MethodCallExpressionToken>(tokens.Single());
            var mct = tokens.Single() as MethodCallExpressionToken;

            Assert.Equal("Something", mct.MethodName);
            Assert.Empty(mct.Arguments);
            Assert.IsType<PropertyExpressionToken>(mct.CalledOnExpression);
            PropertyExpressionToken callee = mct.CalledOnExpression as PropertyExpressionToken;
            Assert.Empty(callee.PropertyChain);
        }

        [Fact]
        public void ExpressionWithMethodCallWithMultipleArguments_ReturnsValidTokens()
        {
            string input = "'a'.ToUpper('abc', 123, true, @.CallingMethod())";
            IReadOnlyList<ExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);

            Assert.Single(tokens);
            Assert.IsType<MethodCallExpressionToken>(tokens.Single());
            
            var mct = tokens.Single() as MethodCallExpressionToken;
            
            Assert.IsType<ConstantStringExpressionToken>(mct.CalledOnExpression);
            Assert.Equal(4, mct.Arguments.Length);
            
            Assert.IsType<ConstantStringExpressionToken>(mct.Arguments[0]);
            Assert.Equal("abc", (mct.Arguments[0] as ConstantStringExpressionToken).StringValue);

            Assert.IsType<ConstantNumberExpressionToken>(mct.Arguments[1]);
            Assert.Equal(123.0, (mct.Arguments[1] as ConstantNumberExpressionToken).Token.NumberValue, 6);

            Assert.IsType<ConstantBoolExpressionToken>(mct.Arguments[2]);
            Assert.True((mct.Arguments[2] as ConstantBoolExpressionToken).Token.BoolValue);

            Assert.IsType<MethodCallExpressionToken>(mct.Arguments[3]);
            var innerMct = mct.Arguments[3] as MethodCallExpressionToken;
            Assert.IsType<PropertyExpressionToken>(innerMct.CalledOnExpression);
            Assert.Empty((innerMct.CalledOnExpression as PropertyExpressionToken).PropertyChain);
            Assert.Equal("CallingMethod", innerMct.MethodName);
        }

        [Theory]
        [InlineData("@.a.DoIt(true,,true)")]
        [InlineData("@.a.DoIt(true true)")]
        [InlineData("@.a.DoIt(true,true,)")]
        public void ExpressionWithInvalidMethodCall_ThrowsException(string input)
        {
            Assert.Throws<UnexpectedTokenException>(() => FilterExpressionTokenizer.Tokenize(input));
        }

        [Fact]
        public void ExpressionMethodCallWithMultipleArgs_ReturnsCorrectTokens()
        {
            string input = "'a'.ToUpper0('abc', true, false)";
            IReadOnlyList<ExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);
            Assert.Equal(3, (tokens.Single() as MethodCallExpressionToken).Arguments.Length);
            
            input = "'a'.ToUpper0('abc', true, false, 1, 2, 3.3, 4)";
            tokens = FilterExpressionTokenizer.Tokenize(input);
            Assert.Equal(7, (tokens.Single() as MethodCallExpressionToken).Arguments.Length);
        }

        [Fact]
        public void ExpressionWithChainedMethodCalls_ReturnValidToken()
        {
            string input = "'a'.ToUpper0().ToUpper1(1).ToUpper2(7, 9.12).ToUpper3('abc', true, false, false, 'A') == \"A\"";
            IReadOnlyList<ExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);

            Assert.Equal(3, tokens.Count);
            Assert.IsType<MethodCallExpressionToken>(tokens.First());

            var toUpper3 = tokens.First() as MethodCallExpressionToken;
            var toUpper2 = toUpper3.CalledOnExpression as MethodCallExpressionToken;
            var toUpper1 = toUpper2.CalledOnExpression as MethodCallExpressionToken;
            var toUpper0 = toUpper1.CalledOnExpression as MethodCallExpressionToken;

            Assert.Equal("ToUpper0", toUpper0.MethodName);
            Assert.Equal("ToUpper1", toUpper1.MethodName);
            Assert.Equal("ToUpper2", toUpper2.MethodName);
            Assert.Equal("ToUpper3", toUpper3.MethodName);

            Assert.Empty(toUpper0.Arguments);

            Assert.Single(toUpper1.Arguments);
            Assert.Equal(1.0, (toUpper1.Arguments[0] as ConstantNumberExpressionToken).Token.NumberValue, 6);

            Assert.Equal(2, toUpper2.Arguments.Length);
            Assert.Equal(7.0, (toUpper2.Arguments[0] as ConstantNumberExpressionToken).Token.NumberValue, 6);
            Assert.Equal(9.12, (toUpper2.Arguments[1] as ConstantNumberExpressionToken).Token.NumberValue, 6);

            Assert.Equal(5, toUpper3.Arguments.Length);
            Assert.Equal("abc", (toUpper3.Arguments[0] as ConstantStringExpressionToken).Token.StringValue);
            Assert.True((toUpper3.Arguments[1] as ConstantBoolExpressionToken).Token.BoolValue);
            Assert.False((toUpper3.Arguments[2] as ConstantBoolExpressionToken).Token.BoolValue);
            Assert.False((toUpper3.Arguments[3] as ConstantBoolExpressionToken).Token.BoolValue);
            Assert.Equal("A", (toUpper3.Arguments[4] as ConstantStringExpressionToken).Token.StringValue);
        }

        [Fact]
        public void ExpressionWithMethodCallWithMixedArgs_ReturnsValidTokens()
        {
            string input = "@.a.c.Call1(@.call2(123, false).call3(), true)";

            IReadOnlyList<ExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);

            Assert.Single(tokens);
            Assert.IsType<MethodCallExpressionToken>(tokens.Single());

            var call1 = tokens.Single() as MethodCallExpressionToken;
            Assert.IsType<PropertyExpressionToken>(call1.CalledOnExpression);
            var prop1 = call1.CalledOnExpression as PropertyExpressionToken;
            Assert.Equal(2, prop1.PropertyChain.Length);
            Assert.Equal("a", prop1.PropertyChain[0].StringValue);
            Assert.Equal("c", prop1.PropertyChain[1].StringValue);
            Assert.Equal("Call1", call1.MethodName);

            Assert.Equal(2, call1.Arguments.Length);
            Assert.IsType<ConstantBoolExpressionToken>(call1.Arguments.Last());
            Assert.True(call1.Arguments.Last() is ConstantBoolExpressionToken cbt && cbt.Token.BoolValue);

            Assert.IsType<MethodCallExpressionToken>(call1.Arguments.First());
            var call3 = call1.Arguments.First() as MethodCallExpressionToken;
            Assert.Equal("call3", call3.MethodName);
            Assert.Empty(call3.Arguments);

            Assert.IsType<MethodCallExpressionToken>(call3.CalledOnExpression);
            var call2 = call3.CalledOnExpression as MethodCallExpressionToken;
            Assert.Equal("call2", call2.MethodName);
            Assert.IsType<PropertyExpressionToken>(call2.CalledOnExpression);
            Assert.Empty((call2.CalledOnExpression as PropertyExpressionToken).PropertyChain);
            Assert.Equal(2, call2.Arguments.Length);

            Assert.True(call2.Arguments[0] is ConstantNumberExpressionToken arg1 && arg1.Token.NumberValue == 123.0);
            Assert.True(call2.Arguments[1] is ConstantBoolExpressionToken arg2 && !arg2.Token.BoolValue);
        }

        [Fact]
        public void ExpressionWithNestedMethodCalls_ReturnsValidTokens()
        {
            string input = "@.call(@.a.SubString(@.a.b.GetLength(@.a.c.Something())))";

            IReadOnlyList<ExpressionToken> tokens = FilterExpressionTokenizer.Tokenize(input);

            Assert.Single(tokens);
            Assert.IsType<MethodCallExpressionToken>(tokens[0]);

            var mct = tokens[0] as MethodCallExpressionToken;
            Assert.Single(mct.Arguments);
            Assert.IsType<MethodCallExpressionToken>(mct.Arguments.Single());
            Assert.IsType<PropertyExpressionToken>(mct.CalledOnExpression);

            var arg = mct.Arguments[0] as MethodCallExpressionToken;
            Assert.Single(arg.Arguments);
            Assert.IsType<MethodCallExpressionToken>(arg.Arguments[0]);
            Assert.IsType<PropertyExpressionToken>(arg.CalledOnExpression);

            var innerArg = arg.Arguments[0] as MethodCallExpressionToken;
            Assert.Single(innerArg.Arguments);
            Assert.IsType<MethodCallExpressionToken>(innerArg.Arguments[0]);
            Assert.IsType<PropertyExpressionToken>(innerArg.CalledOnExpression);

            var innerInnerArg = innerArg.Arguments[0] as MethodCallExpressionToken;
            Assert.Empty(innerInnerArg.Arguments);
            Assert.IsType<PropertyExpressionToken>(innerInnerArg.CalledOnExpression);
        }

        [Fact]
        public void ExpressionWithLogicalOperators_ReturnsTokensOfValidTypes()
        {
            string input = "@.a || (@.b > 3 && @.b <= 5)";
            var tokens = FilterExpressionTokenizer.Tokenize(input);

            Type[] tokenTypes = new[]
            {
                typeof(PropertyExpressionToken),
                typeof(LogicalBinaryOperatorExpressionToken),
                typeof(OpenGroupToken),
                typeof(PropertyExpressionToken),
                typeof(ComparisonOperatorExpressionToken),
                typeof(ConstantNumberExpressionToken),
                typeof(LogicalBinaryOperatorExpressionToken),
                typeof(PropertyExpressionToken),
                typeof(ComparisonOperatorExpressionToken),
                typeof(ConstantNumberExpressionToken),
                typeof(CloseGroupToken)
            };

            Assert.Equal(tokenTypes.Length, tokens.Count);

            for (int i = 0; i < tokens.Count; i++)
            {
                Assert.IsType(tokenTypes[i], tokens[i]);
            }
        }
    }
}
