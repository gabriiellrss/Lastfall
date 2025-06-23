using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI; // Necessário para ScrollRect

public class HackingTerminalSimulator : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI terminalText;
    public GameObject questionPanel; // Mantido para o resultText
    public TextMeshProUGUI resultText;
    public ScrollRect terminalScrollRect; // Adicionado para rolagem automática

    [Header("Audio Components")]
    public AudioSource audioSource;
    [Space(5)]
    public AudioClip typingSound;
    public AudioClip commandSound;
    public AudioClip successSound;
    public AudioClip errorSound;
    public AudioClip navigationSound;
    [Space(5)]
    [Range(0f, 1f)]
    public float audioVolume = 0.5f;

    [Header("Terminal Settings")]
    public float typewriterSpeed = 0.05f;
    public float commandDelay = 1.5f;
    [Space(5)]
    public int maxTerminalLines = 50; // Máximo de linhas no terminal antes de limpar

    [Header("Question Settings - Configure no Inspector")]
    [TextArea(3, 5)]
    public string sustainabilityQuestion = "Autenticação do Protocolo de sustentabilidade: que acontece quando servidores ficam ligados 24h sem necessidade?";

    [Space(10)]
    public string[] answerOptions = {
        "Melhoram o desempenho",
        "Consomem energia à toa e emitem CO2",
        "Evitam falhas técnicas",
        "Reduzem a poluição"
    };

    [Space(10)]
    [Tooltip("Índice da resposta correta (0-3)")]
    public int correctAnswerIndex = 1; // Segunda opção é a correta

    [Header("Visual Settings")]
    public Color normalTextColor = Color.white;
    public Color selectedTextColor = Color.yellow;
    public Color correctTextColor = Color.green;
    public Color incorrectTextColor = Color.red;
    public Color terminalGreenColor = Color.green;

    private string currentUser = "gabriel";
    private string computerName = "HACKSTATION-X1";
    private string currentDirectory = "C:\\Users\\gabriel>";


    private List<string> hackingCommands = new List<string>
    {
        "PROTOCOLO DE SUSTENTABILIDADE ATIVADO COM SUCESSO!\n",
        "Scanning network... Found 15 active hosts",
        "sqlmap -u \"http://target.com/login.php\" --dbs",
        "Análise do Sistema... Extracting data",
        "hydra -l admin -P passwords.txt ssh://target.com",
        "Estabilização em andamento.... Password found: admin123",
        "Emissões controladas.",
        "use exploit/windows/smb/ms17_010_eternalblue",
        "Processos de degradação atenuados.\n",
        "Fluxo de contaminação diminuindo",
        "Payload executed successfully... Shell access granted",
        "whoami",
        "nt authority\\system",
        "net user hacker password123 /add",
        "User account created successfully",
        "net localgroup administrators hacker /add",
        "Access level elevated to administrator",
        "echo \"System compromised\" > hack_complete.txt",
        "Mission accomplished... Initiating sustainability check..."
    };

    private bool isTyping = false;
    private bool questionPhase = false;
    private bool keyboardNavigationEnabled = false;
    private int selectedAnswerIndex = 0;
    private string[] displayedAnswers;
    private int currentLineCount = 0; // Para controlar o número de linhas no terminal
    public ControlaLuzesEnergia controlaLuzes;
    public GameObject coisaparadestruir;


    void Awake()
    {
        InitializeTerminal();
        InitializeAudio(); // Inicializa o áudio no Awake
    }

    void OnEnable()
    {
        // Deixado vazio. A simulação será iniciada explicitamente por ActivateAndStartSimulation().
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void Update()
    {
        if (questionPhase && keyboardNavigationEnabled)
        {
            HandleKeyboardInput();
        }
    }

    void InitializeAudio()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        audioSource.volume = audioVolume;
        audioSource.playOnAwake = false;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, audioVolume);
        }
    }

    void HandleKeyboardInput()
    {
        // Navegação com setas
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            PlaySound(navigationSound); // Toca som de navegação
            ChangeSelection(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            PlaySound(navigationSound); // Toca som de navegação
            ChangeSelection(1);
        }

        // Seleção direta com teclas numéricas (1-4)
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            PlaySound(navigationSound);
            SelectAnswer(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            PlaySound(navigationSound);
            SelectAnswer(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            PlaySound(navigationSound);
            SelectAnswer(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            PlaySound(navigationSound);
            SelectAnswer(3);
        }

        // Confirmar seleção com Enter ou Espaço
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            PlaySound(commandSound); // Toca som de confirmação
            ConfirmSelection();
        }

        // Reiniciar com R
        if (Input.GetKeyDown(KeyCode.R))
        {
            PlaySound(commandSound); // Toca som de comando
            RestartSimulation();
        }
    }

    void ChangeSelection(int direction)
    {
        selectedAnswerIndex += direction;

        // Wrap around
        if (selectedAnswerIndex < 0)
            selectedAnswerIndex = answerOptions.Length - 1;
        else if (selectedAnswerIndex >= answerOptions.Length)
            selectedAnswerIndex = 0;

        UpdateAnswerDisplay();
    }

    void SelectAnswer(int index)
    {
        if (index < 0 || index >= answerOptions.Length) return;

        selectedAnswerIndex = index;
        UpdateAnswerDisplay();
    }

    void UpdateAnswerDisplay()
    {
        if (!questionPhase) return;

        // Criar array de respostas com destaque visual
        displayedAnswers = new string[answerOptions.Length];

        for (int i = 0; i < answerOptions.Length; i++)
        {
            if (i == selectedAnswerIndex)
            {
                displayedAnswers[i] = $"<color=#{ColorUtility.ToHtmlStringRGB(selectedTextColor)}>> {i + 1}. {answerOptions[i]} <</color>";
            }
            else
            {
                displayedAnswers[i] = $"  {i + 1}. {answerOptions[i]}";
            }
        }

        // Atualizar o texto da pergunta com as opções
        string fullQuestionText = sustainabilityQuestion + "\n\n";
        foreach (string answer in displayedAnswers)
        {
            fullQuestionText += answer + "\n";
        }
        fullQuestionText += "\n<color=#00FFFF>Use ↑↓ ou 1-4 para navegar | ENTER/ESPAÇO para confirmar | R para reiniciar</color>";

        // A pergunta agora é exibida no terminalText
        if (terminalText != null)
        {
            terminalText.text = fullQuestionText;
        }
    }

    void ConfirmSelection()
    {
        OnAnswerSelected(selectedAnswerIndex);
    }

    void InitializeTerminal()
    {
        if (terminalText != null)
        {
            terminalText.text = "";
            terminalText.color = terminalGreenColor;
        }
        if (resultText != null) resultText.text = "";

        keyboardNavigationEnabled = false;
        selectedAnswerIndex = 0;
        questionPhase = false;
        currentLineCount = 0; // Reseta a contagem de linhas
    }

    public void StartSimulation()
    {
        StopAllCoroutines();
        InitializeTerminal();
        StartCoroutine(RunHackingSimulation());
    }

    // FUNÇÃO PÚBLICA PARA O BOTÃO - ATIVA O GAMEOBJECT E INICIA A SIMULAÇÃO
    public void ActivateAndStartSimulation()
    {
        // Ativa o GameObject se estiver desativado
        questionPanel.SetActive(true);
        


            StartSimulation();
    }

    IEnumerator ScrollToBottomCoroutine()
    {
        // Espera um frame para o layout ser atualizado após a atualização do texto
        yield return null;

        if (terminalScrollRect != null)
        {
            // Força a atualização do layout do TextMeshPro antes de reconstruir o layout do ScrollRect
            terminalText.ForceMeshUpdate();
            // Força a reconstrução imediata do layout do conteúdo do ScrollRect
            LayoutRebuilder.ForceRebuildLayoutImmediate(terminalScrollRect.content);
            // Define a posição de rolagem para o fundo
            terminalScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    void ScrollToBottom()
    {
        StartCoroutine(ScrollToBottomCoroutine());
    }

    void CheckAndClearTerminal()
    {
        if (currentLineCount > maxTerminalLines)
        {
            // Manter apenas as últimas linhas para evitar que o texto fique muito grande
            string[] lines = terminalText.text.Split("\n");
            int linesToKeep = maxTerminalLines / 2; // Mantém metade das linhas

            string newText = "";
            for (int i = lines.Length - linesToKeep; i < lines.Length; i++)
            {
                if (i >= 0) newText += lines[i] + "\n";
            }

            terminalText.text = newText;
            currentLineCount = linesToKeep;
        }
    }

    IEnumerator RunHackingSimulation()
    {
        /*/ Cabeçalho do sistema
        yield return StartCoroutine(TypeText($"╔══════════════════════════════════════════════════════════════╗\n", false));
        yield return StartCoroutine(TypeText($"║                    CYBER PENETRATION SYSTEM                 ║\n", false));
        yield return StartCoroutine(TypeText($"║                        {computerName}                    ║\n", false));
        yield return StartCoroutine(TypeText($"╚══════════════════════════════════════════════════════════════╝\n\n", false));

        yield return StartCoroutine(TypeText($"Microsoft Windows [Version 10.0.19044.1234]\n", false));
        yield return StartCoroutine(TypeText($"(c) Microsoft Corporation. All rights reserved.\n\n", false));*/

        // Mostrar prompt inicial
        /*yield return StartCoroutine(TypeText($"{currentDirectory} ", false));*/

        // Executar comandos de hacking
        foreach (string command in hackingCommands)
        {
            yield return new WaitForSeconds(commandDelay);

            if (command.StartsWith("C:\\") || command.Contains(">"))
            {
                PlaySound(commandSound); // Toca som de comando
                yield return StartCoroutine(TypeText($"\n{command} ", false));
            }
            else
            {
                yield return StartCoroutine(TypeText($"{command}\n", false));
            }
        }

        yield return new WaitForSeconds(2f);

        // Transição para a pergunta
        if (terminalText != null) terminalText.color = Color.yellow;
        PlaySound(commandSound); // Toca som de transição
        yield return StartCoroutine(TypeText("\n\n[SISTEMA DE VERIFICAÇÃO ATIVADO]\n", false));
        yield return StartCoroutine(TypeText("Protocolo de sustentabilidade iniciado...\n", false));
        yield return StartCoroutine(TypeText("Carregando questionário ambiental...\n\n", false));
        if (terminalText != null) terminalText.color = terminalGreenColor;

        ShowQuestion();
    }

    IEnumerator TypeText(string text, bool clearFirst = true)
    {
        isTyping = true;

        if (terminalText == null)
        {
            Debug.LogError("TerminalText é nulo! Não é possível digitar texto.");
            isTyping = false;
            yield break;
        }

        if (clearFirst)
        {
            terminalText.text = "";
            currentLineCount = 0;
        }

        string currentText = terminalText.text;

        foreach (char c in text)
        {
            currentText += c;
            terminalText.text = currentText;

            // Contar linhas para o controle de limpeza
            if (c == '\n') currentLineCount++;

            // Tocar som de digitação ocasionalmente
            if (typingSound != null && Random.Range(0f, 1f) < 0.3f) // Toca o som 30% das vezes
            {
                PlaySound(typingSound);
            }

            yield return new WaitForSeconds(typewriterSpeed);
        }

        // Verificar se precisa limpar o terminal e rolar para baixo
        CheckAndClearTerminal();
        ScrollToBottom();

        isTyping = false;
    }

    void ShowQuestion()
    {
        questionPhase = true;

        selectedAnswerIndex = 0;
        UpdateAnswerDisplay();

        keyboardNavigationEnabled = true;

        StartCoroutine(TypeText("\nAUTENTICAÇÂO  DE SUSTENTABILIDADE CARREGADO\n", false));
        StartCoroutine(TypeText("Use as instruções na tela para navegar e responder.\n\n", false));
    }

    public void OnAnswerSelected(int answerIndex)
    {
        if (!questionPhase) return;

        questionPhase = false;
        keyboardNavigationEnabled = false;

        StartCoroutine(ShowResult(answerIndex));
    }

    IEnumerator ShowResult(int selectedAnswer)
    {
        // Mostrar resultado visual na pergunta
        displayedAnswers = new string[answerOptions.Length];

        for (int i = 0; i < answerOptions.Length; i++)
        {
            if (i == selectedAnswer && i == correctAnswerIndex)
            {
                // Resposta selecionada e correta
                displayedAnswers[i] = $"<color=#{ColorUtility.ToHtmlStringRGB(correctTextColor)}>✓ {i + 1}. {answerOptions[i]} (CORRETO)</color>";
            }
            else if (i == selectedAnswer && i != correctAnswerIndex)
            {
                // Resposta selecionada mas incorreta
                displayedAnswers[i] = $"<color=#{ColorUtility.ToHtmlStringRGB(incorrectTextColor)}>✗ {i + 1}. {answerOptions[i]} (SUA ESCOLHA)</color>";
            }
            else if (i == correctAnswerIndex)
            {
                // Resposta correta (não selecionada)
                displayedAnswers[i] = $"<color=#{ColorUtility.ToHtmlStringRGB(correctTextColor)}>✓ {i + 1}. {answerOptions[i]} (CORRETO)</color>";
            }
            else
            {
                displayedAnswers[i] = $"  {i + 1}. {answerOptions[i]}";
            }
        }

        // Atualizar display
        string fullQuestionText = sustainabilityQuestion + "\n\n";
        foreach (string answer in displayedAnswers)
        {
            fullQuestionText += answer + "\n"; // Adiciona uma quebra de linha após cada resposta
        }

        // A pergunta agora é exibida no terminalText
        if (terminalText != null)
        {
            terminalText.text = fullQuestionText;
        }

        yield return new WaitForSeconds(1f);

        if (selectedAnswer == correctAnswerIndex)
        {
            PlaySound(successSound); // Toca som de sucesso
            if (resultText != null) resultText.text = "✓ ACESSO AUTORIZADO";
            if (resultText != null) resultText.color = correctTextColor;

            // Adicionar ao terminal
            yield return StartCoroutine(TypeText("\n[RESPOSTA CORRETA] - Verificação aprovada!\n", false));
            yield return StartCoroutine(TypeText("SUA AÇÃO EVITOU UM COLAPSO AMBIENTAL IMEDIATO.\n", false));
            yield return StartCoroutine(TypeText("O sistema agora opera sob parâmetros de emergência, mas a ameaça direta foi contida.\n", false));
            yield return StartCoroutine(TypeText("\nMISSÃO CONCLUÍDA COM SUCESSO!\n", false));

            controlaLuzes.reduzirImpacto = true;

            yield return new WaitForSeconds(3f);
            Destroy(questionPanel);
            Destroy(coisaparadestruir);

        }
        else
        {
            PlaySound(errorSound); // Toca som de erro
            if (resultText != null) resultText.text = "✗ ACESSO NEGADO";
            if (resultText != null) resultText.color = incorrectTextColor;

            // Adicionar ao terminal
            yield return StartCoroutine(TypeText("\n[RESPOSTA INCORRETA] - Verificação falhada!\n", false));
            yield return StartCoroutine(TypeText("Sistema bloqueado... Revise conhecimentos sobre sustentabilidade.\n", false));
            yield return StartCoroutine(TypeText($"RESPOSTA CORRETA: {answerOptions[correctAnswerIndex]}\n", false));

            // REMOVIDO: Não reiniciar automaticamente
            // yield return new WaitForSeconds(3f);
            // yield return StartCoroutine(TypeText("\nPressione R para tentar novamente...\n", false));
            // keyboardNavigationEnabled = true;
        }

        // Sempre permitir reiniciar com R no final, independentemente de acertar ou errar
        yield return StartCoroutine(TypeText("\nPressione R para reiniciar a simulação.\n", false));
        keyboardNavigationEnabled = true;
    }

    public void RestartSimulation()
    {
        StopAllCoroutines();
        InitializeTerminal();
        StartCoroutine(RunHackingSimulation());
    }

    // Método para validar configuração no Inspector
    void OnValidate()
    {
        if (correctAnswerIndex < 0 || correctAnswerIndex >= answerOptions.Length)
        {
            Debug.LogWarning("Índice da resposta correta está fora do range das opções disponíveis!");
        }

        if (answerOptions.Length != 4)
        {
            Debug.LogWarning("Recomenda-se ter exatamente 4 opções de resposta para melhor experiência!");
        }

        // Ajustar volume do áudio se mudado no Inspector
        if (audioSource != null)
        {
            audioSource.volume = audioVolume;
        }
    }
}

