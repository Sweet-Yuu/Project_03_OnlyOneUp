using UnityEngine;

// Dữ liệu checkpoint của scene online; host quyết định khi nào lưu và hồi sinh.
public sealed class OnlineCheckpoint : MonoBehaviour
{
    [SerializeField, Min(1)] private int _checkpointId = 1;
    [Tooltip("Optional center between the two respawning players. Defaults to this checkpoint.")]
    [SerializeField] private Transform _spawnCenter;
    public int CheckpointId => _checkpointId;
    public Vector3 SpawnCenter => _spawnCenter != null ? _spawnCenter.position : transform.position;
    private Collider[] _colliders;

    private void Awake() => _colliders = GetComponentsInChildren<Collider>(true);

    public bool IsNear(Vector3 position, float tolerance)
    {
        if (!isActiveAndEnabled) return false;
        foreach (Collider collider in _colliders)
            if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy &&
                (collider.ClosestPoint(position) - position).sqrMagnitude <= tolerance * tolerance) return true;
        return false;
    }
}
