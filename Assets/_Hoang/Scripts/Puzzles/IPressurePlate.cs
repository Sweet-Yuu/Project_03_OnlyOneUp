using System;

namespace Hoang.Puzzles
{
    // Interface cung cấp trạng thái bàn đạp áp lực cho hệ thống câu đố.
    public interface IPressurePlate
    {
        bool IsPressed { get; }
        event Action<bool> StateChanged;
    }
}
