using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class Target : NetworkBehaviour
{
    public float health = 50f;

    private NetworkObject netObj;
    public GameObject floatingTextPrefab;

    void Awake()
    {
        netObj = GetComponent<NetworkObject>();
    }

    public void TakeDamage(float amount)
    {
        if (!IsServer) return;

        health -= amount;
        Debug.Log(gameObject.name + " took " + amount + " damage. Remaining health: " + health);

        if (health <= 0f)
        {
            Die();
        }
 
    }

    void Die()
    {
        if (IsServer && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
    }
}
