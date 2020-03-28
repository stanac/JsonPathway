using System;
using Xunit;

namespace JsonPath.Net.Tests
{
    public class PositionedCharAndSubSctringTests
    {
        [Fact]
        public void PositionedChar_Constructor_SetsCharAndIndex()
        {
            PositionedChar c = new PositionedChar('c', 3);
            Assert.Equal('c', c.Char);
            Assert.Equal(3, c.Index);
        }

        [Fact]
        public void PositionedChar_GetFromString_GetsCorrectArray()
        {
            string s = "abC";

            PositionedChar[] chars = PositionedChar.CreateFromString(s);

            Assert.Equal(3, chars.Length);

            Assert.Equal('a', chars[0].Char);
            Assert.Equal('b', chars[1].Char);
            Assert.Equal('C', chars[2].Char);

            Assert.Equal(0, chars[0].Index);
            Assert.Equal(1, chars[1].Index);
            Assert.Equal(2, chars[2].Index);
        }

        [Fact]
        public void SubString_Constructor_ThrowsExceptionOnNullString()
        {
            string s = null;

            Assert.Throws<ArgumentNullException>(() => new SubString(s, 1, 2));
        }

        [Fact]
        public void SubString_Constructor_SetsValues()
        {
            SubString s = new SubString("abc", 3, 6);

            Assert.Equal("abc", s.String);
            Assert.Equal(3, s.StartIndexInclusive);
        }

        [Fact]
        public void SubString_Intersects_ReturnsCorrectValues()
        {
            SubString s = new SubString("abc", 3, 5);

            Assert.False(s.Intersects(0));
            Assert.False(s.Intersects(1));
            Assert.False(s.Intersects(2));
            
            Assert.True(s.Intersects(3));
            Assert.True(s.Intersects(4));
            Assert.True(s.Intersects(5));
            
            Assert.False(s.Intersects(6));
            Assert.False(s.Intersects(7));
        }

        [Theory]
        [InlineData("abcd", "abcd")]
        [InlineData("123", "123")]
        [InlineData(@"\\", @"\")]
        [InlineData(@"\\\\", @"\\")]
        [InlineData(@"\\a", @"\a")]
        [InlineData(@"1\\", @"1\")]
        [InlineData(@"123\""A", @"123""A")]
        [InlineData(@"123\'", "123'")]
        [InlineData(@"123\\", @"123\")]
        public void PositionedChar_ReplaceEscapedChars_ValidString_Works(string input, string expected)
        {
            PositionedChar[] chars = PositionedChar.CreateFromString(input);
            PositionedChar[] escapedChars = PositionedChar.ReplaceEscapedChars(chars);
            string escapedString = PositionedChar.CreateString(escapedChars);

            Assert.Equal(expected, escapedString);

            if (input.Contains('\\'))
            {
                Assert.Contains(escapedChars, x => x.IsEscaped);
            }
        }

        [Theory]
        [InlineData("abc\\")]
        [InlineData("abc\\b")]
        [InlineData("\\r")]
        [InlineData(@"\\\")]
        [InlineData(@"\")]
        public void PositionedChar_ReplaceEscapedChars_NotValidChars_ThrowsException(string value)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var chars = PositionedChar.CreateFromString(value);
                PositionedChar.ReplaceEscapedChars(chars);
            });
        }

        [Fact]
        public void SubString_CreateFromPositionedChars_ReturnsCorrectSubString()
        {
            var chars = PositionedChar.CreateFromString("0123456789");
            SubString s = SubString.CreateFromPositionedChars(chars, 1, 3, false);
            Assert.Equal("123", s.String);

            s = SubString.CreateFromPositionedChars(chars, 1, 3, true);
            Assert.Equal("123", s.String);
        }

        [Fact]
        public void SubString_CreateFromPositionedCharsWithSkipQuotes_ReturnsCorrectSubString()
        {
            var chars = PositionedChar.CreateFromString("0'123'456789");
            SubString s = SubString.CreateFromPositionedChars(chars, 1, 5, true);
            Assert.Equal("123", s.String);

            s = SubString.CreateFromPositionedChars(chars, 1, 5, false);
            Assert.Equal("'123'", s.String);

            chars = PositionedChar.CreateFromString("0\"123\"456789");
            s = SubString.CreateFromPositionedChars(chars, 1, 5, true);
            Assert.Equal("123", s.String);

            s = SubString.CreateFromPositionedChars(chars, 1, 5, false);
            Assert.Equal("\"123\"", s.String);
        }
    }
}
