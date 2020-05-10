using JsonPathway.Internal;
using JsonPathway.Internal.Filters;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

        [Fact]
        public void TruthyFilterSubExpression_ReturnsCorrectValue()
        {
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("true || @.b"));
            Assert.IsType<LogicalFilterSubExpression>(exp);
            
            var truthy = (exp as LogicalFilterSubExpression).RightSide;
            Assert.IsType<TruthyFilterSubExpression>(truthy);

            JsonElement data = JsonDocument.Parse("{ \"b\" : 3 }").RootElement;
            JsonElement result = truthy.Execute(data);
            Assert.Equal(_true, result);

            data = JsonDocument.Parse("{ \"b\" : 0 }").RootElement;
            result = truthy.Execute(data);
            Assert.Equal(_false, result);
        }

        [Fact]
        public void NegationFilterSubExpression_ReturnsCorrectValue()
        {
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("!('Abc')"));
            Assert.IsType<NegationFilterSubExpression>(exp);

            JsonElement result = exp.Execute(_null);
            Assert.Equal(_false, result);

            exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("!('')"));
            Assert.IsType<NegationFilterSubExpression>(exp);

            result = exp.Execute(_null);
            Assert.Equal(_true, result);
        }

        [Theory]
        [InlineData("{ `a` : 3 }", true)]
        [InlineData("{ `b` : 3 }", true)]
        [InlineData("{ `c` : 3 }", false)]
        public void LogicalOr_ReturnsCorrectValue(string json, bool expected)
        {
            json = json.Replace("`", "\"");
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.a || @.b"));
            Assert.IsType<LogicalFilterSubExpression>(exp);
            Assert.True((exp as LogicalFilterSubExpression).IsOr);

            JsonElement data = JsonDocument.Parse(json).RootElement;
            JsonElement result = exp.Execute(data);

            if (expected)
            {
                Assert.Equal(_true, result);
            }
            else
            {
                Assert.Equal(_false, result);
            }
        }

        [Theory]
        [InlineData("{ `a` : 3 }", false)]
        [InlineData("{ `b` : 3 }", false)]
        [InlineData("{ `c` : 3 }", false)]
        [InlineData("{ `a` : 3, `b`: 3 }", true)]
        public void LogicalAnd_ReturnsCorrectValue(string json, bool expected)
        {
            json = json.Replace("`", "\"");
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.a && @.b"));
            Assert.IsType<LogicalFilterSubExpression>(exp);
            Assert.True((exp as LogicalFilterSubExpression).IsAnd);

            JsonElement data = JsonDocument.Parse(json).RootElement;
            JsonElement result = exp.Execute(data);

            if (expected)
            {
                Assert.Equal(_true, result);
            }
            else
            {
                Assert.Equal(_false, result);
            }
        }

        [Theory]
        [InlineData("{ `a`: 3, `b`: 3 }", true)]
        [InlineData("{ `a`: true, `b`: true }", true)]
        [InlineData("{ `a`: false, `b`: false }", true)]
        [InlineData("{ `a`: 1.1, `b`: 1.1 }", true)]
        [InlineData("{ `a`: `x`, `b`: `x` }", true)]
        [InlineData("{ `a`: 3, `b`: 4 }", false)]
        [InlineData("{ `a`: true, `b`: false }", false)]
        [InlineData("{ `a`: false, `b`: true }", false)]
        [InlineData("{ `a`: 1.1, `b`: 1.2 }", false)]
        [InlineData("{ `a`: `x`, `b`: `y` }", false)]
        [InlineData("{ `a`: `x`, `b`: 3 }", false)]
        public void ComparisonEqual_ReturnsCorrectValue(string json, bool expected)
        {
            json = json.Replace("`", "\"");
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.a == @.b"));
            Assert.IsType<ComparisonFilterSubExpression>(exp);
            Assert.True((exp as ComparisonFilterSubExpression).IsEqual);

            JsonElement data = JsonDocument.Parse(json).RootElement;
            JsonElement result = exp.Execute(data);

            if (expected)
            {
                Assert.Equal(_true, result);
            }
            else
            {
                Assert.Equal(_false, result);
            }
        }

        [Theory]
        [InlineData("{ `a`: 3, `b`: 3 }", false)]
        [InlineData("{ `a`: true, `b`: true }", false)]
        [InlineData("{ `a`: false, `b`: false }", false)]
        [InlineData("{ `a`: 1.1, `b`: 1.1 }", false)]
        [InlineData("{ `a`: `x`, `b`: `x` }", false)]
        [InlineData("{ `a`: 3, `b`: 4 }", true)]
        [InlineData("{ `a`: true, `b`: false }", true)]
        [InlineData("{ `a`: false, `b`: true }", true)]
        [InlineData("{ `a`: 1.1, `b`: 1.2 }", true)]
        [InlineData("{ `a`: `x`, `b`: `y` }", true)]
        [InlineData("{ `a`: `x`, `b`: 3 }", true)]
        public void ComparisonNotEqual_ReturnsCorrectValue(string json, bool expected)
        {
            json = json.Replace("`", "\"");
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.a != @.b"));
            Assert.IsType<ComparisonFilterSubExpression>(exp);
            Assert.True((exp as ComparisonFilterSubExpression).IsNotEqual);

            JsonElement data = JsonDocument.Parse(json).RootElement;
            JsonElement result = exp.Execute(data);

            if (expected)
            {
                Assert.Equal(_true, result);
            }
            else
            {
                Assert.Equal(_false, result);
            }
        }

        [Theory]
        [InlineData("{ `a`: 3, `b`: 3 }", false)]
        [InlineData("{ `a`: 2, `b`: 3 }", false)]
        [InlineData("{ `a`: -3, `b`: 3 }", false)]
        [InlineData("{ `a`: 33, `b`: 3 }", true)]
        public void ComparisonGreater_ReturnsCorrectValue(string json, bool expected)
        {
            json = json.Replace("`", "\"");
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.a > @.b"));
            Assert.IsType<ComparisonFilterSubExpression>(exp);
            Assert.True((exp as ComparisonFilterSubExpression).IsGreater);

            JsonElement data = JsonDocument.Parse(json).RootElement;
            JsonElement result = exp.Execute(data);

            if (expected)
            {
                Assert.Equal(_true, result);
            }
            else
            {
                Assert.Equal(_false, result);
            }
        }

        [Theory]
        [InlineData("{ `a`: 2, `b`: 3 }", false)]
        [InlineData("{ `a`: -3, `b`: 3 }", false)]
        [InlineData("{ `a`: 3, `b`: 3 }", true)]
        [InlineData("{ `a`: 33, `b`: 3 }", true)]
        public void ComparisonGreaterOrEqual_ReturnsCorrectValue(string json, bool expected)
        {
            json = json.Replace("`", "\"");
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.a >= @.b"));
            Assert.IsType<ComparisonFilterSubExpression>(exp);
            Assert.True((exp as ComparisonFilterSubExpression).IsGreaterOrEqual);

            JsonElement data = JsonDocument.Parse(json).RootElement;
            JsonElement result = exp.Execute(data);

            if (expected)
            {
                Assert.Equal(_true, result);
            }
            else
            {
                Assert.Equal(_false, result);
            }
        }

        [Theory]
        [InlineData("{ `a`: 3, `b`: 3 }", false)]
        [InlineData("{ `a`: 2, `b`: 3 }", true)]
        [InlineData("{ `a`: -3, `b`: 3 }", true)]
        [InlineData("{ `a`: 33, `b`: 3 }", false)]
        public void ComparisonLess_ReturnsCorrectValue(string json, bool expected)
        {
            json = json.Replace("`", "\"");
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.a < @.b"));
            Assert.IsType<ComparisonFilterSubExpression>(exp);
            Assert.True((exp as ComparisonFilterSubExpression).IsLess);

            JsonElement data = JsonDocument.Parse(json).RootElement;
            JsonElement result = exp.Execute(data);

            if (expected)
            {
                Assert.Equal(_true, result);
            }
            else
            {
                Assert.Equal(_false, result);
            }
        }

        [Theory]
        [InlineData("{ `a`: 2, `b`: 3 }", true)]
        [InlineData("{ `a`: -3, `b`: -2 }", true)]
        [InlineData("{ `a`: 3, `b`: 3 }", true)]
        [InlineData("{ `a`: 33, `b`: 3 }", false)]
        public void ComparisonLessOrEqual_ReturnsCorrectValue(string json, bool expected)
        {
            json = json.Replace("`", "\"");
            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.a <= @.b"));
            Assert.IsType<ComparisonFilterSubExpression>(exp);
            Assert.True((exp as ComparisonFilterSubExpression).IsLessOrEqual);

            JsonElement data = JsonDocument.Parse(json).RootElement;
            JsonElement result = exp.Execute(data);

            if (expected)
            {
                Assert.Equal(_true, result);
            }
            else
            {
                Assert.Equal(_false, result);
            }
        }

        [Fact]
        public void PropertyFilterSubExpression_LengthOnString_ReturnsCorrectValue()
        {
            string json = "{ \"a\": \"abc\" }";
            JsonElement data = JsonDocument.Parse(json).RootElement;

            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.a.length == @.b"));
            exp = (exp as ComparisonFilterSubExpression).LeftSide as PropertyFilterSubExpression;
            Assert.NotNull(exp);

            var expected = JsonElementFactory.CreateNumber(3);
            JsonElement result = exp.Execute(data);
            Assert.Equal(expected, result, JsonElementEqualityComparer.Default);
        }

        [Fact]
        public void PropertyFilterSubExpression_LengthOnArray_ReturnsCorrectValue()
        {
            string json = "{ \"a\": [ \"abc\" ] }";
            JsonElement data = JsonDocument.Parse(json).RootElement;

            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.a.length == @.b"));
            exp = (exp as ComparisonFilterSubExpression).LeftSide as PropertyFilterSubExpression;
            Assert.NotNull(exp);

            var expected = JsonElementFactory.CreateNumber(1);
            JsonElement result = exp.Execute(data);
            Assert.Equal(expected, result, JsonElementEqualityComparer.Default);
        }

        [Fact]
        public void PropertyFilterSubExpression_DeeplyNestedProperty_ReturnsCorrectValue()
        {
            string json = @"
                {
                    `a`: {
                        `b`: {
                            `c`: {
                                `d`: {
                                    `e`: 4
                                }
                            }
                        }
                    }        
                }"
                .Replace("`", "\"");
            JsonElement data = JsonDocument.Parse(json).RootElement;

            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.a.b.c.d.e == @.b"));
            exp = (exp as ComparisonFilterSubExpression).LeftSide as PropertyFilterSubExpression;
            Assert.NotNull(exp);

            var expected = JsonElementFactory.CreateNumber(4);
            JsonElement result = exp.Execute(data);
            Assert.Equal(expected, result, JsonElementEqualityComparer.Default);
        }

        [Fact]
        public void PropertyFilterSubExpression_WildcardOnObject_ReturnsCorrectValue()
        {
            string json = @"
                {
                    `x`: {
                        `y`: {
                            `z`: [`1`, `2`, `3`]
                        }
                    }
                }
                ".Replace("`", "\"");

            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.x.y.z.* == @.b"));
            exp = (exp as ComparisonFilterSubExpression).LeftSide as PropertyFilterSubExpression;
            Assert.NotNull(exp);

            JsonElement result = exp.Execute(JsonDocument.Parse(json).RootElement);
            AssertArraysEqual(result, "1", "2", "3");
        }

        [Fact]
        public void PropertyFilterSubExpression_RecursionOnObject_ReturnsCorrectValue()
        {
            string json = @"
                {
                    `x`: {
                        `y`: {
        	                `a`: 1,
        	                `b`: 2,
        	                `c`: 3
                        }
                    },
                    `z`: 123
                }
                ".Replace("`", "\"");

            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.. == @.b"));
            exp = (exp as ComparisonFilterSubExpression).LeftSide as PropertyFilterSubExpression;
            Assert.NotNull(exp);

            string expectedJson = @"
                [
                  {
                    `x`: {
                      `y`: {
                        `a`: 1,
                        `b`: 2,
                        `c`: 3
                      }
                    },
                    `z`: 123
                  },
                  {
                    `y`: {
                      `a`: 1,
                      `b`: 2,
                      `c`: 3
                    }
                  },
                  {
                    `a`: 1,
                    `b`: 2,
                    `c`: 3
                  }
                ]".Replace("`", "\"").RemoveWhiteSpace();

            JsonElement result = exp.Execute(JsonDocument.Parse(json).RootElement);
            string resultJson = JsonSerializer.Serialize(result);
            Assert.Equal(expectedJson, resultJson);
        }

        [Fact]
        public void PropertyFilterSubExpression_RecursionOnArray_ReturnsCorrectValue()
        {
            string json = @"
                                {
                    `x`: {
                        `y`: {
        	                `a`: 1,
        	                `b`: 2,
        	                `c`: 3,
        	                `d`: [ true, true ]
                        }
                    },
                    `z`: 123
                }
                ".Replace("`", "\"");

            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.. == @.b"));
            exp = (exp as ComparisonFilterSubExpression).LeftSide as PropertyFilterSubExpression;
            Assert.NotNull(exp);

            string expectedJson = @"
                [
                  {
                    `x`: {
                      `y`: {
                        `a`: 1,
                        `b`: 2,
                        `c`: 3,
                        `d`: [
                          true,
                          true
                        ]
                      }
                    },
                    `z`: 123
                  },
                  {
                    `y`: {
                      `a`: 1,
                      `b`: 2,
                      `c`: 3,
                      `d`: [
                        true,
                        true
                      ]
                    }
                  },
                  {
                    `a`: 1,
                    `b`: 2,
                    `c`: 3,
                    `d`: [
                      true,
                      true
                    ]
                  },
                  [
                    true,
                    true
                  ]
                ]".Replace("`", "\"").RemoveWhiteSpace();

            JsonElement result = exp.Execute(JsonDocument.Parse(json).RootElement);
            string resultJson = JsonSerializer.Serialize(result);
            Assert.Equal(expectedJson, resultJson);
        }

        [Fact]
        public void ArrayAccessFilterSubExpression_ExactIndex_ReturnsCorrectValue()
        {
            string json = @"
                {
                    `x`: [ `a`, `b`, `c`, `d`, `e`, `f`, `g`, `h`, `i` ]
                }
                ".Replace("`", "\"");

            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.x[2] == @.b"));
            exp = (exp as ComparisonFilterSubExpression).LeftSide as ArrayAccessFilterSubExpression;
            Assert.NotNull(exp);

            JsonElement data = JsonDocument.Parse(json).RootElement;
            JsonElement result = exp.Execute(data);
            AssertArraysEqual(result, "c");
        }

        [Fact]
        public void ArrayAccessFilterSubExpression_NegativeIndex_ReturnsCorrectValue()
        {
            string json = @"
                {
                    `x`: [ `a`, `b`, `c`, `d`, `e`, `f`, `g`, `h`, `i` ]
                }
                ".Replace("`", "\"");

            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.x[-2] == @.b"));
            exp = (exp as ComparisonFilterSubExpression).LeftSide as ArrayAccessFilterSubExpression;
            Assert.NotNull(exp);

            JsonElement data = JsonDocument.Parse(json).RootElement;
            JsonElement result = exp.Execute(data);
            AssertArraysEqual(result, "h");
        }

        [Fact]
        public void ArrayAccessFilterSubExpression_MultiplenIdexes_ReturnsCorrectValue()
        {
            string json = @"
                {
                    `x`: [ `a`, `b`, `c`, `d`, `e`, `f`, `g`, `h`, `i` ]
                }
                ".Replace("`", "\"");

            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.x[4, -2, -12, 0] == @.b"));
            exp = (exp as ComparisonFilterSubExpression).LeftSide as ArrayAccessFilterSubExpression;
            Assert.NotNull(exp);

            JsonElement data = JsonDocument.Parse(json).RootElement;
            JsonElement result = exp.Execute(data);
            AssertArraysEqual(result, "e", "h", "g", "a");
        }

        [Fact]
        public void ArrayAccessFilterSubExpression_AllIndexes_ReturnsCorrectValue()
        {
            string json = @"
                {
                    `x`: [ `a`, `b`, `c`, `d`, `e`, `f`, `g`, `h`, `i` ]
                }
                ".Replace("`", "\"");

            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.x[*] == @.b"));
            exp = (exp as ComparisonFilterSubExpression).LeftSide as ArrayAccessFilterSubExpression;
            Assert.NotNull(exp);

            JsonElement data = JsonDocument.Parse(json).RootElement;
            JsonElement result = exp.Execute(data);
            AssertArraysEqual(result, "a", "b", "c", "d", "e", "f", "g", "h", "i");
        }

        [Fact]
        public void ArrayAccessFilterSubExpression_Slice_ReturnsCorrectValue()
        {
            string json = @"
                {
                    `x`: [ `a`, `b`, `c`, `d`, `e`, `f`, `g`, `h`, `i` ]
                }
                ".Replace("`", "\"");

            FilterSubExpression exp = FilterParser.Parse(FilterExpressionTokenizer.Tokenize("@.x[0:7:2] == @.b"));
            exp = (exp as ComparisonFilterSubExpression).LeftSide as ArrayAccessFilterSubExpression;
            Assert.NotNull(exp);

            JsonElement data = JsonDocument.Parse(json).RootElement;
            JsonElement result = exp.Execute(data);
            AssertArraysEqual(result, "a", "c", "e", "g");
        }

        private void AssertArraysEqual(JsonElement result, params string[] expected)
        {
            Assert.Equal(JsonValueKind.Array, result.ValueKind);

            string[] resultArray = result.EnumerateArray().Select(x => x.GetString()).ToArray();

            Assert.Equal(expected.Length, resultArray.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], resultArray[i]);
            }
        }
    }
}
