using JsonPathway.Internal;
using JsonPathway.Internal.Filters;
using System;
using System.Diagnostics.CodeAnalysis;

namespace JsonPathway;

[ExcludeFromCodeCoverage]
public abstract class JsonPathwayException : Exception
{
    protected JsonPathwayException()
    {
    }

    protected JsonPathwayException(string message) : base(message)
    {
    }

    protected JsonPathwayException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

[ExcludeFromCodeCoverage]
public class InternalJsonPathwayException : JsonPathwayException
{
    public InternalJsonPathwayException() : base("Unexpected internal exception in the library")
    {
    }

    public InternalJsonPathwayException(string message) : base("Unexpected internal exception in the library." + message)
    {
    }

    public InternalJsonPathwayException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

[ExcludeFromCodeCoverage]
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

[ExcludeFromCodeCoverage]
public class UnrecognizedCharSequence : TokenizationException
{
    public UnrecognizedCharSequence()
    {
    }

    public UnrecognizedCharSequence(string message) : base(message)
    {
    }

    public UnrecognizedCharSequence(string message, Exception innerException) : base(message, innerException)
    {
    }

    internal UnrecognizedCharSequence(PositionedChar c)
        : this(c.Index, c.Value)
    {
    }

    public UnrecognizedCharSequence(int index, char startingChar)
        : this($"Unrecognized character sequence starting with '{startingChar}' at position {index}")
    {
    }
}

[ExcludeFromCodeCoverage]
public class UnrecognizedSymbolException : TokenizationException
{
    public UnrecognizedSymbolException()
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

[ExcludeFromCodeCoverage]
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

[ExcludeFromCodeCoverage]
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

[ExcludeFromCodeCoverage]
public class UnrecognizedMethodNameException : TokenizationException
{
    public UnrecognizedMethodNameException()
    {
    }

    public UnrecognizedMethodNameException(string message) : base(message)
    {
    }

    public UnrecognizedMethodNameException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

[ExcludeFromCodeCoverage]
public class ParsingException : JsonPathwayException
{
    public ParsingException()
    {
    }

    public ParsingException(string message) : base(message)
    {
    }

    public ParsingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

[ExcludeFromCodeCoverage]
public class UnexpectedTokenException : ParsingException
{
    public UnexpectedTokenException()
    {
    }

    public UnexpectedTokenException(string message) : base(message)
    {
    }

    public UnexpectedTokenException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public UnexpectedTokenException(Token token)
        : this(token, "")
    {
    }

    internal UnexpectedTokenException(FilterExpressionToken token)
        : this(token, "")
    {
    }

    public UnexpectedTokenException(Token token, string message)
        : this($"Unexpected {token}. {message}")
    {
    }

    internal UnexpectedTokenException(FilterExpressionToken token, string message)
        : this($"Unexpected {token}. {message}")
    {
    }
}

[ExcludeFromCodeCoverage]
public class JsonPatwayMethodNotSupportedException : TokenizationException
{
    public JsonPatwayMethodNotSupportedException()
    {
    }

    public JsonPatwayMethodNotSupportedException(string message) : base(message)
    {
    }

    public JsonPatwayMethodNotSupportedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}