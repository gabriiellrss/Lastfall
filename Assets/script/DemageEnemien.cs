using UnityEngine;

public class DemageEnemien : MonoBehaviour
{

    [Header("Demage")]
    public LayerMask enemylayer;
    public float attackRadius = 3f;
    public float attackDemage = 10f;
    public Transform areaTransform;

    void Update()
    {
        Collider[] hitColliders = Physics.OverlapSphere(areaTransform.position, attackRadius, enemylayer);

        Debug.Log("attack demage");

        foreach (Collider collider in hitColliders)
        {

            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                Debug.Log("attack enemy");
                enemy.TakeDemage(attackDemage);
            }
        }

    }
}
