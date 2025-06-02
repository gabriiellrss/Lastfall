using UnityEngine;

public class DamageEnemy : MonoBehaviour
{
    [Header("Alvo e raio do ataque")]
    [Tooltip("Centro da área de ataque")]
    public Transform areaTransform;           
    [Min(0.1f)] public float attackRadius = 3f;

    [Header("Dano")]
    public float attackDamage = 10f;

    [Header("Layers que podem receber dano")]
    public LayerMask enemyLayer;               

    void Update()
    {
        if (Physics.CheckSphere(areaTransform.position, attackRadius, enemyLayer))
        {
            Damage();
        }
    }

    void Damage()
    {
        Collider[] hits = Physics.OverlapSphere(areaTransform.position, attackRadius, enemyLayer);  
        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out Enemy enemy))
            {
                enemy.TakeDamage(attackDamage);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!areaTransform) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(areaTransform.position, attackRadius);
    }
}
