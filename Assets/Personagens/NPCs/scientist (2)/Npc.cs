using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class npc : MonoBehaviour
{
    [Header("UI")]
    public GameObject pressKeyUIPrefab;
    public GameObject interactionCanvasPrefab;
    public Transform uiParent;
    public GameObject uiToActivateAtEnd; // GameObject que será ligado no final

    [Header("Dialog")]
    [TextArea(3, 5)]
    public string[] dialogLines;
    public AudioClip[] voiceClips; // Clipes de voz para cada fala

    [Header("Keys")]
    public KeyCode interactionKey = KeyCode.E;

    [Header("NPC Animator")]
    private Animator npcAnimator;

    [Header("Objetos a excluir")]
    public GameObject objectToDestroy1;
    public GameObject objectToDestroy2;

    [Header("Cabeça usando IK")]
    public bool useIKHeadLook = true;
    public Transform lookAtTarget; // Normalmente o jogador
    public float lookWeight = 1f;


    private GameObject currentPressKeyUI;
    private GameObject currentInteractionCanvas;
    private TextMeshProUGUI dialogText;

    private bool isPlayerInZone = false;
    private bool isCanvasOpen = false;
    private bool isTyping = false;
    private int currentLineIndex = 0;
    private bool naoPodeMais = true;

    private Coroutine typingCoroutine;
    private AudioSource audioSource;

    private void Start()
    {
        npcAnimator = GetComponent<Animator>();
        audioSource = gameObject.AddComponent<AudioSource>();

    }

    void Update()
    {
        if (isPlayerInZone && Input.GetKeyDown(interactionKey) && naoPodeMais)
        {
            if (!isCanvasOpen)
            {
                OpenDialog();
            }
            else if (!isTyping && !audioSource.isPlaying)
            {
                NextLine();
            }
            // Jogador só pode pular texto se digitação já terminou e o áudio já acabou
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

        if (objectToDestroy1 != null) Destroy(objectToDestroy1);
        if (objectToDestroy2 != null) Destroy(objectToDestroy2);

        npcAnimator?.SetBool("isTalking", true);

        if (uiToActivateAtEnd != null)
            uiToActivateAtEnd.SetActive(false); // Desliga a UI no início

        ShowLine();
    }

    void CloseDialog()
    {
        if (currentInteractionCanvas != null)
            Destroy(currentInteractionCanvas);

        if (currentPressKeyUI != null)
            Destroy(currentPressKeyUI);

        npcAnimator?.SetBool("isTalking", false);

        isCanvasOpen = false;
        isTyping = false;
        naoPodeMais = false;



        // Ativar UI final
        if (uiToActivateAtEnd != null)
            uiToActivateAtEnd.SetActive(true);
    }

    void ShowLine()
    {
        if (currentLineIndex < dialogLines.Length)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeLine(dialogLines[currentLineIndex]));

            // Tocar áudio correspondente
            if (voiceClips != null && currentLineIndex < voiceClips.Length && voiceClips[currentLineIndex] != null)
            {
                audioSource.clip = voiceClips[currentLineIndex];
                audioSource.Play();
            }

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
            yield return new WaitForSeconds(0.03f);
        }

        isTyping = false;
    }

    // ?? Cabeça olha para o jogador durante diálogo
    private void OnAnimatorIK(int layerIndex)
    {
        if (npcAnimator == null || !useIKHeadLook || !isCanvasOpen || lookAtTarget == null)
            return;

        npcAnimator.SetLookAtWeight(lookWeight);
        npcAnimator.SetLookAtPosition(lookAtTarget.position);
    }

}
