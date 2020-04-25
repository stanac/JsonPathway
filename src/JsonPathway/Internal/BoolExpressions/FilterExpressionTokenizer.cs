using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPathway.Internal.BoolExpressions
{
    internal static class FilterExpressionTokenizer
    {
        public static IReadOnlyList<ExpressionToken> Tokenize(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) throw new ArgumentException("Value not set", nameof(expression));

            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(expression);
            List<ExpressionToken> expressionTokens = tokens
                .Select(x => new PrimitiveExpressionToken(x))
                .Where(x => !x.Token.IsWhiteSpaceToken())
                .Cast<ExpressionToken>()
                .ToList();

            return Tokenize(expressionTokens);
        }

        public static IReadOnlyList<ExpressionToken> Tokenize(IReadOnlyList<ExpressionToken> tokens)
        {
            var expressionTokens = ReplaceConstantExpressionTokens(tokens.ToList());
            expressionTokens = ReplacePropertyTokens(expressionTokens);
            expressionTokens = ReplaceGroupTokens(expressionTokens);
            expressionTokens = ReplaceMethodCallsTokens(expressionTokens);
            expressionTokens = ReplaceBinaryOperatorTokens(expressionTokens);
            expressionTokens = ReplaceNegationTokens(expressionTokens);

            EnsureTokensAreValid(expressionTokens);
            return expressionTokens;
        }

        /// <summary>
        /// Finds and replaces constants (bool, number, string constants)
        /// </summary>
        /// <param name="tokens">tokens input</param>
        /// <returns>tokens</returns>
        private static List<ExpressionToken> ReplaceConstantExpressionTokens(List<ExpressionToken> tokens)
        {
            List<ExpressionToken> ret = new List<ExpressionToken>(tokens.Count);

            for (int i = 0; i < tokens.Count; i++)
            {
                ExpressionToken t = tokens[i];

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

        /// <summary>
        /// Replaces chained property tokens with expression property tokens
        /// </summary>
        /// <param name="tokens">tokens input</param>
        /// <returns>tokens</returns>
        private static List<ExpressionToken> ReplacePropertyTokens(List<ExpressionToken> tokens)
        {
            List<PrimitiveExpressionToken> @symbols = tokens
                .Where(x => x is PrimitiveExpressionToken pet && pet.Token.IsSymbolToken('@'))
                .Cast<PrimitiveExpressionToken>()
                .ToList();

            if (!symbols.Any()) return tokens;

            List<(PropertyExpressionToken prop, List<ExpressionToken> primitives)> replacements = new List<(PropertyExpressionToken, List<ExpressionToken>)>();
            
            foreach (var st in @symbols)
            {
                List<PrimitiveExpressionToken> tokensForReplacement = new List<PrimitiveExpressionToken>();
                tokensForReplacement.Add(st);

                int index = tokens.IndexOf(st);
                index++;

                bool isPreviousDot = false;

                while (index < tokens.Count)
                {
                    var t = tokens[index];

                    isPreviousDot = index > 0 && (tokens[index - 1] is PrimitiveExpressionToken pet3 && pet3.Token.IsSymbolToken('.'));

                    bool isDot = t is PrimitiveExpressionToken pet && pet.Token.IsSymbolToken('.');
                    if (isDot)
                    {
                        if (isPreviousDot) throw new UnexpectedTokenException((t as PrimitiveExpressionToken).Token);
                        
                        index++;
                        continue;
                    }

                    if (t is PrimitiveExpressionToken pet2 && pet2.Token.IsPropertyToken())
                    {
                        PropertyToken prop = pet2.Token.CastToPropertyToken();

                        if (prop.Escaped && isPreviousDot) throw new UnexpectedTokenException(prop);

                        tokensForReplacement.Add(t as PrimitiveExpressionToken);
                        index++;
                        continue;
                    }

                    break;
                }

                PropertyExpressionToken propEx = new PropertyExpressionToken(tokensForReplacement.Where(x => ! x.Token.IsSymbolToken('@')).Select(x => x.Token.CastToPropertyToken()).ToArray());

                replacements.Add((propEx, tokensForReplacement.Cast<ExpressionToken>().ToList()));
            }

            var ret = tokens.ToList();

            foreach (var r in replacements)
            {
                List<int> indexes = r.primitives.Select(x => ret.IndexOf(x)).ToList();

                for (int i = indexes.First(); i <= indexes.Last(); i++)
                {
                    ret[i] = null;
                }
                
                ret[indexes[0]] = r.prop;
            }

            return ret.Where(x => x != null).ToList();
        }

        /// <summary>
        /// Replaces > < == >= <= != with operators
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private static List<ExpressionToken> ReplaceBinaryOperatorTokens(List<ExpressionToken> tokens)
        {
            List<List<PrimitiveExpressionToken>> symbolGroups = new List<List<PrimitiveExpressionToken>>();

            if (tokens.First() is PrimitiveExpressionToken fpet && fpet.Token.IsSymbolToken()) throw new UnexpectedTokenException(fpet.Token);
            if (tokens.Last() is PrimitiveExpressionToken lpet && lpet.Token.IsSymbolToken()) throw new UnexpectedTokenException(lpet.Token);

            for (int i = 1; i < tokens.Count - 1; i++)
            {
                bool previousWasSymbol = tokens[i - 1] is PrimitiveExpressionToken ppet && ppet.Token.IsSymbolToken();

                if (tokens[i] is PrimitiveExpressionToken pet && pet.Token.IsSymbolToken() && !pet.Token.IsSymbolToken('!'))
                {
                    if (!previousWasSymbol)
                    {
                        symbolGroups.Add(new List<PrimitiveExpressionToken>());
                    }

                    symbolGroups.Last().Add(pet);
                }
            }

            if (!symbolGroups.Any()) return tokens;

            var ret = tokens.ToList();

            foreach (List<PrimitiveExpressionToken> group in symbolGroups)
            {
                if (group.Count == 2 || (group.Count == 1 && !group[0].Token.IsSymbolToken('!')))
                {
                    OperatorExpressionToken ct = OperatorExpressionToken.Create(group.Select(x => x.Token.CastToSymbolToken()).ToArray());

                    int firstIndex = ret.IndexOf(group.First());
                    int lastIndex = ret.IndexOf(group.Last());

                    for (int i = firstIndex + 1; i <= lastIndex; i++)
                    {
                        ret[i] = null;
                    }

                    ret[firstIndex] = ct;
                }
                else if (group.Count > 2)
                {
                    string sequence = new string(group.Select(x => x.Token.CastToSymbolToken().StringValue[0]).ToArray());
                    throw new UnrecognizedCharSequence($"Unrecognized symbol sequence {sequence} starting at {group[0].Token.StartIndex}");
                }
            }

            return ret.Where(x => x != null).ToList();
        }

        /// <summary>
        /// Replaces ( and ) with open and close group tokens
        /// </summary>
        /// <param name="tokens">tokens input</param>
        /// <returns>tokens</returns>
        private static List<ExpressionToken> ReplaceGroupTokens(List<ExpressionToken> tokens)
        {
            List<SymbolToken> openClosed = tokens.Where(x => x is PrimitiveExpressionToken pet && (pet.Token.IsSymbolToken('(') || pet.Token.IsSymbolToken(')')))
                .Cast<PrimitiveExpressionToken>()
                .Select(x => x.Token.CastToSymbolToken())
                .ToList();

            if (openClosed.Any())
            {
                if (openClosed.First().IsSymbolToken(')')) throw new UnexpectedTokenException(openClosed.First());
                if (openClosed.Last().IsSymbolToken('(')) throw new UnexpectedTokenException(openClosed.Last());

                int openCount = openClosed.Count(x => x.IsSymbolToken('('));
                int closedCount = openClosed.Count(x => x.IsSymbolToken(')'));

                if (openCount > closedCount) throw new UnexpectedTokenException("Not all ( are closed with )");
                if (openCount < closedCount) throw new UnexpectedTokenException("Not all ) have matching (");
            }
            else
            {
                return tokens;
            }

            List<ExpressionToken> ret = tokens.ToList();
            Stack<int> groupIds = new Stack<int>();

            for (int i = 0; i < ret.Count; i++)
            {
                if (ret[i] is PrimitiveExpressionToken pet)
                {
                    if (pet.Token.IsSymbolToken('('))
                    {
                        groupIds.Push(i);
                        ret[i] = new OpenGroupToken(pet.Token.CastToSymbolToken(), i);
                    }
                    else if (pet.Token.IsSymbolToken(')'))
                    {
                        ret[i] = new CloseGroupToken(pet.Token.CastToSymbolToken(), groupIds.Pop());
                    }
                }
            }

            return ret;
        }

        private static List<ExpressionToken> ReplaceMethodCallsTokens(List<ExpressionToken> tokens)
        {
            List<PropertyExpressionToken> propTokens = tokens.Where(x => x is PropertyExpressionToken).Cast<PropertyExpressionToken>().ToList();
            List<ExpressionToken> ret = tokens.ToList();

            foreach (PropertyExpressionToken pt in propTokens)
            {
                int nextIndex = tokens.IndexOf(pt) + 1;
                if (nextIndex < tokens.Count && tokens[nextIndex] is OpenGroupToken)
                {
                    OpenGroupToken open = tokens[nextIndex] as OpenGroupToken;
                    CloseGroupToken close = tokens.First(x => x is CloseGroupToken cgt && cgt.GroupId == open.GroupId) as CloseGroupToken;

                    int openIndex = tokens.IndexOf(open);
                    int closeIndex = tokens.IndexOf(close);

                    List<ExpressionToken> arguments = new List<ExpressionToken>();
                    for (int i = openIndex + 1; i < closeIndex; i++)
                    {
                        arguments.Add(tokens[i]);
                    }

                    string methodName;
                    PropertyExpressionToken allButLast = pt.AllButLast(out methodName);
                    int ptIndex = tokens.IndexOf(pt);

                    ret[ptIndex] = new MethodCallToken(pt, methodName, arguments.ToArray());

                    List<ExpressionToken> toRemove = new List<ExpressionToken>();

                    for (int i = ptIndex + 1; i <= closeIndex; i++)
                    {
                        ret[i] = null;
                    }
                }
            }

            return ret.Where(x => x != null).ToList();
        }

        private static List<ExpressionToken> ReplaceNegationTokens(List<ExpressionToken> tokens)
        {
            var ret = tokens.ToList();

            for (int i = 0; i < tokens.Count; i++)
            {
                if (ret[i] is PrimitiveExpressionToken pet && pet.Token.IsSymbolToken('!'))
                {
                    ret[i] = new NegationExpressionToken(pet.Token.CastToSymbolToken());
                }
            }

            return ret;
        }

        private static void EnsureTokensAreValid(List<ExpressionToken> tokens)
        {
            var primitiveToken = tokens.FirstOrDefault(x => x is PrimitiveExpressionToken) as PrimitiveExpressionToken;

            if (primitiveToken != null)
            {
                throw new UnexpectedTokenException(primitiveToken.Token);
            }
        }
    }
}
