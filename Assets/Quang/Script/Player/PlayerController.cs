using UnityEngine;

// Đảm bảo object luôn có cả 2 script
[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour, IRunnerTarget
{
    // --- TRIỂN KHAI IRUNNERTARGET ---
    public Vector3 Position => transform.position;
    public bool IsActive => gameObject.activeInHierarchy;
    
    // Biến này chính là nơi lưu trữ trạng thái thật sự. 
    // private set giúp bảo vệ dữ liệu: Các class bên ngoài (như Boss) chỉ được đọc (get), không được sửa.
    public bool IsTripped { get; private set; } = false; 
    // -----------------------------------------

    private void Start()
{
    // Lúc này GameManager chắc chắn đã khởi tạo xong
    if (GameManager.Instance != null)
    {
        GameManager.Instance.RegisterTarget(this);
    }
}

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterTarget(this);
        }
    }

    // --- LOGIC XỬ LÝ TRẠNG THÁI ---
    
    // Hàm này có thể được gọi từ một script chuyên quản lý va chạm, ví dụ: PlayerCollision
    public void SetTrippedState(bool state)
    {
        IsTripped = state;
        // Bạn có thể thêm logic ở đây sau này: 
        // Ví dụ: Nếu state == true, gọi sang PlayerMovement để giảm speed xuống
    }
}