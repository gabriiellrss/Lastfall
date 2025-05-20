    using UnityEngine;
    using System.Collections;
    using UnityEngine.UI;
using System;

public class Player : MonoBehaviour
    {
        private CharacterController controller;
        private SlowMotionHandler slowMo;

        private Animator anim;

        public GameObject attackEffectPrefab;
        public Transform effectSpawnPoint;

        [Header("Demage")]
        public LayerMask enemylayer;
        public float attackRadius = 3f;
        public float attackDemage = 10f;
        public Transform areaTransform;

        [Header("Speeds")]
        public float walkSpeed = 4.5f;
        public float runSpeed = 7.5f;
        public float jumpForce = 10f;
        public float gravity = 20f;
        public float backwardSpeedMultiplier = 0.7f; // Multiplicador para velocidade ao andar para trás
        public float rotationSpeed = 15f; // Velocidade de rotação do personagem ao mudar de direção

        private float verticalVelocity;
        private Vector3 moveDirection;
        private bool isJumping = false;
        private bool canDoubleJump = false;

        // Parâmetros para o Animator Blend Tree
        private float inputX = 0f;
        private float inputY = 0f;

        public Transform cameraTransform;


        [Header("Spine Rotation")]
        public Transform spineTransform; // Arraste o GameObject "spine" (peitoral) aqui no Inspector
        public float spineRotationSpeed = 10f; // Velocidade de rotação do spine
        public float maxSpineAngle = 60f; // Ângulo máximo de rotação do spine
        public bool rotateSpineX = true; // Rotacionar spine no eixo X (olhar para cima/baixo)
        public bool rotateSpineY = true; // Rotacionar spine no eixo Y (olhar para os lados)
        public float bodyRotationThreshold = 0.8f; // Limiar para começar a rotacionar o corpo (0-1, onde 1 é o ângulo máximo)
        public float bodyRotationSpeed = 3f; // Velocidade de rotação do corpo quando ultrapassar o limiar

        [Header("Collions Hands e Toes")]
        public GameObject rightHand;
        public GameObject leftHand;
        public GameObject rightToe;

        private bool isAttacking = false;
        private float attackCooldown = 0f;
        private float attackTimer = 0f;

        private float comboWindow = 0.6f; // Janela de combo ajustada

        private int currentCombo1 = 0;
        private int currentCombo2 = 0;
        private float comboTimer1 = 0f;
        private float comboTimer2 = 0f;

        // 🆕 Buffer de ataque
        private bool bufferedAttack1 = false;
        private bool bufferedAttack2 = false;

        private int attack1AnimID = 1;
        private int attack2AnimID = 2;
        private int attack3AnimID = 3;
        private int attack4AnimID = 4;
        private int attack5AnimID = 5;
        private int attack6AnimID = 6;

        private bool noChao = true;

        [Header("Attacks")]
        public float attack1DashDistance = 1.0f;
        public float attack2DashDistance = 1.0f;
        public float attack2DashDuration = 0.1f;
        public float attack3DashDistance = 2.0f;
        public float attack3DashDuration = 0.1f;

        public float timeFireball = 2f;

        [SerializeField] private Image barHealth;

        public float maxHealth = 100;
        public float currentHealth;

        public Gun gun;

        // Novas variáveis para o sistema de arma
        [Header("Weapon System")]
        public GameObject weaponObject; // Arraste o GameObject da sua arma aqui no Inspector
        private bool isWeaponActive = false;

        // Variáveis para armazenar a rotação original do spine
        private Quaternion spineOriginalRotation;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            anim = GetComponent<Animator>();
            slowMo = GetComponent<SlowMotionHandler>();
            currentHealth = maxHealth;

            editBarHealth(currentHealth, maxHealth);

            if (cameraTransform == null)
            {
                cameraTransform = Camera.main.transform;
            }

            // Garante que a arma comece desativada e o estado de tiro também
            if (weaponObject != null)
            {
                weaponObject.SetActive(false);
            }
            if (anim != null)
            {
                anim.SetBool("isShoot", false);
                anim.SetBool("isShooting", false);

                // Inicializa os parâmetros inputX e inputY
                anim.SetFloat("inputX", 0f);
                anim.SetFloat("inputY", 0f);
            }

            // Guarda a rotação original do spine se estiver atribuído
            if (spineTransform != null)
            {
                spineOriginalRotation = spineTransform.localRotation;
            }
            else
            {
                Debug.LogWarning("Spine Transform não atribuído! Arraste o GameObject 'spine' para o campo spineTransform no Inspector.");
            }

            // Cria o crosshair
        }

        // Método para criar o crosshair na tela


        public void editBarHealth(float vidaAtual, float vidaMaxima)
        {
            barHealth.fillAmount = (float)vidaAtual / vidaMaxima;
        }

        void Update()
        {
            // Captura os inputs de movimento
            inputX = Input.GetAxis("Horizontal");
            inputY = Input.GetAxis("Vertical");

            Move();
            HandleAttack();
            HandleCombo();
            UpdateAnimation();
            pose();
            HandleWeaponToggle();
        }

        // LateUpdate é chamado após todas as atualizações de Update
        // Ideal para ajustar a rotação do spine após o movimento do personagem
        void LateUpdate()
        {
            // Só rotaciona o spine se estiver atacando ou com a arma ativa
            if (isAttacking || isWeaponActive)
            {
                RotateSpineTowardsCamera();
            }
            else
            {
                // Se não estiver atacando ou com arma ativa, retorna o spine à rotação original
                if (spineTransform != null)
                {
                    spineTransform.localRotation = Quaternion.Slerp(
                        spineTransform.localRotation,
                        spineOriginalRotation,
                        Time.deltaTime * spineRotationSpeed
                    );
                }
            }
        }

        // Método para rotacionar o spine na direção da câmera
        void RotateSpineTowardsCamera()
        {
            if (spineTransform == null || cameraTransform == null) return;

            // Obtém a direção da câmera
            Vector3 cameraDirection = cameraTransform.forward;

            // Cria uma rotação alvo baseada na direção da câmera
            Quaternion targetRotation = Quaternion.LookRotation(cameraDirection, Vector3.up);

            // Calcula a diferença entre a rotação do corpo e a rotação da câmera
            float yawDifference = Mathf.DeltaAngle(transform.eulerAngles.y, cameraTransform.eulerAngles.y);

            // Normaliza a diferença para o intervalo -180 a 180
            yawDifference = Mathf.Clamp(yawDifference, -180f, 180f);

            // Calcula o quanto o ângulo está próximo do limite máximo (0 = não está no limite, 1 = está no limite)
            float yawRatio = Mathf.Abs(yawDifference) / maxSpineAngle;

            // Se ultrapassar o limiar, rotaciona o corpo do personagem
            // Só rotaciona o corpo se estiver atacando ou com a arma ativa
            if (yawRatio > bodyRotationThreshold && (isAttacking || isWeaponActive))
            {
                // Calcula quanto o corpo deve rotacionar
                float rotationAmount = (yawRatio - bodyRotationThreshold) / (1 - bodyRotationThreshold);

                // Cria uma rotação alvo para o corpo
                Quaternion bodyTargetRotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);

                // Aplica a rotação suavemente ao corpo
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    bodyTargetRotation,
                    Time.deltaTime * bodyRotationSpeed * rotationAmount
                );
            }

            // Ajusta a rotação do spine
            // Obtém o ângulo de pitch da câmera (olhar para cima/baixo)
            float pitchAngle = cameraTransform.eulerAngles.x;

            // Normaliza para o intervalo -180 a 180
            if (pitchAngle > 180) pitchAngle -= 360;

            // Limita o ângulo ao máximo permitido
            pitchAngle = Mathf.Clamp(pitchAngle, -maxSpineAngle, maxSpineAngle);

            // Recalcula a diferença de yaw após a possível rotação do corpo
            yawDifference = Mathf.DeltaAngle(transform.eulerAngles.y, cameraTransform.eulerAngles.y);
            yawDifference = Mathf.Clamp(yawDifference, -maxSpineAngle, maxSpineAngle);

            // Cria a rotação final com os eixos ajustados
            Quaternion spineTargetRotation = Quaternion.Euler(
                rotateSpineX ? pitchAngle : spineOriginalRotation.eulerAngles.x,
                rotateSpineY ? yawDifference : 0,
                spineOriginalRotation.eulerAngles.z
            );

            // Aplica a rotação suavemente ao spine
            spineTransform.localRotation = Quaternion.Slerp(
                spineTransform.localRotation,
                spineTargetRotation,
                Time.deltaTime * spineRotationSpeed
            );
        }

        void HandleWeaponToggle()
        {
            // Tecla para equipar/desequipar arma (ex: G)
            if (Input.GetKeyDown(KeyCode.G))
            {
                isWeaponActive = !isWeaponActive;

                if (weaponObject != null)
                {
                    weaponObject.SetActive(isWeaponActive);
                }

                if (anim != null)
                {
                    anim.SetBool("isShoot", isWeaponActive); // Atualiza o estado 'isShoot' no Animator
                    if (!isWeaponActive)
                    {
                        anim.SetBool("isShooting", false); // Garante que 'isShooting' é falso se a arma for desequipada
                    }
                }
            }
        }

        void pose()
        {
            if (Input.GetKey(KeyCode.I))
            {
                if (anim != null) anim.SetBool("isPose", true);
            }
            else
            {
                if (anim != null) anim.SetBool("isPose", false);
            }
        }

        void Move()
        {
            float horizontalInput = inputX;
            float verticalInput = inputY;
            bool isMoving = false;
            bool isRunning = false;
            float currentSpeed = 0f;
            Vector3 move = Vector3.zero;

            if (!isAttacking)
            {
                isMoving = horizontalInput != 0 || verticalInput != 0;
                isRunning = isMoving && (Input.GetKey(KeyCode.LeftShift) || Input.GetButton("Run"));
                currentSpeed = isRunning ? runSpeed : walkSpeed;

                if (cameraTransform != null)
                {
                    // MODIFICADO: Sempre usa a direção da câmera como referência para o movimento
                    // Isso faz com que o personagem se mova na direção para onde a câmera está apontando
                    Vector3 forward = cameraTransform.forward;
                    Vector3 right = cameraTransform.right;

                    forward.y = 0;
                    right.y = 0;
                    forward.Normalize();
                    right.Normalize();

                    // Aplica multiplicador de velocidade quando se move para trás
                    if (verticalInput < 0)
                    {
                        currentSpeed *= backwardSpeedMultiplier;
                    }

                    move = (forward * verticalInput + right * horizontalInput).normalized * currentSpeed;
                }
                else
                {
                    // Fallback ou aviso se cameraTransform não estiver definida
                    move = (new Vector3(horizontalInput, 0, verticalInput)).normalized * currentSpeed;

                    // Aplica multiplicador de velocidade quando se move para trás
                    if (verticalInput < 0)
                    {
                        move *= backwardSpeedMultiplier;
                    }
                }
            }

            if (controller.isGrounded)
            {
                noChao = true;
                if (isJumping) isJumping = false;

                verticalVelocity = -gravity * Time.deltaTime;
                canDoubleJump = true;

                if (Input.GetButtonDown("Jump") && !isAttacking)
                {
                    Jump();
                }
            }
            else
            {
                noChao = false;

                if (Input.GetButtonDown("Jump") && canDoubleJump && !isAttacking)
                {
                    DoubleJump();
                }

                verticalVelocity -= gravity * Time.deltaTime;
            }

            moveDirection = new Vector3(move.x, verticalVelocity, move.z);
            if (controller != null && controller.enabled)
            {
                controller.Move(moveDirection * Time.deltaTime);
            }

            // MODIFICADO: Rotaciona o personagem na direção do movimento sempre que estiver se movendo
            // Isso faz com que o personagem sempre olhe para onde está indo, como no Fortnite
            if (isMoving && !isAttacking)
            {
                Vector3 lookDirection = new Vector3(move.x, 0, move.z);
                if (lookDirection.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
            }
        }

        void UpdateAnimation()
        {
            if (anim == null) return;

            // Atualiza os parâmetros inputX e inputY no Animator para uso no Blend Tree
            anim.SetFloat("inputX", inputX);
            anim.SetFloat("inputY", inputY);

            Vector3 horizontalVelocity = Vector3.zero;
            if (controller != null && controller.enabled)
            {
                horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
            }
            float currentHorizontalSpeed = horizontalVelocity.magnitude;

            float animatorSpeed = 0f;

            if (!isAttacking)
            {
                bool isRunning = Input.GetButton("Run") || Input.GetKey(KeyCode.LeftShift);
                if (currentHorizontalSpeed > 0.1f)
                {
                    animatorSpeed = isRunning ? 2f : 1f;
                }

                anim.SetFloat("velocidade", animatorSpeed);
            }

            anim.SetBool("noChao", noChao);
        }

        void Jump()
        {
            if (!isJumping)
            {
                isJumping = true;
                verticalVelocity = jumpForce;
                canDoubleJump = true;
                noChao = false;
                if (anim != null) anim.SetTrigger("Pular");
            }
        }

        void DoubleJump()
        {
            verticalVelocity = jumpForce;
            canDoubleJump = false;
            isJumping = true;
            noChao = false;
            if (anim != null) anim.SetTrigger("DoubleJump");
        }

        void HandleAttack()
        {
            if (attackTimer > 0) // Cooldown para ataques melee
            {
                attackTimer -= Time.deltaTime;
            }

            // Ataque Melee (ex: botão direito do rato)
            if (Input.GetButtonDown("Fire2"))
            {
                if (!isWeaponActive && !isAttacking && attackTimer <= 0) // Só ataca melee se arma desequipada e não estiver já a atacar
                {
                    Attack(); // Inicia combo de ataque melee
                }
                else if (!isWeaponActive) // Buffer para ataque melee
                {
                    bufferedAttack1 = true;
                }
            }

            // Lógica de Disparo com Arma (ex: botão esquerdo do rato)
            if (isWeaponActive && gun != null && anim != null)
            {
                if (anim.GetBool("isShoot")) // Verifica se o Animator está no modo de arma
                {
                    if (Input.GetButton("Fire1")) // GetButton para disparo contínuo se allowButtonHold=true em Gun.cs
                    {
                        
                        anim.SetBool("isShooting", true); // ATIVADO: Jogador está a disparar
                        anim.SetTrigger("fire");
                        gun.TryShoot(); // Tenta disparar a arma
                    }
                    else
                    {
                        anim.SetBool("isShooting", false); // DESATIVADO: Jogador parou de disparar
                    }
                }
                else // Se animador não está em modo 'isShoot' (ex: a trocar de arma), não deve estar 'isShooting'
                {
                    anim.SetBool("isShooting", false);
                }
            }
            else
            {
                // Se a arma não está ativa, ou não há arma/animator, garantir que 'isShooting' é falso.
                if (anim != null) anim.SetBool("isShooting", false);

                // Lógica de Fireball (ex: botão esquerdo do rato se arma desequipada)
                if (!isWeaponActive && Input.GetButtonDown("Fire1"))
                {
                    Fireball();
                }
            }
        }

        void Fireball()
        {
            if (anim != null) anim.SetTrigger("Fireball");
            if (attackEffectPrefab != null && effectSpawnPoint != null)
            {
                StartCoroutine(EsperarAnim(timeFireball));
            }
        }

        IEnumerator EsperarAnim(float time)
        {
            yield return new WaitForSeconds(time);
            if (attackEffectPrefab != null && effectSpawnPoint != null)
            {
                Instantiate(attackEffectPrefab, effectSpawnPoint.position, effectSpawnPoint.rotation);
            }
        }

        void SetTrailRenderer(GameObject obj, bool isActive, Color startColor, Color endColor, float startWidth, float endWidth)
        {
            if (obj == null) return;
            TrailRenderer trail = obj.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.emitting = isActive;
                trail.startColor = startColor;
                trail.endColor = endColor;
                trail.startWidth = startWidth;
                trail.endWidth = endWidth;
            }
        }

        void Attack()
        {
            isAttacking = true;
            if (anim != null) anim.SetBool("isAttacking", true);

            attackTimer = attackCooldown;

            int attackAnimIDToPlay = attack1AnimID;
            float dashDistance = 0f;
            float dashDuration = 0f;

            switch (currentCombo1)
            {
                case 0:
                    attackAnimIDToPlay = attack1AnimID;
                    dashDistance = attack1DashDistance;
                    if (anim != null) anim.SetTrigger("Attack1");
                    Demage();
                    SetTrailRenderer(rightToe, false, Color.white, Color.white, 0.1f, 0f);
                    SetTrailRenderer(rightHand, true, Color.red, Color.yellow, 0.2f, 0f);
                    SetTrailRenderer(leftHand, false, Color.white, Color.white, 0.1f, 0f);
                    break;

                case 1:
                    attackAnimIDToPlay = attack2AnimID;
                    dashDistance = attack2DashDistance;
                    dashDuration = attack2DashDuration;
                    if (anim != null) anim.SetTrigger("Attack2");
                    Demage();
                    SetTrailRenderer(leftHand, true, Color.blue, Color.cyan, 0.2f, 0f);
                    SetTrailRenderer(rightHand, false, Color.white, Color.white, 0.1f, 0f);
                    break;

                case 2:
                    attackAnimIDToPlay = attack3AnimID;
                    if (anim != null) anim.SetTrigger("Attack3");
                    dashDistance = attack3DashDistance;
                    dashDuration = attack3DashDuration;
                    Demage();
                    SetTrailRenderer(leftHand, false, Color.white, Color.white, 0.1f, 0f);
                    SetTrailRenderer(rightToe, true, Color.green, Color.cyan, 0.2f, 0f);
                    break;
                case 3:
                    attackAnimIDToPlay = attack4AnimID;
                    if (anim != null) anim.SetTrigger("Attack4");
                    dashDistance = attack3DashDistance;
                    dashDuration = attack3DashDuration;
                    Demage();
                    SetTrailRenderer(leftHand, true, Color.magenta, Color.cyan, 0.2f, 0f);
                    SetTrailRenderer(rightToe, true, Color.green, Color.cyan, 0.2f, 0f);
                    break;
            }

            if (dashDistance > 0 && dashDuration > 0)
            {
                //StartCoroutine(AttackDash(dashDistance, dashDuration));
            }

            comboTimer1 = comboWindow;
            StartCoroutine(ResetAttackState(GetAnimationDuration(attackAnimIDToPlay), true));
        }

        void Attack2()
        {
            isAttacking = true;
            if (anim != null) anim.SetBool("isAttacking", true);

            attackTimer = attackCooldown;

            int attackAnimIDToPlay = attack5AnimID;
            float dashDistance = 0f;
            float dashDuration = 0f;

            switch (currentCombo2)
            {
                case 0:
                    attackAnimIDToPlay = attack5AnimID;
                    dashDistance = attack1DashDistance;
                    if (anim != null) anim.SetTrigger("c2Attack1");
                    Demage();
                    SetTrailRenderer(rightToe, false, Color.white, Color.white, 0.1f, 0f);
                    SetTrailRenderer(rightHand, true, Color.red, Color.yellow, 0.2f, 0f);
                    SetTrailRenderer(leftHand, true, Color.blue, Color.cyan, 0.2f, 0f);
                    break;

                case 1:
                    attackAnimIDToPlay = attack6AnimID;
                    dashDistance = attack2DashDistance;
                    dashDuration = attack2DashDuration;
                    if (anim != null) anim.SetTrigger("c2Attack2");
                    Demage();
                    SetTrailRenderer(leftHand, true, Color.blue, Color.cyan, 0.2f, 0f);
                    SetTrailRenderer(rightHand, false, Color.white, Color.white, 0.1f, 0f);
                    break;
            }

            if (dashDistance > 0 && dashDuration > 0)
            {
                //StartCoroutine(AttackDash(dashDistance, dashDuration));
            }

            comboTimer2 = comboWindow;
            StartCoroutine(ResetAttackState(GetAnimationDuration(attackAnimIDToPlay), false));
        }

        void Demage()
        {
            if (areaTransform == null || enemylayer == 0) return;
            Collider[] hitEnemies = Physics.OverlapSphere(areaTransform.position, attackRadius, enemylayer);
            foreach (Collider enemy in hitEnemies)
            {
                Debug.Log("Hit: " + enemy.name);
                // enemy.GetComponent<EnemyHealth>()?.TakeDamage(attackDemage);
            }
        }

        void HandleCombo()
        {
            if (comboTimer1 > 0) comboTimer1 -= Time.deltaTime;
            else currentCombo1 = 0;

            if (comboTimer2 > 0) comboTimer2 -= Time.deltaTime;
            else currentCombo2 = 0;

            if (bufferedAttack1 && !isAttacking && attackTimer <= 0 && !isWeaponActive)
            {
                Attack();
                bufferedAttack1 = false;
            }
        }
        

        IEnumerator ResetAttackState(float delay, bool isCombo1)
        {
            yield return new WaitForSeconds(delay);
            isAttacking = false;
            if (anim != null) anim.SetBool("isAttacking", false);

            // Desativa todos os trails após a animação de ataque
            SetTrailRenderer(rightHand, false, Color.white, Color.white, 0.1f, 0f);
            SetTrailRenderer(leftHand, false, Color.white, Color.white, 0.1f, 0f);
            SetTrailRenderer(rightToe, false, Color.white, Color.white, 0.1f, 0f);

            if (isCombo1)
            {
                if (comboTimer1 > 0) currentCombo1 = (currentCombo1 + 1) % 4; // Assumindo 4 ataques no combo 1
                else currentCombo1 = 0;
            }
            else
            {
                if (comboTimer2 > 0) currentCombo2 = (currentCombo2 + 1) % 2; // Assumindo 2 ataques no combo 2
                else currentCombo2 = 0;
            }
        }

        float GetAnimationDuration(int attackID)
        {
            // Placeholder para duração da animação
            return 0.5f;
        }

        // Método para visualizar o raio de ataque no editor
        void OnDrawGizmosSelected()
        {
            if (areaTransform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(areaTransform.position, attackRadius);
            }
        }

        void DelayedSlowMotion(float delay, float duration, float scale)
        {
            // if (slowMo != null) slowMo.TriggerSlowMotionTimed(scale, duration, delay); // Ajuste os parâmetros conforme a API do seu SlowMotionHandler
            Debug.Log("Delayed Slow Motion (Placeholder)");
        }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0); // Garante que não fique negativo

        // Atualiza a barra de vida
        editBarHealth(currentHealth, maxHealth);

        // Adicione efeitos/animacoes de dano aqui
        if (anim != null) anim.SetTrigger("Damage");

        // Verifica morte
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Adicione lógica de morte do jogador
        if (anim != null) anim.SetTrigger("Die");

        Destroy(gameObject);

        // Desativa controles
        //enabled = false;
        //controller.enabled = false;

        // Exemplo: Recarrega a cena após 3 segundos
        // StartCoroutine(ReloadScene(3f));
    }


}
