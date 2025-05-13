using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnedCheck : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (IsOwner)
        {
            Debug.Log("I am the local player!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
