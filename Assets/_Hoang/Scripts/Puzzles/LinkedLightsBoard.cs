using System;

namespace Hoang.Puzzles
{
    public sealed class LinkedLightsBoard
    {
        private readonly int _initialMask;
        public int Mask { get; private set; }
        public bool IsSolved => Mask == 0;

        public LinkedLightsBoard(int[] scrambleButtons)
        {
            if (scrambleButtons == null || scrambleButtons.Length == 0)
                throw new ArgumentException("Cần ít nhất một bước tạo bố cục đèn.");
            foreach (int button in scrambleButtons)
            {
                if (button < 1 || button > 9) throw new ArgumentException("Nút tạo bố cục phải từ 1 đến 9.");
                Mask = Toggle(Mask, button);
            }
            if (Mask == 0) throw new ArgumentException("Chuỗi tạo bố cục không được làm bảng tắt sẵn.");
            _initialMask = Mask;
        }

        public bool IsLit(int button)
        {
            if (button < 1 || button > 9) throw new ArgumentOutOfRangeException(nameof(button));
            return (Mask & (1 << (button - 1))) != 0;
        }

        public bool TryPress(int button)
        {
            if (IsSolved || button < 1 || button > 9) return false;
            Mask = Toggle(Mask, button);
            return true;
        }

        public bool TryReset()
        {
            if (IsSolved) return false;
            Mask = _initialMask;
            return true;
        }

        private static int Toggle(int mask, int button)
        {
            int index = button - 1;
            int row = index / 3;
            int column = index % 3;
            mask ^= 1 << index;
            // Kiểm tra cột riêng để cạnh trái/phải không nối vòng sang hàng kế tiếp.
            if (row > 0) mask ^= 1 << (index - 3);
            if (row < 2) mask ^= 1 << (index + 3);
            if (column > 0) mask ^= 1 << (index - 1);
            if (column < 2) mask ^= 1 << (index + 1);
            return mask;
        }
    }
}
