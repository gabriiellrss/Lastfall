// Script do Boss - MODIFICADO: Simples, com 2 ataques básicos e triggers genéricos
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class Boss : MonoBehaviour // Nome da classe alterado
{
    [Header("Referências")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer; // Usado para detecção de visão
    private Player playerScript; // Script do Player
    private Animator animator;

    [Header("Configurações de Movimento")]
    public float chaseSpeed = 6.5f;

    [Header("Configurações de Detecção")]
    public float visionRadius = 18f;
    [Range(0, 360)]
    public float visionAngle = 130f;
    public float hearingRadius = 22f;
    public float loseSightDistance = 30f; // Distância para voltar a Idle

    // --- ATAQUES --- 
    [System.Serializable]
    public class AttackType
    {
        public string attackName = "Ataque Básico";
        public float attackRange = 3.0f;
        public float attackDamage = 20f;
        public float attackCooldown = 3.0f;
        public string animationTrigger = "Attack1"; // Trigger genérico
        public float animationDuration = 1.2f;
        [HideInInspector] public float currentCooldown = 0f;
    }

    [Header("Configurações de Ataque")]
    public List<AttackType> attacks = new List<AttackType>() {

                new AttackType {
                    attackName = "Ataque Rápido",
                    attackRange = 2.8f,
                    attackDamage = 20f,
                    attackCooldown = 2.5f,
                    animationTrigger = "Attack1", // Trigger Genérico 1
                    animationDuration = 1.0f
                },
                new AttackType {
                    attackName = "Ataque Forte",
                    attackRange = 3.5f,
                    attackDamage = 35f,
                    attackCooldown = 4.0f,
                    animationTrigger = "Attack2", // Trigger Genérico 2
                    animationDuration = 1.5f
                }
            };
    private bool isAttacking = false;
    private AttackType currentAttack = null;

    [Header("Configurações de Vida")]
    public float maxHealth = 600f; // Vida ajustada para um boss mais simples
    public float currentHealth;
    private bool isDead = false;
    // [SerializeField] private Image barHealth;

    // Estados da IA (Apenas Idle, Chasing, Attacking, Dead)
    public enum BossState { Idle, Chasing, Attacking, Dead }
    public BossState currentState = BossState.Idle;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                playerScript = playerObject.GetComponent<Player>();
                if (playerScript == null) Debug.LogWarning("Script Player não encontrado!");
            }
            else Debug.LogError("Jogador não encontrado!");
        }
        else
        {
            playerScript = player.GetComponent<Player>();
            if (playerScript == null) Debug.LogWarning("Script Player não encontrado no objeto atribuído!");
        }

        currentHealth = maxHealth;
        agent.speed = chaseSpeed;
        agent.isStopped = true; // Começa parado

        // Adiciona dois ataques de exemplo se a lista estiver vazia
        if (attacks == null || attacks.Count == 0)
        {
            Debug.LogWarning("Nenhum ataque configurado no Inspector. Adicionando 2 ataques de exemplo.");
            attacks = new List<AttackType>()
            {
                new AttackType {
                    attackName = "Ataque Rápido",
                    attackRange = 2.8f,
                    attackDamage = 20f,
                    attackCooldown = 2.5f,
                    animationTrigger = "Attack", // Trigger Genérico 1
                    animationDuration = 1.0f
                },
                new AttackType {
                    attackName = "Ataque Forte",
                    attackRange = 3.5f,
                    attackDamage = 35f,
                    attackCooldown = 4.0f,
                    animationTrigger = "Attack2", // Trigger Genérico 2
                    animationDuration = 1.5f
                }
            };
        }
        // Garante que a lista não tenha mais que 2 ataques se preenchida via código
        else if (attacks.Count > 2)
        {
            Debug.LogWarning("Mais de 2 ataques definidos no Inspector. Usando apenas os 2 primeiros.");
            attacks = attacks.GetRange(0, 2);
        }
        else if (attacks.Count < 2)
        {
            Debug.LogWarning("Menos de 2 ataques definidos no Inspector. Considere adicionar mais um.");
        }


        // Inicializa cooldowns
        foreach (var attack in attacks) { attack.currentCooldown = 0f; }

        currentState = BossState.Idle;
        Debug.Log("Boss Simples inicializado em modo Idle.");
    }

    void Update()
    {
        if (isDead || player == null || playerScript == null || playerScript.isDead)
        {
            if (!isDead && currentState != BossState.Idle)
            {
                StopMovingAndAttacking();
                ChangeState(BossState.Idle);
            }
            return;
        }

        UpdateCooldowns();

        switch (currentState)
        {
            case BossState.Idle:
                if (CanDetectPlayer())
                {
                    ChangeState(BossState.Chasing);
                }
                break;

            case BossState.Chasing:
                ChasePlayer();
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                AttackType availableAttack = ChooseAttack(distanceToPlayer);
                if (availableAttack != null)
                {
                    currentAttack = availableAttack;
                    ChangeState(BossState.Attacking);
                }
                else if (!CanDetectPlayer() && distanceToPlayer > loseSightDistance)
                {
                    ChangeState(BossState.Idle);
                }
                break;

            case BossState.Attacking:
                // Lógica iniciada na transição
                break;

            case BossState.Dead:
                break;
        }
    }

    void ChangeState(BossState newState)
    {
        if (currentState == newState || isDead) return;

        // Lógica de saída
        switch (currentState)
        {
            case BossState.Attacking:
                isAttacking = false;
                if (agent.enabled) agent.isStopped = false;
                break;
            case BossState.Chasing:
                if (agent.enabled) agent.isStopped = true;
                break;
            case BossState.Idle:
                break;
        }

        currentState = newState;

        // Lógica de entrada
        switch (newState)
        {
            case BossState.Idle:
                if (agent.enabled) agent.isStopped = true;
                 animator?.SetBool("isWalking", false);
                break;
            case BossState.Chasing:
                agent.speed = chaseSpeed; // Garante a velocidade correta
                if (agent.enabled) agent.isStopped = false;
                 animator?.SetBool("isWalking", true);
                break;
            case BossState.Attacking:
                StartAttack(currentAttack);
                break;
            case BossState.Dead:
                Die();
                break;
        }
    }

    void UpdateCooldowns()
    {
        foreach (var attack in attacks)
        {
            if (attack.currentCooldown > 0) attack.currentCooldown -= Time.deltaTime;
        }
    }

    void ChasePlayer()
    {
        if (player == null || isAttacking || !agent.enabled) return;
        agent.SetDestination(player.position);
        LookAtPlayer();
    }

    void LookAtPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * agent.angularSpeed);
        }
    }

    AttackType ChooseAttack(float distanceToPlayer)
    {
        if (isAttacking || player == null) return null;

        List<AttackType> availableAttacks = new List<AttackType>();
        foreach (var attack in attacks)
        {
            if (attack.currentCooldown <= 0 && distanceToPlayer <= attack.attackRange)
            {
                availableAttacks.Add(attack);
            }
        }

        if (availableAttacks.Count > 0)
        {
            int randomIndex = Random.Range(0, availableAttacks.Count);
            return availableAttacks[randomIndex];
        }
        return null;
    }

    void StartAttack(AttackType attack)
    {
        if (attack == null || isAttacking) return;

        isAttacking = true;
        if (agent.enabled) agent.isStopped = true;
        LookAtPlayer();

        Debug.Log($"Boss iniciando ataque: {attack.attackName} (Trigger: {attack.animationTrigger})");
        animator?.SetTrigger(attack.animationTrigger); // Usa o trigger genérico definido

        attack.currentCooldown = attack.attackCooldown;
        StartCoroutine(FinishAttack(attack.animationDuration));
        StartCoroutine(ApplyDamageAfterDelay(attack, 0.5f));
    }

    IEnumerator FinishAttack(float duration)
    {
        yield return new WaitForSeconds(duration);
        isAttacking = false;
        currentAttack = null;
        if (!isDead)
        {
            ChangeState(CanDetectPlayer() ? BossState.Chasing : BossState.Idle);
        }
    }

    IEnumerator ApplyDamageAfterDelay(AttackType attack, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isAttacking && player != null && playerScript != null && !playerScript.isDead && Vector3.Distance(transform.position, player.position) <= attack.attackRange * 1.1f)
        {
            Debug.Log($"Boss acertou com {attack.attackName} ({attack.attackDamage} dano).");
            playerScript.TakeDamage(attack.attackDamage);
        }
    }

    // --- Detecção --- 
    bool CanDetectPlayer()
    {
        if (player == null || playerScript.isDead) return false;
        float quickDistCheck = Vector3.Distance(transform.position, player.position);
        if (quickDistCheck > Mathf.Max(visionRadius, hearingRadius) * 1.2f && currentState == BossState.Idle)
        {
            return false;
        }
        return CanSeePlayer() || CanHearPlayer();
    }

    bool CanSeePlayer()
    {
        if (player == null || playerScript.isDead) return false;
        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        if (distanceToPlayer > visionRadius) return false;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer > visionAngle / 2) return false;
        if (Physics.Raycast(transform.position + Vector3.up * 0.8f, directionToPlayer.normalized, distanceToPlayer, obstacleLayer))
        {
            return false;
        }
        return true;
    }

    bool CanHearPlayer()
    {
        if (player == null || playerScript.isDead) return false;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        return distanceToPlayer <= hearingRadius;
    }

    // --- Vida e Morte --- 
    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        Debug.Log($"Boss recebeu {amount} de dano. Vida: {currentHealth}/{maxHealth}");
        animator?.SetTrigger("Hit");
        if (currentHealth <= 0)
        {
            ChangeState(BossState.Dead);
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Boss Simples Morreu!");
        StopMovingAndAttacking();
        if (agent.enabled) agent.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
            animator?.SetTrigger("Die");
            Destroy(gameObject, 10f);
    }

    void StopMovingAndAttacking()
    {
        if (agent != null && agent.enabled) agent.isStopped = true;
        StopAllCoroutines();
        isAttacking = false;
    }

    // Gizmos
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRadius);
        Vector3 fovLine1 = Quaternion.AngleAxis(visionAngle / 2, transform.up) * transform.forward * visionRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(-visionAngle / 2, transform.up) * transform.forward * visionRadius;
        Gizmos.DrawLine(transform.position, transform.position + fovLine1);
        Gizmos.DrawLine(transform.position, transform.position + fovLine2);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        if (attacks != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            foreach (var attack in attacks)
            {
                Gizmos.DrawWireSphere(transform.position, attack.attackRange);
            }
        }
    }
}

// --- Notas Adicionais ---
// 1. Simplicidade: Script focado em Idle -> Chase -> Attack. Sem patrulha, investida ou fúria.
// 2. Dois Ataques: Configurado para usar exatamente dois ataques. Se a lista no Inspector estiver vazia, cria dois exemplos ("Ataque Rápido", "Ataque Forte"). Se tiver mais de dois no Inspector, usa apenas os dois primeiros.
// 3. Triggers Genéricos: Os ataques de exemplo usam "Attack1" e "Attack2" como triggers. Certifique-se de criar estes triggers no seu Animator ou ajuste os nomes no script/Inspector.
// 4. Configuração: Ajuste os parâmetros (velocidade, dano, cooldown, vida, alcance, etc.) no Inspector.
// 5. Animações: Precisa das animações correspondentes aos triggers "Attack1" e "Attack2", e opcionalmente para Idle, Run, TakeDamage, Die.
// 6. Script Player: Precisa de um script 'Player' com TakeDamage(float) e isDead.
// 7. NavMesh: Necessário NavMesh Bakeado.
