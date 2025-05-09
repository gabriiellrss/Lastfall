using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    private Animator anim;
    public float health = 100f;
    private float deathAnimationTime = 5f;
    private float blinkStartDelay = 3f;
    private float blinkDuration = 1f;
    private float blinkInterval = 0.1f;

    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float attackCooldown = 2f;

    private UnityEngine.AI.NavMeshAgent agent;
    private GameObject player;
    private bool isDead = false;
    private bool isAttacking = false;

    public Transform attackPoint;
    public LayerMask playerLayer;

    void Start()
    {
        player = GameObject.Find("Player");

        agent = GetComponentInParent<UnityEngine.AI.NavMeshAgent>();
        anim = GetComponentInParent<Animator>();

        if (anim == null)
            Debug.LogWarning("Animator não encontrado no pai!");

        StartCoroutine(AttackLoop());
    }

    void Update()
    {
        if (player == null || isDead) return;

        Transform playerTransform = player.transform;
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance > attackRange)
        {
            agent.destination = playerTransform.position;
            anim.SetBool("isWalking", true);
        }
        else
        {
            agent.ResetPath();
            anim.SetBool("isWalking", false);

            Vector3 lookDirection = (playerTransform.position - transform.position).normalized;
            lookDirection.y = 0;
            transform.forward = lookDirection;
        }
    }

    IEnumerator AttackLoop()
    {
        while (true)
        {
            if (player != null && !isDead)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= attackRange && !isAttacking)
                {
                    isAttacking = true;
                    anim.SetTrigger("Attack");
                    //Attack();
                    yield return new WaitForSeconds(attackCooldown);
                    isAttacking = false;
                }
            }
            yield return null;
        }
    }

    void Attack()
    {
        if (attackPoint == null) return;

        /*Collider[] hitPlayers = Physics.OverlapSphere(attackPoint.position, attackRange, playerLayer);
        foreach (Collider hit in hitPlayers)
        {
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(attackDamage);
            }
        }*/
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
            Debug.Log("Enemy health: " + health);

            // Só toca a animação de dano se não estiver atacando
            if (!isAttacking)
            {
                anim.SetTrigger("Demage");
            }
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
            renderer.enabled = true;

        Destroy(transform.root.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}
