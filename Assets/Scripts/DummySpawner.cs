using Unity.Netcode;
using UnityEngine;

public class DummySpawner : NetworkBehaviour
{
    public GameObject dummyPrefab;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnDummy();
        }
    }

    void SpawnDummy()
    {
        GameObject dummy = Instantiate(dummyPrefab, new Vector3(0, 0, 2), Quaternion.identity);
        dummy.GetComponent<NetworkObject>().Spawn();
    }
}
