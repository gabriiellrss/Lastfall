// Script C# para Controlador de Boss no Unity (Baseado em EnemyAI e Gun)
// Autor: Manus (adaptado dos scripts do usuário)
// Data: 01/06/2025

/*
 === COMO USAR ESTE SCRIPT ===

 1. Crie um GameObject para o seu Boss.
 2. Adicione este script ("BossController.cs") como um componente a esse GameObject.
 3. Adicione os componentes necessários ao Boss:
    - NavMeshAgent: Para movimento. Configure o Bake da NavMesh na sua cena.
    - Animator: Para controlar as animações.
    - Rigidbody: (Opcional, mas recomendado para colisões físicas, marque como Kinematic se usar NavMeshAgent para movimento principal).
    - Collider: (Ex: CapsuleCollider) Para interações físicas.
 4. Crie um GameObject filho no Boss para representar a "arma" ou ponto de disparo.
    - Adicione o script "Gun.cs" (fornecido por si) a este GameObject filho.
    - Configure o script "Gun.cs" no Inspector (prefab da bala, attackPoint - pode ser o próprio transform deste filho, etc.).
    - **IMPORTANTE:** O script "Gun.cs" original usa a câmera do jogador para mirar (Raycast). Para o Boss (IA), precisará adaptar o método `Shoot` no `Gun.cs` para mirar diretamente no jogador, ignorando a câmera. Uma sugestão é criar um novo método `ShootAtTarget(Vector3 targetPosition)` no `Gun.cs` que calcule a direção do `attackPoint` para o `targetPosition` e dispare.
 5. Crie um GameObject filho no Boss para ser o `meleeAttackPoint` (ponto de origem do ataque corpo a corpo).
 6. Configure o Animator:
    - Crie um Animator Controller e atribua-o ao componente Animator do Boss.
    - Crie os parâmetros no Animator Controller:
        - Bool: `IsWalking`
        - Trigger: `MeleeAttack`
        - Trigger: `RangedAttack`
        - Trigger: `Hit`
        - Trigger: `Die`
    - Configure os estados de animação (Idle, Walk, MeleeAttack, RangedAttack, Hit, Die) e as transições usando esses parâmetros.
 7. Configure as variáveis públicas deste script (`BossController.cs`) no Inspector:
    - Player: Arraste o GameObject do Jogador (deve ter a tag "Player" e um script como `PlayerHealth` para receber dano).
    - Agent: Arraste o componente NavMeshAgent do Boss.
    - Animator: Arraste o componente Animator do Boss.
    - Boss Gun Script: Arraste o GameObject filho que contém o script "Gun.cs".
    - Melee Attack Point: Arraste o GameObject filho que representa o ponto de ataque corpo a corpo.
    - LayerMasks: Defina as layers do jogador e obstáculos.
    - Atributos de Movimento, Combate, Cooldowns e Vida: Ajuste conforme necessário.
 8. Certifique-se que o Jogador tem um Collider e está na layer definida em `Player Layer`.

*/

using UnityEngine;
using UnityEngine.AI;
using System.Collections; // Para Coroutines, se necessário

