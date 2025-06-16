using TMPro;
using UnityEngine;

public class ObjectiveUIController : MonoBehaviour
{
    [Header("Referência da caixa de texto TMP")]
    public TextMeshProUGUI instructionText;

    [Header("Mensagem inicial")]
    [TextArea]
    public string initialMessage = "Encontre a chave para abrir o portão.";

    [Header("Tempo que a mensagem fica visível (0 = infinito)")]
    public float visibleTime = 0f;

    private float timer = 0f;

    void Start()
    {
        ShowInstruction(initialMessage, visibleTime);
    }

    void Update()
    {
        if (visibleTime > 0 && instructionText.gameObject.activeSelf)
        {
            timer += Time.deltaTime;
            if (timer >= visibleTime)
            {
                instructionText.gameObject.SetActive(false);
                timer = 0f;
            }
        }
    }

    /// <summary>
    /// Mostra uma nova instrução.
    /// </summary>
    public void ShowInstruction(string message, float duration = 0f)
    {
        instructionText.text = message;
        instructionText.gameObject.SetActive(true);
        visibleTime = duration;
        timer = 0f;
    }

    /// <summary>
    /// Esconde a instrução atual.
    /// </summary>
    public void HideInstruction()
    {
        instructionText.gameObject.SetActive(false);
        timer = 0f;
        visibleTime = 0f;
    }
}
