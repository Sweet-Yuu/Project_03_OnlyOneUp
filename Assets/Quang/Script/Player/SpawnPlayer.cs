using UnityEngine;

public class SpawnPlayer : MonoBehaviour
{  
    public static SpawnPlayer instance; 
    public Transform spawnPos;

    public GameObject playerObject;
    private ISpawnable playerSpawnable;
    
    void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if(playerObject != null)
        {
            playerSpawnable = playerObject.GetComponent<ISpawnable>();
        }
    }

    public void ResetPlayerPosition()
    {
        Spawn(playerSpawnable);
    }

    void Spawn(ISpawnable entityToSpawn)
    {
        entityToSpawn.SpawnAt(spawnPos.position);
    }
    
}
