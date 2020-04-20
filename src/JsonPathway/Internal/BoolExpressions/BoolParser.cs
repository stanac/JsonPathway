using System;
using System.Collections.Generic;

namespace JsonPathway.Internal.BoolExpressions
{
    internal static class BoolParser
    {
        public static BoolExpression Parse(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) throw new ArgumentException("Value not set", nameof(expression));

            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(expression);

            IReadOnlyList<Either<Token, TokenGroup>> tokenGroups = TokenGroup.GetGroups(tokens);

            return new BoolExpression(tokenGroups);
        }
    }
}
