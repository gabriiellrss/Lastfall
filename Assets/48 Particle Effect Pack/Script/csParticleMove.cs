using UnityEngine;

public class csParticleMove : MonoBehaviour
{
    public float speed = 0.1f; // Você pode ajustar a velocidade aqui no Inspector

    void Update()
    {
        // Move o efeito para frente em sua própria direção local (eixo Z positivo local)
        // Para isso funcionar como esperado, o prefab do efeito deve ser rotacionado corretamente no momento da instanciação.
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}

