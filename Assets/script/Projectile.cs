using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 30f;
    public float lifetime = 5f;
    private Vector3 targetPosition;
    private bool targetSet = false;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void SetTarget(Vector3 target)
    {
        targetPosition = target;
        transform.LookAt(targetPosition);
        targetSet = true;
    }

    void Update()
    {
        if (targetSet)
        {
            // Move na direção inicial (não é homing)
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Lógica de colisão (causar dano, efeito, etc.)
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            // Causa dano ao jogador (defina o valor)
            player.TakeDamage(10f); // Exemplo de dano
        }
        Debug.Log("Projectile hit: " + collision.gameObject.name);
        Destroy(gameObject); // Destroi ao colidir
    }
}

