using System;
using System.Collections;
using System.Collections.Generic;

namespace JsonPathway.Tests.TestData
{
    public class PropertiesDataSource : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            var text = TestDataLoader.LoadFile("PropertiesData.txt");

            var lines = text.Split(Environment.NewLine);

            string path = null;
            List<string> current = new List<string>();

            foreach (var l in lines)
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
