using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    private CharacterController controller;
    private Animator anim;

    public float walkSpeed = 4.5f;
    public float runSpeed = 7.5f;
    public float sprintSpeed = 10f;
    public float jumpForce = 7f;
    public float gravity = 9.81f;

    private float verticalVelocity;
    private Vector3 moveDirection;
    private bool isJumping = false;

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
    }

    void Move()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        bool isSprinting = Input.GetKey(KeyCode.LeftControl) && isRunning;

        float currentSpeed = isSprinting ? sprintSpeed : (isRunning ? runSpeed : walkSpeed);

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 move = (forward * verticalInput + right * horizontalInput).normalized * currentSpeed;

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0)
                verticalVelocity = -2f;

            if (Input.GetKeyDown(KeyCode.Space) && !isJumping)
            {
                StartCoroutine(JumpSequence(isRunning || isSprinting));
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        moveDirection = new Vector3(move.x, verticalVelocity, move.z);
        controller.Move(moveDirection * Time.deltaTime);

        // Animações (se o Animator existir)
        if (anim != null && controller.isGrounded && !isJumping)
        {
            if (horizontalInput != 0 || verticalInput != 0)
            {
                int animationState = GetMovementAnimation(horizontalInput, verticalInput, isRunning, isSprinting);
                anim.SetInteger("transition", animationState);

                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveDirection.x, 0, moveDirection.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
            else
            {
                anim.SetInteger("transition", 0); // Idle
            }
        }
    }

    int GetMovementAnimation(float horizontal, float vertical, bool running, bool sprinting)
    {
        if (sprinting)
        {
            if (vertical > 0 && horizontal == 0) return 6;
            if (vertical > 0 && horizontal < 0) return 7;
            if (vertical > 0 && horizontal > 0) return 8;
        }
        else if (running)
        {
            if (vertical > 0 && horizontal == 0) return 3;
            if (vertical > 0 && horizontal < 0) return 4;
            if (vertical > 0 && horizontal > 0) return 5;
        }
        else
        {
            if (vertical > 0 && horizontal == 0) return 1;
            if (vertical > 0 && horizontal < 0) return 2;
            if (vertical > 0 && horizontal > 0) return 2;
        }

        return 0;
    }

    IEnumerator JumpSequence(bool isRunning)
    {
        isJumping = true;

        if (anim != null)
            anim.SetInteger("transition", isRunning ? 9 : 2);

        yield return new WaitForSeconds(0.3f);

        verticalVelocity = jumpForce;

        yield return new WaitForSeconds(0.3f);

        isJumping = false;

        if (controller.isGrounded && anim != null)
        {
            anim.SetInteger("transition", 0);
        }
    }
}
