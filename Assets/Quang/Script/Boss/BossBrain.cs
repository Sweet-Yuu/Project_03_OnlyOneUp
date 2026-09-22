using System.Collections;
using UnityEngine;
[RequireComponent(typeof(BossTargetFinder), typeof(BossMovement))]
public class BossBrain : MonoBehaviour
{
    private BossTargetFinder targetFinder;
    private BossMovement movement;

    public float normalDistance = 15f;
    public float aggroDistance = 5f;

    void Awake()
    {
        targetFinder = GetComponent<BossTargetFinder>();
        movement = GetComponent<BossMovement>();
    }

    void LateUpdate()
    {
        IRunnerTarget target = targetFinder.CurrentTarget;
        if (target == null) return;

        // Bộ não quyết định khoảng cách
        float currentDistance = target.IsTripped ? aggroDistance : normalDistance;

        // Bộ não tính điểm đến lý tưởng
        Vector3 destination = new Vector3(target.Position.x, transform.position.y, target.Position.z - currentDistance);

        // Ra lệnh cho Đôi chân di chuyển
        movement.MoveTowards(destination);
    }
}