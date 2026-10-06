using UnityEngine;
using Unity.Cinemachine;

// Owns only the local camera binding and its restoration on despawn.
public sealed class LocalPlayerCamera
{
    private readonly Transform transform;
    private GameObject gameObject => transform.gameObject;
    private Camera localCamera;
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private CinemachineCamera followCamera;
    private CameraTarget savedCameraTarget;
    private Transform boundCameraTarget;
    private Transform cameraTrackingTarget;
    private bool useTestCamera;

    public LocalPlayerCamera(Transform player) => transform = player;

    public void Bind(Transform trackingTarget)
    {
        cameraTrackingTarget = trackingTarget;
        localCamera = Camera.main;
        if (localCamera != null)
        {
            savedCameraPosition = localCamera.transform.position;
            savedCameraRotation = localCamera.transform.rotation;
        }
        BindLocalCamera();
    }
    private void BindLocalCamera()
    {
        // Called only by the local owner. Preserve the scene's lens, orbit,
        // composition, damping and input settings; replace only its targets.
        useTestCamera = false;
        if (localCamera == null) return;
        var brain = localCamera.GetComponent<CinemachineBrain>();
        if (brain == null || !brain.isActiveAndEnabled)
        {
            useTestCamera = true;
            return;
        }
        CinemachineCamera onlyCamera = null;
        int count = 0;
        foreach (var candidate in Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None))
        {
            if (!candidate.isActiveAndEnabled || candidate.gameObject.scene != gameObject.scene) continue;
            count++;
            onlyCamera = candidate;
            if (candidate.name == "FreeLook Camera") followCamera = candidate;
        }
        if (followCamera == null && count == 1) followCamera = onlyCamera;
        if (followCamera == null)
        {
            Debug.LogWarning("Local player: enable the scene's FreeLook Camera and Cinemachine Brain. Camera target was not changed.", transform);
            return;
        }
        savedCameraTarget = followCamera.Target;
        boundCameraTarget = cameraTrackingTarget;
        if (boundCameraTarget == null) boundCameraTarget = transform.Find("Carrying");
        if (boundCameraTarget == null) boundCameraTarget = transform;
        var target = followCamera.Target;
        target.TrackingTarget = boundCameraTarget;
        if (target.CustomLookAtTarget) target.LookAtTarget = boundCameraTarget;
        followCamera.Target = target;
        followCamera.PreviousStateIsValid = false;
    }
    public void Tick()
    {
        if (localCamera == null || !useTestCamera) return;
        localCamera.transform.position = transform.position + new Vector3(0f, 5f, -8f);
        localCamera.transform.LookAt(transform.position + Vector3.up * 0.5f);
    }
    public void Unbind()
    {
        if (followCamera != null && followCamera.Target.TrackingTarget == boundCameraTarget)
        {
            followCamera.Target = savedCameraTarget;
            followCamera.PreviousStateIsValid = false;
        }
        followCamera = null;
        boundCameraTarget = null;
        if (localCamera == null) return;
        if (useTestCamera)
            localCamera.transform.SetPositionAndRotation(savedCameraPosition, savedCameraRotation);
        localCamera = null;
        useTestCamera = false;
    }
}
