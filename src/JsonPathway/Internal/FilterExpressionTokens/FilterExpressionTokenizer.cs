using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPathway.Internal.FilterExpressionTokens
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
            // expressionTokens = TokenizeInnerTokens(expressionTokens);

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
                        ret.Add(new ConstantStringExpressionToken(pet.Token.CastToStringToken()));
                    }
                    else if (pet.Token.IsBoolToken())
                    {
                        ret.Add(new ConstantBoolExpressionToken (pet.Token.CastToBoolToken()));
                    }
                    else if (pet.Token.IsNumberToken())
                    {
                        ret.Add(new ConstantNumberExpressionToken(pet.Token.CastToNumberToken()));
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

                PropertyExpressionToken propEx = new PropertyExpressionToken(
                    tokensForReplacement.Where(x => !x.Token.IsSymbolToken('@')).Select(x => x.Token.CastToPropertyToken()).ToArray(),
                    tokensForReplacement.First().StartIndex
                    );

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
            tokens = ReplaceMethodCallsOnProps(tokens);
            tokens = ReplaceMethodCallsOnConstants(tokens);
            return ReplaceMethodCallsOnMethods(tokens);
        }

        private static List<ExpressionToken> ReplaceMethodCallsOnProps(List<ExpressionToken> tokens)
        {
            List<PropertyExpressionToken> propTokens = tokens.Where(x => x is PropertyExpressionToken).Cast<PropertyExpressionToken>().ToList();
            var ret = tokens.ToList();

            foreach (PropertyExpressionToken pt in propTokens)
            {
                int nextIndex = ret.IndexOf(pt) + 1;
                if (nextIndex < ret.Count && ret[nextIndex] is OpenGroupToken)
                {
                    OpenGroupToken open = ret[nextIndex] as OpenGroupToken;
                    CloseGroupToken close = ret.First(x => x is CloseGroupToken cgt && cgt.GroupId == open.GroupId) as CloseGroupToken;

                    int openIndex = ret.IndexOf(open);
                    int closeIndex = ret.IndexOf(close);

                    List<ExpressionToken> args = new List<ExpressionToken>();
                    for (int i = openIndex + 1; i < closeIndex; i++)
                    {
                        args.Add(ret[i]);
                    }
                    args = FormatAndValidateMethodArgumentTokens(args);

                    string methodName;
                    PropertyExpressionToken allButLast = pt.AllButLast(out methodName);
                    int ptIndex = ret.IndexOf(pt);

                    ret[ptIndex] = new MethodCallExpressionToken(allButLast, methodName, args.ToArray());

                    List<ExpressionToken> toRemove = new List<ExpressionToken>();

                    for (int i = ptIndex + 1; i <= closeIndex; i++)
                    {
                        ret[i] = null;
                    }
                }
            }

            return ret.Where(x => x != null).ToList();
        }

        private static List<ExpressionToken> ReplaceMethodCallsOnConstants(List<ExpressionToken> tokens)
        {
            List<ExpressionToken> ret = tokens.ToList();

            foreach (ConstantBaseExpressionToken ct in tokens.Where(x => x is ConstantBaseExpressionToken))
            {
                int index = ret.IndexOf(ct);

                if (index < ret.Count - 4 && ret[index + 1] is PrimitiveExpressionToken pet1 && pet1.Token.IsSymbolToken('.')
                    && ret[index + 2] is PrimitiveExpressionToken pet2 && pet2.Token.IsPropertyToken()
                    && ret[index + 3] is OpenGroupToken)
                {
                    var open = ret[index + 3] as OpenGroupToken;
                    var close = ret.Single(x => x is CloseGroupToken cgt && cgt.GroupId == open.GroupId);

                    var methodName = (ret[index + 2] as PrimitiveExpressionToken).Token.CastToPropertyToken().StringValue;

                    var args = new List<ExpressionToken>();

                    int closeIndex = ret.IndexOf(close);
                    for (int i = index + 4; i < closeIndex; i++)
                    {
                        args.Add(ret[i]);
                    }
                    args = FormatAndValidateMethodArgumentTokens(args);

                    ret[index] = new MethodCallExpressionToken(ct, methodName, args.ToArray());

                    for (int i = index + 1; i <= closeIndex; i++)
                    {
                        ret[i] = null;
                    }
                }
            }

            return ret.Where(x => x != null).ToList();
        }

        private static List<ExpressionToken> ReplaceMethodCallsOnMethods(List<ExpressionToken> tokens)
        {
            int callCount = 0;
            int replacedCount;

            do
            {
                tokens = ReplaceMethodCallsOnMethodsInner(tokens, ref callCount, out replacedCount);
            }
            while (replacedCount > 0);

            return tokens;
        }

        private static List<ExpressionToken> ReplaceMethodCallsOnMethodsInner(List<ExpressionToken> tokens, ref int callCount, out int replacedCount)
        {
            callCount++;
            replacedCount = 0;

            if (callCount > 10 * 1000)
            {
                throw new InternalJsonPathwayException(
                    "Number of calls to ReplaceMethodCallsOnMethodsInner exceeded max expected number of 10000, possible stack overflow or infinite loop.");
            }

            List<ExpressionToken> ret = tokens.ToList();

            foreach (MethodCallExpressionToken mc in tokens.Where(x => x is MethodCallExpressionToken))
            {
                int index = ret.IndexOf(mc);

                if (index < ret.Count - 4 && ret[index + 1] is PrimitiveExpressionToken pet1 && pet1.Token.IsSymbolToken('.')
                    && ret[index + 2] is PrimitiveExpressionToken pet2 && pet2.Token.IsPropertyToken()
                    && ret[index + 3] is OpenGroupToken)
                {
                    var open = ret[index + 3] as OpenGroupToken;
                    var close = ret.Single(x => x is CloseGroupToken cgt && cgt.GroupId == open.GroupId);

                    var methodName = (ret[index + 2] as PrimitiveExpressionToken).Token.CastToPropertyToken().StringValue;

                    var args = new List<ExpressionToken>();

                    int closeIndex = ret.IndexOf(close);
                    for (int i = index + 4; i < closeIndex; i++)
                    {
                        args.Add(ret[i]);
                    }
                    args = FormatAndValidateMethodArgumentTokens(args);

                    ret[index] = new MethodCallExpressionToken(mc, methodName, args.ToArray());

                    for (int i = index + 1; i <= closeIndex; i++)
                    {
                        ret[i] = null;
                    }

                    replacedCount++;
                }
            }

            return ret.Where(x => x != null).ToList();
        }

        private static List<ExpressionToken> FormatAndValidateMethodArgumentTokens(List<ExpressionToken> tokens)
        {
            if (tokens.Count == 0) return tokens;

            bool IsComma(ExpressionToken t) => t is PrimitiveExpressionToken pt && pt.Token.IsSymbolToken(',');
            bool IsPartOfGroup(ExpressionToken t)
            {
                int index = tokens.IndexOf(t);
                List<int> openGroups = new List<int>();

                for (int i = 0; i < index; i++)
                {
                    if (tokens[i] is OpenGroupToken ogt)
                    {
                        openGroups.Add(ogt.GroupId);
                    }
                    else if (tokens[i] is CloseGroupToken cgt)
                    {
                        openGroups.Remove(cgt.GroupId);
                    }
                }

                return openGroups.Any();
            }
            SymbolToken GetComma(ExpressionToken t) => (t as PrimitiveExpressionToken).Token.CastToSymbolToken();

            if (IsComma(tokens.First()))
            {
                throw new UnexpectedTokenException(GetComma(tokens.First()));
            }
            if (IsComma(tokens.Last()))
            {
                throw new UnexpectedTokenException(GetComma(tokens.Last()));
            }

            for (int i = 0; i < tokens.Count - 1; i ++)
            {
                if (IsComma(tokens[i]) && IsComma(tokens[i + 1]))
                {
                    throw new UnexpectedTokenException(GetComma(tokens[i + 1]));
                }
            }

            var groups = new List<List<ExpressionToken>>();
            groups.Add(new List<ExpressionToken>());
            
            foreach (var token in tokens)
            {
                if (IsComma(token) && !IsPartOfGroup(token))
                {
                    groups.Add(new List<ExpressionToken>());
                }
                else
                {
                    groups.Last().Add(token);
                }
            }

            groups = groups.Where(x => x.Count > 0).ToList();

            List<ExpressionToken> ret = new List<ExpressionToken>();

            foreach (var g in groups)
            {
                var args = Tokenize(g);
                if (args.Count > 1)
                {
                    throw new UnexpectedTokenException(args[1]);
                }
                ret.Add(args.Single());
            }

            return ret;
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

        //internal static List<ExpressionToken> TokenizeInnerTokens(List<ExpressionToken> tokens)
        //{
        //    int callCount = 0;
        //    return TokenizeInnerTokens(tokens, ref callCount);
        //}

        //internal static List<ExpressionToken> TokenizeInnerTokens(List<ExpressionToken> tokens, ref int numberOfCalls)
        //{
        //    numberOfCalls++;

        //    if (numberOfCalls > 10 * 1000)
        //    {
        //        throw new InternalJsonPathwayException(
        //            "Number of calls to TokenizeInnerTokens exceeded max expected number of 10000, possible stack overflow or infinite loop.");
        //    }

        //    var ret = tokens.ToList();

        //    foreach (MethodCallExpressionToken t in ret.Where(x => x is MethodCallExpressionToken))
        //    {
        //        ExpressionToken[] inner = t.Arguments;
        //        if (inner.Any())
        //        {
        //            var replacement1 = Tokenize(inner);
        //            t.ReplaceArgumentTokens(replacement1);
        //        }

        //        var calleeMethod = t.CalledOnExpression as MethodCallExpressionToken;
        //        if (calleeMethod != null && calleeMethod.Arguments.Any())
        //        {
        //            var replacement2 = Tokenize(calleeMethod.Arguments);
        //            calleeMethod.ReplaceArgumentTokens(replacement2);
        //        }
        //    }

        //    return ret;
        //}

        private static void EnsureTokensAreValid(IEnumerable<ExpressionToken> tokens)
        {
            int callCount = 0;
            EnsureTokensAreValidInner(tokens, ref callCount);
            EnsureMethodArgumentsAreValid(tokens);
        }

        private static void EnsureTokensAreValidInner(IEnumerable<ExpressionToken> tokens, ref int numberOfCalls)
        {
            numberOfCalls++;

            if (numberOfCalls > 10 * 1000)
            {
                throw new InternalJsonPathwayException(
                    "Number of calls to EnsureTokensAreValidInner exceeded max expected number of 10000, possible stack overflow or infinite loop.");
            }

            foreach (var t in tokens)
            {
                if (t is PrimitiveExpressionToken)
                {
                    throw new UnexpectedTokenException(t);
                }
                
                if (t is MethodCallExpressionToken tc)
                {
                    EnsureTokensAreValidInner(tc.Arguments, ref numberOfCalls);

                    var calleeMethod = tc.CalledOnExpression as MethodCallExpressionToken;
                    while (calleeMethod != null)
                    {
                        EnsureTokensAreValidInner(calleeMethod.Arguments, ref numberOfCalls);
                        calleeMethod = calleeMethod.CalledOnExpression as MethodCallExpressionToken;
                    }
                }
            }

        }

        private static void EnsureMethodArgumentsAreValid(IEnumerable<ExpressionToken> tokens)
        {
            int callCount = 0;
            EnsureMethodArgumentsAreValid(tokens, ref callCount);
        }

        private static void EnsureMethodArgumentsAreValid(IEnumerable<ExpressionToken> tokens, ref int callCount)
        {
            callCount++;

            if (callCount > 10 * 1000)
            {
                throw new InternalJsonPathwayException(
                    "Number of calls to EnsureTokensAreValidInner exceeded max expected number of 10000, possible stack overflow or infinite loop.");
            }

            foreach (MethodCallExpressionToken method in tokens.Where(x => x is MethodCallExpressionToken))
            {
                EnsureMethodArgumentsAreValid(method, ref callCount);
            }
        }

        private static void EnsureMethodArgumentsAreValid(MethodCallExpressionToken mct, ref int callCount)
        {
            callCount++;

            if (callCount > 10 * 1000)
            {
                throw new InternalJsonPathwayException(
                    "Number of calls to EnsureTokensAreValidInner exceeded max expected number of 10000, possible stack overflow or infinite loop.");
            }

            if (mct.CalledOnExpression is MethodCallExpressionToken inner1) EnsureMethodArgumentsAreValid(inner1, ref callCount);

            foreach (var arg in mct.Arguments)
            {
                if (arg is MethodCallExpressionToken inner2) EnsureMethodArgumentsAreValid(inner2, ref callCount);
            }

            var prim = mct.Arguments.FirstOrDefault(x => x is PrimitiveExpressionToken);

            if (prim != null)
                throw new UnexpectedTokenException((prim as PrimitiveExpressionToken).Token);
        }
    }
}
