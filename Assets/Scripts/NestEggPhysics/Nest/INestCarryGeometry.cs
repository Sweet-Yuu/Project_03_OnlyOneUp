using UnityEngine;

public interface INestCarryGeometry
{
    Pose Calculate(Vector3 hostAnchor, Vector3 guestAnchor);
}
