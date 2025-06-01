// Script C# para Projétil no Unity
// Autor: Manus
// Data: 01/06/2025

/*
 === COMO USAR ESTE SCRIPT ===

 1. Crie um Prefab para o seu projétil (ex: uma esfera, um modelo 3D de bala, etc.).
 2. Adicione este script ("Projectile.cs") como um componente a esse Prefab.
 3. Certifique-se que o Prefab tem:
    - Um componente Collider (ex: SphereCollider, BoxCollider). Marque a opção "Is Trigger" neste Collider.
    - Um componente Rigidbody. Desmarque a opção "Use Gravity" se não quiser que o projétil caia, e configure as constraints se necessário.
 4. Configure as variáveis públicas no Inspector do Prefab:
    - Speed: Velocidade de movimento do projétil.
    - Damage: Quantidade de dano que o projétil causa ao atingir o jogador.
    - Lifetime: Tempo em segundos que o projétil existirá antes de se autodestruir (para evitar acumular projéteis na cena).
    - Hit Effect Prefab (Opcional): Arraste um Prefab de efeito visual (ex: explosão, faíscas) para este campo, que será instanciado quando o projétil atingir algo.
 5. No script do Boss (BossController.cs), arraste este Prefab do projétil para o campo "Ranged Projectile Prefab".
 6. Certifique-se que o GameObject do Jogador tem a tag "Player" e um Collider para que a colisão seja detetada.

*/

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))] // Certifique-se que o collider é IsTrigger
public class Projectile : MonoBehaviour
{
    [Header("Atributos do Projétil")]
    public float speed = 15f;
    public float damage = 15f;
    public float lifetime = 5f; // Tempo de vida do projétil

    [Header("Efeitos (Opcional)")]
    public GameObject hitEffectPrefab; // Efeito visual ao atingir (opcional)

    // --- Variáveis Internas ---
    private Rigidbody rb;
    private Vector3 direction = Vector3.forward; // Direção padrão inicial

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Garante que a física não afete estranhamente (se não for projétil balístico)
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero; // Reseta velocidade inicial
        GetComponent<Collider>().isTrigger = true; // Garante que é trigger para OnTriggerEnter funcionar

        // Autodestruição após o tempo de vida
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        // Move o projétil usando Rigidbody para melhor deteção de colisão física
        // A velocidade é definida uma vez em SetDirection e mantida pelo Rigidbody
        // Se precisar de aceleração ou movimento mais complexo, ajuste aqui.
    }

    // Método para definir a direção e velocidade inicial (chamado pelo BossController)
    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;
        // Opcional: Rotacionar o projétil para encarar a direção do movimento
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        // Define a velocidade inicial do Rigidbody
        rb.linearVelocity = direction * speed;
    }

    // Chamado quando o Collider (marcado como Trigger) entra em contato com outro Collider
    void OnTriggerEnter(Collider other)
    {
        // Verifica se colidiu com o jogador pela tag
        if (other.CompareTag("Player"))
        {
            Debug.Log("Projétil atingiu o jogador!");
            // Tenta encontrar o script de vida do jogador para aplicar dano
            Player playerHealth = other.GetComponent<Player>(); // Supondo que o jogador tenha o script PlayerHealth
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            // Instancia o efeito de hit e destrói o projétil
            SpawnHitEffect();
            Destroy(gameObject);
        }
        // Verifica se colidiu com algo que não seja o próprio Boss, outro inimigo ou outro trigger
        // Ajuste as tags/layers conforme a necessidade do seu jogo
        else if (!other.CompareTag("Enemy") && !other.CompareTag("Boss") && !other.isTrigger)
        {
            Debug.Log($"Projétil atingiu {other.name}");
            // Instancia o efeito de hit e destrói o projétil ao colidir com cenário, etc.
            SpawnHitEffect();
            Destroy(gameObject);
        }
        // Nota: Se quiser que o projétil atravesse certos objetos, adicione mais condições aqui
        // ou use Layers e a matriz de colisão do Unity (Physics Settings).
    }

    // Instancia o efeito visual no local da colisão
    void SpawnHitEffect()
    {
        if (hitEffectPrefab != null)
        {
            // Instancia o efeito na posição atual do projétil, sem rotação específica
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
    }
}

