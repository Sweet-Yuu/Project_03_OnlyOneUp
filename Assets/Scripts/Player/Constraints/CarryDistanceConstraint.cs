using UnityEngine;

// Pure annulus projection. The caller supplies world positions and network tie-break ordering.
public static class CarryDistanceConstraint
{
    public static Vector3 Limit(Vector3 requestedMove, Vector3 currentPosition,
        Vector3 partnerPosition, float minDistance, float maxDistance,
        float correctionSpeed, float deltaTime, bool firstPlayer)
    {
        Vector3 wantedPosition = currentPosition + requestedMove;
        Vector3 offset = wantedPosition - partnerPosition;

        float distance = offset.magnitude;

        // Nằm trong khoảng cho phép thì di chuyển bình thường.
        if (distance >= minDistance && distance <= maxDistance)
            return requestedMove;

        Vector3 direction;

        if (distance > 0.001f)
        {
            direction = offset / distance;
        }
        else
        {
            // Nếu vị trí dự kiến trùng nhau, dùng hướng hiện tại.
            Vector3 currentOffset = currentPosition - partnerPosition;

            if (currentOffset.sqrMagnitude > 0.000001f)
            {
                direction = currentOffset.normalized;
            }
            else
            {
                // Hai nhân vật trùng vị trí: chọn hai hướng đối nhau.
                direction = firstPlayer
                    ? Vector3.left
                    : Vector3.right;
            }
        }

        // Quá gần → đưa ra khoảng cách tối thiểu.
        // Quá xa → đưa về khoảng cách tối đa.
        float targetDistance = Mathf.Clamp(
            distance, minDistance, maxDistance);

        Vector3 limitedPosition =
            partnerPosition + direction * targetDistance;

        Vector3 limitedMove = limitedPosition - currentPosition;

        float maxStep = Mathf.Max(
            requestedMove.magnitude,
            correctionSpeed * deltaTime);

        return Vector3.ClampMagnitude(limitedMove, maxStep);
    }
}
