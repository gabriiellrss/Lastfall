using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using static Unity.Burst.Intrinsics.X86.Avx;

public class npc : MonoBehaviour
{
    [Header("UI")]
    public GameObject pressKeyUIPrefab;
    public GameObject interactionCanvasPrefab;
    public Transform uiParent;

    [Header("Dialog")]
    [TextArea(3, 5)]
    public string[] dialogLines; // Linhas do diálogo

    [Header("Keys")]
    public KeyCode interactionKey = KeyCode.E;
    public KeyCode closeKey = KeyCode.Escape;

    [Header("NPC Animator")]
    private Animator npcAnimator;

    private GameObject currentPressKeyUI;
    private GameObject currentInteractionCanvas;
    private TextMeshProUGUI dialogText;

    private bool isPlayerInZone = false;
    private bool isCanvasOpen = false;
    private bool isTyping = false;
    private int currentLineIndex = 0;

    private Coroutine typingCoroutine;


    private void Start()
    {
        npcAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isPlayerInZone && Input.GetKeyDown(interactionKey))
        {
            if (!isCanvasOpen)
            {
                OpenDialog();
            }
            else if (!isTyping)
            {
                NextLine();
            }
            else
            {
                CompleteCurrentLine();
            }
        }

        if (isCanvasOpen && Input.GetKeyDown(closeKey))
        {
            CloseDialog();
        }


    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (currentPressKeyUI == null)
            {
                currentPressKeyUI = Instantiate(pressKeyUIPrefab, uiParent);

                TextMeshProUGUI tmp = currentPressKeyUI.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "Interagir";
                }
            }
            isPlayerInZone = true;
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (currentPressKeyUI != null)
                Destroy(currentPressKeyUI);

            if (isCanvasOpen)
                CloseDialog();

            isPlayerInZone = false;
        }
    }

    void OpenDialog()
    {
        currentInteractionCanvas = Instantiate(interactionCanvasPrefab, uiParent);
        dialogText = currentInteractionCanvas.GetComponentInChildren<TextMeshProUGUI>();

        currentLineIndex = 0;
        isCanvasOpen = true;

        npcAnimator?.SetBool("isTalking", true); // Ativa animação de fala


        ShowLine();
    }

    void CloseDialog()
    {
        if (currentInteractionCanvas != null)
            Destroy(currentInteractionCanvas);

        npcAnimator?.SetBool("isTalking", false); // Desativa animação de fala

        isCanvasOpen = false;
        isTyping = false;
    }

    void ShowLine()
    {
        if (currentLineIndex < dialogLines.Length)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeLine(dialogLines[currentLineIndex]));

            if (currentPressKeyUI != null)
            {
                TextMeshProUGUI tmp = currentPressKeyUI.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = (currentLineIndex == dialogLines.Length - 1) ? "Fechar" : "Avançar";
                }
            }
        }
    }


    void NextLine()
    {
        currentLineIndex++;

        if (currentLineIndex < dialogLines.Length)
        {
            ShowLine();
        }
        else
        {
            CloseDialog();
        }
    }

    void CompleteCurrentLine()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogText.text = dialogLines[currentLineIndex];
        isTyping = false;
    }

    IEnumerator TypeLine(string line)
    {
        dialogText.text = "";
        isTyping = true;

        foreach (char letter in line.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(0.03f); // Velocidade da digitação
        }

        isTyping = false;
    }
}
