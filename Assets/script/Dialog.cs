using UnityEngine;
using TMPro;
using System.Collections;

public class Dialog : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject pressKeyUIPrefab;
    public Transform uiParent;
    public GameObject interactionCanvas;
    public GameObject loginCanvas;

    [Header("Login Settings")]
    public string senhaCorreta = "1234";
    public TMP_InputField inputSenha;
    public TextMeshProUGUI feedbackTexto;
    public float welcomeMessageDuration = 3.0f;
    public float typewriterSpeed = 0.05f;

    [Header("Controls")]
    public KeyCode interactionKey = KeyCode.E;

    [Header("Player/Camera Control")]
    public Player playerScript;
    public MonoBehaviour cameraScript;

    private GameObject currentPressKeyUIInstance;
    private bool isPlayerInZone = false;
    private bool isLoginVerified = false;
    private enum UIState { Hidden, PromptInteractVisible, PromptCloseVisible, LoginVisible, ShowingWelcome, InteractionVisible }
    private UIState currentState = UIState.Hidden;
    private Coroutine activeCoroutine;

    void Start()
    {
        if (interactionCanvas != null) interactionCanvas.SetActive(false);
        if (loginCanvas != null) loginCanvas.SetActive(false);
        if (currentPressKeyUIInstance != null) Destroy(currentPressKeyUIInstance);

        if (inputSenha != null)
        {
            inputSenha.contentType = TMP_InputField.ContentType.IntegerNumber;
            inputSenha.onValueChanged.AddListener(ValidateNumericInput);
        }
    }

    private void ValidateNumericInput(string input)
    {
        if (inputSenha == null) return;
        string filteredInput = "";
        foreach (char c in input)
        {
            if (char.IsDigit(c))
            {
                filteredInput += c;
            }
        }
        if (input != filteredInput)
        {
            inputSenha.text = filteredInput;
        }
    }

    void Update()
    {
        if (isPlayerInZone && Input.GetKeyDown(interactionKey) && currentState != UIState.ShowingWelcome)
        {
            HandleInteractionKeyPress();
        }
    }

    private void HandleInteractionKeyPress()
    {
        switch (currentState)
        {
            case UIState.PromptInteractVisible:
                if (!isLoginVerified)
                {
                    ShowLogin();
                }
                else
                {
                    ShowInteraction();
                }
                break;
            case UIState.LoginVisible:
            case UIState.InteractionVisible:
            case UIState.PromptCloseVisible:
                if (currentState == UIState.LoginVisible)
                {
                    HideLogin();
                }
                else if (currentState == UIState.InteractionVisible)
                {
                    HideInteraction();
                }
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            if (currentState == UIState.Hidden)
            {
                ShowPromptInteract();
            }
            else if (currentState == UIState.LoginVisible || currentState == UIState.InteractionVisible)
            {
                ShowPromptClose();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            if (currentState == UIState.LoginVisible) HideLogin(false);
            if (currentState == UIState.InteractionVisible) HideInteraction(false);
            if (currentState == UIState.ShowingWelcome)
            {
                if (activeCoroutine != null) StopCoroutine(activeCoroutine);
                if (feedbackTexto != null) feedbackTexto.text = "";
                HideLogin(false);
            }
            HidePrompt();
            currentState = UIState.Hidden;
        }
    }

    private void ShowPromptInteract()
    {
        ShowPrompt("Interagir");
        currentState = UIState.PromptInteractVisible;
    }

    private void ShowPromptClose()
    {
        ShowPrompt("Fechar");
    }

    private void ShowPrompt(string text)
    {
        HidePrompt();
        if (pressKeyUIPrefab != null)
        {
            currentPressKeyUIInstance = Instantiate(pressKeyUIPrefab, uiParent != null ? uiParent : transform);
            UpdateUIText(currentPressKeyUIInstance, text);
        }
    }

    private void HidePrompt()
    {
        if (currentPressKeyUIInstance != null)
        {
            Destroy(currentPressKeyUIInstance);
            currentPressKeyUIInstance = null;
        }
    }

    private void ShowLogin()
    {
        if (loginCanvas != null)
        {
            loginCanvas.SetActive(true);
            HidePrompt();
            ShowPromptClose();
            SetPlayerAndCameraActive(false);
            SetCursorState(true);
            currentState = UIState.LoginVisible;
            if (inputSenha != null) inputSenha.text = "";
            if (feedbackTexto != null) feedbackTexto.text = "";
            if (inputSenha != null) inputSenha.Select();
        }
    }

    public void HideLogin(bool showInteractPrompt = true)
    {
        if (loginCanvas != null && (currentState == UIState.LoginVisible || currentState == UIState.ShowingWelcome))
        {
            loginCanvas.SetActive(false);
            HidePrompt();
            SetPlayerAndCameraActive(true);
            SetCursorState(false);
            currentState = UIState.Hidden;
            if (isPlayerInZone && showInteractPrompt) ShowPromptInteract();
        }
    }

    private void ShowInteraction()
    {
        if (interactionCanvas != null)
        {
            interactionCanvas.SetActive(true);
            HidePrompt();
            ShowPromptClose();
            SetPlayerAndCameraActive(false);
            SetCursorState(true);
            currentState = UIState.InteractionVisible;
        }
    }

    public void HideInteraction(bool showInteractPrompt = true)
    {
        if (interactionCanvas != null && currentState == UIState.InteractionVisible)
        {
            interactionCanvas.SetActive(false);
            HidePrompt();
            SetPlayerAndCameraActive(true);
            SetCursorState(false);
            currentState = UIState.Hidden;
            if (isPlayerInZone && showInteractPrompt) ShowPromptInteract();
        }
    }

    public void VerificarSenha()
    {
        if (inputSenha == null || currentState == UIState.ShowingWelcome) return;

        if (inputSenha.text == senhaCorreta)
        {
            isLoginVerified = true;
            if (activeCoroutine != null) StopCoroutine(activeCoroutine);
            activeCoroutine = StartCoroutine(ShowWelcomeAndProceed());
        }
        else
        {
            if (feedbackTexto != null)
            {
                feedbackTexto.text = "Senha incorreta. Tente novamente.";
            }
            inputSenha.text = "";
            inputSenha.Select();
        }
    }

    private IEnumerator ShowWelcomeAndProceed()
    {
        currentState = UIState.ShowingWelcome;
        HidePrompt();

        if (feedbackTexto != null)
        {
            feedbackTexto.gameObject.SetActive(true);
            string welcomeMessage = "bem-vindo cientista Cleber";
            yield return StartCoroutine(TypeText(feedbackTexto, welcomeMessage, typewriterSpeed));
            yield return new WaitForSeconds(welcomeMessageDuration);
            feedbackTexto.text = "";
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (loginCanvas != null) loginCanvas.SetActive(false);
        ShowInteraction();
        activeCoroutine = null;
    }

    private IEnumerator TypeText(TextMeshProUGUI textElement, string message, float delay)
    {
        textElement.text = "";
        foreach (char letter in message.ToCharArray())
        {
            textElement.text += letter;
            yield return new WaitForSeconds(delay);
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

    private void SetPlayerAndCameraActive(bool isActive)
    {
        if (playerScript != null)
        {
            playerScript.StopPlayer(!isActive);
        }
        if (cameraScript != null)
        {
            cameraScript.enabled = isActive;
        }
    }

    private void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    void OnDestroy()
    {
        if (inputSenha != null)
        {
            inputSenha.onValueChanged.RemoveListener(ValidateNumericInput);
        }
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
    }
}

