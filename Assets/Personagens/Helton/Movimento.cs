using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    private CharacterController controller;
    private Animator anim;

    public float walkSpeed = 4.5f;
    public float runSpeed = 7.5f;
    public float jumpForce = 10f;
    public float gravity = 20f;

    private float verticalVelocity;
    private Vector3 moveDirection;
    private bool isJumping = false; // Indica se o pulo atual foi iniciado
    private bool canDoubleJump = false;

    public Transform cameraTransform;

    private bool isAttacking = false;
    public float attackCooldown = 0f; // Tempo mínimo entre iniciar sequências de ataque
    private float attackTimer = 0f;

    private int currentCombo = 0;
    public float comboWindow = 0.2f; // Tempo para continuar o combo
    private float comboTimer = 0f;

    // IDs das animações no Animator (Ajuste se necessário)
    // Recomendo usar Animator.StringToHash para otimizar, mas ints funcionam
    public int attack1AnimID = 1;
    public int attack2AnimID = 2;
    public int attack3AnimID = 3;

    // Variáveis de controle do Animator (simplificadas)
    private bool noChao = true;
    // private float velocidade = 0f; // 'velocidade' no Animator será controlada diretamente

    // Parâmetros para o avanço (dash) dos ataques
    public float attack2DashDistance = 1.5f;
    public float attack2DashDuration = 0.2f;
    public float attack3DashDistance = 2.0f; // Pode ser diferente para o 3º ataque
    public float attack3DashDuration = 0.25f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // A função Move agora SEMPRE é chamada para lidar com gravidade e pulos
        Move();
        HandleAttack();
        HandleCombo();
        UpdateAnimation(); // Atualiza animações todo frame
    }

    void Move()
    {
        float horizontalInput = 0f;
        float verticalInput = 0f;
        bool isMoving = false;
        bool isRunning = false;
        float currentSpeed = 0f;
        Vector3 move = Vector3.zero; // Começa sem movimento horizontal

        // Só processa input de movimento se NÃO estiver atacando
        if (!isAttacking)
        {
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");
            isMoving = horizontalInput != 0 || verticalInput != 0;
            isRunning = isMoving && Input.GetKey(KeyCode.LeftShift) || Input.GetButton("Fire1");
            currentSpeed = isRunning ? runSpeed : walkSpeed;

            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            move = (forward * verticalInput + right * horizontalInput).normalized * currentSpeed;
        }

        // Lógica de Gravidade e Pulo (executada sempre)
        if (controller.isGrounded)
        {
            noChao = true; // Atualiza estado do chão
            if (isJumping)
            {
                isJumping = false; // Resetar estado de pulo ao tocar o chão
            }

            verticalVelocity = -gravity * Time.deltaTime; // Aplica uma pequena força para baixo para manter no chão
            canDoubleJump = true; // Permite pulo duplo ao tocar o chão

            // Input de Pulo (só se não estiver atacando)
            if (Input.GetButtonDown("Jump") && !isAttacking) // Usando "Jump" padrão do Unity (geralmente Espaço)
            {
                Jump();
            }
        }
        else
        {
            noChao = false; // Atualiza estado do chão

            // Input de Pulo Duplo (só se não estiver atacando)
            if (Input.GetButtonDown("Jump") && canDoubleJump && !isAttacking)
            {
                DoubleJump();
            }

            // Aplica gravidade
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // Combina movimento horizontal (do input ou zero se atacando) com movimento vertical (gravidade/pulo)
        moveDirection = new Vector3(move.x, verticalVelocity, move.z);

        // Aplica o movimento final usando CharacterController
        // O movimento do AttackDash será adicionado separadamente pela corrotina
        controller.Move(moveDirection * Time.deltaTime);

        // Rotação do personagem (só se estiver se movendo pelo input)
        if (isMoving && !isAttacking)
        {
            Vector3 lookDirection = new Vector3(move.x, 0, move.z);
            if (lookDirection.magnitude > 0.1f) // Evita rotação se o input for mínimo
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f); // Rotação mais suave
            }
        }
    }

    // Separei a atualização do Animator para melhor organização
    void UpdateAnimation()
    {
        if (anim == null) return;

        // Calcula a velocidade horizontal para o parâmetro 'velocidade'
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        float currentHorizontalSpeed = horizontalVelocity.magnitude;

        // Define a velocidade no animator (considera correndo ou andando)
        // Se estiver atacando, a animação de ataque terá prioridade sobre idle/walk/run
        // mas ainda podemos passar a velocidade caso a animação de ataque use Root Motion ou precise dela.
        // Por simplicidade, vamos usar 1 para andar e 2 para correr quando houver input.
        float animatorSpeed = 0f;
        if (!isAttacking)
        {
            bool isRunning = Input.GetButton("Fire1"); // Verifica se está correndo
            if (currentHorizontalSpeed > 0.1f) // Se está se movendo
            {
                animatorSpeed = isRunning ? 2f : 1f;
            }
        }

        anim.SetFloat("velocidade", animatorSpeed); // Parâmetro 'velocidade' controla idle/walk/run
        anim.SetBool("noChao", noChao);

        // Os triggers/bools de ataque são controlados em Attack() e ResetAttackState()
        // Não precisa definir h1, h2, h3 aqui diretamente todo frame.
    }

    void Jump()
    {
        if (!isJumping) // Evita aplicar força de pulo múltiplas vezes no mesmo pulo
        {
            isJumping = true;
            verticalVelocity = jumpForce;
            canDoubleJump = true; // O primeiro pulo habilita o duplo
            noChao = false; // Imediatamente sai do chão
            anim.SetTrigger("Pular"); // Adicione um Trigger "Pular" no seu Animator
        }
    }

    void DoubleJump()
    {
        verticalVelocity = jumpForce; // Aplica a força do pulo novamente
        canDoubleJump = false; // Desabilita pulo duplo até tocar o chão
        isJumping = true; // Marca que está pulando
        noChao = false;
        anim.SetTrigger("Pular"); // Pode usar o mesmo trigger ou um diferente para pulo duplo
    }

    void HandleAttack()
    {
        // Reduz o cooldown do ataque
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }

        // Detecta o input de ataque (Espaço)
        // Só permite atacar se o cooldown acabou e se NÃO está JÁ atacando (evita spam durante a animação)
        // Permite atacar no ar
        if (Input.GetButtonDown("Fire3") && attackTimer <= 0 && !isAttacking) // Usando "Fire1" (geralmente botão esquerdo do mouse ou Ctrl esquerdo)
        {
            Attack();
        }
    }

    void Attack()
    {
        isAttacking = true; // Marca que está atacando
        attackTimer = attackCooldown; // Reinicia cooldown para evitar iniciar nova sequência imediatamente

        // Determina qual ataque fazer baseado no combo atual
        int attackAnimID = attack1AnimID; // ID da animação a ser tocada
        float dashDistance = 0f;
        float dashDuration = 0f;
        bool performJump = false;

        switch (currentCombo)
        {
            case 0: // Primeiro ataque
                attackAnimID = attack1AnimID;
                anim.SetTrigger("Attack1"); // Usar Triggers é geralmente melhor para ataques
                Debug.Log("Ataque 1");
                break;

            case 1: // Segundo ataque - AVANÇO
                attackAnimID = attack2AnimID;
                dashDistance = attack2DashDistance;
                dashDuration = attack2DashDuration;
                anim.SetTrigger("Attack2");
                Debug.Log("Ataque 2 - Avanço");
                break;

            case 2: // Terceiro ataque - AVANÇO E PULO
                attackAnimID = attack3AnimID;
                dashDistance = attack3DashDistance;
                dashDuration = attack3DashDuration;
                performJump = controller.isGrounded; // Só pula se estiver no chão ao iniciar o 3º ataque
                anim.SetTrigger("Attack3");
                Debug.Log("Ataque 3 - Avanço e Pulo");
                break;
        }

        // Inicia o avanço (dash) se necessário (Ataques 2 e 3)
        if (dashDistance > 0 && dashDuration > 0)
        {
            StartCoroutine(AttackDash(dashDistance, dashDuration));
        }

        // Realiza o pulo se necessário (Apenas Ataque 3 e se estava no chão)
        if (performJump)
        {
            verticalVelocity = jumpForce; // Aplica a força do pulo
            isJumping = true;       // Marca que está pulando
            canDoubleJump = false;  // Pular durante o ataque consome o pulo (e talvez o duplo)
            noChao = false;         // Sai do chão
            // O Animator já foi acionado pelo SetTrigger acima, não precisa de trigger de pulo aqui
        }

        // Inicia a janela de tempo para o próximo combo
        comboTimer = comboWindow;

        // Inicia a rotina para resetar o estado de ataque DEPOIS da animação
        // A duração precisa ser ajustada para corresponder à sua animação!
        StartCoroutine(ResetAttackState(GetAnimationDuration(attackAnimID))); // Passa a duração estimada
    }

    // Função auxiliar para obter a duração estimada da animação (AJUSTE ESSES VALORES!)
    float GetAnimationDuration(int attackID)
    {
        // IMPORTANTE: Substitua estes valores pela duração REAL das suas animações de ataque
        if (attackID == attack1AnimID) return 0.4f;
        if (attackID == attack2AnimID) return 0.5f; // Ataque 2 pode ser mais longo com dash
        if (attackID == attack3AnimID) return 0.6f; // Ataque 3 pode ser mais longo com dash e pulo
        return 0.3f; // Duração padrão
    }


    IEnumerator ResetAttackState(float animationDuration)
    {
        // Espera um tempo baseado na duração da animação antes de permitir o próximo ataque ou movimento
        yield return new WaitForSeconds(animationDuration);

        // Verifica se ainda está dentro da janela do combo para avançar
        if (comboTimer > 0)
        {
            currentCombo = (currentCombo + 1) % 3; // Avança para o próximo ataque (0, 1, 2, depois volta pra 0)
        }
        else
        {
            currentCombo = 0; // Se a janela de combo expirou, reseta para o primeiro ataque
        }

        // Libera o estado de ataque para permitir movimento ou novo ataque
        isAttacking = false;
        Debug.Log("Estado de ataque resetado. Próximo combo: " + currentCombo);
    }

    // Corrotina para o avanço durante o ataque
    IEnumerator AttackDash(float distance, float duration)
    {
        float elapsed = 0f;
        Vector3 dashDirection = transform.forward; // Usa a direção para onde o personagem está olhando
        float speed = distance / duration;

        while (elapsed < duration)
        {
            // Calcula o movimento APENAS para este frame
            // Usamos controller.Move para que colisões sejam detectadas
            // Não multiplicamos por Time.deltaTime aqui porque já está embutido na velocidade (distancia/tempo)
            // e o controller.Move espera um deslocamento por frame.
            float moveAmount = speed * Time.deltaTime;
            controller.Move(dashDirection * moveAmount);

            elapsed += Time.deltaTime;
            yield return null; // Espera o próximo frame
        }
    }

    void HandleCombo()
    {
        // Reduz o timer da janela de combo
        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
            // Se o tempo expirar, reseta o combo
            if (comboTimer <= 0)
            {
                currentCombo = 0;
                Debug.Log("Janela de Combo Expirou. Resetando combo.");
            }
        }
        // Se o timer já é zero ou menos, o combo já foi resetado (não faz nada)
    }
}