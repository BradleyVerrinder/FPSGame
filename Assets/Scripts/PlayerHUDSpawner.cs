using UnityEngine;
using Unity.Netcode;

public class PlayerHUDSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject hudPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        GameObject hudInstance = Instantiate(hudPrefab);
        HUDController hudController = hudInstance.GetComponent<HUDController>();


        // Pass self to HUDController if still needed
        hudController.Initialize(gameObject); 
    }
}
