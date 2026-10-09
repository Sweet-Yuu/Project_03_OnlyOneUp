using System;
using UnityEngine;
using Unity.Netcode;

public sealed class NestEggLifecycle : IDisposable
{
    private readonly Transform transform;
    private readonly Rigidbody body;
    private readonly BoxCollider nestFloor;
    private NestEggBody egg;
    private NetworkObject ignoredHost, ignoredGuest;
    public NestEggBody Egg => egg;
    public event Action Broken;

    public NestEggLifecycle(Transform root, Rigidbody body, BoxCollider floor)
    { transform = root; this.body = body; nestFloor = floor; }

    public void Create(GameObject visual, bool server)
    {
        var obj = new GameObject("NetworkEgg (synced by Nest)");
        egg = obj.AddComponent<NestEggBody>();
        egg.Initialize(transform, visual);
        egg.SetAuthority(server);
        egg.Broken += OnBroken;
    }
    private void OnBroken() => Broken?.Invoke();
    public void Wait() { if (egg != null) egg.WaitForPlayers(); }
    public void Tick(NetworkObject host, NetworkObject guest)
    {
        IgnorePlayers(host, guest);
        if (egg != null && egg.IsSimulating && egg.Body.position.y < -50f) egg.BreakOnServer();
    }
    public void IgnorePlayers(NetworkObject host, NetworkObject guest)
    {
        if (egg == null) return;
        if (host != null && host != ignoredHost) { egg.IgnorePlayer(host.gameObject); ignoredHost = host; }
        if (guest != null && guest != ignoredGuest) { egg.IgnorePlayer(guest.gameObject); ignoredGuest = guest; }
    }

    public void Reset(float eggDropClearance, NetworkObject host, NetworkObject guest)
    {
        if (egg == null) return;
        Vector3 floorTop = nestFloor.transform.TransformPoint(
            nestFloor.center + Vector3.up * nestFloor.size.y * 0.5f);
        Vector3 localTop = transform.InverseTransformPoint(floorTop);
        Vector3 position = body.position + body.rotation *
            (localTop + Vector3.up * (NestEggBody.Height * 0.5f + eggDropClearance));
        egg.ResetOnServer(position, body.rotation);
        // Collider re-enabling during reset can clear ignored pairs.
        ignoredHost = ignoredGuest = null;
        IgnorePlayers(host, guest);
    }
    public void DropForTest()
    {
        if (egg == null) return;
        Bounds bounds = nestFloor.bounds;
        foreach (Collider c in transform.GetComponentsInChildren<Collider>()) bounds.Encapsulate(c.bounds);
        Vector3 position = new Vector3(bounds.max.x + 1f, bounds.max.y + 1f, bounds.center.z);
        egg.ResetOnServer(position, Quaternion.identity);
        ignoredHost = ignoredGuest = null;
    }
    public void Dispose()
    {
        if (egg != null)
        {
            egg.Broken -= OnBroken;
            UnityEngine.Object.Destroy(egg.gameObject);
        }
        egg = null;
        ignoredHost = ignoredGuest = null;
    }
}
