using UnityEngine;

public class Gun : MonoBehaviour
{
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 5f;
    public Camera cam;

    private float nextTimeToFire = 0f;

    void Update()
    {
        if (!cam || !Input.GetButton("Fire1") || Time.time < nextTimeToFire) return;

        nextTimeToFire = Time.time + 1f / fireRate;
        Shoot();
    }

    void Shoot()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Debug.Log("Hit: " + hit.collider.name);
            // Damage logic here

            // Grabs the target script from the target
            Target target = hit.transform.GetComponentInParent<Target>();


            // If target has the target script attached -> take damage
            if (target != null)
            {
                target.TakeDamage(damage);
            }
        }
    }
}
