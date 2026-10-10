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
    private NestEggContainment _containment;
    public bool IsEggInNest
    {
        get
        {
            if (egg == null || !egg.IsSimulating || _containment == null) return false;
            // Dùng pose vật lý của cả hai body, không dùng Transform đã được nội suy.
            Vector3 local = Quaternion.Inverse(body.rotation) * (egg.Body.position - body.position);
            return _containment.Evaluate(local);
        }
    }
    public NestEggBody Egg => egg;
    public event Action Broken;

    public NestEggLifecycle(Transform root, Rigidbody body, BoxCollider floor)
    { transform = root; this.body = body; nestFloor = floor; }

    public void Create(GameObject visual, bool server)
    {
        if (server && nestFloor != null) CacheContainment();
        var obj = new GameObject("NetworkEgg (synced by Nest)");
        egg = obj.AddComponent<NestEggBody>();
        egg.Initialize(transform, visual);
        egg.SetAuthority(server);
        egg.Broken += OnBroken;
    }

    private void CacheContainment()
    {
        Bounds bounds = BoxInNestSpace(nestFloor);
        float floorTop = bounds.max.y;
        // Chỉ đọc collider một lần; các thành tổ quyết định chiều cao vùng chứa.
        foreach (BoxCollider box in transform.GetComponentsInChildren<BoxCollider>())
            if (box.enabled && !box.isTrigger) bounds.Encapsulate(BoxInNestSpace(box));
        Vector3 minimum = bounds.min;
        Vector3 maximum = bounds.max;
        minimum.y = floorTop - 0.1f;
        maximum.y += NestEggBody.Height * 0.5f;
        bounds.SetMinMax(minimum, maximum);
        _containment = new NestEggContainment(bounds);
    }

    private Bounds BoxInNestSpace(BoxCollider box)
    {
        Matrix4x4 toNest = transform.worldToLocalMatrix * box.transform.localToWorldMatrix;
        Bounds bounds = new Bounds(toNest.MultiplyPoint3x4(box.center), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = box.center + Vector3.Scale(box.size * 0.5f,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            bounds.Encapsulate(toNest.MultiplyPoint3x4(corner));
        }
        return bounds;
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
        _containment?.Reset();
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
        _containment?.Reset();
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
