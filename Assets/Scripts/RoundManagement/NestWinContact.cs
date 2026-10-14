using System.Collections.Generic;
using UnityEngine;

// Kiểm tra hình học thật vì tổ kinematic không luôn nhận OnCollisionEnter với vật tĩnh.
public sealed class NestWinContact
{
    private readonly Collider[] _shells;
    private readonly List<BoxCollider> _targets = new();

    public NestWinContact(Transform nest)
    {
        _shells = nest.GetComponentsInChildren<Collider>(true);
        foreach (BoxCollider box in Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (box.gameObject.scene != nest.gameObject.scene || box.transform.IsChildOf(nest)) continue;
            for (Transform current = box.transform; current != null; current = current.parent)
                if (current.tag == "Win_Area") { _targets.Add(box); break; }
        }
    }

    public bool IsTouching()
    {
        foreach (Collider shell in _shells)
        {
            if (shell == null || !shell.enabled || shell.isTrigger || !shell.gameObject.activeInHierarchy) continue;
            foreach (BoxCollider target in _targets)
            {
                if (target == null || !target.enabled || !target.gameObject.activeInHierarchy ||
                    Physics.GetIgnoreLayerCollision(shell.gameObject.layer, target.gameObject.layer) ||
                    Physics.GetIgnoreCollision(shell, target)) continue;
                if (Physics.ComputePenetration(shell, shell.transform.position, shell.transform.rotation,
                    target, target.transform.position, target.transform.rotation, out _, out _)) return true;
            }
        }
        return false;
    }
}
