using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            Debug.Log("Player entered checkpoint");
            SpawnPlayer.instance.spawnPos.position = transform.position;
        }
    }
}
