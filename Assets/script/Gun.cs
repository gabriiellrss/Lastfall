using UnityEngine;
using TMPro;
using Unity.Cinemachine;

public class Gun : MonoBehaviour
{
    //bullet 
    public GameObject bullet;

    //bullet force
    public float shootForce, upwardForce;
    // ↙️  ADICIONE estas duas linhas
    public Camera cam;        // arraste a câmera do jogador aqui
    public float maxRayDistance = 1000f;   // alcance para o raycast da mira


    //Gun stats
    public float timeBetweenShooting, spread, reloadTime, timeBetweenShots;
    public int magazineSize, bulletsPerTap;
    public bool allowButtonHold;

    int bulletsLeft, bulletsShot;

    //Recoil
    public Rigidbody playerRb; // Considerar se ainda é necessário se o tiro é sempre para a frente do jogador
    public float recoilForce;

    //bools
    bool shooting, reloading;

    public bool readyToShoot;

    //Reference
    // public Camera fpsCam; // Comentado/Removido: Não será usado para a direção do tiro base
    public Transform attackPoint; // Ponto de onde o projétil é disparado e que define a direção frontal

    //Graphics
    public GameObject muzzleFlash; // Prefab do efeito de muzzle flash
    public float muzzleFlashDuration = 0.1f; // Duração do efeito de muzzle flash
    public TextMeshProUGUI ammunitionDisplay;

    //bug fixing :D
    public bool allowInvoke = true;

    private void Awake()
    {
        //make sure magazine is full
        bulletsLeft = magazineSize;
        readyToShoot = true;
    }

    public void TryShoot()
    {
        // Chamado pelo Player.cs
        // Verifica se pode disparar (pronto, não a recarregar, com balas)
        if (readyToShoot && !reloading && bulletsLeft > 0)
        {
            bulletsShot = 0; // Reseta a contagem de balas por rajada
            Shoot();
        }
        // Se tentar disparar sem balas e não estiver a recarregar, inicia o reload automaticamente
        else if (readyToShoot && !reloading && bulletsLeft <= 0)
        {
            Reload();
        }
    }

    public void Start()
    {
        ammunitionDisplay.SetText(bulletsLeft / bulletsPerTap + " / " + magazineSize / bulletsPerTap);

    }

    private void Update()
    {
        // A lógica de input foi movida para Player.cs para melhor controlo centralizado
        // MyInput(); 

        //Set ammo display, if it exists :D
        if (ammunitionDisplay != null)
            ammunitionDisplay.SetText(bulletsLeft / bulletsPerTap + " / " + magazineSize / bulletsPerTap);

        // Recarregar com a tecla R (pode ser mantido aqui ou movido para Player.cs se preferir centralizar inputs)
        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !reloading)
        {
            Reload();
        }
    }

    // MyInput() foi removido pois o Player.cs agora chama TryShoot()
    // private void MyInput()
    // {
    //     // ...
    // }

    private void Shoot()
    {
        readyToShoot = false;

        // ────────── 1. Raycast do centro da câmera ──────────
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance)
                            ? hit.point
                            : ray.GetPoint(maxRayDistance);

        // Direção base (sem spread)
        Vector3 dir = (targetPoint - attackPoint.position).normalized;

        // ────────── 2. Spread (opcional) ──────────
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);
        dir = (dir + attackPoint.right * x + attackPoint.up * y).normalized;

        CinemachineShake.Instance.ShakeCamera(5f, .1f);

        // ────────── 3. Instancia o projétil ──────────
        GameObject go = Instantiate(bullet, attackPoint.position, Quaternion.LookRotation(dir));

        // ────────── 4. Dá velocidade para a bala ──────────
        Rigidbody rb = go.GetComponent<Rigidbody>();
        rb.AddForce(dir * shootForce, ForceMode.Impulse);   //  ← sinal trocado                                                             // shootForce = velocidade em m/s
        if (upwardForce != 0)                     // se quiser compensação vertical extra
            rb.linearVelocity += Vector3.up * upwardForce;

        // ────────── 5. Muzzle flash ──────────
        if (muzzleFlash != null)
        {
            GameObject flash = Instantiate(muzzleFlash, attackPoint.position, attackPoint.rotation, attackPoint);
            Destroy(flash, muzzleFlashDuration);
        }

        // ────────── 6. Contadores e rajada ──────────
        bulletsLeft--;
        bulletsShot++;

        if (allowInvoke)
        {
            Invoke(nameof(ResetShot), timeBetweenShooting);
            allowInvoke = false;
        }

        if (bulletsShot < bulletsPerTap && bulletsLeft > 0)
            Invoke(nameof(Shoot), timeBetweenShots);
        else if (bulletsLeft <= 0 && !reloading)
            Reload();
    }



    private void ResetShot()
    {
        //Allow shooting and invoking again
        readyToShoot = true;
        allowInvoke = true;
    }

    private void Reload()
    {
        if (reloading) return; // Evita múltiplas chamadas de Reload
        reloading = true;
        // Debug.Log("Reloading..."); // Opcional: para feedback
        Invoke("ReloadFinished", reloadTime); //Invoke ReloadFinished function with your reloadTime as delay
    }

    private void ReloadFinished()
    {
        //Fill magazine
        bulletsLeft = magazineSize;
        reloading = false;
        readyToShoot = true; // Garante que pode disparar após recarregar
        // Debug.Log("Reload Finished!"); // Opcional: para feedback
    }
}

