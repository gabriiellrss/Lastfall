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
    private bool isJumping = false;
    private bool canDoubleJump = false;

    public Transform cameraTransform;

    private bool isAttacking = false;
    public float attackCooldown = 0.5f;
    private float attackTimer = 0f;

    private int currentCombo = 0;
    public float comboWindow = 1.0f;
    private float comboTimer = 0f;

    public int attack1AnimID = 1;
    public int attack2AnimID = 2;
    public int attack3AnimID = 3;

    private bool noChao = true;
    private float velocidade = 0f;
    private float attackVelocity = 0f;
    private bool h1 = false;
    private bool h2 = false;
    private bool h3 = false;

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
        if (!isAttacking)
        {
            Move();
        }
        HandleAttack();
        HandleCombo();
    }

    void Move()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        bool isMoving = horizontalInput != 0 || verticalInput != 0;
        bool isRunning = isMoving && Input.GetKey(KeyCode.LeftShift);

        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 move = (forward * verticalInput + right * horizontalInput).normalized * currentSpeed;

        if (controller.isGrounded)
        {
            noChao = true;
            if (isJumping)
                isJumping = false;

            verticalVelocity = -1f;
            canDoubleJump = true;

            if (Input.GetKeyDown(KeyCode.Backspace))
                Jump();
        }
        else
        {
            noChao = false;
            if (Input.GetKeyDown(KeyCode.Backspace) && canDoubleJump)
                DoubleJump();

            verticalVelocity -= gravity * Time.deltaTime;
        }

        moveDirection = new Vector3(move.x, verticalVelocity, move.z);
        controller.Move(moveDirection * Time.deltaTime);

        UpdateAnimation(isMoving, isRunning);

        if (isMoving)
        {
            Vector3 moveDirectionFlat = new Vector3(move.x, 0, move.z);
            if (moveDirectionFlat.magnitude > 0)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirectionFlat);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }
    }

    void UpdateAnimation(bool isMoving, bool isRunning)
    {
        if (anim == null) return;

        // Atualiza os parâmetros do animator
        anim.SetBool("noChao", noChao);
        float finalVelocity = isAttacking ? attackVelocity : (isMoving ? (isRunning ? 2f : 1f) : 0f);
        anim.SetFloat("velocidade", finalVelocity);

        // Ativação correta das animações de ataque
        anim.SetBool("h1", h1);
        anim.SetBool("h2", h2);
        anim.SetBool("h3", h3);
    }

    void Jump()
    {
        isJumping = true;
        verticalVelocity = jumpForce;
    }

    void DoubleJump()
    {
        isJumping = true;
        canDoubleJump = false;
        verticalVelocity = jumpForce;
    }

    void HandleAttack()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0) && !isAttacking)
        {
            Attack();
        }
    }

    void Attack()
    {
        isAttacking = true;
        attackVelocity = 0f;

        int attackAnimID = attack1AnimID;
        switch (currentCombo)
        {
            case 0: attackAnimID = attack1AnimID; h1 = true; h2 = false; h3 = false; break;
            case 1: attackAnimID = attack2AnimID; h1 = false; h2 = true; h3 = false; break;
            case 2: attackAnimID = attack3AnimID; h1 = false; h2 = false; h3 = true; break;
        }

        anim.SetBool("h1", h1);
        anim.SetBool("h2", h2);
        anim.SetBool("h3", h3);

        StartCoroutine(ResetAttackState(attackAnimID));
        StartCoroutine(AttackDash(1.5f, 0.2f));

        attackTimer = attackCooldown;
        comboTimer = comboWindow;
    }

    IEnumerator ResetAttackState(int attackAnimID)
    {
        float animationDuration = 0.7f;
        switch (attackAnimID)
        {
            case 1: animationDuration = 0.7f; break;
            case 2: animationDuration = 0.8f; break;
            case 3: animationDuration = 0.6f; verticalVelocity = jumpForce; break;
        }
        yield return new WaitForSeconds(animationDuration);

        isAttacking = false;
        attackVelocity = 0f;
        if (comboTimer > 0)
            currentCombo = (currentCombo + 1) % 3;
        else
            currentCombo = 0;

        h1 = h2 = h3 = false;
    }

    IEnumerator AttackDash(float distance, float duration)
    {
        float elapsed = 0f;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + transform.forward * distance;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void HandleCombo()
    {
        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
        }
        else
        {
            currentCombo = 0;
        }
    }
}