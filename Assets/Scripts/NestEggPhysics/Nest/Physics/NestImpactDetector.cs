using System.Collections.Generic;
using UnityEngine;

// Kiểm tra collider thật của tổ kinematic, kể cả khi chạm môi trường tĩnh.
public sealed class NestImpactDetector
{
    private readonly Transform _root;
    private readonly Collider[] _shells;
    private HashSet<int> _previous = new();
    private HashSet<int> _current = new();

    public NestImpactDetector(Transform root)
    {
        _root = root;
        _shells = root.GetComponentsInChildren<Collider>();
    }

    public bool Sample(Transform host, Transform guest, Transform egg,
        LayerMask layers, out Vector3 away)
    {
        _current.Clear();
        away = Vector3.zero;
        bool entered = false;
        foreach (Collider shell in _shells)
        {
            if (shell == null || !shell.enabled || shell.isTrigger ||
                !shell.gameObject.activeInHierarchy) continue;
            Bounds bounds = shell.bounds;
            foreach (Collider other in Physics.OverlapBox(bounds.center,
                bounds.extents, Quaternion.identity, layers, QueryTriggerInteraction.Ignore))
            {
                if (other.transform.IsChildOf(_root) ||
                    (host != null && other.transform.IsChildOf(host)) ||
                    (guest != null && other.transform.IsChildOf(guest)) ||
                    (egg != null && other.transform.IsChildOf(egg)) ||
                    Physics.GetIgnoreLayerCollision(shell.gameObject.layer, other.gameObject.layer) ||
                    Physics.GetIgnoreCollision(shell, other)) continue;
                if (!Physics.ComputePenetration(shell, shell.transform.position, shell.transform.rotation,
                    other, other.transform.position, other.transform.rotation,
                    out Vector3 direction, out float depth) || depth <= 0.001f) continue;
                int id = other.attachedRigidbody != null
                    ? other.attachedRigidbody.gameObject.GetInstanceID() : other.gameObject.GetInstanceID();
                if (_current.Add(id) && !_previous.Contains(id))
                {
                    entered = true;
                    away += direction;
                }
            }
        }
        (_previous, _current) = (_current, _previous);
        return entered;
    }
}
