using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

public class WeaponManager : NetworkBehaviour
{
    [Header("Weapons")]
    public List<Gun> weaponInventory;
    public Camera playerCamera;
    public GameObject floatingTextPrefab;

    private int currentWeaponIndex = 0;
    private Gun CurrentGun => weaponInventory[currentWeaponIndex];

    [Header("Ammo Tracking")]
    private NetworkVariable<int> currentAmmo = new(30, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> reserveAmmo = new(90, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

    public event Action<int, int> OnAmmoChanged;

    private float nextTimeToFire = 0f;

    private CameraRecoil camRecoil;

    private void Start()
    {
        Debug.Log("WeaponManager: Start");
        EquipWeapon(currentWeaponIndex);

        if (IsOwner)
        {
            Debug.Log("WeaponManager: Subscribed to ammo events");
            currentAmmo.OnValueChanged += AmmoChanged;
            reserveAmmo.OnValueChanged += AmmoChanged;

            camRecoil = playerCamera.GetComponent<CameraRecoil>();
        }
    }

    private void OnEnable()
    {
        Debug.Log("WeaponManager: Subscribed to ammo events");
    }

    private void OnDisable()
    {
        if (IsOwner)
        {
            currentAmmo.OnValueChanged -= AmmoChanged;
            reserveAmmo.OnValueChanged -= AmmoChanged;
        }
    }

    private void AmmoChanged(int _, int __)
    {
        OnAmmoChanged?.Invoke(currentAmmo.Value, reserveAmmo.Value);
    }

    private void Update()
    {
        if (!IsOwner || playerCamera == null)
            return;

        if (IsOwner && TryGetComponent<FirstPersonController>(out var controller))
        {
            controller.isFiring = Input.GetButton("Fire1");
        }

        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
        {
            if (currentAmmo.Value > 0)
            {
                nextTimeToFire = Time.time + 1f / CurrentGun.fireRate;
                CurrentGun.PlayFireEffects();
                TryShoot();
            }
        }

        if (Input.GetKeyDown(KeyCode.R) || currentAmmo.Value == 0)
        {
            TryReload();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchWeapon(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchWeapon(1);
    }

    private void TryShoot()
    {
        if (IsOwner && TryGetComponent<FirstPersonController>(out var controller))
        {
            // 1. Generate recoil
            Vector2 recoil = CurrentGun.recoilKick;
            recoil.y *= UnityEngine.Random.Range(-1f, 1f); // Optional horizontal randomness

            // 2. Apply recoil BEFORE calculating bullet direction
            controller.AddRecoil(recoil);

            // 3. Get updated shoot direction (after recoil)
            Vector3 shootDirection = controller.GetShootDirection();

            // 4. Create the ray from the updated camera view
            Ray ray = new Ray(playerCamera.transform.position, shootDirection);

            // 5. Optional: Visual fire effects
            CurrentGun.PlayFireEffects();

            // 6. Do hit detection
            if (Physics.Raycast(ray, out RaycastHit hit, CurrentGun.range))
            {
                // Floating text for feedback
                if (floatingTextPrefab != null)
                {
                    Vector3 spawnPos = hit.transform.position + Vector3.up * 2f;
                    GameObject instance = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
                    FloatingText ft = instance.GetComponent<FloatingText>();
                    ft.SetText(CurrentGun.damage.ToString());
                    ft.shooterCamera = playerCamera;
                }

                var netObj = hit.transform.GetComponentInParent<NetworkObject>();
                if (netObj != null)
                {
                    ShootServerRpc(netObj.NetworkObjectId, CurrentGun.damage);
                }
                else
                {
                    ShootServerRpc(0, CurrentGun.damage);
                }
            }
            else
            {
                // Still deduct ammo if we miss
                ShootServerRpc(0, CurrentGun.damage);
            }
        }
    }


    [ServerRpc]
    private void ShootServerRpc(ulong targetId, float damage)
    {
        if (currentAmmo.Value <= 0) return;

        currentAmmo.Value--;
        TriggerAmmoHUDUpdateClientRpc(currentAmmo.Value, reserveAmmo.Value);

        if (targetId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject hitObj))
        {
            var health = hitObj.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }
    }

    [ClientRpc]
    private void TriggerAmmoHUDUpdateClientRpc(int current, int reserve)
    {
        if (IsOwner)
        {
            OnAmmoChanged?.Invoke(current, reserve);
        }
    }




    private void TryReload()
    {
        if (currentAmmo.Value == CurrentGun.magazineSize || reserveAmmo.Value <= 0)
            return;

        ReloadServerRpc();
    }

    [ServerRpc]
    private void ReloadServerRpc()
    {
        int needed = CurrentGun.magazineSize - currentAmmo.Value;
        int toReload = Mathf.Min(needed, reserveAmmo.Value);

        currentAmmo.Value += toReload;
        reserveAmmo.Value -= toReload;
    }

    public void SwitchWeapon(int index)
    {
        if (index < 0 || index >= weaponInventory.Count) return;

        weaponInventory[currentWeaponIndex].gameObject.SetActive(false);
        currentWeaponIndex = index;
        EquipWeapon(index);
    }

    private void EquipWeapon(int index)
    {
        weaponInventory[index].gameObject.SetActive(true);

        // Reset ammo if needed (for simplicity)
        if (IsServer)
        {
            currentAmmo.Value = weaponInventory[index].magazineSize;
            reserveAmmo.Value = 90;
        }

        OnAmmoChanged?.Invoke(currentAmmo.Value, reserveAmmo.Value);
    }

    public int CurrentAmmo => currentAmmo.Value;
    public int ReserveAmmo => reserveAmmo.Value;
}
