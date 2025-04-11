using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    private CharacterController controller;
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
            bool isRunning = Input.GetButton("Fire1");
            if (currentHorizontalSpeed > 0.1f)
            {
                animatorSpeed = isRunning ? 2f : 1f;
            }
        }

        anim.SetFloat("velocidade", animatorSpeed);
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
        anim.SetTrigger("Pular");
    }

    void HandleAttack()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }

        // 🔁 Agora registra o clique mesmo durante ataque
        if (Input.GetButtonDown("Fire3"))
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
    }

    void Attack()
    {
        isAttacking = true;
        //GameObject effect = Instantiate(attackEffectPrefab, effectSpawnPoint.position, effectSpawnPoint.rotation);
        //Destroy(effect, 1.5f);

        //wwwwwwwanim.applyRootMotion = true;

        //GameObject effecthand = Instantiate(attackEffectPrefab, rightHand.position, rightHand.rotation);
        //Destroy(effecthand, 1.5f); // Destroi o efeito após 1.5 segundos


        attackTimer = attackCooldown;

        int attackAnimID = attack1AnimID;
        float dashDistance = 0f;
        float dashDuration = 0f;
        bool performJump = false;

        switch (currentCombo)
        {
            case 0:
                attackAnimID = attack1AnimID;
                dashDistance = attack1DashDistance;
                dashDuration = attack2DashDuration;
                anim.SetTrigger("Attack1");
                rightHand.GetComponent<TrailRenderer>().emitting = true;
                leftHand.GetComponent<TrailRenderer>().emitting = false;
                break;

            case 1:
                attackAnimID = attack2AnimID;
                dashDistance = attack2DashDistance;
                dashDuration = attack2DashDuration;
                anim.SetTrigger("Attack2");
                leftHand.GetComponent<TrailRenderer>().emitting = true;
                rightHand.GetComponent<TrailRenderer>().emitting = false;
                break;

            case 2:
                attackAnimID = attack3AnimID;
                anim.SetTrigger("Attack3");
                dashDistance = attack3DashDistance;
                dashDuration = attack3DashDuration;
                performJump = controller.isGrounded;
                leftHand.GetComponent<TrailRenderer>().emitting = false;
                break;
        }

        if (dashDistance > 0 && dashDuration > 0)
        {
            StartCoroutine(AttackDash(dashDistance, dashDuration));
        }

        if (performJump)
        {
            verticalVelocity = jumpForce;
            isJumping = true;
            canDoubleJump = false;
            noChao = false;
        }

        comboTimer = comboWindow;
        StartCoroutine(ResetAttackState(GetAnimationDuration(attackAnimID)));

    }

    float GetAnimationDuration(int attackID)
    {
        if (attackID == attack1AnimID) return 0.4f;
        if (attackID == attack2AnimID) return 0.5f;
        if (attackID == attack3AnimID) return 0.6f;
        return 0.3f;
    }

    IEnumerator ResetAttackState(float animationDuration)
    {
        yield return new WaitForSeconds(animationDuration);

        if (comboTimer > 0)
        {
            currentCombo = (currentCombo + 1) % 3;
        }
        else
        {
            currentCombo = 0;
        }

        // 🔁 Se o jogador clicou durante o ataque, já começa o próximo
        if (bufferedAttack)
        {
            bufferedAttack = false;
            Attack(); // Executa próximo ataque automaticamente
        }
        else
        {
            isAttacking = false;
            anim.applyRootMotion = false;
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
}
