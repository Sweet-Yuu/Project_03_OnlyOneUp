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
    private INestEggCarryPhysics _carryPhysics;
    //Gắn bộ xử lý chuyển động trứng trong tổ
    public void SetCarryPhysics(INestEggCarryPhysics physics) => _carryPhysics = physics;
    //Kiểm tra vị trí vật lý của trứng có nằm trong vùng chứa của tổ không
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
    //Tính vùng chứa từ collider đáy và các BoxCollider của tổ
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
    //Chuyển vùng bao của một BoxCollider về hệ tọa độ tổ
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
    public void Wait() { _carryPhysics?.Stop(); if (egg != null) egg.WaitForPlayers(); }
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
        _carryPhysics?.Reset(new Pose(body.position, body.rotation),
            localTop + Vector3.up * (NestEggBody.Height * 0.5f + 0.03f));
        // Collider re-enabling during reset can clear ignored pairs.
        ignoredHost = ignoredGuest = null;
        IgnorePlayers(host, guest);
    }
    //Đặt trứng ra sát ngoài mép tổ và cho rơi với vận tốc được truyền vào
    public void FallFromImpact(Vector3 direction, float sideSpeed, float upSpeed)
    {
        if (egg == null || !egg.IsSimulating) return;
        _carryPhysics?.Stop();
        // Đặt sát phía ngoài mép tổ để không cần lực hất cao vượt thành collider.
        Bounds bounds = nestFloor.bounds;
        foreach (Collider collider in transform.GetComponentsInChildren<Collider>())
            if (collider.enabled && !collider.isTrigger) bounds.Encapsulate(collider.bounds);
        Vector3 extents = bounds.extents;
        float distance = Mathf.Abs(direction.x) * extents.x + Mathf.Abs(direction.z) * extents.z + 1.05f;
        Vector3 position = bounds.center + direction * distance;
        position.y = egg.Body.position.y;
        egg.Body.position = position;
        egg.FallFromNestImpact(direction * sideSpeed + Vector3.up * upSpeed);
    }
    //Đặt trứng ra ngoài tổ để kiểm thử rơi
    public void DropForTest()
    {
        _carryPhysics?.Stop();
        _containment?.Reset();
        if (egg == null) return;
        Bounds bounds = nestFloor.bounds;
        foreach (Collider c in transform.GetComponentsInChildren<Collider>()) bounds.Encapsulate(c.bounds);
        Vector3 position = new Vector3(bounds.max.x + 1f, bounds.max.y + 1f, bounds.center.z);
        egg.ResetOnServer(position, Quaternion.identity);
        ignoredHost = ignoredGuest = null;
    }
    //Gỡ sự kiện, dừng xử lý và hủy GameObject trứng
    public void Dispose()
    {
        _carryPhysics?.Stop();
        _carryPhysics = null;
        if (egg != null)
        {
            egg.Broken -= OnBroken;
            UnityEngine.Object.Destroy(egg.gameObject);
        }
        egg = null;
        ignoredHost = ignoredGuest = null;
    }
}
