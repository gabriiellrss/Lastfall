// Este script controla o comportamento da Inteligência Artificial do Inimigo.
// MODIFICADO: Inimigo para de seguir e volta a patrulhar quando o Player morre.

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
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                playerScript = playerObject.GetComponent<Player>();
                if (playerScript == null)
                {
                    Debug.LogWarning("Script Player não encontrado no objeto do jogador!");
                }
            }
            else Debug.LogError("Jogador não encontrado! Atribua o Transform do jogador ou marque-o com a tag 'Player'.");
        }
        else
        {
            playerScript = player.GetComponent<Player>();
            if (playerScript == null)
            {
                Debug.LogWarning("Script Player não encontrado no objeto do jogador atribuído!");
            }
        }

        currentHealth = maxHealth;
        agent.speed = patrolSpeed;
        GoToNextPatrolPoint();
    }

    void Update()
    {
        // Não faz nada se o inimigo estiver morto ou sem referência do jogador
        if (isDead || player == null || playerScript == null) return;

        // Atualiza informações do jogador (apenas se ele estiver vivo)
        if (!playerScript.isDead)
        {
            UpdatePlayerInfo();
        }

        // Lógica de transição de estados baseada na detecção e distância
        bool isPlayerAlive = !playerScript.isDead;
        bool canSeePlayer = isPlayerAlive && CanSeePlayer(); // Só pode ver se estiver vivo
        bool canHearPlayer = isPlayerAlive && CanHearPlayer(); // Só pode ouvir se estiver vivo
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
                // Volta ao estado anterior (se não era Dead)
                if (previousState != AIState.Dead)
                {
                    // Se o player morreu enquanto esquivava, vai para Patrolling
                    ChangeState(isPlayerAlive ? previousState : AIState.Patrolling);
                }
            }
            return; // Não faz mais nada enquanto estiver esquivando
        }

        // --- LÓGICA DE TRANSIÇÃO DE ESTADOS MODIFICADA ---
        switch (currentState)
        {
            case AIState.Patrolling:
                Patrol();
                // Só persegue se detectar o jogador E ele estiver VIVO
                if ((canSeePlayer || canHearPlayer) && isPlayerAlive)
                {
                    ChangeState(AIState.Chasing);
                }
                break;

            case AIState.Chasing:
                // Se o jogador MORREU, volta a patrulhar
                if (!isPlayerAlive)
                {
                    ChangeState(AIState.Patrolling);
                    break;
                }

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
                else if (!CanSeePlayer() && distanceToPlayer > chaseDistance) // Re-check CanSeePlayer as it depends on isPlayerAlive
                {
                    lastKnownPlayerPosition = player.position;
                    ChangeState(AIState.Searching);
                }
                break;

            case AIState.Searching:
                // Se o jogador MORREU enquanto procurava, volta a patrulhar
                if (!isPlayerAlive)
                {
                    ChangeState(AIState.Patrolling);
                    break;
                }

                SearchForPlayer();
                // Se encontrar o jogador (e ele estiver vivo), persegue
                if ((canSeePlayer || canHearPlayer) && isPlayerAlive)
                {
                    ChangeState(AIState.Chasing);
                }
                break;

            case AIState.Attacking:
                // Se o jogador MORREU durante o ataque, para tudo e volta a patrulhar
                if (!isPlayerAlive)
                {
                    StopCoroutine(nameof(ApplyDamageWithDelay));
                    StopCoroutine(nameof(ResetAttackState));
                    isAttacking = false;
                    if (animator != null) animator.SetBool("isAttacking", false);
                    // Pode ser necessário resetar o trigger de ataque também
                    if (animator != null) animator.ResetTrigger("Attack");
                    ChangeState(AIState.Patrolling);
                    break;
                }

                // Se não está no meio de uma animação de ataque, inicia uma
                if (!isAttacking && attackTimer <= 0)
                {
                    AttackPlayer();
                }
                // Se o ataque terminou (isAttacking é resetado em ResetAttackState) e ainda vê o player, volta a perseguir
                else if (!isAttacking && isPlayerAlive)
                {
                    ChangeState(AIState.Chasing);
                }
                break;

            case AIState.Flanking:
                // Se o jogador MORREU, volta a patrulhar
                if (!isPlayerAlive)
                {
                    ChangeState(AIState.Patrolling);
                    break;
                }

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
                else if (!CanSeePlayer()) // Re-check CanSeePlayer
                {
                    ChangeState(AIState.Chasing);
                }
                break;

            case AIState.Retreating:
                // Se o jogador MORREU, volta a patrulhar
                if (!isPlayerAlive)
                {
                    ChangeState(AIState.Patrolling);
                    break;
                }

                Retreat();
                // Se recuperou vida suficiente ou o jogador está muito perto
                if (currentHealth > maxHealth * 0.5f || distanceToPlayer <= attackRange)
                {
                    ChangeState(AIState.Chasing);
                }
                // Se perder o jogador de vista enquanto recua, pode voltar a patrulhar ou procurar
                else if (!CanSeePlayer() && distanceToPlayer > chaseDistance * 1.5f)
                {
                    ChangeState(AIState.Patrolling);
                }
                break;
        }
        // ----------------------------------------------------

        // Atualiza a lista de posições recentes do jogador (mesmo se morto, para histórico)
        UpdateRecentPlayerPositions();
    }

    void UpdatePlayerInfo()
    {
        // Esta função agora é chamada apenas se o player está vivo no Update()
        // Verifica se o jogador está atacando
        bool isPlayerAttacking = false;
        // Acessa o Animator do Player diretamente (se possível e seguro)
        Animator playerAnimator = playerScript.GetComponent<Animator>();
        if (playerAnimator != null && (playerAnimator.GetBool("isAttacking") || playerAnimator.GetBool("isShooting")))
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
            playerThreatLevel += 10f;
        }
        playerWasAttacking = isPlayerAttacking;

        // Verifica se o jogador está com arma equipada
        playerHasWeapon = false;
        if (playerAnimator != null && playerAnimator.GetBool("isShoot"))
        {
            playerHasWeapon = true;
            playerThreatLevel += 5f;
        }

        // Verifica a saúde do jogador
        float currentPlayerHealth = playerScript.currentHealth;
        float playerHealthPercentage = (float)currentPlayerHealth / playerScript.maxHealth * 100f;

        if (playerHealthPercentage < lastPlayerHealthPercentage)
        {
            aggressiveness += 5f;
        }
        lastPlayerHealthPercentage = playerHealthPercentage;

        playerThreatLevel = Mathf.Clamp(playerThreatLevel, 0f, 100f);
        aggressiveness = Mathf.Clamp(aggressiveness, 0f, 100f);
        playerThreatLevel -= Time.deltaTime * 2f;
        if (playerThreatLevel < 0f) playerThreatLevel = 0f;
    }

    void UpdateRecentPlayerPositions()
    {
        positionUpdateTimer += Time.deltaTime;
        if (positionUpdateTimer >= positionUpdateInterval)
        {
            positionUpdateTimer = 0f;
            if (player != null) // Adiciona verificação de null para player
            {
                recentPlayerPositions.Add(player.position);
                if (recentPlayerPositions.Count > 5)
                {
                    recentPlayerPositions.RemoveAt(0);
                }
            }
        }
    }

    Vector3 PredictPlayerPosition(float timeAhead)
    {
        if (player == null || recentPlayerPositions.Count < 2)
            return player != null ? player.position : transform.position; // Retorna posição atual se não puder prever

        Vector3 averageVelocity = Vector3.zero;
        for (int i = 1; i < recentPlayerPositions.Count; i++)
        {
            averageVelocity += (recentPlayerPositions[i] - recentPlayerPositions[i - 1]) / positionUpdateInterval;
        }
        averageVelocity /= (recentPlayerPositions.Count - 1);

        return player.position + averageVelocity * timeAhead;
    }

    bool ShouldDodge()
    {
        // Só esquiva se o jogador estiver vivo e atacando
        if (playerScript != null && !playerScript.isDead && playerWasAttacking && distanceToPlayer < attackRange * 1.5f && timeSinceLastPlayerAttack < 0.5f)
        {
            return Random.value < 0.7f;
        }
        return false;
    }

    bool ShouldFlank()
    {
        // Só flanqueia se o jogador estiver vivo
        if (playerScript != null && !playerScript.isDead &&
            (lastPlayerHealthPercentage < 30f || currentHealth > maxHealth * 0.7f) &&
            distanceToPlayer < flankDistance && distanceToPlayer > attackRange)
        {
            return Random.value < 0.6f;
        }
        return false;
    }

    bool ShouldRetreat()
    {
        // Só recua se o jogador estiver vivo
        if (playerScript != null && !playerScript.isDead &&
            currentHealth < maxHealth * 0.3f && lastPlayerHealthPercentage > 50f)
        {
            return Random.value < 0.7f;
        }
        return false;
    }

    void ChangeState(AIState newState)
    {
        if (currentState == newState && currentState != AIState.Patrolling) return; // Permite re-entrar em Patrolling

        // Guarda o estado anterior apenas se não for Dead
        if (currentState != AIState.Dead)
        {
            previousState = currentState;
        }
        currentState = newState;

        // Reset de parâmetros de animação
        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            // Não reseta isAttacking aqui, pois é controlado no estado Attacking
            // animator.SetBool("isAttacking", false);
        }

        // Configurações específicas para cada estado
        switch (currentState)
        {
            case AIState.Patrolling:
                agent.speed = patrolSpeed;
                agent.isStopped = false; // Garante que o agente pode se mover
                if (animator != null) animator.SetBool("isWalking", true);
                // Se estava esperando em um ponto, cancela e vai para o próximo
                if (waitingAtPatrolPoint)
                {
                    StopCoroutine(nameof(WaitAtPatrolPoint));
                    waitingAtPatrolPoint = false;
                }
                GoToNextPatrolPoint();
                break;

            case AIState.Chasing:
                agent.speed = chaseSpeed;
                agent.isStopped = false;
                if (animator != null) animator.SetBool("isWalking", true);
                break;

            case AIState.Searching:
                agent.speed = patrolSpeed;
                agent.isStopped = false;
                currentSearchTime = 0f;
                // Vai para a última posição conhecida apenas se o jogador estava vivo recentemente
                if (player != null) // Verifica se player ainda existe
                {
                    agent.SetDestination(lastKnownPlayerPosition);
                }
                else
                {
                    ChangeState(AIState.Patrolling); // Se player sumiu, patrulha
                }
                if (animator != null) animator.SetBool("isWalking", true);
                break;

            case AIState.Attacking:
                agent.isStopped = true; // Para de andar para atacar
                agent.velocity = Vector3.zero; // Zera a velocidade imediatamente
                // Animação é controlada dentro do estado Attacking
                break;

            case AIState.Flanking:
                agent.speed = chaseSpeed * 1.2f;
                agent.isStopped = false;
                if (animator != null) animator.SetBool("isWalking", true);
                break;

            case AIState.Retreating:
                agent.speed = chaseSpeed * 1.1f;
                agent.isStopped = false;
                if (animator != null) animator.SetBool("isWalking", true);
                break;

            case AIState.Dodging:
                agent.speed = dodgeSpeed;
                agent.isStopped = false;
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

        // Se chegou ao destino e não está esperando, começa a esperar
        if (!agent.pathPending && agent.remainingDistance < 0.5f && !waitingAtPatrolPoint)
        {
            waitingAtPatrolPoint = true;
            agent.isStopped = true; // Para enquanto espera
            agent.velocity = Vector3.zero;
            if (animator != null) animator.SetBool("isWalking", false); // Para animação de andar
            StartCoroutine(WaitAtPatrolPoint());
        }
        // Garante que está andando se não chegou ou não está esperando
        else if (!waitingAtPatrolPoint && agent.hasPath)
        {
            if (animator != null) animator.SetBool("isWalking", true);
        }
    }

    IEnumerator WaitAtPatrolPoint()
    {
        yield return new WaitForSeconds(patrolWaitTime);
        waitingAtPatrolPoint = false;
        agent.isStopped = false; // Libera para mover
        if (animator != null) animator.SetBool("isWalking", true); // Volta a andar
        GoToNextPatrolPoint();
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Count == 0) return;

        // Garante que o agente não está parado
        agent.isStopped = false;

        // Define o próximo ponto de patrulha
        int attempts = 0;
        int maxAttempts = patrolPoints.Count;
        do
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
            attempts++;
        } while (patrolPoints[currentPatrolIndex] == null && attempts < maxAttempts);

        if (patrolPoints[currentPatrolIndex] != null)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            if (animator != null) animator.SetBool("isWalking", true);
        }
        else
        {
            Debug.LogWarning("Todos os pontos de patrulha são nulos ou inválidos.");
            // Fica parado ou volta para um estado padrão?
            // Por enquanto, apenas loga o aviso.
        }
    }

    bool CanSeePlayer()
    {
        // A checagem de isPlayerAlive já é feita no Update antes de chamar esta função
        if (player == null) return false;

        float currentDistance = Vector3.Distance(transform.position, player.position);
        if (currentDistance <= visionRadius)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            if (angleToPlayer <= visionAngle / 2)
            {
                // Define pontos de origem (olhos do inimigo) e alvo (centro do jogador)
                Vector3 eyePosition = transform.position + Vector3.up * 1.6f; // Ajuste a altura conforme necessário
                Vector3 targetCenter = player.position + Vector3.up * 1.0f; // Mira no centro do corpo do jogador

                // Calcula a nova direção e distância a partir dos olhos para o centro do jogador
                Vector3 directionToTargetCenter = (targetCenter - eyePosition).normalized;
                float distanceToTargetCenter = Vector3.Distance(eyePosition, targetCenter);

                // Verifica se há obstáculos entre os olhos e o centro do jogador
                // Nota: Usamos distanceToTargetCenter como distância máxima do raycast
                RaycastHit hit;
                if (!Physics.Raycast(eyePosition, directionToTargetCenter, out hit, distanceToTargetCenter, obstacleLayer))
                {
                    // Se não atingiu nenhum obstáculo na layer de obstáculos, considera visível
                    return true;
                }
                // Opcional: Adicionar uma verificação se o hit.collider é o próprio jogador,
                // caso a layer do jogador não esteja na obstacleLayer.
                // else if (hit.collider != null && hit.collider.transform == player) {
                //     return true; // Atingiu o jogador diretamente
                // }
            }
        }
        return false;
    }

    bool CanHearPlayer()
    {
        // A checagem de isPlayerAlive já é feita no Update antes de chamar esta função
        if (player == null) return false;

        float currentDistance = Vector3.Distance(transform.position, player.position);

        if (currentDistance <= hearingRadius)
        {
            // Considera ouvir se o jogador estava atacando recentemente
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
            Vector3 targetPosition = PredictPlayerPosition(0.5f);
            agent.SetDestination(targetPosition);
        }
    }

    void FlankPlayer()
    {
        if (player == null) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float side = (Vector3.Dot(transform.right, directionToPlayer) > 0) ? -1 : 1;
        Vector3 flankDirection = Quaternion.Euler(0, side * flankAngle, 0) * directionToPlayer;
        Vector3 flankPosition = player.position + flankDirection * flankDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(flankPosition, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            ChasePlayer();
        }
    }

    void Retreat()
    {
        if (player == null) return;

        Vector3 retreatDirection = (transform.position - player.position).normalized;
        Vector3 retreatPosition = transform.position + retreatDirection * 10f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(retreatPosition, out hit, 10f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            if (patrolPoints.Count > 0)
            {
                Transform furthestPoint = null;
                float maxDistance = 0f;
                foreach (Transform point in patrolPoints)
                {
                    if (point == null) continue;
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
        // Guarda o estado atual ANTES de mudar para Dodging
        // previousState = currentState; // Já é feito no Update
        ChangeState(AIState.Dodging);

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float side = (Random.value > 0.5f) ? 1f : -1f;
        dodgeDirection = Quaternion.Euler(0, side * 90f, 0) * directionToPlayer;
        Vector3 dodgePosition = transform.position + dodgeDirection * 3f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(dodgePosition, out hit, 3f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        // Se não encontrar posição válida, cancela a esquiva
        else
        {
            isDodging = false;
            ChangeState(previousState); // Volta ao estado anterior
        }
    }

    void SearchForPlayer()
    {
        currentSearchTime += Time.deltaTime;
        // Se o tempo de busca acabou OU chegou ao destino da busca
        if (currentSearchTime >= searchTime || (!agent.pathPending && agent.remainingDistance < 0.5f))
        {
            ChangeState(AIState.Patrolling);
        }
    }

    void AttackPlayer()
    {
        // attackTimer já foi verificado no Update
        isAttacking = true;
        attackTimer = attackCooldown;

        // Olha para o jogador
        if (player != null)
        {
            transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        }

        if (animator != null)
        {
            animator.SetBool("isWalking", false); // Garante que não está andando
            animator.SetBool("isAttacking", true);
            animator.SetTrigger("Attack");
        }

        StartCoroutine(ApplyDamageWithDelay(0.5f));
        StartCoroutine(ResetAttackState());
    }

    IEnumerator ApplyDamageWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Verifica se ainda está atacando e se o player está vivo e ao alcance
        if (isAttacking && player != null && playerScript != null && !playerScript.isDead &&
            Vector3.Distance(transform.position, player.position) <= attackRange * 1.2f)
        {
            playerScript.TakeDamage(attackDamage);
            aggressiveness += 5f;
        }
    }

    IEnumerator ResetAttackState()
    {
        yield return new WaitForSeconds(attackAnimationDuration);
        isAttacking = false;
        if (animator != null) animator.SetBool("isAttacking", false);

        // Decide o próximo estado APENAS se não foi interrompido (ex: player morreu)
        // A transição agora é feita no Update, verificando isAttacking e isPlayerAlive
        // ChangeState(AIState.Chasing); // Removido daqui
    }

    public void TakeDemage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        consecutiveHits++;
        playerThreatLevel += amount / 5f;
        if (amount > 20f) aggressiveness -= 10f;

        if (animator != null) animator.SetTrigger("Damage");
        editBarHealth(currentHealth, maxHealth);

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
        agent.isStopped = true;
        GetComponent<Collider>().enabled = false;
        if (OnEnemyDied != null) OnEnemyDied();

        // Inicia respawn OU destruição
        if (respawnPoints != null && respawnPoints.Count > 0)
        {
            StartCoroutine(Respawn());
        }
        else if (mutantObject != null)
        {
            StartCoroutine(EsperarEDestruir(mutantObject));
        }
        else
        {
            // Se não tem respawn nem objeto para destruir, apenas desativa
            gameObject.SetActive(false);
            // Ou Destroy(gameObject, 4f); // Destroi após um tempo
        }
    }

    // Coroutine para destruir o objeto (se não houver respawn)
    IEnumerator EsperarEDestruir(GameObject objToDestroy)
    {
        yield return new WaitForSeconds(4f);
        Destroy(objToDestroy);
    }

    // Coroutine para renascer (se houver pontos de respawn)
    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnDelay);

        int respawnIndex = Random.Range(0, respawnPoints.Count);
        // Garante que o ponto de respawn escolhido não seja nulo
        int attempts = 0;
        while (respawnPoints[respawnIndex] == null && attempts < respawnPoints.Count)
        {
            respawnIndex = (respawnIndex + 1) % respawnPoints.Count;
            attempts++;
        }

        if (respawnPoints[respawnIndex] != null)
        {
            transform.position = respawnPoints[respawnIndex].position;
            agent.Warp(respawnPoints[respawnIndex].position);
        }
        else
        {
            Debug.LogWarning("Nenhum ponto de respawn válido encontrado para " + gameObject.name + ". Reaparecendo na posição de morte.");
            // Tenta reaparecer onde morreu, mas pode ser problemático se morreu em local inválido
        }

        GetComponent<Collider>().enabled = true;
        currentHealth = maxHealth;
        editBarHealth(currentHealth, maxHealth);
        isDead = false;
        agent.isStopped = false;
        consecutiveHits = 0;
        playerThreatLevel = 0f;
        aggressiveness = 50f;
        ChangeState(AIState.Patrolling);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        Vector3 fovLine1 = Quaternion.AngleAxis(visionAngle / 2, transform.up) * transform.forward * visionRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(-visionAngle / 2, transform.up) * transform.forward * visionRadius;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);

        if (player != null)
        {
            // Gizmo de linha só desenha se o player estiver vivo
            if (playerScript != null && !playerScript.isDead)
            {
                Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
                Gizmos.DrawLine(transform.position, player.position);
            }
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, flankDistance);

        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                    int nextIndex = (i + 1) % patrolPoints.Count;
                    if (patrolPoints[nextIndex] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
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