using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; // Mantendo por compatibilidade, embora o código use Input Manager

public class Player : MonoBehaviour
{
    private CharacterController controller;
    private Animator anim;

    public GameObject attackEffectPrefab;
    public Transform effectSpawnPoint;

    public float walkSpeed = 4.5f;
    public float runSpeed = 7.5f;
    public float jumpForce = 10f;
    public float gravity = 20f;

    private float verticalVelocity;
    private Vector3 moveDirection;
    private bool isJumping = false;
    private bool canDoubleJump = false;

    public Transform cameraTransform;

    public GameObject rightHand;
    public GameObject leftHand;
    public GameObject rightToe;

    // --- Estado Geral ---
    private bool isAttacking = false;
    private bool noChao = true;

    // --- Combo 1 ("Fire2") - Voltando para 3 ataques ---
    private float attackCooldownC1 = 0f;
    private float attackTimerC1 = 0f;
    private int currentComboC1 = 0;
    private float comboWindowC1 = 0.7f; // Janela pode ser diferente
    private float comboTimerC1 = 0f;
    private bool bufferedAttackC1 = false;
    private int attack1AnimID = Animator.StringToHash("Attack1");
    private int attack2AnimID = Animator.StringToHash("Attack2");
    private int attack3AnimID = Animator.StringToHash("Attack3");
    // private int attack4AnimID = Animator.StringToHash("Attack4"); // 🗑️ REMOVIDO
    public float attack1DashDistance = 1.0f;
    public float attack1DashDuration = 0.1f;
    public float attack2DashDistance = 1.0f;
    public float attack2DashDuration = 0.1f;
    public float attack3DashDistance = 2.0f;
    public float attack3DashDuration = 0.1f;
    // public float attack4DashDistance = 0.5f; // 🗑️ REMOVIDO
    // public float attack4DashDuration = 0.1f; // 🗑️ REMOVIDO

    // --- Combo 2 ("Fire1") ---
    private float attackCooldownC2 = 0f;
    private float attackTimerC2 = 0f;
    private int currentComboC2 = 0;
    private float comboWindowC2 = 0.6f;
    private float comboTimerC2 = 0f;
    private bool bufferedAttackC2 = false;
    private int c2Attack1AnimID = Animator.StringToHash("c2Attack1");
    private int c2Attack2AnimID = Animator.StringToHash("c2Attack2");
    private int c2Attack3AnimID = Animator.StringToHash("c2Attack3");
    public float c2Attack1DashDistance = 1.5f;
    public float c2Attack1DashDuration = 0.1f;
    public float c2Attack2DashDistance = 0.5f;
    public float c2Attack2DashDuration = 0.08f;
    public float c2Attack3DashDistance = 2.5f;
    public float c2Attack3DashDuration = 0.15f;

    private bool isDefender;


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
        HandleAttackInput();
        HandleComboTimers();

        if (!isAttacking)
        {
            Move();
        }

