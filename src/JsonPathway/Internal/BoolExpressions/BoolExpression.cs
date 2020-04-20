using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonPathway.Internal.BoolExpressions
{
    internal class BoolExpression
    {
        public BoolExpression(IReadOnlyList<Either<Token, TokenGroup>> elements)
        {
            Elements = elements;
        }

        public IReadOnlyList<Either<Token, TokenGroup>> Elements { get; }
    }
}
