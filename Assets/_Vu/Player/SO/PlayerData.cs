using UnityEngine;

[CreateAssetMenu(fileName = "New Player Data", menuName = "Data/Player Data")]
public class PlayerData : ScriptableObject
{
    [Header("Player Movement")]
    public float speed = 7f;
    public float accel = 40f;

    [Header("Player Jump")]
    public float jumpHeight = 1.6f;
    public float coyoteTime = 0.18f;
    public float jumpBufferTime = 0.18f;

    [Header("Player Jump - Apex Hang (game feel)")]
    [Tooltip("If |verticalVelocity| is below this, gravity is reduced for a floaty apex.")]
    public float apexThreshold = 1.5f;
    [Tooltip("Gravity multiplier applied near the apex. 1 = no change, <1 = floatier.")]
    public float apexGravityMultiplier = 0.4f;

    [Header("Player Gravity")]
    public float gravity = -20f;
    public float fallGravityMultiplier = 1.8f;
    public float jumpCutGravityMultiplier = 2.5f;
    [Tooltip("Max fall speed (positive number). Prevents tunneling through thin platforms on long falls and keeps falls readable/predictable.")]
    public float maxFallSpeed = 25f;

    [Header("Player Look")]
    public float xSensitivity = 30f;
    public float ySensitivity = 30f;

    [Header("Player Stamina")]
    public float jumpStaminaCost = 20f;

    [Header("Bunny Hop")]
    public float airAccelerate = 100f;
    public float airCap = 25f; // must stay > 0, otherwise air control is effectively disabled
    public float friction = 6.5f;
    public float maxAirSpeed = 7f; // kept equal to ground speed so running + jumping gives no free boost
}