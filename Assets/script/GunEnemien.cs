// Script C# para Arma (Gun) - Adaptado para uso por IA (Boss) e Jogador
// Autor: Manus (baseado no script do usuário)
// Data: 01/06/2025

/*
 === COMO USAR ESTE SCRIPT ===

 1. Adicione este script a um GameObject que representa a arma (pode ser filho do Jogador ou do Boss).
 2. Configure as variáveis públicas no Inspector:
    - Bullet: Prefab do projétil a ser disparado.
    - Shoot Force / Upward Force: Forças aplicadas ao projétil.
    - Cam (PARA JOGADOR): Arraste a câmera principal do jogador AQUI se a arma for usada pelo jogador (para mira via Raycast).
    - Max Ray Distance (PARA JOGADOR): Alcance do Raycast da câmera do jogador.
    - Gun Stats: Configure tempo entre disparos, spread, recarga, tamanho do pente, balas por toque, etc.
    - Attack Point: Transform que indica de onde o projétil sai e a direção base do tiro.
    - Muzzle Flash (Opcional): Prefab do efeito visual de disparo.
    - Audio Source / Shot Clip (Opcional): Para som de disparo.
    - Ammunition Display (PARA JOGADOR): Referência ao TextMeshProUGUI para mostrar munição, se usado pelo jogador.

 3. USO PELO JOGADOR:
    - Chame o método `TryShootPlayer()` a partir do script do jogador quando o botão de tiro for pressionado.
    - A mira será feita com Raycast a partir da `cam` definida.
    - O jogador pode chamar `TryReload()` para recarregar.

 4. USO PELA IA (BOSS):
    - Chame o método `TryShootAI(Vector3 targetPosition)` a partir do script do Boss (ex: BossController.cs).
    - Passe a posição do alvo (ex: `player.position`) como argumento.
    - A mira será calculada diretamente do `attackPoint` para o `targetPosition`.
    - A IA pode chamar `TryReload()` para recarregar.
    - A variável `cam` NÃO é usada pela IA.

 5. O projétil (Prefab `bullet`) deve ter um Rigidbody e um Collider. Pode ter um script próprio para gerir dano e colisões.

*/

using UnityEngine;
using TMPro;
using Unity.Cinemachine; // Se ainda usar CinemachineShake
using System.Collections; // Para muzzle flash

public class GunEnemien : MonoBehaviour
{
    [Header("Referências e Configuração Base")]
    public GameObject bullet; // Prefab do projétil
    public Transform attackPoint; // Ponto de onde o projétil é disparado
    public AudioSource audioSource; // Componente para tocar som
    public AudioClip shotClip; // Som do disparo

    [Header("Força e Direção do Projétil")]
    public float shootForce = 50f;
    public float upwardForce = 0f;

    [Header("Mira (Apenas para Jogador)")]
    public Camera cam; // Câmera do jogador para Raycast (IGNORADO PELA IA)
    public float maxRayDistance = 1000f; // Alcance do Raycast (IGNORADO PELA IA)

    [Header("Atributos da Arma")]
    public float timeBetweenShooting = 0.5f; // Tempo mínimo entre tentativas de disparo (rajada/single)
    public float spread = 0.05f; // Imprecisão do tiro
    public float reloadTime = 1.5f;
    public float timeBetweenShots = 0.1f; // Tempo entre balas numa rajada (se bulletsPerTap > 1)
    public int magazineSize = 30;
    public int bulletsPerTap = 1; // Quantas balas dispara por chamada de TryShoot
    public bool allowButtonHold = true; // Relevante para jogador, IA controla chamadas

    [Header("Munição e Estado")]
    public int bulletsLeft;
    private int bulletsShot; // Balas disparadas na rajada atual
    public bool readyToShoot = true;
    private bool reloading = false;
    private bool shooting = false; // Indica se uma sequência de tiro (rajada) está ativa

    [Header("Efeitos Visuais")]
    public GameObject muzzleFlash; // Prefab do efeito de muzzle flash
    public float muzzleFlashDuration = 0.05f; // Duração do efeito
    public float cameraShakeIntensity = 5f; // Intensidade do shake (se usar Cinemachine)
    public float cameraShakeTime = 0.1f; // Duração do shake (se usar Cinemachine)

    [Header("UI (Apenas para Jogador)")]
    public TextMeshProUGUI ammunitionDisplay;

    // --- Variáveis Internas ---
    // public Rigidbody playerRb; // Removido/Comentado - Recoil deve ser tratado pelo portador da arma (Player/Boss)
    // public float recoilForce; // Removido/Comentado
    private Coroutine reloadCoroutine; // Para garantir que a recarga não seja interrompida incorretamente
    private Coroutine muzzleFlashCoroutine; // Para controlar o muzzle flash

