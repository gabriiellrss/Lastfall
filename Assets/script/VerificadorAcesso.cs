using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VerificadorAcesso : MonoBehaviour
{
    public Player player; // Referência ao player
    public TextMeshProUGUI mensagemTexto; // Mensagem na tela
    public MonoBehaviour componenteParaAtivar; // Ex: porta
    public GameObject pressKeyUIPrefab; // Prefab com "Pressione E"
    public Transform uiParent; // Onde instanciar o prompt

    public KeyCode interactionKey = KeyCode.E;

    private GameObject currentPrompt;
    private bool isPlayerInZone = false;
    private bool acessoConcluido = false;
    private bool processando = false;

    private void Update()
    {
        if (isPlayerInZone && !acessoConcluido && !processando && Input.GetKeyDown(interactionKey))
        {
            StartCoroutine(ProcessarAcesso());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player.gameObject && !acessoConcluido)
        {
            isPlayerInZone = true;
            ShowPrompt("Pressione " + interactionKey.ToString());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == player.gameObject)
        {
            isPlayerInZone = false;
            HidePrompt();
        }
    }

    private IEnumerator ProcessarAcesso()
    {
        processando = true;
        mensagemTexto.text = "Processando...";
        yield return new WaitForSeconds(3f);

        if (player.chave1)
        {
            mensagemTexto.text = "Concluído: Porta liberada.";
            acessoConcluido = true;
            if (componenteParaAtivar != null)
            {
                componenteParaAtivar.enabled = true;
            }
            HidePrompt();
        }
        else
        {
            mensagemTexto.text = "Sem chave de acesso.";
            if (isPlayerInZone)
            {
                ShowPrompt("Pressione " + interactionKey.ToString());
            }
        }

        processando = false;
    }

    private void ShowPrompt(string text)
    {
        HidePrompt(); // Garante que não duplique
        if (pressKeyUIPrefab != null)
        {
            currentPrompt = Instantiate(pressKeyUIPrefab, uiParent != null ? uiParent : transform);
            TextMeshProUGUI tmp = currentPrompt.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }
    }

    private void HidePrompt()
    {
        if (currentPrompt != null)
        {
            Destroy(currentPrompt);
            currentPrompt = null;
        }
    }
}
