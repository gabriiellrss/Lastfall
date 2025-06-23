using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.InputSystem; // Adicionado para o novo Input System

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
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
    public float backwardSpeedMultiplier = 0.7f;
    public float rotationSpeed = 15f;

    private float verticalVelocity;
    private Vector3 moveDirection;
    private bool isJumping = false;
    private bool canDoubleJump = false;
    // private float inputX = 0f; // Removido
    // private float inputY = 0f; // Removido

    private Vector2 moveInput; // Nova variável para o input de movimento
    private bool isRunningInput; // Nova variável para o input de corrida

    public Transform cameraTransform;

    [Header("Spine Rotation")]
    public Transform spineTransform;
    public float spineRotationSpeed = 10f;
    public float maxSpineAngle = 60f;
    public bool rotateSpineX = true;
    public bool rotateSpineY = true;
    public float bodyRotationThreshold = 0.8f;
    public float bodyRotationSpeed = 3f;

    [Header("Collions Hands e Toes")]
    public GameObject rightHand;
    public GameObject leftHand;
    public GameObject rightToe;

    private bool isAttacking = false;
    private float attackCooldown = 0f;
    private float attackTimer = 0f;
    private float comboWindow = 0.6f;
    private int currentCombo1 = 0;
    private int currentCombo2 = 0;
    private float comboTimer1 = 0f;
    private float comboTimer2 = 0f;
    private bool bufferedAttack1 = false;
    private bool bufferedAttack2 = false;
    private int attack1AnimID = 1;
    private int attack2AnimID = 2;
    private int attack3AnimID = 3;
    private int attack4AnimID = 4;
    private int attack5AnimID = 5;
    private int attack6AnimID = 6;
    private bool noChao = true;
    private bool wasGrounded = true;

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

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioSource jumpLandSource;
    public AudioClip walkClip;
    public AudioClip runClip;
    public AudioClip jumpClip;
    public AudioClip landClip;
    public AudioClip hitClip;
    public AudioClip deathClip;
    public AudioClip attack1Clip;
    public AudioClip attack2Clip;
    public AudioClip attack3Clip;
    public AudioClip attack4Clip;
    public AudioClip attack5Clip;
    public AudioClip attack6Clip;
    public AudioClip fireballClip;
    public AudioClip equipWeaponClip;
    public AudioClip unequipWeaponClip;

    [Header("Weapon System")]
    public GameObject weaponObject;
    private bool isWeaponActive = false;
    private Quaternion spineOriginalRotation;
    private bool isStopped = false;

    // --- NOVAS VARIÁVEIS PARA RENASCIMENTO ---
    public bool isDead = false;
    public float respawnDelay = 10f; // Tempo para renascer
    private Vector3 initialPosition; // Posição inicial para renascer
                                     // -----------------------------------------

    private bool isShootingHeld = false;
    public bool chave1 = false;
    public bool chave2 = false;



    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        slowMo = GetComponent<SlowMotionHandler>();
        currentHealth = maxHealth;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        editBarHealth(currentHealth, maxHealth);

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (weaponObject != null)
        {
            weaponObject.SetActive(false);
        }
        if (anim != null)
        {
            anim.SetBool("isShoot", false);
            anim.SetBool("isShooting", false);
            anim.SetFloat("inputX", 0f);
            anim.SetFloat("inputY", 0f);
        }

        if (spineTransform != null)
        {
            spineOriginalRotation = spineTransform.localRotation;
        }
        else
        {
            Debug.LogWarning("Spine Transform não atribuído!");
        }
        wasGrounded = controller.isGrounded;

        initialPosition = transform.position;

    }

    public void editBarHealth(float vidaAtual, float vidaMaxima)
    {
        if (barHealth != null) barHealth.fillAmount = (float)vidaAtual / vidaMaxima;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return; // Jogo está pausado, ignora Update

        if (isStopped)
        {
            // Se o jogador estiver parado, garantimos que a animação de movimento seja zero
            if (anim != null)
            {
                anim.SetFloat("inputX", 0f);
                anim.SetFloat("inputY", 0f);
                anim.SetFloat("velocidade", 0f);
            }
            // Não retornamos aqui para permitir que LateUpdate e outras lógicas de estado (como isShootingHeld) ainda funcionem,
            // mas o movimento e ataques são bloqueados nos métodos específicos.
        }

        Move();
        HandleCombo();
        UpdateAnimation();
        HandleLandingSound();

        // Lógica de tiro contínuo (se a arma permitir e o botão estiver pressionado)
        if (!isStopped && isWeaponActive && gun != null && anim != null) // Adicionado isStopped aqui
        {
            if (gun.allowButtonHold && isShootingHeld)
            {
                if (gun.CanShoot)
                {
                    gun.TryShoot();
                    anim.SetBool("isShooting", true);
                    anim.SetTrigger("fire");
                }
                else
                {
                    anim.SetBool("isShooting", false);
                }
            }
        }
    }

    void LateUpdate()
    {
        // A rotação da espinha ainda pode ser desejada mesmo quando parado para efeitos visuais,
        // mas se quiser bloquear completamente, adicione 'if (isStopped) return;' aqui também.
        // Por enquanto, vamos permitir a rotação da espinha para a câmera, mesmo quando parado,
        // pois isso não afeta o movimento ou ataque.
        if (isAttacking || isWeaponActive)
        {
            RotateSpineTowardsCamera();
        }
        else
        {
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

    public void StopPlayer(bool stop)
    {
        isStopped = stop;
        if (stop)
        {
            moveDirection = Vector3.zero;
            verticalVelocity = 0f;
            // Desativa o CharacterController para impedir qualquer movimento físico
            if (controller != null)
            {
                controller.enabled = false;
            }
            if (anim != null)
            {
                anim.SetFloat("inputX", 0f);
                anim.SetFloat("inputY", 0f);
                anim.SetFloat("velocidade", 0f);
                anim.ResetTrigger("Pular");
                anim.ResetTrigger("DoubleJump");
                anim.ResetTrigger("Attack1");
                anim.ResetTrigger("Attack2");
                anim.ResetTrigger("Attack3");
                anim.ResetTrigger("Attack4");
                anim.ResetTrigger("c2Attack1");
                anim.ResetTrigger("c2Attack2");
                anim.ResetTrigger("Fireball");
                anim.SetBool("isAttacking", false);
                anim.SetBool("isShooting", false);
                anim.SetBool("isPose", false); // Garante que a pose também seja resetada
            }
            isAttacking = false;
            isShootingHeld = false; // Garante que o tiro contínuo pare
            moveInput = Vector2.zero; // Zera o input de movimento
            isRunningInput = false; // Zera o input de corrida
        }
        else
        {
            // Reativa o CharacterController quando o jogador não está mais parado
            if (controller != null)
            {
                controller.enabled = true;
            }
        }
    }

    void RotateSpineTowardsCamera()
    {
        if (spineTransform == null || cameraTransform == null) return;
        Vector3 cameraDirection = cameraTransform.forward;
        Quaternion targetRotation = Quaternion.LookRotation(cameraDirection, Vector3.up);
        float yawDifference = Mathf.DeltaAngle(transform.eulerAngles.y, cameraTransform.eulerAngles.y);
        yawDifference = Mathf.Clamp(yawDifference, -180f, 180f);
        float yawRatio = Mathf.Abs(yawDifference) / maxSpineAngle;

        if (yawRatio > bodyRotationThreshold && (isAttacking || isWeaponActive))
        {
            float rotationAmount = (yawRatio - bodyRotationThreshold) / (1 - bodyRotationThreshold);
            Quaternion bodyTargetRotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, bodyTargetRotation, Time.deltaTime * bodyRotationSpeed * rotationAmount);
        }

        float pitchAngle = cameraTransform.eulerAngles.x;
        if (pitchAngle > 180) pitchAngle -= 360;
        pitchAngle = Mathf.Clamp(pitchAngle, -maxSpineAngle, maxSpineAngle);
        yawDifference = Mathf.DeltaAngle(transform.eulerAngles.y, cameraTransform.eulerAngles.y);
        yawDifference = Mathf.Clamp(yawDifference, -maxSpineAngle, maxSpineAngle);

        Quaternion spineTargetRotation = Quaternion.Euler(
            rotateSpineX ? pitchAngle : spineOriginalRotation.eulerAngles.x,
            rotateSpineY ? yawDifference : 0,
            spineOriginalRotation.eulerAngles.z
        );
        spineTransform.localRotation = Quaternion.Slerp(spineTransform.localRotation, spineTargetRotation, Time.deltaTime * spineRotationSpeed);
    }

    void Move()
    {
        float horizontalInput = moveInput.x; // Usando moveInput
        float verticalInput = moveInput.y; // Usando moveInput
        bool isMoving = false;
        bool isRunning = isRunningInput; // Usando isRunningInput
        float currentSpeed = 0f;
        Vector3 move = Vector3.zero;

        if (!isAttacking && !isStopped) // Adicionado isStopped aqui
        {
            isMoving = horizontalInput != 0 || verticalInput != 0;
            currentSpeed = isRunning ? runSpeed : walkSpeed;

            if (cameraTransform != null)
            {
                Vector3 forward = cameraTransform.forward;
                Vector3 right = cameraTransform.right;
                forward.y = 0; right.y = 0; forward.Normalize(); right.Normalize();
                if (verticalInput < 0) currentSpeed *= backwardSpeedMultiplier;
                move = (forward * verticalInput + right * horizontalInput).normalized * currentSpeed;
            }
            else
            {
                move = (new Vector3(horizontalInput, 0, verticalInput)).normalized * currentSpeed;
                if (verticalInput < 0) move *= backwardSpeedMultiplier;
            }
        }

        if (controller.isGrounded)
        {
            noChao = true;
            if (isJumping) isJumping = false;
            verticalVelocity = -gravity * Time.deltaTime;
            canDoubleJump = true;
            // O input de pulo agora é tratado no OnJump, que já tem a verificação isStopped
        }
        else
        {
            noChao = false;
            verticalVelocity -= gravity * Time.deltaTime;
        }

        moveDirection = new Vector3(move.x, verticalVelocity, move.z);
        if (controller != null && controller.enabled) // controller.enabled já é verificado
        {
            controller.Move(moveDirection * Time.deltaTime);
        }

        if (isMoving && !isAttacking && !isStopped) // Adicionado isStopped aqui
        {
            Vector3 lookDirection = new Vector3(move.x, 0, move.z);
            if (lookDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }

        // Lógica de áudio de movimento
        if (isMoving && controller.isGrounded && !isStopped) // Adicionado isStopped aqui
        {
            if (!audioSource.isPlaying || (audioSource.clip != walkClip && audioSource.clip != runClip))
            {
                PlaySound(isRunning ? runClip : walkClip, true);
            }
            else if (audioSource.isPlaying && audioSource.loop)
            {
                AudioClip targetClip = isRunning ? runClip : walkClip;
                if (audioSource.clip != targetClip)
                {
                    audioSource.Stop();
                    PlaySound(targetClip, true);
                }
            }
        }
        else if (audioSource.isPlaying && (audioSource.clip == walkClip || audioSource.clip == runClip))
        {
            audioSource.Stop();
        }
    }

    void HandleLandingSound()
    {
        bool currentlyGrounded = controller.isGrounded;
        if (!wasGrounded && currentlyGrounded)
        {
            jumpLandSource.PlayOneShot(landClip);
        }
        wasGrounded = currentlyGrounded;
    }

    void UpdateAnimation()
    {
        if (anim == null) return;
        if (isStopped) // Se estiver parado, força as animações de movimento para zero
        {
            anim.SetFloat("inputX", 0f);
            anim.SetFloat("inputY", 0f);
            anim.SetFloat("velocidade", 0f);
        }
        else
        {
            anim.SetFloat("inputX", moveInput.x); // Usando moveInput
            anim.SetFloat("inputY", moveInput.y); // Usando moveInput
            Vector3 horizontalVelocity = Vector3.zero;
            if (controller != null && controller.enabled) horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
            float currentHorizontalSpeed = horizontalVelocity.magnitude;
            float animatorSpeed = 0f;
            if (!isAttacking)
            {
                bool isRunning = isRunningInput; // Usando isRunningInput
                if (currentHorizontalSpeed > 0.1f) animatorSpeed = isRunning ? 2f : 1f;
                anim.SetFloat("velocidade", animatorSpeed);
            }
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
            jumpLandSource.PlayOneShot(jumpClip);
        }
    }

    void DoubleJump()
    {
        verticalVelocity = jumpForce;
        canDoubleJump = false;
        isJumping = true;
        noChao = false;
        if (anim != null) anim.SetTrigger("DoubleJump");
        PlaySound(jumpClip);
    }

    void Fireball()
    {
        if (anim != null) anim.SetTrigger("Fireball");
        PlaySound(fireballClip);
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
            //Instantiate(attackEffectPrefab, effectSpawnPoint.position, effectSpawnPoint.rotation);
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
        //float dashDistance = 0f; // Não usado diretamente aqui
        //float dashDuration = 0f; // Não usado diretamente aqui
        AudioClip attackSound = null;

        switch (currentCombo1)
        {
            case 0:
                attackAnimIDToPlay = attack1AnimID; //dashDistance = attack1DashDistance;
                if (anim != null) anim.SetTrigger("Attack1"); Demage(); attackSound = attack1Clip;
                SetTrailRenderer(rightToe, false, Color.white, Color.white, 0.1f, 0f);
                SetTrailRenderer(rightHand, true, Color.red, Color.yellow, 0.2f, 0f);
                SetTrailRenderer(leftHand, false, Color.white, Color.white, 0.1f, 0f);
                break;
            case 1:
                attackAnimIDToPlay = attack2AnimID; //dashDistance = attack2DashDistance; dashDuration = attack2DashDuration;
                if (anim != null) anim.SetTrigger("Attack2"); Demage(); attackSound = attack2Clip;
                SetTrailRenderer(leftHand, true, Color.blue, Color.cyan, 0.2f, 0f);
                SetTrailRenderer(rightHand, false, Color.white, Color.white, 0.1f, 0f);
                break;
            case 2:
                attackAnimIDToPlay = attack3AnimID; //dashDistance = attack3DashDistance; dashDuration = attack3DashDuration;
                if (anim != null) anim.SetTrigger("Attack3"); Demage(); attackSound = attack3Clip;
                SetTrailRenderer(leftHand, false, Color.white, Color.white, 0.1f, 0f);
                SetTrailRenderer(rightToe, true, Color.green, Color.cyan, 0.2f, 0f);
                break;
            case 3:
                attackAnimIDToPlay = attack4AnimID; //dashDistance = attack3DashDistance; dashDuration = attack3DashDuration;
                if (anim != null) anim.SetTrigger("Attack4"); Demage(); attackSound = attack4Clip;
                SetTrailRenderer(leftHand, true, Color.magenta, Color.cyan, 0.2f, 0f);
                SetTrailRenderer(rightToe, true, Color.green, Color.cyan, 0.2f, 0f);
                break;
        }
        PlaySound(attackSound);
        comboTimer1 = comboWindow;
        StartCoroutine(ResetAttackState(GetAnimationDuration(attackAnimIDToPlay), true));
    }

    void Attack2()
    {
        isAttacking = true;
        if (anim != null) anim.SetBool("isAttacking", true);
        attackTimer = attackCooldown;
        int attackAnimIDToPlay = attack5AnimID;
        //float dashDistance = 0f; // Não usado diretamente aqui
        //float dashDuration = 0f; // Não usado diretamente aqui
        AudioClip attackSound = null;

        switch (currentCombo2)
        {
            case 0:
                attackAnimIDToPlay = attack5AnimID; //dashDistance = attack1DashDistance;
                if (anim != null) anim.SetTrigger("c2Attack1"); Demage(); attackSound = attack5Clip;
                SetTrailRenderer(rightToe, false, Color.white, Color.white, 0.1f, 0f);
                SetTrailRenderer(rightHand, true, Color.red, Color.yellow, 0.2f, 0f);
                SetTrailRenderer(leftHand, true, Color.blue, Color.cyan, 0.2f, 0f);
                break;
            case 1:
                attackAnimIDToPlay = attack6AnimID; //dashDistance = attack2DashDistance; dashDuration = attack2DashDuration;
                if (anim != null) anim.SetTrigger("c2Attack2"); Demage(); attackSound = attack6Clip;
                SetTrailRenderer(leftHand, true, Color.blue, Color.cyan, 0.2f, 0f);
                SetTrailRenderer(rightHand, false, Color.white, Color.white, 0.1f, 0f);
                break;
        }
        PlaySound(attackSound);
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
            enemy.GetComponent<Enemy>()?.TakeDamage(attackDemage);
        }
    }

    void HandleCombo()
    {
        if (comboTimer1 > 0) comboTimer1 -= Time.deltaTime; else currentCombo1 = 0;
        if (comboTimer2 > 0) comboTimer2 -= Time.deltaTime; else currentCombo2 = 0;
        // O bufferedAttack1 só deve ser processado se o jogador não estiver parado
        if (bufferedAttack1 && !isAttacking && attackTimer <= 0 && !isWeaponActive && !isStopped)
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
        SetTrailRenderer(rightHand, false, Color.white, Color.white, 0.1f, 0f);
        SetTrailRenderer(leftHand, false, Color.white, Color.white, 0.1f, 0f);
        SetTrailRenderer(rightToe, false, Color.white, Color.white, 0.1f, 0f);
        if (isCombo1) { if (comboTimer1 > 0) currentCombo1 = (currentCombo1 + 1) % 4; else currentCombo1 = 0; }
        else { if (comboTimer2 > 0) currentCombo2 = (currentCombo2 + 1) % 2; else currentCombo2 = 0; }
    }

    float GetAnimationDuration(int attackID)
    {
        // Esta função deve retornar a duração real da animação correspondente ao attackID
        // Para simplificar, mantive um valor fixo, mas em um jogo real, você buscaria isso do Animator.
        // Exemplo: anim.runtimeAnimatorController.animationClips[attackID].length;
        // Ou usar um AnimationEvent no final da animação para chamar ResetAttackState.
        return 0.5f;
    }

    void OnDrawGizmosSelected()
    {
        if (areaTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(areaTransform.position, attackRadius);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0);
        editBarHealth(currentHealth, maxHealth);
        if (anim != null) anim.SetTrigger("Damage");
        PlaySound(hitClip);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead == false)
        {
            anim.SetTrigger("Die");
            isDead = true;
        }
        PlaySound(deathClip);
        StopPlayer(true); // Para o jogador completamente ao morrer
        StartCoroutine(RespawnCoroutine());
    }

    private void PlaySound(AudioClip clip, bool loop = false)
    {
        if (audioSource != null && clip != null)
        {
            if (loop)
            {
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.Play();
            }
            else
            {
                if (audioSource.isPlaying && audioSource.loop) audioSource.Stop();
                audioSource.loop = false;
                audioSource.PlayOneShot(clip);
            }
        }
    }

    IEnumerator RespawnCoroutine()
    {
        Debug.Log("Jogador morreu. Renascendo em " + respawnDelay + " segundos...");
        yield return new WaitForSeconds(respawnDelay);
        Debug.Log("Renascendo jogador...");

        // Reseta a vida
        currentHealth = maxHealth;
        editBarHealth(currentHealth, maxHealth);

        // Reseta a posição
        transform.position = initialPosition;

        // Reseta velocidade e estado de pulo
        verticalVelocity = 0f;
        moveDirection = Vector3.zero;
        isJumping = false;
        canDoubleJump = true; // Permite pular após renascer

        // Reseta estados de ataque e combos
        isAttacking = false;
        bufferedAttack1 = false;
        bufferedAttack2 = false;
        currentCombo1 = 0;
        currentCombo2 = 0;
        comboTimer1 = 0f;
        comboTimer2 = 0f;

        // Reseta o estado da animação
        if (anim != null)
        {
            anim.ResetTrigger("Die");
            anim.ResetTrigger("Damage");
            anim.Play("Idle"); // Substitua "Idle" pelo nome do seu estado de animação padrão
            anim.SetBool("isAttacking", false);
            anim.SetBool("isShooting", false);
            anim.SetBool("isPose", false);
        }

        // Marca como vivo novamente
        isDead = false;
        StopPlayer(false); // Reativa o jogador e o CharacterController
        Debug.Log("Jogador renasceu.");
    }

    // --- Métodos para o novo Input System ---
    public void OnMove(InputAction.CallbackContext context)
    {
        if (isStopped)
        {
            moveInput = Vector2.zero; // Garante que o input de movimento seja zero quando parado
            return; // Impede movimento quando parado
        }
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (isStopped) return; // Impede pulo quando parado
        if (context.performed && !isAttacking)
        {
            if (controller.isGrounded)
            {
                Jump();
            }
            else if (canDoubleJump)
            {
                DoubleJump();
            }
        }
    }

    public void OnAttack1(InputAction.CallbackContext context)
    {
        if (isStopped) return; // Impede ataque quando parado
        if (context.performed && !isWeaponActive && !isAttacking && attackTimer <= 0)
        {
            Attack();
        }
        else if (context.performed && !isWeaponActive)
        {
            bufferedAttack1 = true;
        }
    }

    public void OnAttack2(InputAction.CallbackContext context)
    {
        if (isStopped) return; // Impede ataque quando parado
        if (context.performed && !isWeaponActive)
        {
            Fireball();
        }
    }

    public void OnToggleWeapon(InputAction.CallbackContext context)
    {
        if (isStopped) return; // Impede alternar arma quando parado
        if (context.performed)
        {
            isWeaponActive = !isWeaponActive;
            if (weaponObject != null) weaponObject.SetActive(isWeaponActive);
            if (anim != null)
            {
                anim.SetBool("isShoot", isWeaponActive);
                if (!isWeaponActive) anim.SetBool("isShooting", false);
            }
            PlaySound(isWeaponActive ? equipWeaponClip : unequipWeaponClip);
        }
    }

    public void OnPose(InputAction.CallbackContext context)
    {
        if (isStopped) return; // Impede pose quando parado
        if (anim != null)
        {
            if (context.performed)
            {
                anim.SetBool("isPose", true);
            }
            else if (context.canceled)
            {
                anim.SetBool("isPose", false);
            }
        }
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (isStopped)
        {
            isRunningInput = false; // Garante que o input de corrida seja falso quando parado
            return; // Impede corrida quando parado
        }
        if (context.performed)
        {
            isRunningInput = true;
        }
        else if (context.canceled)
        {
            isRunningInput = false;
        }
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (isStopped)
        {
            isShootingHeld = false; // Garante que o tiro seja interrompido quando parado
            return; // Impede tiro quando parado
        }

        if (!isWeaponActive || gun == null || anim == null)
            return;

        if (context.started)
        {
            isShootingHeld = true;

            if (!gun.allowButtonHold && gun.CanShoot)
            {
                gun.TryShoot();
                anim.SetBool("isShooting", true);
                anim.SetTrigger("fire");
            }
        }

        if (context.canceled)
        {
            isShootingHeld = false;
            anim.SetBool("isShooting", false);
        }
    }
}