    void Awake()
    {
        bulletsLeft = magazineSize;
        readyToShoot = true;
        reloading = false;
        shooting = false;

        if (attackPoint == null) attackPoint = transform; // Usa o próprio transform se não definido
        if (audioSource == null) audioSource = GetComponent<AudioSource>(); // Tenta pegar AudioSource
    }

    void Update()
    {
        // Atualiza a UI de munição se configurada (relevante para jogador)
        if (ammunitionDisplay != null)
        {
            UpdateAmmoDisplay();
        }

        // Lógica de input de recarga (exemplo para jogador, pode ser movida)
        // if (Input.GetKeyDown(KeyCode.R)) // Exemplo
        // {
        //     TryReload();
        // }
    }

    // --- Métodos Públicos para Controle Externo (Jogador/IA) ---

    /// <summary>
    /// Tenta iniciar um disparo usando a mira da câmera (para Jogador).
    /// </summary>
    public void TryShootPlayer()
    {
        if (!readyToShoot || reloading || bulletsLeft <= 0 || shooting)
            return;

        // Calcula a direção usando a câmera
        if (cam == null)
        {
            Debug.LogError("Gun: Câmera não definida para TryShootPlayer!");
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance)
                            ? hit.point
                            : ray.GetPoint(maxRayDistance);

        Vector3 direction = (targetPoint - attackPoint.position).normalized;

        // Inicia a sequência de tiro
        StartShootingSequence(direction);
    }

    /// <summary>
    /// Tenta iniciar um disparo mirando numa posição específica (para IA).
    /// </summary>
    /// <param name="targetPosition">A posição no mundo para onde mirar.</param>
    public void TryShootAI(Vector3 targetPosition)
    {
        if (!readyToShoot || reloading || bulletsLeft <= 0 || shooting)
            return;

        // Calcula a direção diretamente para o alvo
        Vector3 direction = (targetPosition - attackPoint.position).normalized;

        // Inicia a sequência de tiro
        StartShootingSequence(direction);
    }

    /// <summary>
    /// Tenta iniciar a recarga da arma.
    /// </summary>
    public void TryReload()
    {
        if (bulletsLeft < magazineSize && !reloading)
        {
            Reload();
        }
    }

    // --- Lógica Interna de Disparo e Recarga ---

    private void StartShootingSequence(Vector3 initialDirection)
    {
        shooting = true; // Marca que uma sequência de tiro começou
        readyToShoot = false;
        bulletsShot = 0;
        ExecuteShot(initialDirection); // Dispara a primeira bala

        // Agenda o reset para permitir nova sequência de tiro após timeBetweenShooting
        Invoke(nameof(ResetShootingSequence), timeBetweenShooting);
    }

    /// <summary>
    /// Executa a lógica de um único disparo (instancia bala, aplica força, efeitos).
    /// </summary>
    /// <param name="baseDirection">A direção base do tiro antes do spread.</param>
    private void ExecuteShot(Vector3 baseDirection)
    {
        if (bulletsLeft <= 0 || reloading)
        {
            shooting = false; // Interrompe a sequência se ficar sem balas ou começar a recarregar
            return;
        }

        // 1. Calcular Spread
        float spreadX = Random.Range(-spread, spread);
        float spreadY = Random.Range(-spread, spread);
        Vector3 directionWithSpread = (baseDirection + attackPoint.right * spreadX + attackPoint.up * spreadY).normalized;

        // 2. Instanciar Projétil
        GameObject currentBullet = Instantiate(bullet, attackPoint.position, Quaternion.LookRotation(directionWithSpread));

        // 3. Aplicar Força
        Rigidbody bulletRb = currentBullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.AddForce(directionWithSpread * shootForce, ForceMode.Impulse);
            if (upwardForce != 0)
            {
                bulletRb.AddForce(Vector3.up * upwardForce, ForceMode.Impulse); // Usa Impulse aqui também
            }
        }
        else
        {
            Debug.LogWarning("Prefab do projétil não tem Rigidbody!");
        }

        // 4. Efeitos Visuais e Sonoros
        ShowMuzzleFlash();
        PlayShotSound();
        TriggerCameraShake(); // Se usar Cinemachine

        // 5. Contagem de Munição
        bulletsLeft--;
        bulletsShot++;
        if (ammunitionDisplay != null) UpdateAmmoDisplay();

