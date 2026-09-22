using UnityEngine;
public class BossMovement : MonoBehaviour
{
    public float laneChangeSpeed = 5f;
    public float forwardSpeed = 15f;

    public void MoveTowards(Vector3 destination)
    {
        float smoothX = Mathf.Lerp(transform.position.x, destination.x, laneChangeSpeed * Time.deltaTime);
        float smoothZ = Mathf.Lerp(transform.position.z, destination.z, forwardSpeed * Time.deltaTime);
        
        transform.position = new Vector3(smoothX, transform.position.y, smoothZ);
    }
}