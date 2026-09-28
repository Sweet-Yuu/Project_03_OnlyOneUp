using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour, ISpawnable
{
    public static PlayerController instance;
    public Collider playerCollider;
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
        playerCollider = GetComponent<Collider>();
    }
   public void SpawnAt(Vector3 pos)
   {
       transform.position = pos;
   }

   void OnCollisionEnter(Collision collision)
   {
       if(collision.gameObject.CompareTag("Dead"))
       {
           Debug.Log("Player hit a deadly object");
           SpawnPlayer.instance.ResetPlayerPosition();
       }
   }
}