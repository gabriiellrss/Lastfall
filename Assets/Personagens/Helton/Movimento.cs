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

    // Ataque
    private bool isAttacking = false;
    public float attackCooldown = 0.5f; // Tempo entre ataques
    private float attackTimer = 0f;

    // Sistema de Combo
    private int currentCombo = 0;
    public float comboWindow = 1.0f; // Tempo para continuar o combo
    private float comboTimer = 0f;

    // IDs das animações de ataque
    public int attack1AnimID = 1;
    public int attack2AnimID = 2;
    public int attack3AnimID = 3;

    private bool noChao = true;
    private float velocidade = 0f;
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
        Move();
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

        // Controle de gravidade e pulo
        if (controller.isGrounded)
        {
            noChao = true;
            if (isJumping)
            {
                isJumping = false;
            }

            verticalVelocity = -1f;
            canDoubleJump = true;

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                Jump();
            }
        }
        else
        {
            noChao = false;
            if (Input.GetKeyDown(KeyCode.Backspace) && canDoubleJump)
            {
                DoubleJump();
            }

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

        // REMOVIDO: if (isAttacking) return;

        // Atualiza os parametros do animator
        anim.SetBool("noChao", noChao);
        velocidade = isMoving ? (isRunning ? 2f : 1f) : 0f;
        anim.SetFloat("velocidade", velocidade);

        // Atualiza os parâmetros de ataque INDEPENDENTEMENTE se está atacando ou não
        // A lógica em Attack() e ResetAttackState() controla os valores de h1, h2, h3
        anim.SetBool("h1", h1);
        anim.SetBool("h2", h2);
        anim.SetBool("h3", h3);

        // Adicione este log para ter certeza que está sendo chamado com os valores corretos
        // Debug.Log($"UpdateAnimation - h1:{h1}, h2:{h2}, h3:{h3}");
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
            Debug.Log("Tentando atacar");
            Attack();
        }
    }

    void Attack()
    {
        isAttacking = true;

        // Determina qual animação de ataque usar com base no combo
        int attackAnimID = attack1AnimID; // Ataque padrão
        switch (currentCombo)
        {
            case 0:
                attackAnimID = attack1AnimID;
                h1 = true;
                h2 = false;
                h3 = false;
                break;
            case 1:
                attackAnimID = attack2AnimID;
                h1 = false;
                h2 = true;
                h3 = false;
                break;
            case 2:
                attackAnimID = attack3AnimID;
                h1 = false;
                h2 = false;
                h3 = true;
                break;
        }

        StartCoroutine(ResetAttackState(attackAnimID));  // Passa o ID da animação para a corrotina.

        attackTimer = attackCooldown;
        comboTimer = comboWindow; // Inicia a janela do combo.
    }

    IEnumerator ResetAttackState(int attackAnimID)
    {
        float animationDuration = 0.0f;
        switch (attackAnimID)
        {
            case 1:  //attack1AnimID
                animationDuration = 0.7f; // Duração da animação de ataque 1
                break;
            case 2:  //attack2AnimID
                animationDuration = 0.8f; // Duração da animação de ataque 2
                break;
            case 3:  //attack3AnimID
                animationDuration = 0.6f; // Duração da animação de ataque 3
                verticalVelocity = jumpForce;
                break;
            default:
                animationDuration = 0.7f;  // Duração padrão
                break;
        }
        yield return new WaitForSeconds(animationDuration);

        isAttacking = false;

        // Avança o combo ou reseta se a janela tiver expirado
        if (comboTimer > 0)
        {
            currentCombo = (currentCombo + 1) % 3; // Avança o combo (0 -> 1 -> 2 -> 0...)
        }
        else
        {
            currentCombo = 0;  // Reseta o combo
        }

        // Reseta os triggers de ataque
        h1 = false;
        h2 = false;
        h3 = false;
    }

    void HandleCombo()
    {
        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
        }
        else
        {
            currentCombo = 0; // Reseta o combo se a janela expirar.
        }
    }
}