using UnityEngine;

public class CameraRecoil : MonoBehaviour
{
    [Header("Recoil Settings")]
    public float returnSpeed = 3f;
    public float returnDelay = 1f;

    private Vector2 targetRecoil;  // Final recoil target (accumulates when shooting)
    private Vector2 currentRecoil; // Smoothly interpolates towards targetRecoil

    private float lastFireTime;

    private Vector3 originalRotation;

    private void Start()
    {
        originalRotation = transform.localEulerAngles;
    }

    public void ApplyRecoil(Vector2 recoil)
    {
        targetRecoil += recoil;
        lastFireTime = Time.time;
    }

    private void Update()
    {
        // Smoothly approach targetRecoil
        currentRecoil = Vector2.Lerp(currentRecoil, targetRecoil, Time.deltaTime * returnSpeed);

        // Apply recoil relative to original rotation
        Vector3 recoilEuler = new Vector3(-currentRecoil.x, currentRecoil.y, 0f);
        transform.localEulerAngles = originalRotation + recoilEuler;

        // After delay, target recoil returns to zero
        if (Time.time > lastFireTime + returnDelay)
        {
            targetRecoil = Vector2.Lerp(targetRecoil, Vector2.zero, Time.deltaTime * returnSpeed);

            if (targetRecoil.magnitude < 0.01f)
                targetRecoil = Vector2.zero;
        }
    }
}
