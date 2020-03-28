using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPath.Net
{
    internal abstract class Token
    {
        public abstract int Index { get; }

        public static IReadOnlyList<Token> GetTokens(string s)
        {
            if (s is null) throw new ArgumentNullException(nameof(s));

            PositionedChar[] chars = PositionedChar.CreateFromString(s);
            IReadOnlyList<SubString> subStrings = SubStringFinder.FindStrings(s).ToList();

            return GetTokens(chars, subStrings).ToList();
        }

        public static IReadOnlyList<Token> GetTokens(PositionedChar[] chars, IReadOnlyList<SubString> subStrings)
        {
            if (chars is null) throw new ArgumentNullException(nameof(chars));
            if (subStrings is null) throw new ArgumentNullException(nameof(subStrings));

            return GetTokensInternal(chars, subStrings).ToList();
        }

        public static IReadOnlyList<SecondLeveToken> GetSecondLevelTokens(string path) => GetSecondLevelTokens(GetTokens(path));

        public static IReadOnlyList<SecondLeveToken> GetSecondLevelTokens(IReadOnlyList<Token> tokens)
        {
            if (tokens is null) throw new ArgumentNullException(nameof(tokens));

            tokens = ReplaceArrayAccessTokens(tokens);

            List<SecondLeveToken> ret = new List<SecondLeveToken>();

            List<CharToken> charTokens = new List<CharToken>();

            foreach (var t in tokens)
            {
                if (t is SecondLeveToken slt)
                {
                    if (charTokens.Any())
                    {
                        var pt = new PathToken(charTokens, int.MinValue, int.MaxValue);
                        ret.Add(pt);
                        charTokens.Clear();
                    }

                    ret.Add(slt);
                }
                else if (t is StringToken st)
                {
                    if (charTokens.Any() && !charTokens.All(x => x.IsWhiteSpace))
                    {
                        var pt = new PathToken(charTokens, int.MinValue, int.MaxValue);
                        ret.Add(pt);
                        charTokens.Clear();
                    }

                    ret.Add(st.ToPathToken());
                }
                else
                {
                    charTokens.Add((CharToken)t);
                }
            }

            if (charTokens.Any())
            {
                var pt = new PathToken(charTokens, int.MinValue, int.MaxValue);
                ret.Add(pt);
            }

            return ret;
        }

        public static void EnsureSecondLevelTokensAreValid(IReadOnlyList<SecondLeveToken> tokens)
        {
            if (tokens is null) throw new ArgumentNullException(nameof(tokens));

            if (tokens.Any())
            {
                var first = tokens.First();
                var last = tokens.Last();

                if (first is PathSeparatorToken || last is PathSeparatorToken) throw new ArgumentException("Variable name cannot start or end with path separator (dot, .)");
                if (first is CloseStringToken) throw new ArgumentException("Unexpected ] at the beginning of the path");
                if (last is OpenStringToken) throw new ArgumentException("Unexpected [ at the end of the path");

                if (!(first.IsUnEscapedPath) && !(first is OpenStringToken))
                {
                    throw new ArgumentException("Unexpected first token");
                }

                if (!(last.IsUnEscapedPath) && !(last is CloseStringToken))
                {
                    throw new ArgumentException("Unexpected first token");
                }

                tokens = tokens.Where(x => !x.IsUnEscapedEmptyPath).ToList();

                if (tokens.Count > 1)
                {
                    for (int i = 0; i < tokens.Count - 1; i++)
                    {
                        var t1 = tokens[i];
                        var t2 = tokens[i + 1];

                        if (t1.GetType() == t2.GetType())
                        {
                            throw new ArgumentException("Unexpected repeated token");
                        }

                        if (t1 is PathSeparatorToken && (t2 is OpenStringToken || t2 is CloseStringToken))
                        {
                            throw new ArgumentException("Path separator (dot, .) shouldn't be followed by open or close escape character ([ or ])");
                        }

                        if (t1 is OpenStringToken && !t2.IsEscapedPath)
                        {
                            throw new ArgumentException("Expected string after [");
                        }

                        if (t1.IsEscapedPath && !(t2 is CloseStringToken))
                        {
                            throw new ArgumentException("Expected ] after string");
                        }
                    }
                }
                
                if (tokens.Count == 1)
                {
                    if (!tokens[0].IsUnEscapedPath) throw new ArgumentException($"Unexpected first token");
                }
            }
        }

        private static IReadOnlyList<Token> ReplaceArrayAccessTokens(IReadOnlyList<Token> tokens)
        {
            var tokenGroups = GetTokensBetweenOpenCloseStringTokenInclusive(tokens).ToList();

            tokenGroups = tokenGroups.Where(x =>
            {
                if (x.Count < 3) return false;

                if (IsTokenGroupStringPath(x) || IsTokenGroupEscapedPath(x)) return false;

                return true;

            }).ToList();

            if (tokenGroups.Any())
            {
                List<(int minIndex, int maxIndex)> toReplace = new List<(int minIndex, int maxIndex)>();

                foreach (var tg in tokenGroups)
                {
                    if (!IsTokenTokenGroupIndexAccessor(tg))
                    {
                        if (!IsTokenGroupEscapedPath(tg)) throw new ArgumentException("Unexpected sequence of chars between [ and ] . " +
                            "When accessing array member use index number (e.g. 7) or :last or :any . When access object property use string in (double or single) quotes.");
                    }
                    else
                    {
                        toReplace.Add((tg.First().Index, tg.Last().Index));
                    }
                }

                if (toReplace.Any())
                {
                    return ReplaceArrayAccessTokens(tokens, toReplace);
                }
            }

            return tokens;
        }

        private static IReadOnlyList<Token> ReplaceArrayAccessTokens(IReadOnlyList<Token> tokens, IReadOnlyList<(int minIndex, int maxIndex)> indexes)
        {
            List<(int minIndex, int maxIndex, PathToken path)> replacements = new List<(int minIndex, int maxIndex, PathToken path)>();

            if (indexes.Count == 0) return tokens;

            foreach (var index in indexes)
            {
                string stringReplacement = new string(
                    tokens
                        .Where(x => x.Index >= index.minIndex && x.Index <= index.maxIndex && x is CharToken)
                        .Cast<CharToken>()
                        .Select(x => x.Value)
                        .ToArray()
                    )
                    .Trim();

                if (int.TryParse(stringReplacement, out int t))
                {
                    stringReplacement = JsonValuePathParser.ArrayAccessPrefix + t;
                }
                else if (stringReplacement == ":any")
                {
                    stringReplacement = JsonValuePathParser.ArrayAccessAny;
                }
                else if (stringReplacement == ":last")
                {
                    stringReplacement = JsonValuePathParser.ArrayAccessLast;
                }
                else if (stringReplacement == ":none")
                {
                    stringReplacement = JsonValuePathParser.ArrayAccessNone;
                }
                else
                {
                    throw new ArgumentException("Unexpected sequence of chars between [ and ] . " +
                            "When accessing array member use index number (e.g. 33) or :last or :any . When access object property use string in (double or single) quotes.");
                }

                replacements.Add((index.minIndex, index.maxIndex, new PathToken(new SubString(stringReplacement, index.minIndex, index.maxIndex))));
            }

            List<Token> retTokens = new List<Token>();

            foreach (var t in tokens)
            {
                if (replacements.Any(x => t.Index >= x.minIndex && t.Index <= x.maxIndex))
                {
                    var replacementToken = replacements.First(x => t.Index >= x.minIndex && t.Index <= x.maxIndex).path;

                    if (t is OpenStringToken) retTokens.Add(t);

                    if (!retTokens.Contains(replacementToken))
                    {
                        retTokens.Add(replacementToken);
                    }

                    if (t is CloseStringToken) retTokens.Add(t);
                }
                else
                {
                    retTokens.Add(t);
                }
            }

            return retTokens;
        }

        private static bool IsTokenGroupEscapedPath(IReadOnlyList<Token> tokens)
        {
            tokens = tokens.Where(x => !x.IsWhiteSpace()).ToList();

            if (tokens.Count != 3) return false;

            return tokens[0] is OpenStringToken && tokens[1] is PathToken && tokens[2] is CloseStringToken;
        }

        private static bool IsTokenGroupStringPath(IReadOnlyList<Token> tokens)
        {
            tokens = tokens.Where(x => !x.IsWhiteSpace()).ToList();

            if (tokens.Count != 3) return false;

            return tokens[0] is OpenStringToken && tokens[1] is StringToken && tokens[2] is CloseStringToken;
        }

        private static bool IsTokenTokenGroupIndexAccessor(IReadOnlyList<Token> tokens)
        {
            tokens = tokens.Where(x => !x.IsWhiteSpace()).ToList();

            if (tokens.Count < 3) return false;

            if (!tokens.First().IsOpenString() || !tokens.Last().IsCloseString()) return false;

            if (!tokens.Skip(1).Take(tokens.Count - 2).All(x => x is CharToken)) return false;

            var tokenString = new string(tokens.Skip(1).Take(tokens.Count - 2).Cast<CharToken>().Select(x => x.Value).ToArray()).Trim();

            bool isNumber = int.TryParse(tokenString, out _);
            bool isLast = tokenString == ":last";
            bool isAny = tokenString == ":any";
            bool isNone = tokenString == ":none";

            return isNumber || isLast || isAny || isNone;
        }

        private static IEnumerable<IReadOnlyList<Token>> GetTokensBetweenOpenCloseStringTokenInclusive(IReadOnlyList<Token> tokens, bool ignoreWhiteSpaceTokens = true)
        {
            List<OpenStringToken> openStringTokens = tokens.Where(x => x is OpenStringToken).Cast<OpenStringToken>().ToList();

            foreach (var ost in openStringTokens)
            {
                List<Token> retList = new List<Token>();

                foreach (var t in tokens.Where(x => x.Index >= ost.Index))
                {
                    retList.Add(t);

                    if (t is CloseStringToken)
                    {
                        break;
                    }
                }

                if (!(retList.Last() is CloseStringToken)) throw new ArgumentException($"Opening [ at position {retList.First().Index} doesn't have closing ]");

                if (ignoreWhiteSpaceTokens)
                {
                    retList = retList.Where(x => !x.IsWhiteSpace()).ToList();
                }

                yield return retList;
            }
        }
        
        private static IEnumerable<Token> GetTokensInternal(PositionedChar[] chars, IReadOnlyList<SubString> subStrings)
        {
            List<SubString> returnedStrings = new List<SubString>();

            foreach (var c in chars)
            {
                var intersectingSubStr = subStrings.FirstOrDefault(x => x.Intersects(c.Index));

                if (intersectingSubStr != null)
                {
                    if (!returnedStrings.Contains(intersectingSubStr))
                    {
                        yield return new StringToken(intersectingSubStr.String, intersectingSubStr.StartIndexInclusive);
                        returnedStrings.Add(intersectingSubStr);
                    }
                }
                else if (c.Char == '[') yield return new OpenStringToken(c.Index);
                else if (c.Char == ']') yield return new CloseStringToken(c.Index);
                else if (c.Char == '.') yield return new PathSeparatorToken(c.Index);
                else yield return new CharToken(c);
            }
        }
    }

    /// <summary>
    /// Token that is not primitive, tokens that are variables and variables and separators which is
    /// all token types except <see cref="CharToken"/> and <see cref="StringToken"/>
    /// </summary>
    internal abstract class SecondLeveToken: Token
    {
        public abstract bool IsEscapedPath { get; }
        public abstract bool IsUnEscapedPath { get; }
        public abstract bool IsUnEscapedEmptyPath { get; }
    }

    internal class StringToken: Token
    {
        public string Value { get; }
        public override int Index { get; }

        public StringToken(string value, int index)
        {
            Value = value;
            Index = index;
        }

        public override string ToString() => $"StringToken {Value} at {Index}";

        public PathToken ToPathToken() => new PathToken(new SubString(Value, int.MinValue, int.MaxValue));
    }

    internal class PathToken: SecondLeveToken
    {
        public string Value { get; }
        public override int Index { get; }
        public bool IsEscaped { get; }

        public override bool IsEscapedPath => IsEscaped;
        
        public override bool IsUnEscapedPath => !IsEscaped;

        public override bool IsUnEscapedEmptyPath => IsUnEscapedPath && Value.All(Char.IsWhiteSpace);

        public PathToken(SubString s)
        {
            Value = s.String;
            Index = s.StartIndexInclusive;
            IsEscaped = true;
        }

        public PathToken(IReadOnlyList<Token> tokens, int startIndexInclusive, int endIndexInclusive)
        {
            List<char> chars = new List<char>();

            for (int i = 0; i < tokens.Count; i++)
            {
                if (i >= startIndexInclusive && i <= endIndexInclusive)
                {
                    if (tokens[i] is CharToken ct)
                    {
                        chars.Add(ct.Value);
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot use {tokens[i].GetType()} in PathToken, only CharToken is supported");
                    }
                }
            }

            Value = new string(chars.ToArray());
            Index = startIndexInclusive;

            EnsureValid();
        }

        public void EnsureValid()
        {
            if (IsEscaped) return;

            var value = Value.Trim();

            if (value == "") return;

            if (char.IsDigit(value[0])) throw new ArgumentException("Variable name cannot start with digit");

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                if (!char.IsLetterOrDigit(c) && c != '_' && c != '$')
                {
                    string character = new string(new[] { c });
                    if (character == " ") character = "white-space";

                    throw new ArgumentException($"Character {c} is not valid for variable name");
                }
            }
        }

        public override string ToString() => $"PathToken: {Value} at {Index}";

        public string GetPathVariableName() => IsEscaped ? Value : Value.Trim();
    }

    internal class OpenStringToken: SecondLeveToken
    {
        public override int Index { get; }

        public override bool IsEscapedPath => false;

        public override bool IsUnEscapedPath => false;

        public override bool IsUnEscapedEmptyPath => false;

        public OpenStringToken(int index)
        {
            Index = index;
        }

        public override string ToString() => $"OpenStringToken at {Index}";
    }

    internal class CloseStringToken: SecondLeveToken
    {
        public override int Index { get; }

        public override bool IsEscapedPath => false;

        public override bool IsUnEscapedPath => false;

        public override bool IsUnEscapedEmptyPath => false;

        public CloseStringToken(int index)
        {
            Index = index;
        }

        public override string ToString() => $"CloseStringToken at {Index}";
    }

    internal class CharToken: Token
    {
        public char Value { get; }
        public override int Index { get; }

        public CharToken(PositionedChar value)
        {
            Value = value.Char;
            Index = value.Index;
        }

        public bool IsValidChar => char.IsLetterOrDigit(Value) || Value == '_' || Value == '$';

        public bool IsWhiteSpace => char.IsWhiteSpace(Value);

        public bool IsValidStartChar => char.IsLetter(Value) || Value == '_' || Value == '$';

        public override string ToString() => $"CharToken: {Value} at {Index}";
    }

    internal class PathSeparatorToken: SecondLeveToken
    {
        public override int Index { get; }

        public override bool IsEscapedPath => false;

        public override bool IsUnEscapedPath => false;

        public override bool IsUnEscapedEmptyPath => false;

        public PathSeparatorToken(int index)
        {
            Index = index;
        }

        public override string ToString() => $"PathSeparatorToken at {Index}";
    }

    internal static class TokenExtensions
    {
        public static bool IsWhiteSpace(this Token token)
        {
            return token is CharToken ct && ct.IsWhiteSpace;
        }

        public static bool IsOpenString(this Token token)
        {
            return token is OpenStringToken;
        }

        public static bool IsCloseString(this Token token)
        {
            return token is CloseStringToken;
        }
    }   
}
