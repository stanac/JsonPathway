using System;

namespace JsonPath.Net
{
    public class PathElement
    {
        private PathElement(string value, bool isVar, bool isWildcard, bool isFilter)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));

            IsVariable = isVar;
            IsWildcard = IsWildcard;
            IsFilter = IsFilter;

            EnsureFilterIsValid();
        }

        public static PathElement CreateVariableAccess(string value) => new PathElement(value, true, false, false);

        public static PathElement CreateWildcard() => new PathElement("*", false, true, false);

        public static PathElement CreateFilter(string value) => new PathElement(value, false, false, true);

        public bool IsVariable { get; }
        public bool IsWildcard { get; }
        public bool IsFilter { get; }

        public string Value { get; }

        private void EnsureFilterIsValid()
        {
            if (!IsFilter) return;
        }

        public static implicit operator string (PathElement pe)
        {
            if (pe == null) return null;
            return pe.Value;
        }

        public override string ToString() => Value;
    }
}
