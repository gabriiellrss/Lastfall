using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AdvancedHackingTerminal : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI terminalText;
    public ScrollRect terminalScrollRect;
    public Button[] answerButtons;
    public GameObject questionPanel;
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI[] answerTexts;
    public TextMeshProUGUI resultText;
    public Button restartButton;
    public Button nextQuestionButton;
    
    [Header("Visual Effects")]
    public Image screenFlicker;
    public AudioSource typingSound;
    public AudioSource successSound;
    public AudioSource errorSound;
    
    [Header("Terminal Settings")]
    public float typewriterSpeed = 0.03f;
    public float commandDelay = 1.2f;
    public Color terminalGreen = Color.green;
    public Color terminalRed = Color.red;
    public Color terminalYellow = Color.yellow;
    
    [Header("Question System")]
    public QuestionDatabase questionDatabase;
    public bool randomizeQuestions = true;
    public int maxQuestions = 3;
    
    private string currentUser;
    private string computerName;
    private string currentDirectory;
    private int currentQuestionIndex = 0;
    private int correctAnswers = 0;
    private int totalQuestions = 0;
    
    private List<string> hackingCommands = new List<string>
    {
        "Iniciando sistema de penetração...",
        "nmap -sS -O 192.168.1.0/24",
        "Scanning network topology... [████████████] 100%",
        "Found 23 active hosts | 15 open ports detected",
        "",
        "sqlmap -u \"http://target-corp.com/login\" --dbs --batch",
        "Database injection vector found... Exploiting SQLi",
        "[INFO] retrieved: information_schema, users, financial_data",
        "Database dump successful... 15,847 records extracted",
        "",
        "hydra -l admin -P /usr/share/wordlists/rockyou.txt ssh://target-corp.com",
        "Brute force attack initiated... Testing 14,344,391 passwords",
        "[ATTEMPT 1,337] admin:password123 - SUCCESS!",
        "SSH credentials compromised... Establishing connection",
        "",
        "msfconsole -q",
        "use exploit/windows/smb/ms17_010_eternalblue",
        "set RHOSTS 192.168.1.100",
        "set PAYLOAD windows/x64/meterpreter/reverse_tcp",
        "exploit",
        "",
        "[*] Sending stage (200262 bytes) to 192.168.1.100",
        "[*] Meterpreter session 1 opened",
        "Payload executed successfully... Shell access granted",
        "",
        "getuid",
        "Server username: NT AUTHORITY\\SYSTEM",
        "hashdump",
        "Administrator:500:aad3b435b51404eeaad3b435b51404ee:31d6cfe0d16ae931b73c59d7e0c089c0:::",
        "",
        "upload backdoor.exe C:\\\\Windows\\\\System32\\\\",
        "Backdoor installed... Persistence established",
        "reg add HKLM\\\\SOFTWARE\\\\Microsoft\\\\Windows\\\\CurrentVersion\\\\Run /v SecurityUpdate /t REG_SZ /d C:\\\\Windows\\\\System32\\\\backdoor.exe",
        "Registry key added successfully",
        "",
        "echo 'MISSION STATUS: COMPROMISED' > /tmp/hack_complete.log",
        "netstat -an | grep LISTEN",
        "ps aux | grep -v grep",
        "",
        "=== SISTEMA COMPROMETIDO COM SUCESSO ===",
        "Iniciando protocolo de verificação de sustentabilidade...",
        "Carregando módulo de consciência ambiental...",
        ""
    };
    
    private bool isTyping = false;
    private bool questionPhase = false;
    
    void Start()
    {
        InitializeTerminal();
        GenerateRandomSystemInfo();
        StartCoroutine(RunHackingSimulation());
    }
    
    void InitializeTerminal()
    {
        terminalText.text = "";
        terminalText.color = terminalGreen;
        questionPanel.SetActive(false);
        resultText.text = "";
        currentQuestionIndex = 0;
        correctAnswers = 0;
        totalQuestions = 0;
        
        // Configurar botões
        restartButton.onClick.AddListener(RestartSimulation);
        nextQuestionButton.onClick.AddListener(LoadNextQuestion);
        nextQuestionButton.gameObject.SetActive(false);
        
        // Configurar botões de resposta
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
        }
        
        // Efeito de flicker inicial
        if (screenFlicker != null)
            StartCoroutine(ScreenFlickerEffect());
    }
    
    void GenerateRandomSystemInfo()
    {
        string[] userNames = { "gabriel", "admin", "hacker", "user", "operator", "cyber_agent" };
        string[] computerNames = { "HACKSTATION-X1", "CYBER-TERMINAL", "PENETRATION-BOX", "SECURITY-LAB", "EXPLOIT-MACHINE" };
        
        currentUser = userNames[Random.Range(0, userNames.Length)];
        computerName = computerNames[Random.Range(0, computerNames.Length)];
        currentDirectory = $"C:\\Users\\{currentUser}>";
    }
    
    IEnumerator ScreenFlickerEffect()
    {
        if (screenFlicker == null) yield break;
        
        for (int i = 0; i < 3; i++)
        {
            screenFlicker.color = new Color(0, 1, 0, 0.1f);
            yield return new WaitForSeconds(0.1f);
            screenFlicker.color = Color.clear;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    IEnumerator RunHackingSimulation()
    {
        // Cabeçalho do sistema
        yield return StartCoroutine(TypeText($"╔══════════════════════════════════════════════════════════════╗\n", false));
        yield return StartCoroutine(TypeText($"║                    CYBER PENETRATION SYSTEM                 ║\n", false));
        yield return StartCoroutine(TypeText($"║                        {computerName}                    ║\n", false));
        yield return StartCoroutine(TypeText($"╚══════════════════════════════════════════════════════════════╝\n\n", false));
        
        yield return StartCoroutine(TypeText($"Microsoft Windows [Version 10.0.19044.1234]\n", false));
        yield return StartCoroutine(TypeText($"(c) Microsoft Corporation. All rights reserved.\n\n", false));
        
        // Mostrar prompt inicial
        yield return StartCoroutine(TypeText($"{currentDirectory} ", false));
        
        // Executar comandos de hacking
        foreach (string command in hackingCommands)
        {
            yield return new WaitForSeconds(commandDelay);
            
            if (string.IsNullOrEmpty(command))
            {
                yield return StartCoroutine(TypeText("\n", false));
                continue;
            }
            
            if (command.StartsWith("C:\\") || command.Contains(">"))
            {
                yield return StartCoroutine(TypeText($"\n{command} ", false));
            }
            else if (command.StartsWith("==="))
            {
                terminalText.color = terminalYellow;
                yield return StartCoroutine(TypeText($"{command}\n", false));
                terminalText.color = terminalGreen;
            }
            else
            {
                yield return StartCoroutine(TypeText($"{command}\n", false));
            }
            
            // Auto-scroll para baixo
            if (terminalScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                terminalScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        yield return new WaitForSeconds(2f);
        
        // Transição para perguntas
        terminalText.color = terminalYellow;
        yield return StartCoroutine(TypeText("\n[PROTOCOLO DE VERIFICAÇÃO ATIVADO]\n", false));
        yield return StartCoroutine(TypeText("Sistema requer validação de conhecimento em sustentabilidade...\n", false));
        yield return StartCoroutine(TypeText("Carregando questionário de consciência ambiental...\n\n", false));
        terminalText.color = terminalGreen;
        
        LoadNextQuestion();
    }
    
    IEnumerator TypeText(string text, bool clearFirst = true)
    {
        isTyping = true;
        
        if (clearFirst)
            terminalText.text = "";
        
        string currentText = terminalText.text;
        
        foreach (char c in text)
        {
            currentText += c;
            terminalText.text = currentText;
            
            // Som de digitação
            if (typingSound != null && c != ' ' && c != '\n')
                typingSound.Play();
            
            yield return new WaitForSeconds(typewriterSpeed);
        }
        
        isTyping = false;
    }
    
    void LoadNextQuestion()
    {
        if (questionDatabase == null || questionDatabase.questions.Length == 0)
        {
            Debug.LogError("Question Database não configurado!");
            return;
        }
        
        if (totalQuestions >= maxQuestions)
        {
            ShowFinalResults();
            return;
        }
        
        QuestionData currentQuestion;
        
        if (randomizeQuestions)
        {
            currentQuestion = questionDatabase.GetRandomQuestion();
        }
        else
        {
            currentQuestion = questionDatabase.GetQuestionByIndex(currentQuestionIndex);
        }
        
        if (currentQuestion != null)
        {
            ShowQuestion(currentQuestion);
            currentQuestionIndex++;
            totalQuestions++;
        }
    }
    
    void ShowQuestion(QuestionData questionData)
    {
        questionPhase = true;
        questionText.text = $"PERGUNTA {totalQuestions + 1}/{maxQuestions}: {questionData.question}";
        questionPanel.SetActive(true);
        
        // Configurar respostas
        for (int i = 0; i < answerButtons.Length && i < questionData.answers.Length; i++)
        {
            answerTexts[i].text = $"○ {questionData.answers[i]}";
            answerButtons[i].gameObject.SetActive(true);
            answerButtons[i].interactable = true;
            answerButtons[i].GetComponent<Image>().color = Color.white;
        }
        
        // Esconder botões extras se houver menos de 4 respostas
        for (int i = questionData.answers.Length; i < answerButtons.Length; i++)
        {
            answerButtons[i].gameObject.SetActive(false);
        }
        
        nextQuestionButton.gameObject.SetActive(false);
        StartCoroutine(AnimateButtons());
    }
    
    IEnumerator AnimateButtons()
    {
        foreach (Button button in answerButtons)
        {
            if (button.gameObject.activeInHierarchy)
            {
                button.transform.localScale = Vector3.zero;
            }
        }
        
        yield return new WaitForSeconds(0.5f);
        
        foreach (Button button in answerButtons)
        {
            if (button.gameObject.activeInHierarchy)
            {
                StartCoroutine(ScaleButton(button.transform));
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
    
    IEnumerator ScaleButton(Transform buttonTransform)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float scale = Mathf.Lerp(0f, 1f, elapsed / duration);
            buttonTransform.localScale = Vector3.one * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        buttonTransform.localScale = Vector3.one;
    }
    
    public void OnAnswerSelected(int answerIndex)
    {
        if (!questionPhase) return;
        
        questionPhase = false;
        
        // Desativar todos os botões
        foreach (Button button in answerButtons)
        {
            button.interactable = false;
        }
        
        QuestionData currentQuestion = questionDatabase.GetQuestionByIndex(currentQuestionIndex - 1);
        if (randomizeQuestions)
        {
            // Para perguntas aleatórias, precisamos armazenar a pergunta atual
            // Por simplicidade, vamos usar a primeira pergunta como referência
            currentQuestion = questionDatabase.questions[0];
        }
        
        StartCoroutine(ShowResult(answerIndex, currentQuestion));
    }
    
    IEnumerator ShowResult(int selectedAnswer, QuestionData questionData)
    {
        yield return new WaitForSeconds(0.5f);
        
        bool isCorrect = selectedAnswer == questionData.correctAnswerIndex;
        
        // Destacar a resposta selecionada
        answerButtons[selectedAnswer].GetComponent<Image>().color = isCorrect ? Color.green : Color.red;
        
        // Mostrar a resposta correta se errou
        if (!isCorrect)
        {
            answerButtons[questionData.correctAnswerIndex].GetComponent<Image>().color = Color.green;
        }
        
        yield return new WaitForSeconds(1f);
        
        if (isCorrect)
        {
            correctAnswers++;
            resultText.text = "✓ CORRETO! Acesso autorizado.";
            resultText.color = Color.green;
            
            if (successSound != null)
                successSound.Play();
            
            // Adicionar ao terminal
            yield return StartCoroutine(TypeText($"\n[RESPOSTA CORRETA] - Verificação aprovada!\n", false));
            if (!string.IsNullOrEmpty(questionData.explanation))
            {
                yield return StartCoroutine(TypeText($"INFO: {questionData.explanation}\n", false));
            }
        }
        else
        {
            resultText.text = "✗ INCORRETO! Acesso negado.";
            resultText.color = Color.red;
            
            if (errorSound != null)
                errorSound.Play();
            
            // Adicionar ao terminal
            yield return StartCoroutine(TypeText($"\n[RESPOSTA INCORRETA] - Verificação falhada!\n", false));
            yield return StartCoroutine(TypeText($"RESPOSTA CORRETA: {questionData.answers[questionData.correctAnswerIndex]}\n", false));
            if (!string.IsNullOrEmpty(questionData.explanation))
            {
                yield return StartCoroutine(TypeText($"EXPLICAÇÃO: {questionData.explanation}\n", false));
            }
        }
        
        yield return new WaitForSeconds(2f);
        
        if (totalQuestions < maxQuestions)
        {
            nextQuestionButton.gameObject.SetActive(true);
        }
        else
        {
            ShowFinalResults();
        }
    }
    
    void ShowFinalResults()
    {
        questionPanel.SetActive(false);
        
        float percentage = (float)correctAnswers / totalQuestions * 100f;
        string grade = percentage >= 80f ? "EXCELENTE" : percentage >= 60f ? "BOM" : "PRECISA MELHORAR";
        
        StartCoroutine(TypeFinalResults(percentage, grade));
    }
    
    IEnumerator TypeFinalResults(float percentage, string grade)
    {
        yield return StartCoroutine(TypeText("\n" + new string('=', 50) + "\n", false));
        yield return StartCoroutine(TypeText("RELATÓRIO FINAL DE SUSTENTABILIDADE\n", false));
        yield return StartCoroutine(TypeText(new string('=', 50) + "\n\n", false));
        
        yield return StartCoroutine(TypeText($"Perguntas respondidas: {totalQuestions}\n", false));
        yield return StartCoroutine(TypeText($"Respostas corretas: {correctAnswers}\n", false));
        yield return StartCoroutine(TypeText($"Percentual de acerto: {percentage:F1}%\n", false));
        yield return StartCoroutine(TypeText($"Classificação: {grade}\n\n", false));
        
        if (percentage >= 80f)
        {
            terminalText.color = terminalGreen;
            yield return StartCoroutine(TypeText("🌱 PARABÉNS! Você demonstrou excelente consciência ambiental!\n", false));
            yield return StartCoroutine(TypeText("Sistema desbloqueado... Bem-vindo ao futuro sustentável!\n", false));
        }
        else if (percentage >= 60f)
        {
            terminalText.color = terminalYellow;
            yield return StartCoroutine(TypeText("⚠️ Bom trabalho! Continue aprendendo sobre sustentabilidade.\n", false));
            yield return StartCoroutine(TypeText("Acesso parcial liberado... Estude mais sobre Green Computing!\n", false));
        }
        else
        {
            terminalText.color = terminalRed;
            yield return StartCoroutine(TypeText("❌ Conhecimento insuficiente sobre sustentabilidade.\n", false));
            yield return StartCoroutine(TypeText("Acesso negado... Revise conceitos de tecnologia sustentável!\n", false));
        }
        
        terminalText.color = terminalGreen;
        yield return StartCoroutine(TypeText("\nPressione RESTART para tentar novamente.\n", false));
        
        restartButton.gameObject.SetActive(true);
    }
    
    public void RestartSimulation()
    {
        StopAllCoroutines();
        InitializeTerminal();
        GenerateRandomSystemInfo();
        StartCoroutine(RunHackingSimulation());
    }
}

