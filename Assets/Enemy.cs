// EnemyAI.cs
// Este script controla o comportamento da Inteligência Artificial do Inimigo.

using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic; // Necessário para List<T>

public class Enemy : MonoBehaviour
{
    [Header("Referências")]
    public NavMeshAgent agent; // Componente NavMeshAgent para movimentação
    public Transform player; // Transform do jogador para perseguição
    public LayerMask playerLayer; // Layer do jogador para detecção
    public LayerMask obstacleLayer; // Layer de obstáculos para o campo de visão

    [Header("Configurações de Patrulha")]
    public List<Transform> patrolPoints; // Lista de pontos para patrulha
    public float patrolSpeed = 3.5f;
    public float patrolWaitTime = 2f; // Tempo que o inimigo espera em cada ponto de patrulha
    private int currentPatrolIndex = 0;
    private bool waitingAtPatrolPoint = false;

    [Header("Configurações de Campo de Visão")]
    public float visionRadius = 10f; // Raio de detecção do jogador
    [Range(0, 360)]
    public float visionAngle = 90f; // Ângulo do campo de visão

    [Header("Configurações de Perseguição")]
    public float chaseSpeed = 5f;
    public float chaseDistance = 15f; // Distância máxima para continuar perseguindo
    public float attackRange = 2f; // Distância para "atacar" (pode ser expandido)

    [Header("Configurações de Procura")]
    public float searchTime = 5f; // Tempo que o inimigo procura o jogador após perdê-lo
    private float currentSearchTime = 0f;
    private Vector3 lastKnownPlayerPosition;

    [Header("Configurações de Vida e Reaparecimento")]
    public float maxHealth = 100f;
    private float currentHealth;
    public List<Transform> respawnPoints; // Lista de pontos para reaparecimento
    public float respawnDelay = 5f;
    private bool isDead = false;

    private Animator animator;

    // Estados da IA
    public enum AIState { Patrolling, Chasing, Searching, Attacking, Dead }
    public AIState currentState = AIState.Patrolling;

    void Start()
    {
        animator = GetComponentInParent<Animator>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (player == null)
        {
            // Tenta encontrar o jogador pela tag "Player" se não estiver atribuído
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
            else Debug.LogError("Jogador não encontrado! Atribua o Transform do jogador ou marque-o com a tag 'Player'.");
        }

        currentHealth = maxHealth;
        agent.speed = patrolSpeed;
        GoToNextPatrolPoint();
    }

