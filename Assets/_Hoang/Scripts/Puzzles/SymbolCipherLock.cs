using System;
using System.Globalization;

namespace Hoang.Puzzles
{
    public sealed class SymbolCipherLock
    {
        public string Code { get; private set; }
        public bool IsUnlocked { get; private set; }

        public SymbolCipherLock(int circleTriangle, int triangleSquare, int circleSquare)
        {
            if (circleTriangle < 0 || circleTriangle > 18 || triangleSquare < 0 ||
                triangleSquare > 18 || circleSquare < 0 || circleSquare > 18)
                throw new ArgumentException("Mỗi tổng phải nằm trong khoảng 0–18.");

            // Cộng hai phương trình chứa tròn rồi trừ phương trình còn lại để khử hai ẩn.
            int twiceCircle = circleTriangle + circleSquare - triangleSquare;
            int circle = twiceCircle / 2;
            int triangle = circleTriangle - circle;
            int square = circleSquare - circle;
            if (twiceCircle % 2 != 0 || circle < 0 || circle > 9 ||
                triangle < 0 || triangle > 9 || square < 0 || square > 9)
                throw new ArgumentException("Ba tổng phải cho nghiệm nguyên, mỗi ký hiệu từ 0 đến 9.");

            Code = (circle * 100 + triangle * 10 + square).ToString("D3", CultureInfo.InvariantCulture);
        }

        public bool TryUnlock(string entry)
        {
            if (string.Equals(entry, Code, StringComparison.Ordinal)) IsUnlocked = true;
            return IsUnlocked;
        }
    }
}
