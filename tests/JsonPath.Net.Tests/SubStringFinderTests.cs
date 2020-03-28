using System.Linq;
using Xunit;

namespace JsonPath.Net.Tests
{
    public class SubStringFinderTests
    {
        [Fact]
        public void FindStrings_EmptyPath_YieldsEmptyCollection()
        {
            var list = SubStringFinder.FindStrings("").ToList();
            Assert.Empty(list);
        }

        [Fact]
        public void FindStrings_PathWithoutStrings_YieldsEmptyCollection()
        {
            var list = SubStringFinder.FindStrings("abc 123 qwe").ToList();
            Assert.Empty(list);
        }

        [Theory]
        [InlineData("abc abc '' 123 qwe", "")]
        [InlineData("abc abc '123' 123 qwe", "123")]
        [InlineData("abc abc \"123\" 123 qwe", "123")]
        [InlineData("abc abc \"123\" 123 qwe '444'", "123;444")]
        [InlineData("'111'\"222\"'333'", "111;222;333")]
        [InlineData("'a\\\'b'", "a\'b")]
        [InlineData("'\\\''", "\'")]
        public void FindStrings_PathWithQuotes_YieldsCorrectStrings(string s, string expectedValuesCsv)
        {
            var list = SubStringFinder.FindStrings(s).ToList();

            string[] expectedValues = expectedValuesCsv.Split(";");

            Assert.NotEmpty(list);
            Assert.Equal(expectedValues.Length, list.Count);

            for (int i = 0; i < expectedValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], list[i].String);
            }
        }
    }
}
