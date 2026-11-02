using System;
using UnityEngine;

public class EggController : MonoBehaviour
{
    [SerializeField] private string[] breakTag;
    [SerializeField] private float breakForceThreshold = 3.0f;

    public event Action OnEggBroken;
    public bool IsBroken { get; private set; }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsBroken) return;
        if (collision.relativeVelocity.magnitude < breakForceThreshold) return;

        foreach (string tag in breakTag)
        {
            if (collision.gameObject.CompareTag(tag))
            {
                IsBroken = true;
                OnEggBroken?.Invoke();
                return;
            }
        }
    }
}
