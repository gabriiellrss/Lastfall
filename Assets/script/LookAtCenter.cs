using UnityEngine;

public class LookAtCenter : MonoBehaviour
{
    [Header("Alvo para olhar")]
    public Transform target; // Arraste um objeto aqui no inspetor

    void Update()
    {
        if (target != null)
        {
            // Faz o spot olhar para o alvo definido
            transform.LookAt(target);
        }
        else
        {
            // Se não tiver alvo, olha para o centro do mundo (0,0,0)
            transform.LookAt(Vector3.zero);
        }
    }
}
