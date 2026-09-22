using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Danh sách chứa các mục tiêu (chỉ quan tâm Interface)
    private List<IRunnerTarget> activeTargets = new List<IRunnerTarget>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Hàm để Player gọi khi mới sinh ra
    public void RegisterTarget(IRunnerTarget target)
    {
        if (!activeTargets.Contains(target))
        {
            activeTargets.Add(target);
        }
    }

    // Hàm để Player gọi khi bị chết hoặc ngắt kết nối
    public void UnregisterTarget(IRunnerTarget target)
    {
        if (activeTargets.Contains(target))
        {
            activeTargets.Remove(target);
        }
    }

    // Hàm để Boss lấy danh sách ra để tính toán
    public List<IRunnerTarget> GetAllTargets()
    {
        return activeTargets;
    }
}