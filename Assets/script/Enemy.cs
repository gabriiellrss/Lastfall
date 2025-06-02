// Este script controla o comportamento da Inteligência Artificial do Inimigo.
// MODIFICADO: Inimigo para de seguir e volta a patrulhar quando o Player morre.
// MELHORIAS ADICIONAIS (Manus): Campo de detecção aprimorado (audição), início imediato da patrulha confirmado, lógica de patrulha aleatória.
// ADICIONADO (Manus): Logs de depuração para problema de ataque e ajuste na condição de ataque.

using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Enemy : MonoBehaviour // Renomeado para indicar adição de logs
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
    private int currentPatrolIndex = -1; // Inicializado como -1 para garantir que o primeiro ponto seja escolhido aleatoriamente
    private bool waitingAtPatrolPoint = false;
    public bool randomPatrol = true; // Adicionado: Opção para patrulha aleatória

    [Header("Configurações de Campo de Visão e Detecção")] // Renomeado Header para clareza
    public float visionRadius = 10f; // Raio de detecção visual do jogador
    [Range(0, 360)]
    public float visionAngle = 90f; // Ângulo do campo de visão (não alterado conforme solicitado)
    public float hearingRadius = 15f; // Raio MÁXIMO para ouvir o jogador (ataques, tiros, etc.)
    public float closeHearingRadius = 5f; // Raio para ouvir sons mais sutis (passos, etc. - requer implementação no Player) - Adicionado para aprimoramento

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
    public AIState currentState = AIState.Patrolling; // Estado inicial definido como Patrolling
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

    // Adicionado: Referência para sons do jogador (opcional, para audição aprimorada)
    // public PlayerAudio playerAudio; // Descomente e atribua se tiver um script de áudio no Player

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
                // playerAudio = playerObject.GetComponent<PlayerAudio>(); // Descomente se usar PlayerAudio
                if (playerScript == null) Debug.LogWarning("Script Player não encontrado no objeto do jogador!");
                // if (playerAudio == null) Debug.LogWarning("Script PlayerAudio não encontrado no objeto do jogador!"); // Descomente se usar PlayerAudio
            }
            else Debug.LogError("Jogador não encontrado! Atribua o Transform do jogador ou marque-o com a tag 'Player'.");
        }
        else
        {
            playerScript = player.GetComponent<Player>();
            // playerAudio = player.GetComponent<PlayerAudio>(); // Descomente se usar PlayerAudio
            if (playerScript == null) Debug.LogWarning("Script Player não encontrado no objeto do jogador atribuído!");
            // if (playerAudio == null) Debug.LogWarning("Script PlayerAudio não encontrado no objeto do jogador atribuído!"); // Descomente se usar PlayerAudio
        }

        currentHealth = maxHealth;
        agent.speed = patrolSpeed;
        currentState = AIState.Patrolling; // Garante que o estado inicial é Patrolling
        GoToNextPatrolPoint(); // Inicia a patrulha imediatamente
        Debug.Log(gameObject.name + " iniciando patrulha."); // Log para confirmar início
    }

    void Update()
    {
        if (isDead || player == null || playerScript == null) return;

        bool isPlayerAlive = !playerScript.isDead;

        if (isPlayerAlive)
        {
            UpdatePlayerInfo();
            distanceToPlayer = Vector3.Distance(transform.position, player.position); // Atualiza distância aqui
        }
        else
        {
            distanceToPlayer = float.MaxValue; // Define distância como infinita se player morto
        }

        // A detecção só ocorre se o jogador estiver vivo
        bool canSeePlayer = isPlayerAlive && CanSeePlayer();
        bool canHearPlayer = isPlayerAlive && CanHearPlayer();

        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        if (isDodging)
        {
            dodgeTimer -= Time.deltaTime;
            if (dodgeTimer <= 0)
            {
                isDodging = false;
                if (previousState != AIState.Dead)
                {
                    ChangeState(isPlayerAlive ? previousState : AIState.Patrolling);
                }
            }
            return;
        }

        // --- LÓGICA DE TRANSIÇÃO DE ESTADOS ---
        switch (currentState)
        {
            case AIState.Patrolling:
                Patrol();
                if ((canSeePlayer || canHearPlayer) && isPlayerAlive) // Verifica isPlayerAlive explicitamente
                {
                    ChangeState(AIState.Chasing);
                }
                break;

            case AIState.Chasing:
                if (!isPlayerAlive) { ChangeState(AIState.Patrolling); break; }
                ChasePlayer();

                // Debug para verificar condições de ataque
                Debug.Log($"Chasing - Dist: {distanceToPlayer}, AttackRange: {attackRange}, Timer: {attackTimer}, CanAttack: {distanceToPlayer <= attackRange * 1.1f && attackTimer <= 0}"); // LOG ADICIONADO E CONDIÇÃO AJUSTADA

                if (ShouldDodge()) { DodgeAttack(); }
                // AJUSTE ALTERNATIVO: Usar attackRange * 1.1f para entrar em ataque um pouco antes
                else if (distanceToPlayer <= attackRange * 1.1f && attackTimer <= 0) { ChangeState(AIState.Attacking); } // CONDIÇÃO AJUSTADA
                else if (ShouldFlank()) { ChangeState(AIState.Flanking); }
                else if (ShouldRetreat()) { ChangeState(AIState.Retreating); }
                else if (!canSeePlayer && distanceToPlayer > chaseDistance) // Usa canSeePlayer já calculado
                {
                    if (player != null) lastKnownPlayerPosition = player.position; // Guarda a última posição válida
                    ChangeState(AIState.Searching);
                }
                break;

            case AIState.Searching:
                if (!isPlayerAlive) { ChangeState(AIState.Patrolling); break; }
                SearchForPlayer();
                if ((canSeePlayer || canHearPlayer) && isPlayerAlive) // Verifica isPlayerAlive explicitamente
                {
                    ChangeState(AIState.Chasing);
                }
                break;

            case AIState.Attacking:
                if (!isPlayerAlive)
                {
                    // Interrompe ataque se jogador morrer
                    StopCoroutine(nameof(ApplyDamageWithDelay));
                    StopCoroutine(nameof(ResetAttackState));
                    isAttacking = false;
                    if (animator != null) animator.SetBool("isAttacking", false);
                    if (animator != null) animator.ResetTrigger("Attack");
                    ChangeState(AIState.Patrolling);
                    break;
                }
                if (!isAttacking && attackTimer <= 0) { AttackPlayer(); }
                else if (!isAttacking && isPlayerAlive) // Se terminou ataque e player vivo, volta a perseguir
                {
                    // Verifica novamente se pode ver ou ouvir antes de voltar a perseguir
                    if (CanSeePlayer() || CanHearPlayer())
                    {
                        ChangeState(AIState.Chasing);
                    }
                    else
                    {
                        if (player != null) lastKnownPlayerPosition = player.position; // Guarda a última posição válida
                        ChangeState(AIState.Searching); // Se perdeu o jogador, procura
                    }
                }
                break;

            case AIState.Flanking:
                if (!isPlayerAlive) { ChangeState(AIState.Patrolling); break; }
                FlankPlayer();
                if (ShouldDodge()) { DodgeAttack(); }
                else if (distanceToPlayer <= attackRange && attackTimer <= 0) { ChangeState(AIState.Attacking); }
                else if (!canSeePlayer) // Usa canSeePlayer já calculado
                {
                    // Se perdeu de vista durante o flanco, volta a perseguir (pode ter se escondido)
                    ChangeState(AIState.Chasing);
                }
                break;

            case AIState.Retreating:
                if (!isPlayerAlive) { ChangeState(AIState.Patrolling); break; }
                Retreat();
                if (currentHealth > maxHealth * 0.5f || distanceToPlayer <= attackRange) { ChangeState(AIState.Chasing); }
                else if (!canSeePlayer && distanceToPlayer > chaseDistance * 1.5f) // Usa canSeePlayer já calculado
                {
                    ChangeState(AIState.Patrolling); // Se perdeu de vista e está longe, volta a patrulhar
                }
                break;

            case AIState.Dead:
                // Lógica de morte já tratada em Die() e Respawn()
                break;
        }
        // ----------------------------------------------------

        UpdateRecentPlayerPositions(); // Atualiza mesmo se morto para histórico, se necessário
    }

    void UpdatePlayerInfo()
    {
        // Esta função é chamada apenas se o player está vivo
        Animator playerAnimator = playerScript.GetComponent<Animator>(); // Pega o animator do player
        bool isPlayerAttackingNow = false;
        if (playerAnimator != null && (playerAnimator.GetBool("isAttacking") || playerAnimator.GetBool("isShooting"))) // Verifica animações de ataque/tiro
        {
            isPlayerAttackingNow = true;
            timeSinceLastPlayerAttack = 0f;
        }
        else
        {
            timeSinceLastPlayerAttack += Time.deltaTime;
        }

        if (isPlayerAttackingNow && !playerWasAttacking) playerThreatLevel += 10f;
        playerWasAttacking = isPlayerAttackingNow;

        playerHasWeapon = (playerAnimator != null && playerAnimator.GetBool("isShoot")); // Verifica se está na animação de tiro
        if (playerHasWeapon) playerThreatLevel += 5f;

        float currentPlayerHealth = playerScript.currentHealth;
        float playerHealthPercentage = (float)currentPlayerHealth / playerScript.maxHealth * 100f;
        if (playerHealthPercentage < lastPlayerHealthPercentage) aggressiveness += 5f;
        lastPlayerHealthPercentage = playerHealthPercentage;

        playerThreatLevel = Mathf.Clamp(playerThreatLevel - Time.deltaTime * 2f, 0f, 100f); // Ameaça decai com o tempo
        aggressiveness = Mathf.Clamp(aggressiveness, 0f, 100f);
    }

    void UpdateRecentPlayerPositions()
    {
        positionUpdateTimer += Time.deltaTime;
        if (positionUpdateTimer >= positionUpdateInterval)
        {
            positionUpdateTimer = 0f;
            if (player != null && !playerScript.isDead) // Só adiciona se player existe e está vivo
            {
                recentPlayerPositions.Add(player.position);
                if (recentPlayerPositions.Count > 5) recentPlayerPositions.RemoveAt(0);
            }
            else if (recentPlayerPositions.Count > 0)
            {
                // Limpa posições se o jogador morrer ou sumir
                recentPlayerPositions.Clear();
            }
        }
    }

    Vector3 PredictPlayerPosition(float timeAhead)
    {
        if (player == null || playerScript.isDead || recentPlayerPositions.Count < 2)
            return player != null ? player.position : transform.position; // Retorna posição atual se não puder prever

        Vector3 averageVelocity = Vector3.zero;
        for (int i = 1; i < recentPlayerPositions.Count; i++)
        {
            averageVelocity += (recentPlayerPositions[i] - recentPlayerPositions[i - 1]) / positionUpdateInterval;
        }
        averageVelocity /= (recentPlayerPositions.Count - 1);

        // Verifica se a posição prevista é válida no NavMesh
        Vector3 predictedPosition = player.position + averageVelocity * timeAhead;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(predictedPosition, out hit, 2.0f, NavMesh.AllAreas))
        {
            return hit.position; // Retorna a posição válida mais próxima no NavMesh
        }

        return player.position; // Retorna a posição atual se a previsão for inválida
    }

    bool ShouldDodge()
    {
        if (playerScript != null && !playerScript.isDead && playerWasAttacking && distanceToPlayer < attackRange * 1.5f && timeSinceLastPlayerAttack < 0.5f)
        {
            return Random.value < (aggressiveness < 30 ? 0.8f : 0.6f); // Mais propenso a esquivar se menos agressivo
        }
        return false;
    }

    bool ShouldFlank()
    {
        if (playerScript != null && !playerScript.isDead &&
            (lastPlayerHealthPercentage < 40f || currentHealth > maxHealth * 0.6f) && // Ajustado limiares
            distanceToPlayer < flankDistance && distanceToPlayer > attackRange &&
            Vector3.Angle(transform.forward, player.position - transform.position) < 120f) // Só flanqueia se o jogador estiver mais ou menos na frente
        {
            return Random.value < (aggressiveness > 70 ? 0.7f : 0.5f); // Mais propenso a flanquear se agressivo
        }
        return false;
    }

    bool ShouldRetreat()
    {
        if (playerScript != null && !playerScript.isDead &&
            currentHealth < maxHealth * 0.25f && lastPlayerHealthPercentage > 40f && // Ajustado limiares
            playerThreatLevel > 50f) // Só recua se a ameaça for alta
        {
            return Random.value < 0.7f;
        }
        return false;
    }

    void ChangeState(AIState newState)
    {
        // Verifica se o estado atual é o mesmo que o novo estado e se não é Patrolling
        // Permite reentrar no estado Patrolling para reiniciar a lógica de ir para o próximo ponto
        if (currentState == newState && currentState != AIState.Patrolling) return;

        // Log de mudança de estado para debug (descomente se necessário)
        // Debug.Log(gameObject.name + " mudando de " + currentState + " para " + newState);

        // Guarda o estado anterior apenas se não for Dead
        if (currentState != AIState.Dead) previousState = currentState;
        currentState = newState;

        // Reset comum de animações e estado do agente
        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            // Não reseta isAttacking aqui, é controlado no estado Attacking
        }
        if (agent.isOnNavMesh) agent.isStopped = false; // Garante que o agente pode se mover por padrão, se estiver no NavMesh

        // Configurações específicas ao entrar no novo estado
        switch (currentState)
        {
            case AIState.Patrolling:
                agent.speed = patrolSpeed;
                if (waitingAtPatrolPoint) // Se estava esperando, cancela a espera
                {
                    StopCoroutine(nameof(WaitAtPatrolPoint));
                    waitingAtPatrolPoint = false;
                }
                GoToNextPatrolPoint(); // Inicia movimento para o próximo ponto
                // A animação de caminhada é ativada em GoToNextPatrolPoint se um destino válido for definido
                break;

            case AIState.Chasing:
                agent.speed = chaseSpeed;
                if (animator != null) animator.SetBool("isWalking", true);
                break;

            case AIState.Searching:
                agent.speed = patrolSpeed; // Procura com velocidade de patrulha
                currentSearchTime = 0f;
                if (player != null) // Verifica se player ainda existe para ter uma lastKnownPlayerPosition válida
                {
                    // Tenta ir para a última posição conhecida válida no NavMesh
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(lastKnownPlayerPosition, out hit, 5.0f, NavMesh.AllAreas))
                    {
                        if (agent.isOnNavMesh) agent.SetDestination(hit.position);
                        if (animator != null) animator.SetBool("isWalking", true);
                    }
                    else
                    {
                        // Se a última posição for inválida, volta a patrulhar
                        ChangeState(AIState.Patrolling);
                    }
                }
                else { ChangeState(AIState.Patrolling); } // Se player sumiu, patrulha
                break;

            case AIState.Attacking:
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true; // Para de andar para atacar
                    agent.velocity = Vector3.zero; // Zera a velocidade imediatamente
                }
                // Animação controlada em AttackPlayer()
                break;

            case AIState.Flanking:
                agent.speed = chaseSpeed * 1.1f; // Levemente mais rápido para flanquear
                if (animator != null) animator.SetBool("isWalking", true);
                break;

            case AIState.Retreating:
                agent.speed = chaseSpeed * 0.9f; // Levemente mais lento ao recuar (cautela)
                if (animator != null) animator.SetBool("isWalking", true);
                break;

            case AIState.Dodging:
                agent.speed = dodgeSpeed;
                // Animação de esquiva (se houver)
                if (animator != null) animator.SetTrigger("Dodge"); // Assumindo um trigger "Dodge"
                break;

            case AIState.Dead:
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
                if (animator != null) animator.SetTrigger("Dead"); // Assumindo um trigger "Dead"
                break;
        }
    }

    void Patrol()
    {
        if (patrolPoints.Count == 0 || !agent.isOnNavMesh || agent.pathPending) return; // Não faz nada se não há pontos, não está no NavMesh ou está calculando caminho

        // Se chegou ao destino E não está esperando, começa a esperar
        if (!agent.pathPending && agent.remainingDistance < 0.5f && !waitingAtPatrolPoint)
        {
            waitingAtPatrolPoint = true;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            if (animator != null) animator.SetBool("isWalking", false);
            StartCoroutine(WaitAtPatrolPoint());
        }
        // Garante que está andando se não chegou ou não está esperando e tem um caminho
        else if (!waitingAtPatrolPoint && agent.hasPath && !agent.isStopped)
        {
            if (animator != null) animator.SetBool("isWalking", true);
        }
    }

    IEnumerator WaitAtPatrolPoint()
    {
        yield return new WaitForSeconds(patrolWaitTime);
        waitingAtPatrolPoint = false;
        if (agent.isOnNavMesh) agent.isStopped = false; // Libera para mover apenas se ainda estiver no NavMesh
        GoToNextPatrolPoint(); // Vai para o próximo ponto após esperar
    }

    // --- LÓGICA DE PATRULHA MELHORADA --- (Patrulha Aleatória ou Sequencial)
    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Count == 0)
        {
            Debug.LogWarning(gameObject.name + ": Sem pontos de patrulha definidos.");
            if (agent.isOnNavMesh) agent.isStopped = true;
            if (animator != null) animator.SetBool("isWalking", false);
            return;
        }

        // Remove pontos nulos da lista para evitar problemas (melhor fazer isso no Start, mas ok aqui por segurança)
        patrolPoints.RemoveAll(item => item == null);
        if (patrolPoints.Count == 0)
        { // Verifica novamente após remover nulos
            Debug.LogError(gameObject.name + ": Todos os pontos de patrulha eram nulos ou foram removidos.");
            if (agent.isOnNavMesh) agent.isStopped = true;
            if (animator != null) animator.SetBool("isWalking", false);
            return;
        }

        if (!agent.isOnNavMesh) return; // Sai se não estiver no NavMesh

        agent.isStopped = false; // Garante que pode se mover

        int nextPatrolIndex = currentPatrolIndex;

        if (randomPatrol && patrolPoints.Count > 1)
        {
            // Escolhe um índice aleatório DIFERENTE do atual
            int attempts = 0;
            do
            {
                nextPatrolIndex = Random.Range(0, patrolPoints.Count);
                attempts++;
            } while (nextPatrolIndex == currentPatrolIndex && attempts < patrolPoints.Count * 2); // Evita loop infinito se algo der errado
        }
        else // Patrulha sequencial (ou se só tem 1 ponto)
        {
            nextPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
        }

        currentPatrolIndex = nextPatrolIndex;

        // Define o destino
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        if (animator != null) animator.SetBool("isWalking", true); // Anima o início do movimento
        // Debug.Log(gameObject.name + " indo para ponto de patrulha: " + patrolPoints[currentPatrolIndex].name);

    }
    // ------------------------------------

    // --- DETECÇÃO VISUAL (CanSeePlayer) ---
    // Mantida a lógica original, mas com comentários adicionais e verificações.
    // O ângulo de visão (visionAngle) não foi alterado.
    // O raio de visão (visionRadius) pode ser ajustado no Inspector.
    bool CanSeePlayer()
    {
        if (player == null || playerScript.isDead) return false;

        // Verifica a distância primeiro (otimização)
        // distanceToPlayer já calculado no Update() se player vivo
        if (distanceToPlayer > visionRadius) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        // Evita erro se a direção for zero (inimigo e player na mesma posição)
        if (directionToPlayer == Vector3.zero) return true; // Considera que vê se estão no mesmo ponto

        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        // Verifica se o jogador está dentro do ângulo de visão
        if (angleToPlayer > visionAngle / 2) return false;

        // Verifica se há obstáculos entre o inimigo e o jogador
        Vector3 eyePosition = transform.position + Vector3.up * 1.6f; // Altura dos olhos (ajustar conforme modelo)
        Vector3 targetCenter = player.position + Vector3.up * 1.0f; // Centro do jogador (ajustar)
        Vector3 directionToTargetCenter = (targetCenter - eyePosition).normalized;
        float distanceToTargetCenter = Vector3.Distance(eyePosition, targetCenter);

        // Evita erro se a direção for zero
        if (directionToTargetCenter == Vector3.zero) return true;

        RaycastHit hit;
        // Lança o raio APENAS até a distância do jogador, usando a layer de obstáculos.
        if (Physics.Raycast(eyePosition, directionToTargetCenter, out hit, distanceToTargetCenter, obstacleLayer))
        {
            // Atingiu um obstáculo ANTES de atingir o jogador
            // Debug.DrawRay(eyePosition, directionToTargetCenter * hit.distance, Color.red, 0.1f); // Para debug visual
            return false;
        }
        else
        {
            // Não atingiu nenhum obstáculo na layer especificada -> Vê o jogador
            // Debug.DrawRay(eyePosition, directionToTargetCenter * distanceToTargetCenter, Color.green, 0.1f); // Para debug visual
            return true;
        }
    }
    // ------------------------------------

    // --- DETECÇÃO AUDITIVA (CanHearPlayer) ---
    // Lógica aprimorada para considerar diferentes tipos de sons e distância.
    bool CanHearPlayer()
    {
        if (player == null || playerScript.isDead) return false;

        // distanceToPlayer já calculado no Update() se player vivo

        // Audição de sons altos (tiros, ataques recentes) - Raio maior
        if (distanceToPlayer <= hearingRadius)
        {
            // Considera ouvir se o jogador estava atacando recentemente ou tem arma e atacou há pouco tempo
            if (playerWasAttacking || (playerHasWeapon && timeSinceLastPlayerAttack < 1.5f))
            {
                // Quanto mais perto, maior a chance de ouvir
                float hearingProbability = Mathf.Clamp01(1.0f - (distanceToPlayer / hearingRadius)); // Probabilidade linear inversa (0 a 1)
                // Debug.Log("Tentando ouvir som alto. Dist: " + distanceToPlayer + " Prob: " + hearingProbability);
                if (Random.value < hearingProbability) return true; // Adiciona um pouco de incerteza
            }
        }

        // Audição de sons baixos (passos, etc.) - Raio menor (requer info do Player)
        if (distanceToPlayer <= closeHearingRadius)
        {
            // Exemplo: Verificar se o jogador está correndo ou fazendo barulho
            // Esta parte depende de como o estado/som do jogador é exposto.
            // Supondo que playerScript tenha uma propriedade ou método como IsRunning()
            // if (playerScript.IsRunning()) // Exemplo: Substitua por sua lógica real
            // {
            //     float closeHearingProbability = Mathf.Clamp01(1.0f - (distanceToPlayer / closeHearingRadius));
            //     // Debug.Log("Tentando ouvir som baixo (corrida). Dist: " + distanceToPlayer + " Prob: " + closeHearingProbability);
            //     if (Random.value < closeHearingProbability * 0.7f) return true; // Menos chance de ouvir sons baixos
            // }
        }

        return false; // Não ouviu nada
    }
    // ------------------------------------


    void ChasePlayer()
    {
        if (player != null && !playerScript.isDead && agent.isOnNavMesh) // Verifica se player existe, está vivo e agente está no NavMesh
        {
            Vector3 targetPosition = PredictPlayerPosition(0.3f); // Previsão um pouco menor
            agent.SetDestination(targetPosition);
        }
        else if (currentState == AIState.Chasing)
        {
            // Se estava perseguindo e o jogador morreu/sumiu ou saiu do NavMesh, volta a patrulhar
            ChangeState(AIState.Patrolling);
        }
    }

    void FlankPlayer()
    {
        if (player == null || playerScript.isDead) { ChangeState(AIState.Patrolling); return; }
        if (!agent.isOnNavMesh) { ChangeState(AIState.Patrolling); return; } // Volta a patrulhar se sair do NavMesh

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        // Escolhe lado aleatório para flanquear
        float side = (Random.value > 0.5f) ? 1f : -1f;
        Vector3 flankDirection = Quaternion.Euler(0, side * flankAngle, 0) * directionToPlayer;
        Vector3 desiredFlankPosition = player.position + flankDirection * flankDistance;

        // Encontra a posição válida mais próxima no NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(desiredFlankPosition, out hit, 3.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // Se não conseguir encontrar posição de flanco, apenas persegue
            ChasePlayer();
        }
    }

    void Retreat()
    {
        if (player == null || playerScript.isDead) { ChangeState(AIState.Patrolling); return; }
        if (!agent.isOnNavMesh) { ChangeState(AIState.Patrolling); return; } // Volta a patrulhar se sair do NavMesh

        Vector3 retreatDirection = (transform.position - player.position).normalized;
        // Se estiverem muito próximos, escolhe uma direção aleatória para evitar ficar preso
        if (retreatDirection == Vector3.zero) retreatDirection = Random.insideUnitSphere.normalized;

        Vector3 desiredRetreatPosition = transform.position + retreatDirection * 10f; // Tenta se afastar 10 unidades

        // Encontra a posição válida mais próxima no NavMesh para recuar
        NavMeshHit hit;
        if (NavMesh.SamplePosition(desiredRetreatPosition, out hit, 5.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // Se não encontrar ponto de recuo direto, tenta ir para o ponto de patrulha mais distante
            Transform furthestPoint = FindFurthestPatrolPoint();
            if (furthestPoint != null)
            {
                agent.SetDestination(furthestPoint.position);
            }
            else
            {
                // Se não há pontos de patrulha, tenta se mover em uma direção aleatória válida
                Vector3 randomDir = Random.insideUnitSphere * 5f;
                NavMeshHit randomHit;
                if (NavMesh.SamplePosition(transform.position + randomDir, out randomHit, 5.0f, NavMesh.AllAreas))
                {
                    agent.SetDestination(randomHit.position);
                }
                else
                {
                    // Último recurso: para de recuar e volta a perseguir
                    ChangeState(AIState.Chasing);
                }
            }
        }
    }

    Transform FindFurthestPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Count == 0 || player == null) return null;

        Transform furthestPoint = null;
        float maxDistanceSqr = -1f;

        foreach (Transform point in patrolPoints)
        {
            if (point == null) continue;
            float distSqr = (player.position - point.position).sqrMagnitude; // Usa sqrMagnitude para eficiência
            if (distSqr > maxDistanceSqr)
            {
                maxDistanceSqr = distSqr;
                furthestPoint = point;
            }
        }
        return furthestPoint;
    }


    void DodgeAttack()
    {
        if (isDodging || !agent.isOnNavMesh) return; // Não esquiva se já estiver esquivando ou fora do NavMesh

        isDodging = true;
        dodgeTimer = dodgeDuration;
        ChangeState(AIState.Dodging); // Muda para o estado Dodging

        // Calcula direção de esquiva (lateralmente em relação ao jogador)
        Vector3 directionToPlayer = (player != null) ? (player.position - transform.position).normalized : transform.forward;
        // Se direção for zero, escolhe aleatória
        if (directionToPlayer == Vector3.zero) directionToPlayer = Random.insideUnitSphere.normalized;

        float side = (Random.value > 0.5f) ? 1f : -1f; // Escolhe lado aleatório
        Vector3 dodgeDirection = Quaternion.Euler(0, side * 90f, 0) * -directionToPlayer; // Esquiva para o lado/trás
        Vector3 desiredDodgePosition = transform.position + dodgeDirection * 3f; // Distância da esquiva

        NavMeshHit hit;
        if (NavMesh.SamplePosition(desiredDodgePosition, out hit, 2.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // Se não encontrar posição válida, cancela a esquiva imediatamente
            isDodging = false;
            dodgeTimer = 0f;
            // Volta ao estado anterior (que não era Dead nem Dodging)
            bool isPlayerAlive = (playerScript != null && !playerScript.isDead);
            if (previousState != AIState.Dead && previousState != AIState.Dodging)
            {
                ChangeState(isPlayerAlive ? previousState : AIState.Patrolling);
            }
            else
            {
                ChangeState(AIState.Patrolling); // Fallback seguro
            }
        }
    }

    void SearchForPlayer()
    {
        if (!agent.isOnNavMesh) { ChangeState(AIState.Patrolling); return; } // Volta a patrulhar se sair do NavMesh

        currentSearchTime += Time.deltaTime;
        // Se o tempo de busca acabou OU chegou ao destino da busca (última posição conhecida)
        if (currentSearchTime >= searchTime || (!agent.pathPending && agent.remainingDistance < 0.5f))
        {
            ChangeState(AIState.Patrolling); // Volta a patrulhar se não encontrou
        }
    }

    void AttackPlayer()
    {
        // LOG ADICIONADO
        Debug.Log("Entering AttackPlayer() - Agent stopped: " + agent.isStopped);

        if (player == null || playerScript.isDead)
        {
            // Se player sumir/morrer antes do ataque, cancela
            isAttacking = false;
            if (animator != null) animator.SetBool("isAttacking", false);
            ChangeState(AIState.Patrolling);
            return;
        }
        if (!agent.isOnNavMesh)
        { // Se saiu do NavMesh durante o ataque, cancela
            isAttacking = false;
            if (animator != null) animator.SetBool("isAttacking", false);
            ChangeState(AIState.Patrolling);
            return;
        }

        isAttacking = true;
        attackTimer = attackCooldown; // Inicia cooldown

        // Para o agente e olha para o jogador antes de atacar
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        // Ativa animação de ataque
        if (animator != null)
        {
            animator.SetBool("isWalking", false); // Garante que não está andando
            animator.SetBool("isAttacking", true); // Ativa flag de animação
            animator.SetTrigger("Attack"); // Dispara a animação específica
        }

        // Aplica dano após um delay (simula o ponto de impacto da animação)
        StartCoroutine(ApplyDamageWithDelay(attackAnimationDuration * 0.5f)); // Aplica dano na metade da animação
        // Reseta o estado de ataque após a duração da animação
        StartCoroutine(ResetAttackState());
    }

    IEnumerator ApplyDamageWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // LOG ADICIONADO
        Debug.Log($"Attempting Damage - IsAttacking: {isAttacking}, Player Alive: {(playerScript != null && !playerScript.isDead)}, Dist Check: {Vector3.Distance(transform.position, player.position) <= attackRange * 1.1f}");

        // Verifica novamente se ainda deve aplicar o dano (estado, player, distância)
        if (isAttacking && player != null && playerScript != null && !playerScript.isDead &&
            Vector3.Distance(transform.position, player.position) <= attackRange * 1.1f) // Pequena margem extra
        {
            // Debug.Log(gameObject.name + " aplicando dano ao jogador.");
            playerScript.TakeDamage(attackDamage);
            aggressiveness = Mathf.Clamp(aggressiveness + 5f, 0f, 100f); // Aumenta agressividade ao acertar
        }
        else
        {
            // Debug.Log(gameObject.name + " dano cancelado (jogador morto/longe/ataque interrompido).");
        }
    }

    IEnumerator ResetAttackState()
    {
        // LOG ADICIONADO
        Debug.Log("Entering ResetAttackState() - Will re-enable agent movement.");

        yield return new WaitForSeconds(attackAnimationDuration); // Espera a animação terminar
        isAttacking = false;
        if (animator != null) animator.SetBool("isAttacking", false); // Reseta flag da animação
        if (agent.isOnNavMesh) agent.isStopped = false; // Permite que o agente se mova novamente

        // A transição para o próximo estado agora é feita no Update,
        // verificando se isAttacking é false e se o jogador ainda é detectável.
    }

    public void TakeDamage(float amount) // CORRIGIDO: Nome da função para TakeDamage
    {
        if (isDead) return;

        currentHealth -= amount;
        consecutiveHits++;
        playerThreatLevel = Mathf.Clamp(playerThreatLevel + amount / 4f, 0f, 100f); // Aumenta ameaça ao levar dano
        aggressiveness = Mathf.Clamp(aggressiveness - amount / 10f, 0f, 100f); // Diminui agressividade ao levar dano

        if (animator != null) animator.SetTrigger("Damage"); // Trigger de animação de dano
        editBarHealth(currentHealth, maxHealth); // Atualiza barra de vida

        // Chance de tentar esquivar ao levar dano (se não estiver atacando ou já esquivando)
        if (!isDodging && !isAttacking && Random.value < 0.3f)
        {
            DodgeAttack();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Poderia resetar a contagem de hits consecutivos aqui após um tempo
            // StartCoroutine(ResetConsecutiveHits());
        }
        // Resetar consecutiveHits quando o inimigo ataca ou após um tempo sem levar dano
        // if (currentState == AIState.Attacking) consecutiveHits = 0;
    }

    void Die()
    {
        if (isDead) return; // Evita chamar Die múltiplas vezes

        isDead = true;
        ChangeState(AIState.Dead); // Muda para o estado Dead (que para o agente e toca animação)
        GetComponent<Collider>().enabled = false; // Desativa collider para não bloquear
        if (OnEnemyDied != null) OnEnemyDied(); // Notifica outros scripts

        // Lógica de Respawn ou Destruição
        if (respawnPoints != null && respawnPoints.Count > 0 && GetRandomValidRespawnPoint() != null) // Verifica se há pontos válidos
        {
            StartCoroutine(Respawn());
        }
        else if (mutantObject != null) // Se tem um objeto específico para destruir
        {
            StartCoroutine(EsperarEDestruir(mutantObject, 4f)); // Usa a coroutine existente
        }
        else
        {
            // Se não tem respawn nem objeto específico, destrói o GameObject raiz após um tempo
            StartCoroutine(EsperarEDestruir(gameObject, 4f));
        }
    }

    // Coroutine genérica para esperar e destruir um objeto
    IEnumerator EsperarEDestruir(GameObject objToDestroy, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (objToDestroy != null) // Verifica se o objeto ainda existe
        {
            Destroy(objToDestroy);
        }
    }

    IEnumerator Respawn()
    {
        // Espera o tempo de respawn
        yield return new WaitForSeconds(respawnDelay);

        // Escolhe um ponto de respawn válido aleatoriamente
        Transform validRespawnPoint = GetRandomValidRespawnPoint();

        if (validRespawnPoint != null)
        {
            // Move o inimigo para o ponto de respawn
            if (agent.isOnNavMesh) agent.Warp(validRespawnPoint.position); // Warp é melhor que setar position diretamente com NavMeshAgent
            transform.position = validRespawnPoint.position;
            transform.rotation = validRespawnPoint.rotation; // Opcional: resetar rotação

            // Reativa o inimigo
            GetComponent<Collider>().enabled = true;
            currentHealth = maxHealth;
            editBarHealth(currentHealth, maxHealth);
            isDead = false;
            // agent.isStopped = false; // Já é feito em ChangeState
            consecutiveHits = 0;
            playerThreatLevel = 0f; // Reseta ameaça
            aggressiveness = 50f; // Reseta agressividade para o padrão
            // Volta ao estado de Patrulha
            ChangeState(AIState.Patrolling);
        }
        else
        {
            Debug.LogError(gameObject.name + ": Falha no respawn - Nenhum ponto de respawn válido encontrado! O objeto será destruído.");
            // Se não há ponto válido, destrói o objeto para evitar problemas.
            Destroy(gameObject);
        }
    }

    Transform GetRandomValidRespawnPoint()
    {
        if (respawnPoints == null || respawnPoints.Count == 0) return null;

        List<Transform> validPoints = new List<Transform>();
        foreach (Transform point in respawnPoints)
        {
            if (point != null)
            {
                // Opcional: Verificar se o ponto está no NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(point.position, out hit, 1.0f, NavMesh.AllAreas))
                {
                    validPoints.Add(point);
                }
            }
        }

        if (validPoints.Count == 0) return null;

        int respawnIndex = Random.Range(0, validPoints.Count);
        return validPoints[respawnIndex];
    }


    // Atualiza a barra de vida (mantido como estava)
    public void editBarHealth(float currentHealth, float maxHealth)
    {
        if (barHealth != null)
        {
            barHealth.fillAmount = currentHealth / maxHealth;
        }
    }

    // Gizmos para visualização no Editor (opcional, mas útil)
    void OnDrawGizmosSelected()
    {
        // Desenha o raio de visão
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        // Desenha o ângulo de visão
        Vector3 fovLine1 = Quaternion.AngleAxis(visionAngle / 2, transform.up) * transform.forward * visionRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(-visionAngle / 2, transform.up) * transform.forward * visionRadius;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);

        // Desenha o raio de audição (sons altos)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        // Desenha o raio de audição (sons baixos)
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, closeHearingRadius);

        // Desenha o alcance de ataque
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Desenha linhas para os pontos de patrulha
        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            Gizmos.color = Color.green;
            // Desenha esfera em cada ponto válido
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                }
            }
            // Desenha linha do inimigo para o ponto de patrulha atual (se válido)
            if (currentPatrolIndex >= 0 && currentPatrolIndex < patrolPoints.Count && patrolPoints[currentPatrolIndex] != null)
            {
                Gizmos.DrawLine(transform.position, patrolPoints[currentPatrolIndex].position);
            }
        }
    }
}