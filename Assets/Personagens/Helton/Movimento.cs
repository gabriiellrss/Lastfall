using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;



public class Player : MonoBehaviour
{
    private CharacterController controller;
    private SlowMotionHandler slowMo;

    private Animator anim;

    public GameObject attackEffectPrefab;     // arrasta o prefab do efeito aqui no Inspector
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

    private bool isAttacking = false;
    private float attackCooldown = 0f;
    private float attackTimer = 0f;

    private int currentCombo = 0;
    private float comboWindow = 0.6f; // Janela de combo ajustada
    private float comboTimer = 0f;

    // 🆕 Buffer de ataque
    private bool bufferedAttack = false;

    private int attack1AnimID = 1;
    private int attack2AnimID = 2;
    private int attack3AnimID = 3;
    private int attack4AnimID = 4;
    private int attack5AnimID = 5;
    private int attack6AnimID = 6;

    private bool noChao = true;

    public float attack1DashDistance = 1.0f;
    public float attack2DashDistance = 1.0f;
    public float attack2DashDuration = 0.1f;
    public float attack3DashDistance = 2.0f;
    public float attack3DashDuration = 0.1f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        slowMo = GetComponent<SlowMotionHandler>();


        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        Move();
        HandleAttack();
        HandleCombo();
        UpdateAnimation();
        pose();

    }

    void pose()
    {
        if (Input.GetKey(KeyCode.I))
        {
            anim.SetBool("isPose", true);
        }
        else
        {
            anim.SetBool("isPose", false);
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


        if (!isAttacking)
        {
            rightHand.GetComponent<TrailRenderer>().emitting = false;
            leftHand.GetComponent<TrailRenderer>().emitting = false;
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");
            isMoving = horizontalInput != 0 || verticalInput != 0;
            isRunning = isMoving && Input.GetKey(KeyCode.LeftShift) || Input.GetButton("Run");
            currentSpeed = isRunning ? runSpeed : walkSpeed;

            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            move = (forward * verticalInput + right * horizontalInput).normalized * currentSpeed;
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

        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        float currentHorizontalSpeed = horizontalVelocity.magnitude;

        float animatorSpeed = 0f;
        if (!isAttacking)
        {
            bool isRunning = Input.GetButton("Run");
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

    void HandleAttack()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }

        // 🔁 Agora registra o clique mesmo durante ataque
        if (Input.GetButtonDown("Fire2"))
        {
            if (!isAttacking && attackTimer <= 0)
            {
                Attack();
            }
            else
            {
                bufferedAttack = true; // Armazena o clique
            }
        }

        if (Input.GetButtonDown("Fire1"))
        {
            if (!isAttacking && attackTimer <= 0)
            {
                Attack2();
            }
            else
            {
                bufferedAttack = true; // Armazena o clique
            }
        }
    }

    void Attack()
    {
        isAttacking = true;
        anim.SetBool("isAttacking", true); // <-- Aqui ativamos

        attackTimer = attackCooldown;

        int attackAnimID = attack1AnimID;
        float dashDistance = 0f;
        float dashDuration = 0f;

        switch (currentCombo)
        {
            case 0:
                attackAnimID = attack1AnimID;
                dashDistance = attack1DashDistance;
                dashDuration = attack2DashDuration;
                anim.SetTrigger("Attack1");
                //CinemachineShake.Instance.ShakeCamera(5f, .1f);
                //slowMo.TriggerSlowMotionTimed(0.3f, 5f, 1f); // 30% velocidade, suaviza com speed 5, dura 1 segundo
                //SlowMotion(0.1f, 0.1f); // desacelera para 30% por 0.5s

                rightToe.GetComponent<TrailRenderer>().emitting = false;
                rightHand.GetComponent<TrailRenderer>().emitting = true;
                leftHand.GetComponent<TrailRenderer>().emitting = false;
                break;

            case 1:
                attackAnimID = attack2AnimID;
                dashDistance = attack2DashDistance;
                dashDuration = attack2DashDuration;
                anim.SetTrigger("Attack2");
                //SlowMotion(0.1f, 0.2f);
                leftHand.GetComponent<TrailRenderer>().emitting = true;
                rightHand.GetComponent<TrailRenderer>().emitting = false;
                break;

            case 2:
                attackAnimID = attack3AnimID;
                anim.SetTrigger("Attack3");
                dashDistance = attack3DashDistance;
                dashDuration = attack3DashDuration;
                //SlowMotion(0.5f, 0.3f);
                leftHand.GetComponent<TrailRenderer>().emitting = false;
                rightToe.GetComponent<TrailRenderer>().emitting = true;
                break;
            case 3:
                attackAnimID = attack4AnimID;
                anim.SetTrigger("Attack4");
                dashDistance = attack3DashDistance;
                dashDuration = attack3DashDuration;
                DelayedSlowMotion(0.2f, 0.3f, 0.4f); // Slowmotion começa 0.2s depois
                leftHand.GetComponent<TrailRenderer>().emitting = true;
                rightToe.GetComponent<TrailRenderer>().emitting = true;
                break;
        }

        if (dashDistance > 0 && dashDuration > 0)
        {
            StartCoroutine(AttackDash(dashDistance, dashDuration));
        }

        comboTimer = 5f;
        StartCoroutine(ResetAttackState(GetAnimationDuration(attackAnimID)));
    }

    void Attack2()
    {
        isAttacking = true;
        anim.SetBool("isAttacking", true); // <-- Aqui ativamos

        attackTimer = attackCooldown;

        int attackAnimID = attack1AnimID;
        float dashDistance = 0f;
        float dashDuration = 0f;

        switch (currentCombo)
        {
            case 0:
                attackAnimID = 5;
                dashDistance = attack1DashDistance;
                dashDuration = attack2DashDuration;
                anim.SetTrigger("c2Attack1");
                //CinemachineShake.Instance.ShakeCamera(5f, .1f);
                //slowMo.TriggerSlowMotionTimed(0.3f, 5f, 1f); // 30% velocidade, suaviza com speed 5, dura 1 segundo
                //SlowMotion(0.1f, 0.1f); // desacelera para 30% por 0.5s

                rightToe.GetComponent<TrailRenderer>().emitting = false;
                rightHand.GetComponent<TrailRenderer>().emitting = true;
                leftHand.GetComponent<TrailRenderer>().emitting = false;
                break;

            case 1:
                attackAnimID = 6;
                dashDistance = attack2DashDistance;
                dashDuration = attack2DashDuration;
                anim.SetTrigger("c2Attack2");
                //SlowMotion(0.1f, 0.2f);
                leftHand.GetComponent<TrailRenderer>().emitting = true;
                rightHand.GetComponent<TrailRenderer>().emitting = false;
                break;

            case 2:
                attackAnimID = 7;
                anim.SetTrigger("c2Attack3");
                dashDistance = attack3DashDistance;
                dashDuration = attack3DashDuration;
                //SlowMotion(0.5f, 0.3f);
                leftHand.GetComponent<TrailRenderer>().emitting = false;
                rightToe.GetComponent<TrailRenderer>().emitting = true;
                break;
            case 3:
                attackAnimID = 8;
                anim.SetTrigger("Attack4");
                dashDistance = attack3DashDistance;
                dashDuration = attack3DashDuration;
                DelayedSlowMotion(0.2f, 0.3f, 0.4f); // Slowmotion começa 0.2s depois
                leftHand.GetComponent<TrailRenderer>().emitting = true;
                rightToe.GetComponent<TrailRenderer>().emitting = true;
                break;
        }

        if (dashDistance > 0 && dashDuration > 0)
        {
            StartCoroutine(AttackDash(dashDistance, dashDuration));
        }

        comboTimer = 5f;
        StartCoroutine(ResetAttackState(GetAnimationDuration(attackAnimID)));
    }

    float GetAnimationDuration(int attackID)
    {
        //combo 1
        if (attackID == attack1AnimID) return 0.4f;
        if (attackID == attack2AnimID) return 0.5f;
        if (attackID == attack3AnimID) return 0.6f;
        if (attackID == attack4AnimID) return 0.7f;

        //combo2
        if (attackID == 5) return 0.4f;
        if (attackID == 6) return 0.6f;
        if (attackID == 7) return 0.6f;
        if (attackID == 8) return 0.7f;
        return 0.4f;
    }

    IEnumerator ResetAttackState(float animationDuration)
    {

        // Se for o ataque 3, adiciona mais tempo antes de resetar
        if (currentCombo == 2)
        {
            animationDuration += 0.1f; // adiciona 0.5 segundos extras (ajuste como quiser)
        }

        if (currentCombo == 3)
        {
            animationDuration += 0.5f; // adiciona 0.5 segundos extras (ajuste como quiser)
        }

       

        yield return new WaitForSeconds(animationDuration);

        if (comboTimer > 0)
        {
            currentCombo = (currentCombo + 1) % 4;
        }
        else
        {
            currentCombo = 0;
        }

        if (bufferedAttack)
        {
            bufferedAttack = false;
            Attack(); // Executa próximo ataque automaticamente
        }
        else
        {
            isAttacking = false;
            anim.SetBool("isAttacking", false);
            Debug.Log("Estado de ataque resetado. Próximo combo: " + currentCombo);
        }
    }

    IEnumerator AttackDash(float distance, float duration)
    {
        float elapsed = 0f;
        Vector3 dashDirection = transform.forward;
        float speed = distance / duration;

        while (elapsed < duration)
        {
            float moveAmount = speed * Time.deltaTime;
            controller.Move(dashDirection * moveAmount);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void HandleCombo()
    {
        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0)
            {
                currentCombo = 0;
                Debug.Log("Janela de Combo Expirou. Resetando combo.");
            }
        }
    }


    // SlowMotion com atraso
    void DelayedSlowMotion(float delay, float slowAmount, float duration)
    {
        StartCoroutine(DoDelayedSlowMotion(delay, slowAmount, duration));
    }

    private IEnumerator DoDelayedSlowMotion(float delay, float slowAmount, float duration)
    {
        yield return new WaitForSecondsRealtime(delay); // espera antes de iniciar o slow

        Time.timeScale = slowAmount;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(duration); // dura o tempo necessário

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

}