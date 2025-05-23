using UnityEngine;
using TMPro;

public class Dialog : MonoBehaviour
{
    [Header("UI")]
    public GameObject pressKeyUIPrefab;       // Prefab do botão "Interagir"/"Fechar"
    public Transform uiParent;                // Onde o botão será instanciado
    public GameObject interactionCanvas;      // Canvas principal (fixo na cena, NÃO instanciado)

    [Header("Controles")]
    public KeyCode interactionKey = KeyCode.E;

    [Header("Bloqueios")]
    public MonoBehaviour playerScript;        // Script do player (ex.: movimento)
    public MonoBehaviour cameraScript;        // Script da câmera (ex.: MouseLook)

    private GameObject currentPressKeyUI;

    private bool isPlayerInZone = false;
    private bool isCanvasOpen = false;

    void Start()
    {
        if (interactionCanvas != null)
            interactionCanvas.SetActive(false);
    }

    void Update()
    {
        if (isPlayerInZone && Input.GetKeyDown(interactionKey))
        {
            if (!isCanvasOpen)
            {
                OpenCanvas();
                UpdateUIText(currentPressKeyUI, "Fechar");
            }
            else
            {
                CloseCanvas();
                UpdateUIText(currentPressKeyUI, "Interagir");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && currentPressKeyUI == null)
        {
            currentPressKeyUI = Instantiate(pressKeyUIPrefab, uiParent);

            UpdateUIText(currentPressKeyUI, isCanvasOpen ? "Fechar" : "Interagir");

            isPlayerInZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (currentPressKeyUI != null)
                Destroy(currentPressKeyUI);

            isPlayerInZone = false;
        }
    }

    // 🔸 Atualiza o texto do botão (TextMeshProUGUI dentro do prefab)
    private void UpdateUIText(GameObject uiObject, string text)
    {
        if (uiObject == null) return;

        TextMeshProUGUI tmp = uiObject.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = text;
    }

    // 🔸 Abre o Canvas de interação
    private void OpenCanvas()
    {
        if (interactionCanvas != null)
            interactionCanvas.SetActive(true);

        // Bloqueia controles do player e da câmera
        if (playerScript != null)
            playerScript.enabled = false;

        if (cameraScript != null)
            cameraScript.enabled = false;

        // Ativa o cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        isCanvasOpen = true;
    }

    // 🔸 Fecha o Canvas de interação
    private void CloseCanvas()
    {
        if (interactionCanvas != null)
            interactionCanvas.SetActive(false);

        // Libera controles do player e da câmera
        if (playerScript != null)
            playerScript.enabled = true;

        if (cameraScript != null)
            cameraScript.enabled = true;

        // Esconde o cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        isCanvasOpen = false;
    }
}