public class BossController : MonoBehaviour
{
    // --- Referências (Configuráveis no Inspector) ---
    [Header("Referências Principais")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;
    public GunEnemien bossGunScript; // Referência ao script Gun.cs no objeto da arma do Boss
    public Transform meleeAttackPoint; // Ponto de origem para o ataque corpo a corpo

    [Header("Layers")]
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    [Header("Atributos de Movimento")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;
    public float rotationSpeed = 10f; // Velocidade de rotação ao encarar o jogador

    [Header("Atributos de Combate")]
    public float detectionRadius = 20f; // Raio de deteção geral
    public float meleeAttackRange = 2.5f; // Distância para iniciar ataque corpo a corpo
    public float rangedAttackMinRange = 5f; // Distância mínima para começar a atirar
    public float rangedAttackMaxRange = 18f; // Distância máxima para continuar atirando
    public float stoppingDistanceMelee = 2f; // Distância para parar antes do ataque melee
    public float stoppingDistanceRanged = 10f; // Distância ideal para parar e atirar

    [Header("Ataque Corpo a Corpo (Soco)")]
    public float meleeDamage = 25f;
    public float meleeAttackRadius = 1f; // Raio da área de dano do soco
    public float meleeAttackCooldown = 2.0f;
    // public float meleeAnimationDuration = 1.0f; // Opcional: se não usar Animation Events

    [Header("Ataque à Distância (Tiro)")]
    public float rangedAttackCooldown = 3.0f; // Cooldown entre rajadas/tentativas de tiro
    // O cooldown entre tiros individuais é controlado pelo Gun.cs (timeBetweenShots)

    [Header("Vida do Boss")]
    public float maxHealth = 1000f;
    public float currentHealth;
    // public Image healthBar; // Opcional: Referência à barra de vida UI

    // --- Estados da IA ---
    private enum BossState { Idle, Chasing, AttackingMelee, AttackingRanged, Cooldown, Dead }
    private BossState currentState = BossState.Idle;

    // --- Variáveis Internas ---
    private float lastMeleeAttackTime = -Mathf.Infinity;
    private float lastRangedAttackTime = -Mathf.Infinity;
    private bool isPlayerInDetectionRange = false;
    private bool isPlayerVisible = false; // Para verificações de linha de visão
    private Player playerHealth; // Script de vida do jogador

    void Awake()
    {
        // Obter componentes automaticamente se não atribuídos
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Start()
    {
        currentHealth = maxHealth;

        // Encontrar jogador se não atribuído
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                Debug.LogError("BossController: Jogador com tag 'Player' não encontrado! Desativando IA.");
                enabled = false;
                return;
            }
        }
        playerHealth = player.GetComponent<Player>(); // Obter script de vida do jogador
        if (playerHealth == null)
        {
            Debug.LogWarning("BossController: Script PlayerHealth não encontrado no jogador. O Boss não poderá verificar se o jogador está vivo.");
        }

        // Validações de referências essenciais
        if (bossGunScript == null)
        {
            Debug.LogError("BossController: Referência ao script 'Gun' do Boss não definida! Ataque à distância não funcionará.");
        }
        if (meleeAttackPoint == null)
        {
            Debug.LogWarning("BossController: Melee Attack Point não definido. Usando a posição do Boss como padrão.");
            meleeAttackPoint = transform;
        }

        // Configuração inicial do NavMeshAgent
        agent.speed = walkSpeed;
        agent.stoppingDistance = stoppingDistanceMelee; // Começa com a distância melee

        currentState = BossState.Idle;
    }

    void Update()
    {
        if (currentState == BossState.Dead || player == null) return;

        // Verifica se o jogador está vivo (se o script PlayerHealth existir)
        bool isPlayerAlive = (playerHealth == null || !playerHealth.isDead); // Assume vivo se não houver script
        if (!isPlayerAlive)
        {
            // Se o jogador morreu, volta ao estado Idle (ou Patrulha, se implementado)
            if (currentState != BossState.Idle)
            {
                ChangeState(BossState.Idle);
            }
            // Para a execução do Update se o jogador não estiver vivo
            return;
        }

        // Calcula distância e visibilidade
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        isPlayerInDetectionRange = distanceToPlayer <= detectionRadius;
        isPlayerVisible = CheckLineOfSight();

        // Atualiza a máquina de estados
        UpdateStateMachine(distanceToPlayer);

        // Executa ações do estado atual
        ExecuteCurrentStateAction(distanceToPlayer);
    }

    // --- Máquina de Estados ---
    void UpdateStateMachine(float distance)
    {
        if (currentState == BossState.Dead) return;

        // Lógica de Cooldown
        if (currentState == BossState.Cooldown)
        {
            // Verifica se ambos os cooldowns terminaram para sair do estado
            // Uma máquina mais complexa poderia ter cooldowns separados
            if (Time.time >= lastMeleeAttackTime + meleeAttackCooldown && Time.time >= lastRangedAttackTime + rangedAttackCooldown)
            {
                ChangeState(BossState.Chasing); // Volta a perseguir após cooldown
            }
            return; // Permanece em Cooldown
        }

        // Transições principais
        if (!isPlayerInDetectionRange || !isPlayerVisible)
        {
            // Se perdeu o jogador de vista/alcance, volta a Idle (ou Patrulha)
            if (currentState != BossState.Idle)
                ChangeState(BossState.Idle);
        }
        else // Jogador detectado e visível
        {
            bool canMelee = distance <= meleeAttackRange && Time.time >= lastMeleeAttackTime + meleeAttackCooldown;
            // Verifica se está no range de tiro e se a arma está pronta (do Gun.cs)
            bool canRanged = distance > meleeAttackRange && distance <= rangedAttackMaxRange && Time.time >= lastRangedAttackTime + rangedAttackCooldown && (bossGunScript != null && bossGunScript.readyToShoot && bossGunScript.bulletsLeft > 0);

            if (canMelee)
            {
                ChangeState(BossState.AttackingMelee);
            }
            else if (canRanged)
            {
                ChangeState(BossState.AttackingRanged);
            }
            else if (currentState != BossState.AttackingMelee && currentState != BossState.AttackingRanged)
            {
                // Se não está atacando, persegue
                ChangeState(BossState.Chasing);
            }
            // Se está no range de um ataque mas ele está em cooldown, continua perseguindo
            else if ((distance <= meleeAttackRange && !canMelee) || (distance > meleeAttackRange && distance <= rangedAttackMaxRange && !canRanged))
            {
                if (currentState != BossState.Chasing) ChangeState(BossState.Chasing);
            }
        }
    }

    void ChangeState(BossState newState)
    {
        if (currentState == newState) return; // Não faz nada se já está no estado

        // Debug.Log($"Boss mudando de {currentState} para {newState}");
        currentState = newState;

        // Configurações ao entrar no novo estado
        switch (currentState)
        {
            case BossState.Idle:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                animator.SetBool("IsWalking", false);
                // Poderia iniciar uma rotina de patrulha aqui se desejado
                break;

            case BossState.Chasing:
                agent.isStopped = false;
                agent.speed = runSpeed;
                agent.stoppingDistance = stoppingDistanceMelee; // Persegue até ficar perto para melee
                animator.SetBool("IsWalking", true);
                break;

            case BossState.AttackingMelee:
                agent.isStopped = true; // Para para atacar
                agent.velocity = Vector3.zero;
                animator.SetBool("IsWalking", false);
                break;

            case BossState.AttackingRanged:
                agent.isStopped = false; // Pode precisar ajustar a posição
                agent.speed = walkSpeed; // Move-se mais devagar enquanto atira/prepara
                agent.stoppingDistance = stoppingDistanceRanged; // Tenta manter distância ideal
                animator.SetBool("IsWalking", false); // Para animação de andar ao preparar/atirar
                break;

            case BossState.Cooldown:
                agent.isStopped = true; // Fica parado durante cooldown geral
                agent.velocity = Vector3.zero;
                animator.SetBool("IsWalking", false);
                break;

            case BossState.Dead:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                if (agent.enabled) agent.enabled = false; // Desativa NavMeshAgent
                // Desativar colliders, etc.
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
                animator.SetTrigger("Die");
                break;
        }
    }

    void ExecuteCurrentStateAction(float distance)
    {
        switch (currentState)
        {
            case BossState.Idle:
                // Lógica Idle (ex: olhar em volta, animação idle)
                break;

            case BossState.Chasing:
                FacePlayer();
                if (agent.isOnNavMesh && !agent.pathPending)
                {
                    agent.SetDestination(player.position);
                }
                break;

            case BossState.AttackingMelee:
                FacePlayer();
                // Inicia o ataque (a animação/dano pode ser via Trigger/Event)
                PerformMeleeAttack();
                break;

            case BossState.AttackingRanged:
                FacePlayer();
                // Tenta manter a distância ideal enquanto atira
                if (agent.isOnNavMesh && !agent.pathPending)
                {
                    agent.SetDestination(player.position);
                    // Para de andar se estiver muito perto da distância ideal
                    if (Mathf.Abs(distance - stoppingDistanceRanged) < 1.0f)
                    {
                        agent.isStopped = true;
                        agent.velocity = Vector3.zero;
                    }
                    else
                    {
                        agent.isStopped = false;
                    }
                }
                // Inicia o ataque à distância
                PerformRangedAttack();
                break;

            case BossState.Cooldown:
                FacePlayer(); // Continua encarando o jogador
                break;

            case BossState.Dead:
                // Lógica de morte já tratada em ChangeState e Die()
                break;
        }
    }

    // --- Ações Específicas ---

    void FacePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Mantém a rotação no plano horizontal
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    bool CheckLineOfSight()
    {
        if (player == null) return false;
        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // Verifica se há obstáculos entre o boss e o jogador
        // Adiciona altura aos pontos de origem/destino para evitar que o Raycast acerte o chão
        Vector3 origin = transform.position + Vector3.up * 1.0f; // Ajuste a altura conforme o pivô do boss
        Vector3 target = player.position + Vector3.up * 1.0f; // Ajuste a altura conforme o pivô do player
        Vector3 directionToTarget = (target - origin).normalized;

        if (Physics.Raycast(origin, directionToTarget, out RaycastHit hit, distanceToPlayer, obstacleLayer))
        {
            // Acertou um obstáculo antes de chegar ao jogador
            return false;
        }
        return true;
    }

    void PerformMeleeAttack()
    {
        // ANIMAÇÃO: Dispara o trigger para a animação de soco
        animator.SetTrigger("MeleeAttack");

        // Marca o tempo do último ataque para iniciar o cooldown
        lastMeleeAttackTime = Time.time;

        // Muda para o estado de Cooldown APÓS iniciar o ataque
        // O dano real pode ser aplicado via Animation Event chamado pela animação "MeleeAttack"
        // Se não usar Animation Event, chame ApplyMeleeDamage() aqui ou com um pequeno delay.
        // ApplyMeleeDamage(); // Exemplo: Dano imediato

        ChangeState(BossState.Cooldown);
    }

    // Função para ser chamada por Animation Event da animação "MeleeAttack"
    public void ApplyMeleeDamage()
    {
        Collider[] hitPlayers = Physics.OverlapSphere(meleeAttackPoint.position, meleeAttackRadius, playerLayer);
        foreach (Collider hitPlayer in hitPlayers)
        {
            Debug.Log("Boss acertou o jogador com soco!");
            Player health = hitPlayer.GetComponent<Player>();
            if (health != null)
            {
                health.TakeDamage(meleeDamage);
            }
        }
        // Adicionar SFX/VFX de impacto aqui
    }

    void PerformRangedAttack()
    {
        if (bossGunScript == null)
        {
            Debug.LogError("Tentativa de ataque ranged sem referência ao Gun script!");
            ChangeState(BossState.Cooldown); // Entra em cooldown mesmo se falhar
            return;
        }

        // ANIMAÇÃO: Dispara o trigger para a animação de tiro
        animator.SetTrigger("RangedAttack");

        // LÓGICA DE TIRO:
        // Chama o método de tiro do script Gun.cs
        // IMPORTANTE: Garanta que o método Shoot no Gun.cs foi adaptado para mirar no 'player.position'
        // em vez de usar Camera Raycast, ou crie um método específico como ShootAtTarget.
        bossGunScript.TryShootAI(player.position); // Ou chame um método adaptado: bossGunScript.ShootAtTarget(player.position);

        // Marca o tempo do último ataque para iniciar o cooldown da RAJADA
        lastRangedAttackTime = Time.time;

        // Muda para o estado de Cooldown APÓS iniciar a tentativa de tiro
        // O Gun.cs controlará o tempo entre disparos individuais (timeBetweenShots)
        ChangeState(BossState.Cooldown);
    }

    // --- Vida e Morte ---
    public void TakeDamage(float amount)
    {
        if (currentState == BossState.Dead) return;

        currentHealth -= amount;
        // if (healthBar != null) healthBar.fillAmount = currentHealth / maxHealth;

        Debug.Log($"Boss recebeu {amount} de dano. Vida: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // ANIMAÇÃO: Dispara o trigger de Hit (dano recebido)
            animator.SetTrigger("Hit");
            // Opcional: Interromper ação atual, entrar em estado de "Flinch"?
        }
    }

    void Die()
    {
        if (currentState == BossState.Dead) return;
        Debug.Log("Boss foi derrotado!");
        currentHealth = 0;
        ChangeState(BossState.Dead);

        // Adicionar lógica de loot, eventos, etc.

        // Destruir o objeto após um tempo (opcional, pode ser feito pela animação)
        // Destroy(gameObject, 5f);
    }

    // --- Gizmos para Debug Visual ---
    void OnDrawGizmosSelected()
    {
        // Detection Radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Melee Attack Range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);

        // Ranged Attack Range (Min/Max)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, rangedAttackMaxRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangedAttackMinRange);

        // Melee Attack Area
        Gizmos.color = Color.magenta;
        if (meleeAttackPoint != null)
        {
            Gizmos.DrawWireSphere(meleeAttackPoint.position, meleeAttackRadius);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position + transform.forward * 0.5f, meleeAttackRadius); // Posição padrão se não houver ponto
        }

        // Line of Sight Check (Debug)
        if (player != null)
        {
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3 target = player.position + Vector3.up * 1.0f;
            if (isPlayerVisible)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(origin, target);
            }
            else
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(origin, target);
            }
        }
    }
}

// Lembre-se de ter um script PlayerHealth.cs no seu jogador com um método TakeDamage(float amount)
// e um método ou propriedade como IsDead() para verificar se o jogador está vivo.


