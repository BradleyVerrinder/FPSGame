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

    public void TakeDamage(float amount, Camera shooterCamera)
    {
        if (!IsServer) return;
        
        health -= amount;
        Debug.Log(gameObject.name + " took " + amount + " damage. Remaining health: " + health);

        if (health <= 0f)
        {
            Die();
        }

        if (floatingTextPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 2f;
            GameObject instance = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
            instance.GetComponent<FloatingText>().SetText(amount.ToString());

            FloatingText ft = instance.GetComponent<FloatingText>();
            ft.SetText(amount.ToString());
            ft.shooterCamera = shooterCamera;
        }
    }
    

    void Die()
    {
        Debug.Log($"{gameObject.name} died. IsServer: {IsServer}, IsClient: {IsClient}");

        if (IsServer && netObj.IsSpawned)
        {
            Debug.Log("Despawning target on server.");
            netObj.Despawn(); // This will sync to clients if scene object is correctly registered
        }
        else
        {
            Debug.LogWarning("Despawn failed: either not server or not spawned.");
        }
    }
}
