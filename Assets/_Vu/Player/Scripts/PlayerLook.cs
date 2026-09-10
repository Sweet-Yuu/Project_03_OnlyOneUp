using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    public PlayerData playerData;
    private float xRotation = 0f;

    private void Start()
    {
        Cursor.visible = false;
    }
    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;

        xRotation -= mouseY * playerData.ySensitivity * Time.deltaTime;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX * playerData.xSensitivity * Time.deltaTime);
    }
}

