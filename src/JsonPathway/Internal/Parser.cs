using System.Collections.Generic;
using System.Linq;

namespace JsonPathway.Internal
{
    internal static class Parser
    {
        public static IEnumerable<Expression> Parse(IReadOnlyList<Token> tokens)
        {
            EnsureTokensAreValid(tokens.ToList());

            foreach (Token t in tokens.Where(x => !x.IsSymbolToken('.')))
            {
                foreach (Expression e in Parse(t))
                {
                    yield return e;
                }
            }
        }

        private static void EnsureTokensAreValid(List<Token> tokens)
        {
            if (!tokens.Any()) return;

            Token unexpectedToken = tokens.FirstOrDefault(x => !x.CanBeConvertedToExpression() && !x.IsSymbolToken('.'));
            if (unexpectedToken != null) throw new UnexpectedTokenException(unexpectedToken);

            if (tokens[0].IsSymbolToken('.') && ! tokens[0].IsSymbolToken('.')) throw new UnexpectedTokenException(tokens[0]);
            if (tokens.Last().IsSymbolToken('.')) throw new UnexpectedTokenException(tokens.Last());

            var pointTokens = tokens.Where(x => x.IsSymbolToken('.'));
            
            foreach (SymbolToken pt in pointTokens)
            {
                int index = tokens.IndexOf(pt);
                var next = tokens[index + 1];

                if (!next.IsPropertyToken() && !next.IsChildPropertiesToken()) throw new UnexpectedTokenException(next, "Expected property accessor");

                if (next.IsPropertyToken() && next.CastToPropertyToken().Escaped) throw new UnexpectedTokenException(next, "Expected unescaped property accessor");
            }
        }

        private static IEnumerable<Expression> Parse(Token t)
        {
            switch (t)
            {
                case PropertyToken pt: yield return new PropertyAccessExpression(pt);
                    break;

                case ChildPropertiesToken pt: yield return new PropertyAccessExpression(pt);
                    break;

                case RecursivePropertiesToken pt: yield return new PropertyAccessExpression(pt);
                    break;

                case MultiplePropertiesToken pt: yield return new PropertyAccessExpression(pt);
                    break;

                case ArrayElementsToken at: yield return new ArrayElementsExpression(at);
                    break;

                case AllArrayElementsToken at: yield return new ArrayElementsExpression(at);
                    break;

                case FilterToken ft: yield return new FilterExpression(ft);
                    break;
            }
        }
    }
}
