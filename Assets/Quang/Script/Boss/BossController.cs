using UnityEngine;

public class BossController : MonoBehaviour
{
    public static BossController instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        
    }
}
