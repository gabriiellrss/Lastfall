using UnityEngine;
using TMPro;

public class Gun : MonoBehaviour
{
    //bullet 
    public GameObject bullet;

    //bullet force
    public float shootForce, upwardForce;

    //Gun stats
    public float timeBetweenShooting, spread, reloadTime, timeBetweenShots;
    public int magazineSize, bulletsPerTap;
    public bool allowButtonHold;

    int bulletsLeft, bulletsShot;

    //Recoil
    public Rigidbody playerRb; // Considerar se ainda é necessário se o tiro é sempre para a frente do jogador
    public float recoilForce;

    //bools
    bool shooting, readyToShoot, reloading;

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

        // MODIFICADO: Direção do tiro é para a frente do attackPoint
        // Não usa mais raycast da câmara para determinar o ponto de mira inicial.
        Vector3 directionWithoutSpread = attackPoint.forward;

        // Calcular spread (dispersão)
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);
        // Adicionar o spread à direção. Nota: Adicionar spread em eixos locais da arma pode ser mais realista.
        // Esta implementação adiciona no espaço do mundo, o que pode ser aceitável.
        Vector3 directionWithSpread = directionWithoutSpread + new Vector3(x, y, 0);
        // Para um spread mais relativo à orientação da arma, poderia ser: 
        // Vector3 directionWithSpread = directionWithoutSpread + attackPoint.right * x + attackPoint.up * y;

        //Instantiate bullet/projectile
        GameObject currentBullet = Instantiate(bullet, attackPoint.position, /*Quaternion.LookRotation(directionWithSpread.normalized)*/ attackPoint.rotation);
        //Rotate bullet to shoot direction (já feito pelo Quaternion.LookRotation na instanciação)
        // currentBullet.transform.forward = directionWithSpread.normalized;

        //Add forces to bullet
        currentBullet.GetComponent<Rigidbody>().AddForce(directionWithSpread.normalized * shootForce, ForceMode.Impulse);
        // A força para cima (upwardForce) pode precisar de ajuste ou ser relativa à câmara se ainda desejado
        // Se for para compensar gravidade ou dar um arco, pode ser mantida. Se fpsCam não existe mais, usar Vector3.up ou attackPoint.up.
        if (upwardForce != 0)
        {
            currentBullet.GetComponent<Rigidbody>().AddForce(Vector3.up * upwardForce, ForceMode.Impulse); // Usando Vector3.up global
        }

        //Instantiate muzzle flash, if you have one
        if (muzzleFlash != null)
        {
            GameObject muzzleFlashInstance = Instantiate(muzzleFlash, attackPoint.position, attackPoint.rotation, attackPoint); // Instancia como filho do attackPoint
            Destroy(muzzleFlashInstance, muzzleFlashDuration); // MODIFICADO: Destrói o efeito após muzzleFlashDuration
        }

        bulletsLeft--;
        bulletsShot++;

        //Invoke resetShot function (if not already invoked), with your timeBetweenShooting
        if (allowInvoke)
        {
            Invoke("ResetShot", timeBetweenShooting);
            allowInvoke = false;

            //Add recoil to player (should only be called once)
            // A lógica de recoil pode precisar de revisão se playerRb e a direção baseada na câmara mudaram.
            // if (playerRb != null) playerRb.AddForce(-directionWithSpread.normalized * recoilForce, ForceMode.Impulse);
        }

        //if more than one bulletsPerTap make sure to repeat shoot function
        if (bulletsShot < bulletsPerTap && bulletsLeft > 0)
        {
            Invoke("Shoot", timeBetweenShots);
        }
        else if (bulletsLeft <= 0 && !reloading) // Se acabaram as balas e não está a recarregar, recarrega
        {
            Reload();
        }
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

