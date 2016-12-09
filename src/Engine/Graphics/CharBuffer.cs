using System;

namespace Engine.Graphics
{
    public class CharBuffer
    {
        private char[] _chars;
        private uint _count;

        public CharBuffer(uint capacity)
        {
            _chars = new char[capacity];
        }

        public uint Count => _count;

        public char[] Buffer => _chars;

        public void Clear()
        {
            _count = 0;
        }

        public void Append(char[] chars, uint start, uint count)
        {
            EnsureSize(_count + count);

            for (int i = 0; i < count; i++)
            {
                _chars[_count] = chars[start + i];
                _count += 1;
            }
        }

        public void Append(string s, uint start, uint count)
        {
            EnsureSize(_count + count);

            for (int i = 0; i < count; i++)
            {
                _chars[_count] = s[(int)start + i];
                _count += 1;
            }
        }

        public void Append(char value)
        {
            EnsureSize(_count + 1);
            _chars[_count] = value;
            _count += 1;
        }

        public void Append(uint value, uint zeroPadDigits)
        {
            // Count digits
            uint valueToCountDigits = value;
            uint digitsCount = 1;
            while (valueToCountDigits >= 10UL)
            {
                valueToCountDigits = valueToCountDigits / 10U;
                digitsCount++;
            }

            uint addedDigits = Math.Max(digitsCount, zeroPadDigits);
            uint sizeNeeded = _count + addedDigits;
            EnsureSize(sizeNeeded);

            if (zeroPadDigits > digitsCount)
            {
                for (uint i = 0; i < zeroPadDigits - digitsCount; i++)
                {
                    Append('0');
                }
            }

            uint realDigitsAdded = digitsCount;
            uint index = digitsCount;
            while (digitsCount != 0)
            {
                digitsCount--;
                uint digit = value % 10U;
                value /= 10U;
                _chars[_count + --index] = (char)(digit + '0');
                if (digitsCount == 0)
                {
                    break;
                }
            }

            _count += realDigitsAdded;
        }

        private void EnsureSize(uint newSize)
        {
            if ((uint)_chars.Length < newSize)
            {
                Resize(newSize);
            }
        }

        private void Resize(uint newSize)
        {
            Array.Resize(ref _chars, (int)newSize);
        }
    }
}
