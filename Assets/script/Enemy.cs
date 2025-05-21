// EnemyAI_Improved.cs
// Este script controla o comportamento da Inteligência Artificial do Inimigo com melhorias baseadas no código do player.

using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    [Header("Referências")]
    public NavMeshAgent agent; // Componente NavMeshAgent para movimentação
    public Transform player; // Transform do jogador para perseguição
    public LayerMask playerLayer; // Layer do jogador para detecção
    public LayerMask obstacleLayer; // Layer de obstáculos para o campo de visão
    private Player playerScript; // Referência ao script do Player para acessar seus estados

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
    public float hearingRadius = 15f; // Raio para ouvir o jogador (ataques, tiros, etc.)

    [Header("Configurações de Perseguição")]
    public float chaseSpeed = 5f;
    public float chaseDistance = 15f; // Distância máxima para continuar perseguindo
    public float attackRange = 2f; // Distância para atacar
    public float flankDistance = 5f; // Distância para flanquear o jogador
    public float flankAngle = 45f; // Ângulo para flanquear o jogador

    [Header("Configurações de Procura")]
    public float searchTime = 5f; // Tempo que o inimigo procura o jogador após perdê-lo
    private float currentSearchTime = 0f;
    private Vector3 lastKnownPlayerPosition;

    [Header("Configurações de Combate")]
    public float attackCooldown = 1.5f; // Tempo entre ataques
    private float attackTimer = 0f;
    public float attackDamage = 10f; // Dano causado pelo ataque
    public float attackAnimationDuration = 1.2f; // Duração da animação de ataque
    public float dodgeSpeed = 8f; // Velocidade de esquiva
    public float dodgeDuration = 0.5f; // Duração da esquiva
    private bool isDodging = false;
    private bool isAttacking = false;
    private float dodgeTimer = 0f;
    private Vector3 dodgeDirection;

    [Header("Configurações de Vida e Reaparecimento")]
    public float maxHealth = 100f;
    public float currentHealth;
    public List<Transform> respawnPoints; // Lista de pontos para reaparecimento
    public float respawnDelay = 5f;
    private bool isDead = false;

    [SerializeField] private Image barHealth;

    private Animator animator;

    public GameObject mutantObject;

    // Estados da IA
    public enum AIState { Patrolling, Chasing, Searching, Attacking, Flanking, Retreating, Dodging, Dead }
    public AIState currentState = AIState.Patrolling;
    private AIState previousState;

    // Variáveis para comportamento adaptativo
    private float playerThreatLevel = 0f; // Nível de ameaça do jogador (0-100)
    private float aggressiveness = 50f; // Nível de agressividade do inimigo (0-100)
    private int consecutiveHits = 0; // Hits consecutivos recebidos sem contra-atacar
    private float lastPlayerHealthPercentage = 100f; // Último percentual de vida do jogador observado
    private bool playerWasAttacking = false; // Se o jogador estava atacando na última verificação
    private bool playerHasWeapon = false; // Se o jogador está com arma equipada
    private float timeSinceLastPlayerAttack = 0f; // Tempo desde o último ataque do jogador
    private float distanceToPlayer = 0f; // Distância atual até o jogador
    private List<Vector3> recentPlayerPositions = new List<Vector3>(); // Posições recentes do jogador para prever movimento
    private float positionUpdateInterval = 0.5f; // Intervalo para atualizar a lista de posições
    private float positionUpdateTimer = 0f;

    public static System.Action OnEnemyDied { get; internal set; }

    void Start()
    {
        animator = GetComponentInParent<Animator>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (player == null)
        {
            // Tenta encontrar o jogador pela tag "Player" se não estiver atribuído
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                playerScript = playerObject.GetComponent<Player>(); // Obtém referência ao script do Player
                if (playerScript == null)
                {
                    Debug.LogWarning("Script Player não encontrado no objeto do jogador!");
                }
            }
            else Debug.LogError("Jogador não encontrado! Atribua o Transform do jogador ou marque-o com a tag 'Player'.");
        }
        else
        {
            playerScript = player.GetComponent<Player>(); // Obtém referência ao script do Player
        }

        currentHealth = maxHealth;
        agent.speed = patrolSpeed;
        GoToNextPatrolPoint();
    }

    void Update()
    {
        if (isDead || player == null) return; // Não faz nada se estiver morto ou sem referência do jogador

        // Atualiza informações do jogador
        UpdatePlayerInfo();

        // Lógica de transição de estados baseada na detecção e distância
        bool canSeePlayer = CanSeePlayer();
        distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Atualiza o timer de ataque
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }

        // Atualiza o timer de esquiva
        if (isDodging)
        {
            dodgeTimer -= Time.deltaTime;
            if (dodgeTimer <= 0)
            {
                isDodging = false;
                if (previousState != AIState.Dead)
                {
                    ChangeState(previousState);
                }
            }
            return; // Não faz mais nada enquanto estiver esquivando
        }

        // Lógica de transição de estados
        switch (currentState)
        {
            case AIState.Patrolling:
                Patrol();
                if (canSeePlayer || CanHearPlayer())
                {
                    ChangeState(AIState.Chasing);
                }
                break;

            case AIState.Chasing:
                ChasePlayer();

                // Se o jogador estiver atacando e estivermos perto, considere esquivar
                if (ShouldDodge())
                {
                    DodgeAttack();
                }
                // Se estiver perto o suficiente para atacar
                else if (distanceToPlayer <= attackRange && attackTimer <= 0)
                {
                    ChangeState(AIState.Attacking);
                }
                // Se o jogador estiver com pouca vida, seja mais agressivo
                else if (ShouldFlank())
                {
                    ChangeState(AIState.Flanking);
                }
                // Se o inimigo estiver com pouca vida, considere recuar
                else if (ShouldRetreat())
                {
                    ChangeState(AIState.Retreating);
                }
                // Se perder o jogador de vista e estiver longe
                else if (!canSeePlayer && distanceToPlayer > chaseDistance)
                {
                    lastKnownPlayerPosition = player.position;
                    ChangeState(AIState.Searching);
                }
                break;

            case AIState.Searching:
                SearchForPlayer();
                if (canSeePlayer || CanHearPlayer())
                {
                    ChangeState(AIState.Chasing);
                }
                break;

            case AIState.Attacking:
                if (!isAttacking)
                {
                    AttackPlayer();
                }
                break;

            case AIState.Flanking:
                FlankPlayer();
                // Se o jogador estiver atacando e estivermos perto, considere esquivar
                if (ShouldDodge())
                {
                    DodgeAttack();
                }
                // Se estiver perto o suficiente para atacar
                else if (distanceToPlayer <= attackRange && attackTimer <= 0)
                {
                    ChangeState(AIState.Attacking);
                }
                // Se perder o jogador de vista
                else if (!canSeePlayer)
                {
                    ChangeState(AIState.Chasing);
                }
                break;

            case AIState.Retreating:
                Retreat();
                // Se recuperou vida suficiente ou o jogador está muito perto
                if (currentHealth > maxHealth * 0.5f || distanceToPlayer <= attackRange)
                {
                    ChangeState(AIState.Chasing);
                }
                break;
        }

        // Atualiza a lista de posições recentes do jogador
        UpdateRecentPlayerPositions();
    }

    void UpdatePlayerInfo()
    {
        if (playerScript != null)
        {
            // Verifica se o jogador está atacando
            bool isPlayerAttacking = false;
            if (animator != null && animator.GetBool("isAttacking"))
            {
                isPlayerAttacking = true;
                timeSinceLastPlayerAttack = 0f;
            }
            else
            {
                timeSinceLastPlayerAttack += Time.deltaTime;
            }

            // Detecta mudança no estado de ataque do jogador
            if (isPlayerAttacking && !playerWasAttacking)
            {
                // Jogador começou a atacar
                playerThreatLevel += 10f;
            }
            playerWasAttacking = isPlayerAttacking;

            // Verifica se o jogador está com arma equipada
            playerHasWeapon = false;
            if (animator != null && animator.GetBool("isShoot"))
            {
                playerHasWeapon = true;
                playerThreatLevel += 5f;
            }

            // Verifica a saúde do jogador
            float currentPlayerHealth = playerScript.currentHealth;
            float playerHealthPercentage = (float)currentPlayerHealth / playerScript.maxHealth * 100f;

            // Se a saúde do jogador diminuiu, ele pode estar vulnerável
            if (playerHealthPercentage < lastPlayerHealthPercentage)
            {
                aggressiveness += 5f;
            }
            lastPlayerHealthPercentage = playerHealthPercentage;

            // Limita os valores
            playerThreatLevel = Mathf.Clamp(playerThreatLevel, 0f, 100f);
            aggressiveness = Mathf.Clamp(aggressiveness, 0f, 100f);

            // Decai o nível de ameaça com o tempo
            playerThreatLevel -= Time.deltaTime * 2f;
            if (playerThreatLevel < 0f) playerThreatLevel = 0f;
        }
    }

    void UpdateRecentPlayerPositions()
    {
        positionUpdateTimer += Time.deltaTime;
        if (positionUpdateTimer >= positionUpdateInterval)
        {
            positionUpdateTimer = 0f;
            recentPlayerPositions.Add(player.position);

            // Mantém apenas as últimas 5 posições
            if (recentPlayerPositions.Count > 5)
            {
                recentPlayerPositions.RemoveAt(0);
            }
        }
    }

    Vector3 PredictPlayerPosition(float timeAhead)
    {
        if (recentPlayerPositions.Count < 2)
            return player.position;

        // Calcula a velocidade média do jogador com base nas posições recentes
        Vector3 averageVelocity = Vector3.zero;
        for (int i = 1; i < recentPlayerPositions.Count; i++)
        {
            averageVelocity += (recentPlayerPositions[i] - recentPlayerPositions[i - 1]) / positionUpdateInterval;
        }
        averageVelocity /= (recentPlayerPositions.Count - 1);

        // Prevê a posição futura
        return player.position + averageVelocity * timeAhead;
    }

    bool ShouldDodge()
    {
        // Esquiva se o jogador estiver atacando e estivermos perto
        if (playerWasAttacking && distanceToPlayer < attackRange * 1.5f && timeSinceLastPlayerAttack < 0.5f)
        {
            return Random.value < 0.7f; // 70% de chance de esquivar
        }
        return false;
    }

    bool ShouldFlank()
    {
        // Flanqueia se o jogador estiver com pouca vida ou se estivermos com muita vida
        if ((lastPlayerHealthPercentage < 30f || currentHealth > maxHealth * 0.7f) &&
            distanceToPlayer < flankDistance && distanceToPlayer > attackRange)
        {
            return Random.value < 0.6f; // 60% de chance de flanquear
        }
        return false;
    }

    bool ShouldRetreat()
    {
        // Recua se estivermos com pouca vida e o jogador estiver com muita
        if (currentHealth < maxHealth * 0.3f && lastPlayerHealthPercentage > 50f)
        {
            return Random.value < 0.7f; // 70% de chance de recuar
        }
        return false;
    }

    void ChangeState(AIState newState)
    {
        if (currentState == newState) return;

        previousState = currentState;
        currentState = newState;

        // Reset de parâmetros de animação
        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isAttacking", false);
        }

        switch (currentState)
        {
            case AIState.Patrolling:
                agent.speed = patrolSpeed;
                if (animator != null) animator.SetBool("isWalking", true);
                GoToNextPatrolPoint();
                break;

            case AIState.Chasing:
                agent.speed = chaseSpeed;
                if (animator != null) animator.SetBool("isWalking", true);
                break;

            case AIState.Searching:
                agent.speed = patrolSpeed;
                currentSearchTime = 0f;
                agent.SetDestination(lastKnownPlayerPosition);
                if (animator != null) animator.SetBool("isWalking", true);
                break;

            case AIState.Attacking:
                agent.SetDestination(transform.position); // Para de andar
                if (animator != null)
                {
                    animator.SetBool("isAttacking", true);
                    animator.SetTrigger("Attack");
                }
                break;

            case AIState.Flanking:
                agent.speed = chaseSpeed * 1.2f; // Mais rápido ao flanquear
                if (animator != null) animator.SetBool("isWalking", true);
                break;

            case AIState.Retreating:
                agent.speed = chaseSpeed * 1.1f; // Ligeiramente mais rápido ao recuar
                if (animator != null) animator.SetBool("isWalking", true);
                break;

            case AIState.Dodging:
                agent.speed = dodgeSpeed;
                if (animator != null) animator.SetTrigger("Dodge");
                break;

            case AIState.Dead:
                agent.isStopped = true;
                if (animator != null) animator.SetTrigger("Dead");
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
        if (patrolPoints.Count == 0)
            return;
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

    bool CanHearPlayer()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Pode ouvir o jogador se ele estiver atacando ou atirando
        if (distanceToPlayer <= hearingRadius)
        {
            if (playerWasAttacking || (playerHasWeapon && timeSinceLastPlayerAttack < 1.0f))
            {
                return true;
            }
        }

        return false;
    }

    void ChasePlayer()
    {
        if (player != null)
        {
            // Persegue a posição prevista do jogador em vez da atual
            Vector3 targetPosition = PredictPlayerPosition(0.5f);
            agent.SetDestination(targetPosition);
        }
    }

    void FlankPlayer()
    {
        if (player == null) return;

        // Calcula uma posição para flanquear o jogador
        Vector3 directionToPlayer = (player.position - transform.position).normalized;

        // Rotaciona a direção para a esquerda ou direita com base na posição atual
        float side = (Vector3.Dot(transform.right, directionToPlayer) > 0) ? -1 : 1;
        Vector3 flankDirection = Quaternion.Euler(0, side * flankAngle, 0) * directionToPlayer;

        // Calcula a posição de flanqueamento
        Vector3 flankPosition = player.position + flankDirection * flankDistance;

        // Verifica se a posição é válida no NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(flankPosition, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // Se não for válida, volta para perseguição normal
            ChasePlayer();
        }
    }

    void Retreat()
    {
        if (player == null) return;

        // Calcula uma direção oposta ao jogador
        Vector3 retreatDirection = (transform.position - player.position).normalized;
        Vector3 retreatPosition = transform.position + retreatDirection * 10f;

        // Verifica se a posição é válida no NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(retreatPosition, out hit, 10f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // Se não for válida, tenta encontrar um ponto de patrulha distante
            if (patrolPoints.Count > 0)
            {
                Transform furthestPoint = null;
                float maxDistance = 0f;

                foreach (Transform point in patrolPoints)
                {
                    float dist = Vector3.Distance(player.position, point.position);
                    if (dist > maxDistance)
                    {
                        maxDistance = dist;
                        furthestPoint = point;
                    }
                }

                if (furthestPoint != null)
                {
                    agent.SetDestination(furthestPoint.position);
                }
            }
        }
    }

    void DodgeAttack()
    {
        if (isDodging) return;

        isDodging = true;
        dodgeTimer = dodgeDuration;
        previousState = currentState;
        ChangeState(AIState.Dodging);

        // Calcula uma direção de esquiva perpendicular à direção do jogador
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float side = (Random.value > 0.5f) ? 1f : -1f; // Esquiva para esquerda ou direita aleatoriamente
        dodgeDirection = Quaternion.Euler(0, side * 90f, 0) * directionToPlayer;

        // Aplica o movimento de esquiva
        Vector3 dodgePosition = transform.position + dodgeDirection * 3f;

        // Verifica se a posição é válida no NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(dodgePosition, out hit, 3f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
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

    void AttackPlayer()
    {
        if (attackTimer > 0) return;

        isAttacking = true;
        attackTimer = attackCooldown;

        // Olha para o jogador antes de atacar
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        // Inicia a animação de ataque
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Aplica dano ao jogador após um pequeno delay para sincronizar com a animação
        StartCoroutine(ApplyDamageWithDelay(0.5f));

        // Reseta o estado após a duração da animação
        StartCoroutine(ResetAttackState());
    }

    IEnumerator ApplyDamageWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Verifica se o jogador ainda está ao alcance
        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange * 1.2f)
        {
            // Aplica dano ao jogador
            Player playerHealth = player.GetComponent<Player>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);

                // Aumenta a agressividade após um ataque bem-sucedido
                aggressiveness += 5f;
            }
        }
    }

    IEnumerator ResetAttackState()
    {
        yield return new WaitForSeconds(attackAnimationDuration);
        isAttacking = false;

        // Volta para perseguição após o ataque
        ChangeState(AIState.Chasing);
    }

    public void TakeDemage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        consecutiveHits++;

        // Aumenta o nível de ameaça do jogador quando sofremos dano
        playerThreatLevel += amount / 5f;

        // Reduz a agressividade quando sofre muito dano
        if (amount > 20f)
        {
            aggressiveness -= 10f;
        }

        if (animator != null) animator.SetTrigger("Damage"); // animação de levar dano

        editBarHealth(currentHealth, maxHealth);

        // Considera esquivar após receber dano
        if (!isDodging && Random.value < 0.4f && currentState != AIState.Attacking)
        {
            DodgeAttack();
        }

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

        // Desativa o collider
        GetComponent<Collider>().enabled = false;

        // Dispara o evento de morte
        if (OnEnemyDied != null)
        {
            OnEnemyDied();
        }

        StartCoroutine(EsperarEDestruir(mutantObject));
    }

    IEnumerator EsperarEDestruir(GameObject MutantObject)
    {
        yield return new WaitForSeconds(4f);
        Destroy(MutantObject);
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
        }

        // Reativa o inimigo
        GetComponent<Collider>().enabled = true;

        currentHealth = maxHealth;
        isDead = false;
        agent.isStopped = false;
        consecutiveHits = 0;
        playerThreatLevel = 0f;
        aggressiveness = 50f;
        ChangeState(AIState.Patrolling); // Volta a patrulhar após reaparecer
    }

    // Gizmos para visualização no Editor do Unity
    void OnDrawGizmosSelected()
    {
        // Campo de Visão
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        // Campo de Audição
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

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

        // Raio de Flanqueamento
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Laranja semi-transparente
        Gizmos.DrawWireSphere(transform.position, flankDistance);

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

    public void editBarHealth(float vidaAtual, float vidaMaxima)
    {
        if (barHealth != null)
        {
            barHealth.fillAmount = (float)vidaAtual / vidaMaxima;
        }
    }
}
