using UnityEngine;
using System.Collections.Generic;
public class BossTargetFinder : MonoBehaviour
{
    public IRunnerTarget CurrentTarget { get; private set; }

    void LateUpdate()
    {
        // Thuật toán duyệt vòng lặp lấy người có trục Z nhỏ nhất để vào CurrentTarget
        CurrentTarget = FindSlowestTarget(GameManager.Instance.GetAllTargets());
    }
    
    private IRunnerTarget FindSlowestTarget(List<IRunnerTarget> targets)
    {
        // Chốt chặn 2: Kiểm tra danh sách rỗng để tránh văng lỗi
        if (targets == null || targets.Count == 0) return null;

        IRunnerTarget slowest = null;
        
        // Đặt giá trị minZ ban đầu là mức lớn nhất có thể của kiểu float
        // Đảm bảo bất kỳ người chơi nào cũng sẽ có Z nhỏ hơn số này
        float minZ = float.MaxValue;

        foreach (var target in targets)
        {
            // Chốt chặn 3: Chỉ xét những mục tiêu tồn tại và đang active (chưa chết)
            if (target != null && target.IsActive)
            {
                if (target.Position.z < minZ)
                {
                    minZ = target.Position.z;
                    slowest = target;
                }
            }
        }

        return slowest;
    }
}