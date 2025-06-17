using UnityEngine;
using TMPro;

public class UITriggerController : MonoBehaviour
{
    public GameObject pressKeyUIPrefab;
    public Transform uiParent;
    public GameObject infoCanvas;
    public Player playerScript;

    public KeyCode interactionKey = KeyCode.E;

    private GameObject currentPrompt;
    private bool isPlayerInZone = false;
    private bool isUIOpen = false;

    private enum UIState { Hidden, PromptOpen, PromptClose, UIVisible }
    private UIState currentState = UIState.Hidden;

    void Start()
    {
        if (infoCanvas != null) infoCanvas.SetActive(false);
    }

    void Update()
    {
        if (isPlayerInZone && Input.GetKeyDown(interactionKey))
        {
            ToggleUI();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            ShowPrompt(isUIOpen ? "Fechar" : "Abrir");
            currentState = isUIOpen ? UIState.PromptClose : UIState.PromptOpen;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            HidePrompt();
            currentState = UIState.Hidden;

            // ? Libera o jogador ao sair
            if (playerScript != null)
            {
                playerScript.StopPlayer(false);
            }

            // Fecha a UI se estiver aberta
            if (infoCanvas != null && isUIOpen)
            {
                infoCanvas.SetActive(false);
                isUIOpen = false;
            }
        }
    }

    private void ShowPrompt(string text)
    {
        HidePrompt();
        if (pressKeyUIPrefab != null)
        {
            currentPrompt = Instantiate(pressKeyUIPrefab, uiParent != null ? uiParent : transform);
            UpdateUIText(currentPrompt, text);
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

    private void ToggleUI()
    {
        isUIOpen = !isUIOpen;

        if (infoCanvas != null)
        {
            infoCanvas.SetActive(isUIOpen);
        }

        // ? Chama StopPlayer dependendo do estado
        if (playerScript != null)
        {
            playerScript.StopPlayer(isUIOpen);
        }

        if (isUIOpen)
        {
            currentState = UIState.UIVisible;
            ShowPrompt("Fechar");
        }
        else
        {
            currentState = UIState.PromptOpen;
            ShowPrompt("Abrir");
        }
    }

    private void UpdateUIText(GameObject uiObject, string text)
    {
        if (uiObject == null) return;
        TextMeshProUGUI tmp = uiObject.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = text;
        }
    }
}
