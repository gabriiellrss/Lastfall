using UnityEngine;

public class DamageArea : MonoBehaviour
{
    public float attackDamage = 10f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") && other.TryGetComponent(out Enemy enemy))
        {
            enemy.TakeDamage(attackDamage);
        }
    }
}
