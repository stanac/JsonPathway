using System.Collections.Generic;
using System.Linq;

namespace JsonPathway.Internal
{
    internal class PositionedCharStringList
    {
        private readonly List<PositionedChar> _chars = new List<PositionedChar>();

        public bool IsLastEscapeSymbol
        {
            get
            {
                if (_chars.Any())
                {
                    return _chars.Last().IsEscapeSymbol && !_chars.Last().IsEscaped;
                }

                return false;
            }
        }

        public int? MinIndex
        {
            get
            {
                if (_chars.Any()) return _chars.First().Index;
                return null;
            }
        }

        public int? MaxIndex
        {
            get
            {
                if (_chars.Any()) return _chars.Last().Index;
                return null;
            }
        }

        public override string ToString()
        {
            if (_chars.Any(x => x.IsEscapeSymbol && !x.IsEscaped))
            {
                PositionedChar unescaped = _chars.First(x => x.IsEscapeSymbol && !x.IsEscaped);
                throw new UnescapedCharacterException(unescaped);
            }
            
            return new string(_chars.Select(x => x.Value).ToArray());
        }

        public void Add(PositionedChar c)
        {
            if (_chars.Any())
            {
                PositionedChar last = _chars.Last();
                if (last.IsEscapeSymbol && !last.IsEscaped)
                {
                    PositionedChar escaped = PositionedChar.Escape(c);
                    _chars.Remove(last);
                    _chars.Add(escaped);
                }
                else
                {
                    _chars.Add(c);
                }
            }
            else
            {
                _chars.Add(c);
            }
        }
    }
}
