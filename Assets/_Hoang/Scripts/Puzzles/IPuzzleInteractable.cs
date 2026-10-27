namespace Hoang.Puzzles
{
    // Interface trừu tượng cho các vật thể có thể kích hoạt trong giải đố (cửa, cầu, bẫy,...).
    public interface IPuzzleInteractable
    {
        bool IsActive { get; }
        void SetActive(bool active);
    }
}
