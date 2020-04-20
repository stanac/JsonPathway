using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPathway.Internal.BoolExpressions
{
    internal static class BoolParser
    {
        public static IReadOnlyList<ExpressionToken> Parse(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) throw new ArgumentException("Value not set", nameof(expression));

            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(expression);
            List<ExpressionToken> expressionTokens = tokens
                .Select(x => new PrimitiveExpressionToken(x))
                .Where(x => !x.Token.IsWhiteSpaceToken())
                .Cast<ExpressionToken>()
                .ToList();

            expressionTokens = ReplaceConstantExpressionTokens(expressionTokens);
            expressionTokens = ReplacePropertyTokens(expressionTokens);
            expressionTokens = ReplaceMethodCallsTokens(expressionTokens);
            expressionTokens = ReplaceGroupTokens(expressionTokens);
            expressionTokens = ReplaceOperatorTokens(expressionTokens);

            EnsureTokensAreValid(expressionTokens);

            return expressionTokens;
        }

        private static List<ExpressionToken> ReplaceConstantExpressionTokens(List<ExpressionToken> tokens)
        {
            List<ExpressionToken> ret = new List<ExpressionToken>(tokens.Count);

            for (int i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i];
                if (t is PrimitiveExpressionToken pet)
                {
                    if (pet.Token.IsStringToken())
                    {
                        ret.Add(new ExpressionConstantStringToken(pet.Token.CastToStringToken()));
                    }
                    else if (pet.Token.IsBoolToken())
                    {
                        ret.Add(new ExpressionConstantBoolToken(pet.Token.CastToBoolToken()));
                    }
                    else if (pet.Token.IsNumberToken())
                    {
                        ret.Add(new ExpressionConstantNumberToken(pet.Token.CastToNumberToken()));
                    }
                    else
                    {
                        ret.Add(pet);
                    }
                }
                else
                {
                    ret.Add(t);
                }
            }

            return ret;
        }

        private static List<ExpressionToken> ReplacePropertyTokens(List<ExpressionToken> tokens)
        {
            return tokens;
        }

        private static List<ExpressionToken> ReplaceGroupTokens(List<ExpressionToken> tokens)
        {
            bool started = false;

            List<SymbolToken> openClosed = tokens.Where(x => x is PrimitiveExpressionToken pet && (pet.Token.IsSymbolToken('(') || pet.Token.IsSymbolToken(')')))
                .Cast<PrimitiveExpressionToken>()
                .Select(x => x.Token.CastToSymbolToken())
                .ToList();

            List<ExpressionToken> ret = tokens.ToList();

            for (int i = 0; i < ret.Count; i++)
            {
                if (ret[i] is PrimitiveExpressionToken pet)
                {
                    if (pet.Token.IsSymbolToken('('))
                    {
                        ret[i] = new OpenGroupToken(pet.Token.CastToSymbolToken(), )
                    }
                }
            }

            return ret;
        }

        private static List<ExpressionToken> ReplaceMethodCallsTokens(List<ExpressionToken> tokens)
        {
            return tokens;

        }

        private static List<ExpressionToken> ReplaceOperatorTokens(List<ExpressionToken> tokens)
        {
            return tokens;

        }

        private static void EnsureTokensAreValid(List<ExpressionToken> tokens)
        {
            

        }
    }
}
