using Unity.Netcode;
using UnityEngine;

public class Gun : NetworkBehaviour
{
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 5f;
    public Camera cam;

    private float nextTimeToFire = 0f;

    void Update()
    {
        if (!IsOwner || !cam || !Input.GetButton("Fire1") || Time.time < nextTimeToFire) return;

        nextTimeToFire = Time.time + 1f / fireRate;
        Shoot();
    }

    void Shoot()
    {
        // Only local detection, the actual logic happens on the server
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Vector3 hitPoint = hit.point;
            ulong targetNetworkObjectId = 0;

            NetworkObject targetNetObj = hit.transform.GetComponentInParent<NetworkObject>();
            if (targetNetObj != null)
                targetNetworkObjectId = targetNetObj.NetworkObjectId;

            ShootServerRpc(hitPoint, targetNetworkObjectId);
        }
    }

    [ServerRpc]
    void ShootServerRpc(Vector3 hitPoint, ulong targetId)
    {
        // Optional: validate client fire rate, etc.

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetObj))
        {
            Target target = targetObj.GetComponent<Target>();
            if (target != null)
            {
                target.TakeDamage(damage, cam);
            }
        }

        // Call ClientRpc to show effects on all clients
        ShootClientRpc(hitPoint);
    }

    [ClientRpc]
    void ShootClientRpc(Vector3 hitPoint)
    {
        // TODO: Play hit effect / muzzle flash / audio / animation on all clients
        Debug.Log("Playing hit effect at " + hitPoint);
    }
}
