using UnityEngine;
using Unity.Netcode;

public class PlayerHUDSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject hudPrefab;
    [SerializeField] private GameObject deathScreenPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        GameObject hudInstance = Instantiate(hudPrefab);
        HUDController hudController = hudInstance.GetComponent<HUDController>();
        GameObject deathScreenInstance = Instantiate(deathScreenPrefab);
        hudController.deathScreen = deathScreenInstance;

        // Pass self to HUDController if still needed
        hudController.Initialize(gameObject);
    }
}