    void Update()
    {
        if (isDead || player == null) return; // Não faz nada se estiver morto ou sem referência do jogador

        // Lógica de transição de estados baseada na detecção e distância
        bool canSeePlayer = CanSeePlayer();
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case AIState.Patrolling:
                Patrol();
                if (canSeePlayer)
                {
                    ChangeState(AIState.Chasing);
                }
                break;

            case AIState.Chasing:
                ChasePlayer();
                if (!canSeePlayer && distanceToPlayer > chaseDistance)
                {
                    lastKnownPlayerPosition = player.position;
                    ChangeState(AIState.Searching);
                }
                else if (distanceToPlayer <= attackRange)
                {
                    // Implementar lógica de ataque aqui se necessário
                    // ChangeState(AIState.Attacking);
                    // Por enquanto, apenas para de se mover perto do jogador
                    agent.SetDestination(transform.position);
                }
                break;

            case AIState.Searching:
                SearchForPlayer();
                if (canSeePlayer)
                {
                    ChangeState(AIState.Chasing);
                }
                break;

                // case AIState.Attacking: 
                // Lógica de ataque aqui
                // if (distanceToPlayer > attackRange || !canSeePlayer) ChangeState(AIState.Chasing);
                // break;
        }
    }

    void ChangeState(AIState newState)
    {
        if (currentState == newState) return;
        currentState = newState;

        // Reset de parâmetros de animação
        animator.SetBool("isWalking", false);
        animator.SetBool("isAttacking", false);

        switch (currentState)
        {
            case AIState.Patrolling:
                agent.speed = patrolSpeed;
                animator.SetBool("isWalking", true);
                GoToNextPatrolPoint();
                break;

            case AIState.Chasing:
                agent.speed = chaseSpeed;
                animator.SetBool("isWalking", true);
                break;

            case AIState.Searching:
                agent.speed = patrolSpeed;
                currentSearchTime = 0f;
                agent.SetDestination(lastKnownPlayerPosition);
                animator.SetBool("isWalking", true);
                break;

            case AIState.Attacking:
                agent.SetDestination(transform.position); // Para de andar
                animator.SetBool("isAttacking", true);
                animator.SetTrigger("Attack");
                break;

            case AIState.Dead:
                agent.isStopped = true;
                animator.SetTrigger("Dead");
                break;
        }
    }


    void Patrol()
    {
        if (patrolPoints.Count == 0) return;

        if (!agent.pathPending && agent.remainingDistance < 0.5f && !waitingAtPatrolPoint)
        {
            waitingAtPatrolPoint = true;
            StartCoroutine(WaitAtPatrolPoint());
        }
    }

    IEnumerator WaitAtPatrolPoint()
    {
        yield return new WaitForSeconds(patrolWaitTime);
        GoToNextPatrolPoint();
        waitingAtPatrolPoint = false;
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Count == 0) return;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= visionRadius)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            if (angleToPlayer <= visionAngle / 2)
            {
                // Verifica se há obstáculos entre o inimigo e o jogador
                if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer))
                {
                    return true;
                }
            }
        }
        return false;
    }

    void ChasePlayer()
    {
        if (player != null)
        {
            agent.SetDestination(player.position);
        }
    }

    void SearchForPlayer()
    {
        currentSearchTime += Time.deltaTime;
        if (currentSearchTime >= searchTime || (!agent.pathPending && agent.remainingDistance < 0.5f))
        {
            ChangeState(AIState.Patrolling);
        }
    }

    public void TakeDemage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        animator.SetTrigger("Damage"); // animação de levar dano

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    void Die()
    {
        isDead = true;
        ChangeState(AIState.Dead);
        agent.isStopped = true; // Para o NavMeshAgent
        // Adicionar animação de morte, efeitos sonoros, etc.
        // Debug.Log(gameObject.name + " morreu.");

        // Desativa o GameObject do inimigo (ou apenas seus componentes visuais/colisores)
        // gameObject.SetActive(false); // Simplesmente desativar
        // Para um controle mais fino, você pode desabilitar renderers e colliders:
        GetComponent<Collider>().enabled = false;
        // Se tiver um Renderer, desabilite-o também:
        // GetComponent<Renderer>().enabled = false; 
        // Se tiver outros scripts que precisam ser desabilitados:
        // GetComponent<OutroScript>().enabled = false;

        StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (respawnPoints.Count > 0)
        {
            int respawnIndex = Random.Range(0, respawnPoints.Count);
            transform.position = respawnPoints[respawnIndex].position;
            agent.Warp(respawnPoints[respawnIndex].position); // Importante para NavMeshAgent
        }
        else
        {
            Debug.LogWarning("Nenhum ponto de respawn definido para " + gameObject.name + ". Reaparecendo na posição original de morte.");
            // Ou reaparecer em uma posição padrão, ou não reaparecer
        }

        // Reativa o inimigo
        // gameObject.SetActive(true);
        GetComponent<Collider>().enabled = true;
        // GetComponent<Renderer>().enabled = true;
        // GetComponent<OutroScript>().enabled = true;

        currentHealth = maxHealth;
        isDead = false;
        agent.isStopped = false;
        ChangeState(AIState.Patrolling); // Volta a patrulhar após reaparecer
        // Debug.Log(gameObject.name + " reapareceu.");
    }

    // Gizmos para visualização no Editor do Unity
    void OnDrawGizmosSelected()
    {
        // Campo de Visão
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        Vector3 fovLine1 = Quaternion.AngleAxis(visionAngle / 2, transform.up) * transform.forward * visionRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(-visionAngle / 2, transform.up) * transform.forward * visionRadius;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);

        if (player != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }

        // Raio de Ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Pontos de Patrulha
        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                    if (i < patrolPoints.Count - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                    else if (patrolPoints.Count > 1 && patrolPoints[0] != null) // Linha do último para o primeiro
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                    }
                }
            }
        }
    }
}

