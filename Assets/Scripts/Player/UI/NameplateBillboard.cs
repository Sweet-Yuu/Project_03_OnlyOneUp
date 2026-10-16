using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(2000)]
public sealed class NameplateBillboard : MonoBehaviour
{
    private Camera _camera;
    private Coroutine _binding;

    private void OnEnable() => _binding = StartCoroutine(BindCamera());

    private IEnumerator BindCamera()
    {
        var interval = new WaitForSecondsRealtime(0.5f);
        while (true)
        {
            // Tìm camera theo chu kỳ, hỗ trợ camera thay đổi sau spawn/đổi scene.
            _camera = Camera.main;
            yield return interval;
        }
    }

    private void LateUpdate()
    {
        if (_camera != null) transform.rotation = _camera.transform.rotation;
    }

    private void OnDisable()
    {
        if (_binding != null) StopCoroutine(_binding);
        _binding = null;
        _camera = null;
    }
}
