using UnityEngine;

public interface IRoundSession
{
    bool IsRespawning { get; }
    Vector3 RespawnCenter { get; }
    void ReportPlayerContact(Collider contact, uint sequence);
}
