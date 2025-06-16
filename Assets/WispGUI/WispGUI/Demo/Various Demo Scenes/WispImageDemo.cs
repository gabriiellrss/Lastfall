using UnityEngine;

public class WispImageDemo : MonoBehaviour
{
    // Torna as URLs visíveis no Inspector para editar no Unity
    public string[] imageUrls;

    void Start()
    {
        // Se não houver URLs definidas, evita erro
        if (imageUrls == null || imageUrls.Length == 0)
        {
            Debug.LogWarning("Nenhuma URL definida no WispImageDemo.");
            return;
        }

        // Sorteia uma imagem aleatória da lista
        int index = Random.Range(0, imageUrls.Length);
        string selectedImageUrl = imageUrls[index];

        // Define a imagem sorteada no componente WispImage
        GetComponent<WispImage>().SetValue(selectedImageUrl);
    }
}

