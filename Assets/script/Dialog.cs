using UnityEngine;

public class Dialog : MonoBehaviour
{
    public GameObject pressKeyUIPrefab;
    public GameObject interactionCanvasPrefab;
    public Transform uiParent; // <- Onde os objetos serão instanciados

    public KeyCode interactionKey = KeyCode.E;
    public KeyCode closeKey = KeyCode.Escape;

    private GameObject currentPressKeyUI;
    private GameObject currentInteractionCanvas;

    private bool isPlayerInZone = false;
    private bool isCanvasOpen = false;

    void Update()
    {
        if (isPlayerInZone && Input.GetKeyDown(interactionKey))
        {
            if (currentInteractionCanvas == null)
            {
                currentInteractionCanvas = Instantiate(interactionCanvasPrefab, uiParent);
                isCanvasOpen = true;
            }

            if (currentPressKeyUI != null)
                Destroy(currentPressKeyUI);
        }

        if (isCanvasOpen && Input.GetKeyDown(closeKey))
        {
            if (currentInteractionCanvas != null)
                Destroy(currentInteractionCanvas);

            isCanvasOpen = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && currentPressKeyUI == null)
        {
            currentPressKeyUI = Instantiate(pressKeyUIPrefab, uiParent);
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
}
