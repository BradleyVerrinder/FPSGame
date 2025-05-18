using UnityEngine;

public class Target : MonoBehaviour
{
    public float health = 50f;

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log(gameObject.name + " took " + amount + " damage. Remaining health: " + health);

        if (health <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " died.");
        Destroy(gameObject);
    }
}