        UpdateAnimation();
        Defender();
    }

    void Defender()
    {
         if(Input.GetButton("Defender"))
        {
            isAttacking = true;
            anim.SetBool("isDefender", true);
            rightHand.GetComponent<TrailRenderer>().emitting = true;
            leftHand.GetComponent<TrailRenderer>().emitting = true;


        }
        else
        {
            isAttacking = false;
            rightHand.GetComponent<TrailRenderer>().emitting = false;
            leftHand.GetComponent<TrailRenderer>().emitting = false;
            anim.SetBool("isDefender", false);
        }
    }

    void Move()
    {
        float horizontalInput = 0f;
        float verticalInput = 0f;
        bool isMoving = false;
        bool isRunning = false;
        float currentSpeed = 0f;
        Vector3 move = Vector3.zero;

        rightHand.GetComponent<TrailRenderer>().emitting = false;
        leftHand.GetComponent<TrailRenderer>().emitting = false;
        rightToe.GetComponent<TrailRenderer>().emitting = false;

        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        isMoving = horizontalInput != 0 || verticalInput != 0;
        isRunning = isMoving && (Input.GetKey(KeyCode.LeftShift) || Input.GetButton("Run"));
        currentSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        move = (forward * verticalInput + right * horizontalInput).normalized * currentSpeed;


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
        controller.Move(moveDirection * Time.deltaTime);

        if (isMoving && !isAttacking)
        {
            Vector3 lookDirection = new Vector3(move.x, 0, move.z);
            if (lookDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }
    }

    void UpdateAnimation()
    {
        if (anim == null) return;

        if (!isAttacking)
        {
            Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
            float currentHorizontalSpeed = horizontalVelocity.magnitude;
            float animatorSpeed = 0f;
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
            anim.SetTrigger("Pular");
        }
    }

    void DoubleJump()
    {
        verticalVelocity = jumpForce;
        canDoubleJump = false;
        isJumping = true;
        noChao = false;
        anim.SetTrigger("DoubleJump");
    }

    // --- Lógica de Ataque Unificada ---

    void HandleAttackInput()
    {
        if (attackTimerC1 > 0) attackTimerC1 -= Time.deltaTime;
        if (attackTimerC2 > 0) attackTimerC2 -= Time.deltaTime;

        // Combo 1 ("Fire2")
        if (Input.GetButtonDown("Fire2"))
        {
            if (!isAttacking && attackTimerC1 <= 0)
            {
                StartAttackC1();
            }
            else if (isAttacking && comboTimerC1 > 0)
            {
                bufferedAttackC1 = true;
            }
        }

        // Combo 2 ("Fire1")
        if (Input.GetButtonDown("Fire1"))
        {
            if (!isAttacking && attackTimerC2 <= 0)
            {
                StartAttackC2();
            }
            else if (isAttacking && comboTimerC2 > 0)
            {
                bufferedAttackC2 = true;
            }
        }
    }

    void HandleComboTimers()
    {
        if (comboTimerC1 > 0)
        {
            comboTimerC1 -= Time.deltaTime;
            if (comboTimerC1 <= 0)
            {
                currentComboC1 = 0;
            }
        }
        if (comboTimerC2 > 0)
        {
            comboTimerC2 -= Time.deltaTime;
            if (comboTimerC2 <= 0)
            {
                currentComboC2 = 0;
            }
        }
    }

    // --- Lógica Específica Combo 1 ("Fire2") ---

    void StartAttackC1()
    {
        isAttacking = true;
        attackTimerC1 = attackCooldownC1;
        // anim.applyRootMotion = true; // Opcional

        int attackAnimHash = 0;
        float dashDistance = 0f;
        float dashDuration = 0f;
        bool performJump = false;

        rightHand.GetComponent<TrailRenderer>().emitting = false;
        leftHand.GetComponent<TrailRenderer>().emitting = false;
        rightToe.GetComponent<TrailRenderer>().emitting = false;

        switch (currentComboC1)
        {
            case 0:
                attackAnimHash = attack1AnimID;
                dashDistance = attack1DashDistance;
                dashDuration = attack1DashDuration;
                anim.SetTrigger("Attack1");
                rightHand.GetComponent<TrailRenderer>().emitting = true;
                break;

            case 1:
                attackAnimHash = attack2AnimID;
                dashDistance = attack2DashDistance;
                dashDuration = attack2DashDuration;
                anim.SetTrigger("Attack2");
                leftHand.GetComponent<TrailRenderer>().emitting = true;
                break;

            case 2:
                attackAnimHash = attack3AnimID;
                dashDistance = attack3DashDistance;
                dashDuration = attack3DashDuration;
                anim.SetTrigger("Attack3");
                performJump = controller.isGrounded;
                rightToe.GetComponent<TrailRenderer>().emitting = true;
                break;

                // case 3: // 🗑️ REMOVIDO O QUARTO ATAQUE
                //     break;
        }

        if (dashDistance > 0 && dashDuration > 0)
        {
            StartCoroutine(AttackDash(dashDistance, dashDuration));
        }

        if (performJump)
        {
            PerformDelayedJump();
        }

        comboTimerC1 = comboWindowC1;
        StartCoroutine(ResetAttackStateC1(GetAnimationDurationC1(currentComboC1)));
    }

    void PerformDelayedJump()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = jumpForce;
            isJumping = true;
            canDoubleJump = false;
            noChao = false;
        }
    }


    float GetAnimationDurationC1(int comboIndex)
    {
        switch (comboIndex)
        {
            case 0: return 0.4f; // Duração Attack1
            case 1: return 0.5f; // Duração Attack2
            case 2: return 0.6f; // Duração Attack3
            // case 3: return 0.55f; // 🗑️ REMOVIDO
            default: return 0.3f;
        }
    }

    IEnumerator ResetAttackStateC1(float animationDuration)
    {
        yield return new WaitForSeconds(animationDuration);

        if (comboTimerC1 > 0)
        {
            // Avança para o próximo passo, volta ao início se chegou ao fim (agora 3 passos)
            currentComboC1 = (currentComboC1 + 1) % 3; // ✨ MUDOU DE VOLTA PARA 3
        }
        else
        {
            currentComboC1 = 0;
        }

        if (bufferedAttackC1 && comboTimerC1 > 0)
        {
            bufferedAttackC1 = false;
            StartAttackC1();
        }
        else
        {
            isAttacking = false;
            bufferedAttackC1 = false;
            //anim.applyRootMotion = false;

            if (comboTimerC1 <= 0)
            {
                currentComboC1 = 0;
            }
        }
    }


    // --- Lógica Específica Combo 2 ("Fire1") ---

    void StartAttackC2()
    {
        isAttacking = true;
        attackTimerC2 = attackCooldownC2;
        // anim.applyRootMotion = true; // Opcional

        int attackAnimHash = 0;
        float dashDistance = 0f;
        float dashDuration = 0f;

        rightHand.GetComponent<TrailRenderer>().emitting = false;
        leftHand.GetComponent<TrailRenderer>().emitting = false;
        rightToe.GetComponent<TrailRenderer>().emitting = false;

        switch (currentComboC2)
        {
            case 0:
                attackAnimHash = c2Attack1AnimID;
                dashDistance = c2Attack1DashDistance;
                dashDuration = c2Attack1DashDuration;
                anim.SetTrigger("c2Attack1");
                leftHand.GetComponent<TrailRenderer>().emitting = true;
                break;

            case 1:
                attackAnimHash = c2Attack2AnimID;
                dashDistance = c2Attack2DashDistance;
                dashDuration = c2Attack2DashDuration;
                anim.SetTrigger("c2Attack2");
                rightHand.GetComponent<TrailRenderer>().emitting = true;
                break;

            case 2:
                attackAnimHash = c2Attack3AnimID;
                dashDistance = c2Attack3DashDistance;
                dashDuration = c2Attack3DashDuration;
                anim.SetTrigger("c2Attack3");
                rightToe.GetComponent<TrailRenderer>().emitting = true;
                break;
        }

        if (dashDistance > 0 && dashDuration > 0)
        {
            StartCoroutine(AttackDash(dashDistance, dashDuration));
        }

        comboTimerC2 = comboWindowC2;
        StartCoroutine(ResetAttackStateC2(GetAnimationDurationC2(currentComboC2)));
    }

    float GetAnimationDurationC2(int comboIndex)
    {
        switch (comboIndex)
        {
            case 0: return 0.45f; // Duração c2Attack1
            case 1: return 0.35f; // Duração c2Attack2
            case 2: return 0.7f;  // Duração c2Attack3
            default: return 0.3f;
        }
    }

    IEnumerator ResetAttackStateC2(float animationDuration)
    {
        yield return new WaitForSeconds(animationDuration);

        if (comboTimerC2 > 0)
        {
            currentComboC2 = (currentComboC2 + 1) % 3;
        }
        else
        {
            currentComboC2 = 0;
        }

        if (bufferedAttackC2 && comboTimerC2 > 0)
        {
            bufferedAttackC2 = false;
            StartAttackC2();
        }
        else
        {
            isAttacking = false;
            bufferedAttackC2 = false;
            //anim.applyRootMotion = false;

            if (comboTimerC2 <= 0)
            {
                currentComboC2 = 0;
            }
        }
    }

    // --- Funções Auxiliares (Comuns) ---

    IEnumerator AttackDash(float distance, float duration)
    {
        float elapsed = 0f;
        Vector3 dashDirection = transform.forward;
        float speed = distance / duration;

        while (elapsed < duration)
        {
            float moveAmount = speed * Time.deltaTime;
            CollisionFlags flags = controller.Move(dashDirection * moveAmount);
            if (flags != CollisionFlags.None)
            {
                break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}