using UnityEngine;

public class Item : MonoBehaviour
{
    public GameObject UiItem;
    public Player playerScript;


    private void Start()
    {
        UiItem.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            UiItem.SetActive(true);
            playerScript.chave1 = true;
            Destroy(gameObject);
        }
    }
}
