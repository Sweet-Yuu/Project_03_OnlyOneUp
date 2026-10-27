namespace Hoang.Puzzles
{
    // Interface cung cấp thông tin góc nghiêng và trạng thái thăng bằng của cầu bập bênh.
    public interface ISeesawPlatform
    {
        float CurrentTiltAngle { get; }
        float MaxTiltAngle { get; }
        bool IsBalanced { get; }
        int PlayerCount { get; }
    }
}
