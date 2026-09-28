using UnityEngine;

public class Coin : MonoBehaviour
{
    public Collider coinCollider;
    void Awake()
    {
        coinCollider = GetComponent<Collider>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player collected a coin");
            CurrencyManager.instance.AddCoin(1);
            Destroy(gameObject);
        }
    }
}
