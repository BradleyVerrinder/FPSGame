using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.Netcode.Components;
using NUnit.Framework.Internal.Commands;
using System;
using TMPro;

public class PlayerHealth : NetworkBehaviour
{
    public float maxHealth = 100f;
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        writePerm: NetworkVariableWritePermission.Server
);

    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(
        false,
        writePerm: NetworkVariableWritePermission.Server
    );

    public event Action<bool> onDeathStateChanged;

    public float RespawnDelay = 5f;

    public TextMeshProUGUI countdownText;

    public float CurrentHealthNormalised => currentHealth.Value / maxHealth;

    public event Action<float> onHealthChanged;

    public GameObject deathScreen;

    private FirstPersonController firstPersonController;
    private Renderer[] renderers;

    public override void OnNetworkSpawn()
    {

        if (IsOwner)
        {
            if (deathScreen != null)
            {
                deathScreen.SetActive(false);
            }
            var wm = GetComponent<WeaponManager>();
        }


        base.OnNetworkSpawn();
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        firstPersonController = GetComponent<FirstPersonController>();
        renderers = GetComponentsInChildren<Renderer>();
    }


    private void OnIsDeadChanged(bool oldVal, bool newVal)
    {
        if (!IsOwner) return;

        onDeathStateChanged?.Invoke(newVal); // true = dead, false = alive
    }

    //Event which the UI will subscribe to so that it can update the health slider
    private void OnEnable()
    {
        currentHealth.OnValueChanged += OnHealthChangedCallback; //Calls that function when OnValueChanged is triggered
        isDead.OnValueChanged += OnIsDeadChanged;
    }

    private void OnDisable()
    {
        currentHealth.OnValueChanged -= OnHealthChangedCallback; //Stops calling function when value is changed upon game object being disabled
        isDead.OnValueChanged -= OnIsDeadChanged;
    }

    private void OnHealthChangedCallback(float oldVal, float newVal) // Variables automatically given by netcode for game objects function "OnValueChanged"
    {
        float normalised = newVal / maxHealth;
        onHealthChanged?.Invoke(normalised); // C# event (avoids polling)
    }

    public void TakeDamage(float amount)
    {
        if (!IsServer) return;

        float newHealth = Mathf.Clamp(currentHealth.Value - amount, 0, maxHealth);
        currentHealth.Value = newHealth;
        if (currentHealth.Value <= 0f)
        {
            Die();
        }
    }

    public float CurrentHealthNormalized => currentHealth.Value / maxHealth;

    void Die()
    {

        // Disable character on server immediately
        EnableCharacter(false);
        // Disable visuals and controls
        SetPlayerStateClientRpc(false);


        if (IsServer)
        {
            // Tell the owner client to show their death screen
            isDead.Value = true;
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

        isDead.Value = false;

        // Re-enable visuals and colliders for everyone
        EnableCharacter(true);
    }

    [ClientRpc]
    void RespawnClientRpc(Vector3 newPosition)
    {
    
        EnableCharacter(true);
    
        if (IsOwner && firstPersonController != null)
        {
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
        return new Vector3(UnityEngine.Random.Range(-10, 10), 1, UnityEngine.Random.Range(-10, 10));
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
