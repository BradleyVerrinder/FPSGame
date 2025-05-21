using Unity.Netcode;
using UnityEngine;

public class Gun : NetworkBehaviour
{
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 5f;
    public Camera cam;
    public GameObject floatingTextPrefab; // assign in inspector

    private float nextTimeToFire = 0f;

    void Update()
    {
        if (!IsOwner || !cam || !Input.GetButton("Fire1") || Time.time < nextTimeToFire) return;

        nextTimeToFire = Time.time + 1f / fireRate;
        Shoot();
    }

    void Shoot()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Vector3 hitPoint = hit.point;

            // Show floating text locally for the shooter
            if (floatingTextPrefab != null)
            {
                Vector3 spawnPos = hit.transform.position + Vector3.up * 2f;
                GameObject instance = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
                FloatingText ft = instance.GetComponent<FloatingText>();
                ft.SetText(damage.ToString());
                ft.shooterCamera = cam;
            }

            // Send damage to the server
            NetworkObject targetNetObj = hit.transform.GetComponentInParent<NetworkObject>();
            if (targetNetObj != null)
            {
                Debug.Log("Called Shoot ServerRpc");
                ShootServerRpc(hitPoint, targetNetObj.NetworkObjectId);
            }
        }
    }

    [ServerRpc]
    void ShootServerRpc(Vector3 hitPoint, ulong targetId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetObj))
        {
           //Target target = targetObj.GetComponent<Target>();
           //if (target != null)
           //{
           //    target.TakeDamage(damage);
           //    return;
           //}

            // Damaging the player
            PlayerHealth playerHealth = targetObj.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                return;
            }
        }
    }
}
