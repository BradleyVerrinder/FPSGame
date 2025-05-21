using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.Netcode.Components;

public class PlayerHealth : NetworkBehaviour
{
    public float maxHealth = 100f;
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        writePerm: NetworkVariableWritePermission.Server
);

    public float RespawnDelay = 5f;

    private FirstPersonController firstPersonController;
    private Renderer[] renderers;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        firstPersonController = GetComponent<FirstPersonController>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void TakeDamage(float amount)
    {
        if (!IsServer) return;

        currentHealth.Value -= amount;
        if (currentHealth.Value <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} died.");

        // Disable character on server immediately
        EnableCharacter(false);
        // Disable visuals and controls
        SetPlayerStateClientRpc(false);


        if (IsServer)
        {
            StartCoroutine(RespawnAfterDelay());
        }
    }

    System.Collections.IEnumerator RespawnAfterDelay()
    {

        yield return new WaitForSeconds(RespawnDelay);

        currentHealth.Value = maxHealth;
        transform.position = GetRandomSpawnPoint();
        Quaternion spawnRotation = Quaternion.identity; // Or any desired rotation
        Vector3 spawnScale = transform.localScale;  
        GetComponent<NetworkTransform>()?.Teleport(transform.position, spawnRotation, spawnScale);

        //gameObject.SetActive(true);
        SetPlayerStateClientRpc(true);

        // Notify client to teleport and re-enable
        RespawnClientRpc(transform.position);

        // Re-enable visuals and colliders for everyone
        EnableCharacter(true);
    }

    [ClientRpc]
    void RespawnClientRpc(Vector3 newPosition)
    {
        Debug.Log($"[CLIENT] {gameObject.name} RespawnClientRpc called. Owner? {IsOwner}");
    
        EnableCharacter(true);
    
        if (IsOwner && firstPersonController != null)
        {
            Debug.Log($"[CLIENT] Enabling FirstPersonController for {gameObject.name}");
            firstPersonController.enabled = true;
        }
    }

    void EnableCharacter(bool state)
{
    // Re-enable movement controller (if on host or owner)
    var cc = GetComponent<CharacterController>();
    if (cc != null) cc.enabled = state;

    // Enable visual components (like MeshRenderer or SkinnedMeshRenderer)
    foreach (var renderer in GetComponentsInChildren<Renderer>())
    {
        renderer.enabled = state;
    }

    // Enable collider(s)
    foreach (var collider in GetComponentsInChildren<Collider>())
    {
        collider.enabled = state;
    }
}

    Vector3 GetRandomSpawnPoint()
    {
        // Replace with your actual spawn point logic
        return new Vector3(Random.Range(-10, 10), 1, Random.Range(-10, 10));
    }

    public float GetHealth() => currentHealth.Value;

    [ServerRpc]
    void SetPlayerStateServerRpc(bool isAlive)
    {
        SetPlayerStateClientRpc(isAlive);
    }
    
    [ClientRpc]
    void SetPlayerStateClientRpc(bool isAlive)
    {
        if (firstPersonController != null)
            firstPersonController.enabled = isAlive;

        foreach (var r in renderers)
            r.enabled = isAlive;

            // Disable colliders too
        foreach (var collider in GetComponentsInChildren<Collider>())
            collider.enabled = isAlive;

        var cc = GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = isAlive;

        if (!isAlive && IsOwner)
        {
            // Optional: show UI countdown here
        }
    }
}
