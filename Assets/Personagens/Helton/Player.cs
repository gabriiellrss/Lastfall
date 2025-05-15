using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; // Esta linha pode não ser necessária se estiver a usar Input.GetKey/GetButtonDown
using UnityEngine.UI;


public class Player : MonoBehaviour
{
    private CharacterController controller;
    private SlowMotionHandler slowMo;

    private Animator anim;

    public GameObject attackEffectPrefab;     // arrasta o prefab do efeito aqui no Inspector
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

    private float verticalVelocity;
    private Vector3 moveDirection;
    private bool isJumping = false;
    private bool canDoubleJump = false;

    public Transform cameraTransform;

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

    public int maxHealth = 100;
    private int currentHealth;

    public Gun gun;

    // Novas variáveis para o sistema de arma
    [Header("Weapon System")]
    public GameObject weaponObject; // Arraste o GameObject da sua arma aqui no Inspector
    private bool isWeaponActive = false;
    // A variável anim.GetBool("isShoot") pode ser usada para verificar o estado de "shoot"

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
        }
    }

    public void editBarHealth(float vidaAtual, float vidaMaxima)
    {
        barHealth.fillAmount = (float)vidaAtual / vidaMaxima;
    }

    void Update()
    {
        Move();
        HandleAttack(); // Considere se HandleAttack deve ser desabilitado quando isWeaponActive = true
        HandleCombo();  // Considere se HandleCombo deve ser desabilitado quando isWeaponActive = true
        UpdateAnimation();
        pose();
        HandleWeaponToggle(); // Lógica para ativar/desativar arma e modo de tiro
        
        // Se a arma estiver ativa (modo "isShoot"), você pode querer adicionar uma lógica de tiro aqui
        // Ex: if (isWeaponActive && anim.GetBool("isShoot") && Input.GetButtonDown("Fire1")) { Shoot(); }
    }

    void HandleWeaponToggle()
    {
        // Usaremos a tecla "G" para alternar a arma como exemplo.
        // Mude para o botão desejado (ex: Input.GetButtonDown("NomeDoSeuBotao")).
        if (Input.GetKeyDown(KeyCode.G)) 
        {
            isWeaponActive = !isWeaponActive; // Alterna o estado da arma

            if (weaponObject != null)
            {
                weaponObject.SetActive(isWeaponActive);
            }

            // Define o parâmetro "isShoot" no Animator.
            // Se a arma está ativa, isShoot = true. Se desativada, isShoot = false.
            if (anim != null) 
            {
                anim.SetBool("isShoot", isWeaponActive);
            }

            // Debug para verificar o estado
            // Debug.Log("Weapon Active: " + isWeaponActive + ", isShoot Animator: " + (anim != null ? anim.GetBool("isShoot").ToString() : "Animator not found"));

            // Se estiver a desativar a arma, pode ser necessário interromper outras ações
            // relacionadas ao modo de tiro, se houver.
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
        float horizontalInput = 0f;
        float verticalInput = 0f;
        bool isMoving = false;
        bool isRunning = false;
        float currentSpeed = 0f;
        Vector3 move = Vector3.zero;

        if (!isAttacking) 
        {
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");
            isMoving = horizontalInput != 0 || verticalInput != 0;
            isRunning = isMoving && (Input.GetKey(KeyCode.LeftShift) || Input.GetButton("Run"));
            currentSpeed = isRunning ? runSpeed : walkSpeed;

            if (cameraTransform != null)
            {
                Vector3 forward = cameraTransform.forward;
                Vector3 right = cameraTransform.right;

                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                move = (forward * verticalInput + right * horizontalInput).normalized * currentSpeed;
            }
            else
            {
                // Fallback ou aviso se cameraTransform não estiver definida
                move = (new Vector3(horizontalInput, 0, verticalInput)).normalized * currentSpeed;
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

        Vector3 horizontalVelocity = Vector3.zero;
        if (controller != null && controller.enabled) {
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
        // Considere: if (isWeaponActive) return; 
        
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Fire2")) 
        {
            if (!isAttacking && attackTimer <= 0) Attack();
            else bufferedAttack1 = true;
        }

        if (Input.GetButton ("Fire1")) 
        {
            if (isWeaponActive && anim.GetBool("isShoot")) 
            { /* Lógica de Tiro*/
                anim.SetTrigger("fire");
                gun.TryShoot();
            } else 
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
                // dashDuration = attack2DashDuration; // Parece que esta linha não é usada para o Attack1
                if (anim != null) anim.SetTrigger("Attack1");
                Demage();
                SetTrailRenderer(rightToe, false, Color.white, Color.white, 0.1f, 0f); // Exemplo de cores e larguras
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
                dashDistance = attack3DashDistance; // Reutiliza attack3DashDistance
                dashDuration = attack3DashDuration; // Reutiliza attack3DashDuration
                Demage();
                // DelayedSlowMotion(0.2f, 0.3f, 0.4f); 
                SetTrailRenderer(leftHand, true, Color.magenta, Color.cyan, 0.2f, 0f);
                SetTrailRenderer(rightToe, true, Color.green, Color.cyan, 0.2f, 0f);
                break;
        }

        if (dashDistance > 0 && dashDuration > 0)
        {
            //StartCoroutine(AttackDash(dashDistance, dashDuration));
        }

        comboTimer1 = 1f; 
        StartCoroutine(ResetAttackState(GetAnimationDuration(attackAnimIDToPlay), true)); 
    }

    void Attack2() 
    {
        isAttacking = true;
        if (anim != null) anim.SetBool("isAttacking", true); 

        attackTimer = attackCooldown;

        int attackAnimIDToPlay = attack5AnimID; // Usar IDs corretos para combo2
        float dashDistance = 0f;
        float dashDuration = 0f;

        switch (currentCombo2)
        {
            case 0:
                attackAnimIDToPlay = attack5AnimID; // Ex: c2Attack1
                dashDistance = attack1DashDistance;
                // dashDuration = attack2DashDuration;
                if (anim != null) anim.SetTrigger("c2Attack1");
                Demage();
                SetTrailRenderer(rightToe, false, Color.white, Color.white, 0.1f, 0f);
                SetTrailRenderer(rightHand, true, Color.red, Color.yellow, 0.2f, 0f);
                SetTrailRenderer(leftHand, true, Color.blue, Color.cyan, 0.2f, 0f);
                break;

            case 1:
                attackAnimIDToPlay = attack6AnimID; // Ex: c2Attack2
                dashDistance = attack2DashDistance;
                dashDuration = attack2DashDuration;
                if (anim != null) anim.SetTrigger("c2Attack2");
                Demage();
                SetTrailRenderer(leftHand, true, Color.blue, Color.cyan, 0.2f, 0f);
                SetTrailRenderer(rightHand, false, Color.white, Color.white, 0.1f, 0f);
                break;
            // Adicione case 2 e 3 para c2Attack3 e c2Attack4 se existirem no Animator e GetAnimationDuration
        }

        if (dashDistance > 0 && dashDuration > 0)
        {
            //StartCoroutine(AttackDash(dashDistance, dashDuration));
        }

        comboTimer2 = 6f; 
        StartCoroutine(ResetAttackState(GetAnimationDuration(attackAnimIDToPlay), false)); 
    }
    
    void Demage()
    {
        // Debug.Log("Damage Dealt (Placeholder)");
        if (areaTransform == null || enemylayer == 0) return;
        Collider[] hitEnemies = Physics.OverlapSphere(areaTransform.position, attackRadius, enemylayer);
        foreach (Collider enemy in hitEnemies)
        {
            // Supondo que o inimigo tem um script com o método TakeDamage
            // Ex: enemy.GetComponent<EnemyHealth>()?.TakeDamage(attackDemage);
            Debug.Log("Hit: " + enemy.name);
        }
    }

    void DelayedSlowMotion(float delay, float duration, float scale)
    {
        // if (slowMo != null) slowMo.TriggerSlowMotionTimed(scale, duration, delay); // Ajuste os parâmetros conforme a API do seu SlowMotionHandler
        Debug.Log("Delayed Slow Motion (Placeholder)");
    }
    
    float GetAnimationDuration(int attackID)
    {
        //combo 1
        if (attackID == attack1AnimID) return 0.4f;
        if (attackID == attack2AnimID) return 0.5f;
        if (attackID == attack3AnimID) return 0.6f;
        if (attackID == attack4AnimID) return 0.7f;

        //combo2
        if (attackID == attack5AnimID) return 0.4f; // c2Attack1
        if (attackID == attack6AnimID) return 0.6f; // c2Attack2
        // if (attackID == 7) return 0.6f; // c2Attack3 (attack7AnimID)
        // if (attackID == 8) return 0.7f; // c2Attack4 (attack8AnimID)
        return 0.4f; 
    }

    IEnumerator ResetAttackState(float animationDuration, bool isCombo1)
    {
        int currentCombo = isCombo1 ? currentCombo1 : currentCombo2;

        if(isCombo1 == true)
        {
            if (currentCombo == 2) // Attack3 do combo1
            {
                animationDuration += 0.1f; 
            }

            if (currentCombo == 3) // Attack4 do combo1
            {
                animationDuration += 0.5f; 
            }
        }
        // Adicionar lógica similar para isCombo2 se os ataques tiverem durações que precisam de ajuste

        yield return new WaitForSeconds(animationDuration);

        if (isCombo1)
        {
            if (comboTimer1 > 0)
                currentCombo1 = (currentCombo1 + 1) % 4; 
            else
                currentCombo1 = 0;
        }
        else 
        {
            // Ajuste o módulo conforme o número de ataques no combo2 (ex: % 2 se só tiver c2Attack1 e c2Attack2 implementados)
            int maxCombo2Attacks = 2; // Se tiver mais, aumente este número
            if (comboTimer2 > 0)
                currentCombo2 = (currentCombo2 + 1) % maxCombo2Attacks; 
            else
                currentCombo2 = 0;
        }

        if (isCombo1 && bufferedAttack1)
        {
            bufferedAttack1 = false;
            Attack(); 
        }
        else if (!isCombo1 && bufferedAttack2)
        {
            bufferedAttack2 = false;
            Attack2(); 
        }
        else
        {
            isAttacking = false;
            if (anim != null) anim.SetBool("isAttacking", false);
            // Debug.Log("Estado de ataque resetado.");
        }
    }

    IEnumerator AttackDash(float distance, float duration)
    {
        if (controller == null || !controller.enabled) yield break;
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
        if (comboTimer1 > 0)
        {
            comboTimer1 -= Time.deltaTime;
            if (comboTimer1 <= 0)
            {
                currentCombo1 = 0;
            }
        }

        if (comboTimer2 > 0)
        {
            comboTimer2 -= Time.deltaTime;
            if (comboTimer2 <= 0)
            {
                currentCombo2 = 0;
            }
        }
    }
}

