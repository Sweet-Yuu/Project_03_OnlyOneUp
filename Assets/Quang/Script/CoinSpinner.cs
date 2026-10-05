using UnityEngine;

public class CoinSpinner : MonoBehaviour
{
    public float rotationSpeed = 100f;

    // Trục xoay (mặc định là Vector3.up - trục Y, xoay ngang)
    public Vector3 rotationAxis = Vector3.up;

    void Update()
    {
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}