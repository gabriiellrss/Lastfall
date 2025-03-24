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
    private bool canDoubleJump = false; // Adicionado para controle do pulo duplo

    private bool isAttacking = false;
    private int attackIndex = 1;
    private float comboTimer = 0f;
    public float comboResetTime = 1f; // Tempo para resetar o combo


    public Transform cameraTransform;

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
            if (isJumping)
            {
                isJumping = false;
            }

            verticalVelocity = -1f; // Mantém o personagem no chão
            canDoubleJump = true; // Reseta a habilidade de pulo duplo ao tocar o chão

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                Jump();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Backspace) && canDoubleJump) // Verifica se o pulo duplo pode ser usado
            {
                DoubleJump();
            }

            verticalVelocity -= gravity * Time.deltaTime;
        }

        moveDirection = new Vector3(move.x, verticalVelocity, move.z);
        controller.Move(moveDirection * Time.deltaTime);

        // Atualiza animação corretamente
        UpdateAnimation(isMoving, isRunning);

        // Rotação do personagem apenas se estiver se movendo
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

        if (isJumping)
        {
            anim.SetInteger("transition", isRunning ? 4 : 3);
        }
        else if (isMoving)
        {
            anim.SetInteger("transition", isRunning ? 2 : 1);
        }
        else
        {
            anim.SetInteger("transition", 0);
        }
    }

    void Jump()
    {
        isJumping = true;
        verticalVelocity = jumpForce; // Aplica a força do pulo imediatamente
        anim.SetInteger("transition", 3); // Define animação de pulo normal
    }

    void DoubleJump() // Função adicional para pulo duplo
    {
        isJumping = true;
        canDoubleJump = false; // Impede outro pulo duplo
        verticalVelocity = jumpForce; // Aplica a força do pulo duplo
        anim.SetInteger("transition", 3); // Define animação de pulo normal
    }

    void HandleAttack()
    {
        if (isAttacking)
        {
            comboTimer += Time.deltaTime;
            if (comboTimer > comboResetTime)
            {
                ResetCombo();
            }
        }

        if (Input.GetKeyDown(KeyCode.Space)) // Botão de ataque
        {
            if (!isAttacking)
            {
                attackIndex = 1;
                isAttacking = true;
                comboTimer = 0f;
                anim.SetInteger("attackIndex", attackIndex);
                anim.SetTrigger("attack"); // Ativa o primeiro ataque
            }
            else if (attackIndex < 3) // Limita o combo a 3 ataques
            {
                attackIndex++;
                comboTimer = 0f;
                anim.SetInteger("attackIndex", attackIndex);
                anim.SetTrigger("attack");
            }
        }

    }

    void ResetCombo()
    {
        isAttacking = false;
        attackIndex = 0;
        anim.SetInteger("attackIndex", 0);
    }

    public void EndAttack()
    {
        anim.ResetTrigger("attack"); // Reseta o trigger
        if (attackIndex >= 3) ResetCombo(); // Reseta o combo
    }

}
