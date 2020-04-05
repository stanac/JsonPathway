using JsonPathway.Internal;
using System;

namespace JsonPathway
{
    public abstract class JsonPathwayException : Exception
    {
        public JsonPathwayException()
        {
        }

        public JsonPathwayException(string message) : base(message)
        {
        }

        public JsonPathwayException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public abstract class TokenizationException : JsonPathwayException
    {
        public TokenizationException()
        {
        }

        public TokenizationException(string message) : base(message)
        {
        }

        public TokenizationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class UnrecognizedSymbolException : TokenizationException
    {
        public UnrecognizedSymbolException() : base()
        {
        }

        public UnrecognizedSymbolException(string message) : base(message)
        {
        }

        public UnrecognizedSymbolException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public UnrecognizedSymbolException(char symbol, int index) : this($"Unrecognized symbol '{symbol}' at position {index}")
        {
        }
    }

    public class UnescapedCharacterException : TokenizationException
    {
        public UnescapedCharacterException()
        {
        }

        public UnescapedCharacterException(string message) : base(message)
        {
        }

        public UnescapedCharacterException(string message, Exception innerException) : base(message, innerException)
        {
        }

        internal UnescapedCharacterException(PositionedChar c)
            : this($"Unecscaped character '{c.Value}' and position {c.Index}")
        {
        }
    }

    public class UnclosedStringException : TokenizationException
    {
        public UnclosedStringException()
        {

        }

        public UnclosedStringException(string message) : base(message)
        {

        }

        public UnclosedStringException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public UnclosedStringException(char openQuote, int index)
            : base($"String opened with {openQuote} at {index} isn't closed")
        {
        }
    }
}
