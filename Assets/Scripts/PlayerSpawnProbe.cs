using UnityEngine;

// Queries the starting floor and checks the controller footprint.
public sealed class PlayerSpawnProbe
{
    private readonly CharacterController controller;
    private GameObject gameObject => controller.gameObject;
    public PlayerSpawnProbe(CharacterController controller) => this.controller = controller;
    public bool StartPositionIsClear(Vector3 position, out string reason)
    {
        // Slightly inset so merely touching the supporting floor is allowed.
        float radius = Mathf.Max(0.01f, controller.radius - 0.02f);
        Vector3 center = position + controller.center;
        float halfSegment = Mathf.Max(0f, controller.height * 0.5f - controller.radius);
        foreach (var obstacle in Physics.OverlapCapsule(center + Vector3.up * halfSegment,
                     center - Vector3.up * halfSegment, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (obstacle.GetComponentInParent<NetworkNest>() != null ||
                obstacle.GetComponentInParent<NetworkTestMovement>() != null ||
                Physics.GetIgnoreLayerCollision(gameObject.layer, obstacle.gameObject.layer)) continue;
            reason = $"Spawn {position:F2} overlaps '{obstacle.name}' (radius={controller.radius:F2}, height={controller.height:F2}). Move Nest Start Center to open ground.";
            return false;
        }
        reason = null;
        return true;
    }
    public bool TryGetStartPosition(Vector3 approximate, out Vector3 position)
    {
        position = approximate;
        float highest = float.NegativeInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(approximate + Vector3.up * 5f,
                     Vector3.down, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y < 0.98f || hit.collider.GetComponentInParent<NetworkNest>() != null ||
                hit.collider.GetComponentInParent<NetworkTestMovement>() != null) continue;
            highest = Mathf.Max(highest, hit.point.y);
        }
        if (float.IsNegativeInfinity(highest)) return false;
        position.y = highest + controller.height * 0.5f - controller.center.y + controller.skinWidth;
        return true;
    }
}
