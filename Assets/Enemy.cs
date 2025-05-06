using UnityEngine;

public class Enemy : MonoBehaviour
{

    public float health = 1f;

    public void TakeDemage(float demage)
    {
        health -= demage;
        Debug.Log("Enemy health" + health);
        
        if (health <= 0)
        {
            Die();
        }

    }

    void Die()
    {
        Debug.Log("morreu inimigo");
        Destroy(gameObject);
    }
}
