using UnityEngine;

public class Target : MonoBehaviour
{
    public float health = 50f;
    public GameObject floatingTextPrefab;

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log(gameObject.name + " took " + amount + " damage. Remaining health: " + health);

        if (health <= 0f)
        {
            Die();
        }

        if (floatingTextPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 2f;
            GameObject instance = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
            instance.GetComponent<FloatingText>().SetText(amount.ToString());
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " died.");
        Destroy(gameObject);
    }
}