        // 6. Lógica de Rajada (se bulletsPerTap > 1)
        if (bulletsShot < bulletsPerTap && bulletsLeft > 0)
        {
            // Dispara a próxima bala da rajada após timeBetweenShots
            Invoke(nameof(ExecuteShotInBurst), timeBetweenShots);
        }
        else
        {
            // Se a rajada terminou (ou era tiro único) e ficou sem balas, tenta recarregar
            if (bulletsLeft <= 0)
            {
                TryReload();
            }
        }
    }

    // Função auxiliar para chamar ExecuteShot com a mesma direção base durante uma rajada
    private void ExecuteShotInBurst()
    {
        // Precisa recalcular a direção base caso o alvo tenha se movido significativamente
        // Se for para IA, poderia pegar a posição atualizada do player
        // Se for para Player, poderia refazer o Raycast
        // Para simplificar, vamos reusar a última direção calculada (pode causar imprecisão em rajadas longas)
        // Uma solução melhor seria passar a referência do alvo ou refazer o cálculo de direção aqui.

        // Exemplo simples: Reusa a direção do attackPoint.forward (menos preciso)
        // Vector3 currentDirection = attackPoint.forward;

        // Exemplo (requer referência ao alvo ou recálculo):
        // Vector3 targetPos = GetCurrentTargetPosition(); // Função hipotética
        // Vector3 currentDirection = (targetPos - attackPoint.position).normalized;

        // Vamos usar a direção forward do ponto de ataque como fallback simples
        ExecuteShot(attackPoint.forward);
    }


    /// <summary>
    /// Reseta o estado para permitir uma nova sequência de tiro.
    /// </summary>
    private void ResetShootingSequence()
    {
        readyToShoot = true;
        shooting = false; // Permite iniciar nova sequência
    }

    private void Reload()
    {
        reloading = true;
        Debug.Log(gameObject.name + " reloading...");
        // Cancela qualquer recarga anterior para evitar problemas
        if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
        reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        // Adicionar animação de recarga aqui, se houver
        // animator.SetTrigger("Reload");

        yield return new WaitForSeconds(reloadTime);
        ReloadFinished();
    }

    private void ReloadFinished()
    {
        bulletsLeft = magazineSize;
        reloading = false;
        readyToShoot = true; // Garante que pode disparar após recarregar
        reloadCoroutine = null;
        if (ammunitionDisplay != null) UpdateAmmoDisplay();
        Debug.Log(gameObject.name + " reload finished!");
    }

    // --- Funções Auxiliares para Efeitos ---

    private void ShowMuzzleFlash()
    {
        if (muzzleFlash != null)
        {
            // Garante que não haja múltiplos flashes sobrepostos rapidamente
            if (muzzleFlashCoroutine != null) StopCoroutine(muzzleFlashCoroutine);
            muzzleFlashCoroutine = StartCoroutine(MuzzleFlashRoutine());
        }
    }

    private IEnumerator MuzzleFlashRoutine()
    {
        GameObject flashInstance = Instantiate(muzzleFlash, attackPoint.position, attackPoint.rotation, attackPoint); // Filho do attackPoint
        yield return new WaitForSeconds(muzzleFlashDuration);
        if (flashInstance != null) Destroy(flashInstance);
        muzzleFlashCoroutine = null;
    }

    private void PlayShotSound()
    {
        if (audioSource != null && shotClip != null)
        {
            audioSource.PlayOneShot(shotClip);
        }
    }

    private void TriggerCameraShake()
    {
        // Verifica se a instância do CinemachineShake existe antes de chamar
        if (CinemachineShake.Instance != null)
        {
            CinemachineShake.Instance.ShakeCamera(cameraShakeIntensity, cameraShakeTime);
        }
    }

    private void UpdateAmmoDisplay()
    {
        if (reloading)
        {
            ammunitionDisplay.SetText("RECARREGANDO");
        }
        else
        {
            // Mostra balas restantes / tamanho do pente (considerando balas por toque para UI)
            // Se bulletsPerTap for 1, mostra direto. Se for > 1, pode mostrar rajadas restantes.
            // Exemplo simples mostrando balas individuais:
            ammunitionDisplay.SetText(bulletsLeft + " / " + magazineSize);
            // Exemplo mostrando "rajadas":
            // ammunitionDisplay.SetText(bulletsLeft / bulletsPerTap + " / " + magazineSize / bulletsPerTap);
        }
    }

    // --- Propriedades Públicas (Opcional) ---
    public bool IsReloading => reloading;
    public int CurrentAmmo => bulletsLeft;
    public int MaxAmmo => magazineSize;

}

