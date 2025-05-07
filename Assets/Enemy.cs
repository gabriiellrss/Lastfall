using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    private Animator anim;
    public float health = 100f;
    private float deathAnimationTime = 5f; // Tempo da animação de morte
    private float blinkStartDelay = 3f;    // Tempo para esperar antes de começar o pisca-pisca
    private float blinkDuration = 1f;      // Duração do pisca-pisca
    private float blinkInterval = 0.1f;    // Intervalo entre piscadas

    private bool isDead = false;


    void Start()
    {
        anim = GetComponent<Animator>();
    }   

    public void TakeDemage(float demage)
    {
        if (health <= 0)
        {
            Die();
        }
        else
        {
            health -= demage;
            Debug.Log("Enemy health" + health);
            anim.SetTrigger("Demage");

        }






    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        anim.SetTrigger("Dead");
        StartCoroutine(HandleDeath());
    }

    private IEnumerator HandleDeath()
    {
        yield return new WaitForSeconds(deathAnimationTime + blinkStartDelay);
        StartCoroutine(BlinkBeforeDestroy());
    }

    private IEnumerator BlinkBeforeDestroy()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        float elapsed = 0f;

        while (elapsed < blinkDuration)
        {
            if (renderer != null)
                renderer.enabled = !renderer.enabled;

            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        if (renderer != null)
            renderer.enabled = true; // Garante que fique visível antes de sumir

        Destroy(gameObject);
    }

}
