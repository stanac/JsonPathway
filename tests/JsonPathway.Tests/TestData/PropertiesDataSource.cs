using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace JsonPathway.Tests.TestData
{
    [ExcludeFromCodeCoverage]
    public class PropertiesDataSource : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            string text = TestDataLoader.LoadFile("PropertiesData.txt");

            string[] lines = text.Split(Environment.NewLine);

            string path = null;
            List<string> current = new List<string>();

            foreach (string l in lines)
            {
                if (!l.StartsWith("/*"))
                {
                    if (l.Trim().StartsWith("//"))
                    {
                        if (path != null)
                        {
                            yield return new object[] { path, string.Join(Environment.NewLine, current).RemoveWhiteSpace() };
                            current = new List<string>();
                        }

                        path = l.Replace("//", "");
                    }
                    else
                    {
                        current.Add(l);
                    }
                }
            }

            if (path != null)
            {
                yield return new object[] { path, string.Join(Environment.NewLine, current).RemoveWhiteSpace() };
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
