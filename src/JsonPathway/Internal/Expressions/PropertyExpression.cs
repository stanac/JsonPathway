using System;

namespace JsonPathway.Internal.Expressions
{
    internal class PropertyExpression
    {
        public PropertyExpression(string value, bool isEscaped)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            IsEscaped = isEscaped;

            if (!isEscaped && string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("PropertyExpression with empty value is not allowed for values that are not escaped.");
            }
        }

        public string Value { get; }
        public bool IsEscaped { get; }
    }
}
