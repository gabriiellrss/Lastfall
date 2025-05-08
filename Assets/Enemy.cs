using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    private Animator anim;
    public float health = 100f;
    private float deathAnimationTime = 5f; // Tempo da anima��o de morte
    private float blinkStartDelay = 3f;    // Tempo para esperar antes de come�ar o pisca-pisca
    private float blinkDuration = 1f;      // Dura��o do pisca-pisca
    private float blinkInterval = 0.1f;    // Intervalo entre piscadas
    public float attackRange = 2f;
    private UnityEngine.AI.NavMeshAgent agent;

    private GameObject player;

    private bool isDead = false;


    void Start()
    {
        player = GameObject.Find("Player");

        agent = GetComponentInParent<UnityEngine.AI.NavMeshAgent>();
        anim = GetComponentInParent<Animator>();
        if (anim == null)
        {
            Debug.LogWarning("Animator n�o encontrado no pai!");
        }
    }

    void Update()
    {
        if (player == null) return;

        Transform playerTransform = player.transform;
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance > attackRange)
        {
            // Seguir o player
            agent.destination = playerTransform.position;
            //animator.SetBool("isWalking", true);
            //animator.SetBool("isAttacking", false);
        }
        else
        {
            // Atacar o player
            agent.ResetPath(); // Parar o movimento
                               // animator.SetBool("isWalking", false);
                               // animator.SetBool("isAttacking", true);

            // Olhar para o player
            Vector3 lookDirection = (playerTransform.position - transform.position).normalized;
            lookDirection.y = 0;
            transform.forward = lookDirection;
        }

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
            renderer.enabled = true; // Garante que fique vis�vel antes de sumir

        Destroy(transform.root.gameObject); // destr�i o topo da hierarquia (a "fam�lia inteira")

    }

}
