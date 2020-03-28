using System;
using System.Collections.Generic;
using Xunit;

namespace JsonPath.Net.Tests
{
    public class TokenTests
    {
        [Fact]
        public void GetTokens_ReturnCorrectTokens()
        {
            string s = "abc";
            IReadOnlyList<Token> tokens = Token.GetTokens(s);

            Assert.Equal(3, tokens.Count);
            foreach (var t in tokens)
            {
                Assert.IsType<CharToken>(t);
            }

            s = "a['b\\'cd'].e";

            tokens = Token.GetTokens(s);

            Assert.Equal(6, tokens.Count);
            Assert.Equal("b'cd", (tokens[2] as StringToken).Value);

            Assert.IsType<CharToken>(tokens[0]);
            Assert.IsType<OpenStringToken>(tokens[1]);
            Assert.IsType<StringToken>(tokens[2]);
            Assert.IsType<CloseStringToken>(tokens[3]);
            Assert.IsType<PathSeparatorToken>(tokens[4]);
            Assert.IsType<CharToken>(tokens[5]);
        }

        [Fact]
        public void PathToken_ContructorFromSubString_SetsValue()
        {
            string value = "abc";
            var token = new PathToken(new SubString(value, 0, value.Length - 1));
            Assert.Equal(value, token.Value);
        }

        [Fact]
        public void PathToken_ContructorFromTokens_SetsValue()
        {
            string value = "abcdef";
            IReadOnlyList<Token> tokens = Token.GetTokens(value);
            var pathToken = new PathToken(tokens, 1, 4);
            pathToken.EnsureValid();
            Assert.Equal("bcde", pathToken.Value);
            Assert.Equal(1, pathToken.Index);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("a'b'c")]
        [InlineData("4577")]
        [InlineData("\"")]
        [InlineData(" ")]
        public void PathToken_ContructorFromEscapedString_AlwaysValid(string value)
        {
            PathToken pathToken = new PathToken(new SubString(value, 0, value.Length - 1));
            pathToken.EnsureValid();
            // nothing thrown, assertion passed
        }

        [Theory]
        [InlineData("$")]
        [InlineData("$abc")]
        [InlineData("_abc")]
        [InlineData("_")]
        [InlineData("_123")]
        [InlineData("a123")]
        [InlineData("a")]
        [InlineData("a$_4")]
        public void PathToken_ContructorFromCharTokens_ValidValues_DoesntThrowExceptions(string value)
        {
            IReadOnlyList<Token> tokens = Token.GetTokens(value);
            PathToken pathToken = new PathToken(tokens, 0, value.Length - 1);
            pathToken.EnsureValid();
            // nothing thrown, assertion passed
        }

        [Theory]
        [InlineData("1")]
        [InlineData("1a")]
        [InlineData("&")]
        [InlineData("abc&")]
        [InlineData("abc&def")]
        [InlineData("abcdef_*")]
        public void PathToken_ContructorFromCharTokens_NotValidValues_ThrowsExceptions(string value)
        {
            IReadOnlyList<Token> tokens = Token.GetTokens(value);

            Assert.Throws<ArgumentException>(() => new PathToken(tokens, 0, value.Length - 1));
        }

        [Fact]
        public void PathToken_GetPathTokens_ReturnsCorrectTokens()
        {
            IReadOnlyList<Token> tokens = Token.GetTokens("abc.def.ghi['1\"2\\\'3'][\"456\"].jkl.mno");

            IReadOnlyList<SecondLeveToken> slt = Token.GetSecondLevelTokens(tokens);

            Assert.Equal(15, slt.Count);

            Assert.IsType<PathToken>(slt[0]);
            Assert.Equal("abc", (slt[0] as PathToken).Value);

            Assert.IsType<PathSeparatorToken>(slt[1]);

            Assert.IsType<PathToken>(slt[2]);
            Assert.Equal("def", (slt[2] as PathToken).Value);

            Assert.IsType<PathSeparatorToken>(slt[3]);

            Assert.IsType<PathToken>(slt[4]);
            Assert.Equal("ghi", (slt[4] as PathToken).Value);

            Assert.IsType<OpenStringToken>(slt[5]);

            Assert.IsType<PathToken>(slt[6]);
            Assert.Equal("1\"2'3", (slt[6] as PathToken).Value);

            Assert.IsType<CloseStringToken>(slt[7]);

            Assert.IsType<OpenStringToken>(slt[8]);

            Assert.IsType<PathToken>(slt[9]);
            Assert.Equal("456", (slt[9] as PathToken).Value);

            Assert.IsType<CloseStringToken>(slt[10]);

            Assert.IsType<PathSeparatorToken>(slt[11]);

            Assert.IsType<PathToken>(slt[12]);
            Assert.Equal("jkl", (slt[12] as PathToken).Value);

            Assert.IsType<PathSeparatorToken>(slt[13]);

            Assert.IsType<PathToken>(slt[14]);
            Assert.Equal("mno", (slt[14] as PathToken).Value);
        }

        [Theory]
        [InlineData(".a")]
        [InlineData("a.")]
        [InlineData(".a.")]
        [InlineData("[")]
        [InlineData("]")]
        [InlineData("a..b")]
        [InlineData("a[[]")]
        [InlineData("a[[]]")]
        [InlineData("abc def")]
        [InlineData("['abc'].['abc']")]
        [InlineData("['abc'].]'abc']")]
        [InlineData("[]")]
        [InlineData("['\"]")]
        [InlineData("[\"']")]
        [InlineData("[abc]")]
        [InlineData("[a'bc']")]
        [InlineData("a.v.['abc']")]
        public void Token_EnsureSecondLevelTokensAreValid_NotValidTokens_ThrowsException(string path)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var tokens = Token.GetSecondLevelTokens(path);
                Token.EnsureSecondLevelTokensAreValid(tokens);
            });
        }

        [Theory]
        [InlineData("a.a")]
        [InlineData("a.v['abc']")]
        [InlineData("['']")]
        [InlineData(@"[""""]")]
        [InlineData("a['']")]
        [InlineData(" abc . def  ")]
        public void Token_EnsureSecondLevelTokensAreValid_ValidOrderOfToken_DoesNotThrowException(string path)
        {
            var tokens = Token.GetSecondLevelTokens(path);
            Token.EnsureSecondLevelTokensAreValid(tokens);
            // nothing thrown, assertion passed
        }
    }
}
