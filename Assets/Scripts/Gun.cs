using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Gun Stats")]
    public string weaponName = "Rifle";
    public float damage = 25f;
    public float fireRate = 10f;
    public int magazineSize = 30;
    public float range = 100f;

    [Header("Visual/Effects")]
    public ParticleSystem muzzleFlash;
    public AudioSource shotSound;

    public void PlayFireEffects()
    {
        if (muzzleFlash != null) muzzleFlash.Play();
        if (shotSound != null) shotSound.Play();
    }

    public Ray GetShotRay(Camera cam)
    {
        return cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    }
}
