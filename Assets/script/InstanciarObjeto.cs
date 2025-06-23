using UnityEngine;

public class InstanciarObjeto : MonoBehaviour
{
    public GameObject objetoParaInstanciar;
    public Transform localDeInstanciacao;

    public void Instanciar()
    {
        Instantiate(objetoParaInstanciar, localDeInstanciacao.position, Quaternion.identity);
    }
}
