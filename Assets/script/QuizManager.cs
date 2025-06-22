using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuizManager : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        public string questionText;
        public List<string> options;
        public int correctAnswerIndex;
    }

    public List<Question> questions = new List<Question>
    {
        new Question
        {
            questionText = "O que representa o maior risco em confiar unicamente na tecnologia para resolver os problemas ambientais causados pelo ser humano?",
            options = new List<string>
            {
                "A) A tecnologia pode ser muito cara e causar desigualdade no acesso",
                "B) Pode depender de recursos naturais escassos e gerar novos impactos",
                "C) Pode criar uma falsa sensação de que não precisamos mudar nosso comportamento atual",
                "D) Tecnologias ecológicas geralmente têm resultados muito lentos"
            },
            correctAnswerIndex = 2
        },
        new Question
        {
            questionText = "Quando uma espécie é extinta devido à ação humana, qual é a consequência menos visível, porém mais perigosa?",
            options = new List<string>
            {
                "A) Perda do potencial científico de se estudar aquela espécie extinta",
                "B) Rompimento de cadeias ecológicas, afetando todo o equilíbrio do ecossistema",
                "C) Diminuição da diversidade genética disponível para adaptações futuras",
                "D) Redução das opções alimentares de populações locais"
            },
            correctAnswerIndex = 1
        },
        new Question
        {
            questionText = "Por que políticas públicas ambientais sozinhas não são suficientes para mudar a realidade ambiental do planeta?",
            options = new List<string>
            {
                "A) Porque muitas vezes elas são mal planejadas ou não têm continuidade",
                "B) Porque sua aplicação depende de interesses econômicos que nem sempre priorizam o meio ambiente",
                "C) Porque sem mudança cultural e participação popular, as leis não são respeitadas nem fiscalizadas",
                "D) Porque o impacto real só vem a longo prazo, o que desmotiva a população"
            },
            correctAnswerIndex = 2
        },
        new Question
        {
            questionText = "Em áreas que sofreram desmatamento total, qual o maior desafio para a regeneração do ecossistema?",
            options = new List<string>
            {
                "A) A ausência de cobertura vegetal que impede a retenção de umidade",
                "B) A alteração do microclima local, que atrasa o crescimento de novas plantas",
                "C) Perda da biodiversidade do solo e desequilíbrio da fauna, que impede a volta da vegetação nativa",
                "D) O aumento da exposição ao vento e à luz solar direta"
            },
            correctAnswerIndex = 2
        },
        new Question
        {
            questionText = "Por que a substituição de florestas nativas por plantações de eucalipto pode ser prejudicial ao meio ambiente, mesmo sendo uma forma de reflorestamento?",
            options = new List<string>
            {
                "A) Porque o eucalipto exige mais tempo para crescer do que as espécies nativas",
                "B) Porque o eucalipto reduz a biodiversidade e consome grandes quantidades de água, afetando o solo e nascentes",
                "C) Porque o eucalipto não pode ser usado na indústria madeireira",
                "D) Porque o eucalipto é uma árvore frágil e facilmente derrubada por ventos"
            },
            correctAnswerIndex = 1
        },
        new Question
        {
            questionText = "O que torna os microplásticos um dos maiores perigos ambientais invisíveis para os seres humanos?",
            options = new List<string>
            {
                "A) Eles causam acúmulo de lixo visível em praias e rios",
                "B) São recicláveis apenas em condições muito específicas",
                "C) Entram na cadeia alimentar e já foram encontrados até no sangue humano e em órgãos vitais",
                "D) São produzidos apenas por grandes indústrias, sendo fácil evitá-los"
            },
            correctAnswerIndex = 2
        },
        new Question
        {
            questionText = "Por que comunidades tradicionais (indígenas, ribeirinhas, quilombolas) são consideradas essenciais na luta pela preservação ambiental?",
            options = new List<string>
            {
                "A) Porque vivem isoladas e não causam impacto algum ao ambiente",
                "B) Porque possuem conhecimento ancestral sobre o uso sustentável da terra e mantêm florestas vivas há séculos",
                "C) Porque são os principais responsáveis por fiscalizar áreas protegidas",
                "D) Porque plantam grandes quantidades de árvores em suas regiões"
            },
            correctAnswerIndex = 1
        }
    };

    public TextMeshProUGUI questionTextUI;
    public List<Button> answerButtons;
    public TextMeshProUGUI timerTextUI;
    public TextMeshProUGUI feedbackTextUI;
    public TextMeshProUGUI errorTextUI;

    public GameObject quizPanel;
    public PCInteraction pcInteractionScript; // Referência ao script de interação do PC

    private int currentQuestionIndex = 0;
    private int correctAnswersCount = 0;
    private int incorrectAttempts = 0;
    private const int MAX_INCORRECT_ATTEMPTS = 3;
    private const int QUESTIONS_TO_WIN = 4;
    private const float TIME_PER_QUESTION = 120f; // 2 minutos
    private float currentQuestionTime;
    private bool quizActive = false;

    void Start()
    {
        quizPanel.SetActive(false); // Esconde o painel do quiz no início
        // Certifique-se de que o TextMeshPro está atribuído no Inspector
        if (questionTextUI == null) Debug.LogError("Question Text UI is not assigned!");
        if (timerTextUI == null) Debug.LogError("Timer Text UI is not assigned!");
        if (feedbackTextUI == null) Debug.LogError("Feedback Text UI is not assigned!");
        if (errorTextUI == null) Debug.LogError("Error Text UI is not assigned!");

        // Adiciona listeners aos botões de resposta
        for (int i = 0; i < answerButtons.Count; i++)
        {
            int buttonIndex = i; // Para evitar problemas com closure em loops
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(buttonIndex));
        }
    }

    void Update()
    {
        if (quizActive)
        {
            currentQuestionTime -= Time.deltaTime;
            timerTextUI.text = "Tempo: " + Mathf.Max(0, Mathf.FloorToInt(currentQuestionTime)).ToString() + "s";

            if (currentQuestionTime <= 0)
            {
                OnTimeUp();
            }
        }
    }

    public void StartQuiz()
    {
        quizPanel.SetActive(true);
        currentQuestionIndex = 0;
        correctAnswersCount = 0;
        incorrectAttempts = 0;
        quizActive = true;
        errorTextUI.text = "Erros: 0/" + MAX_INCORRECT_ATTEMPTS;
        feedbackTextUI.text = ""; // Limpa feedback anterior
        LoadQuestion();
    }

    void LoadQuestion()
    {
        if (currentQuestionIndex < questions.Count)
        {
            Question q = questions[currentQuestionIndex];
            questionTextUI.text = q.questionText;

            for (int i = 0; i < answerButtons.Count; i++)
            {
                if (i < q.options.Count)
                {
                    answerButtons[i].gameObject.SetActive(true);
                    answerButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = q.options[i];
                    answerButtons[i].interactable = true; // Reativa botões
                    // Reseta a cor dos botões para o normal (verde médio)
                    ColorBlock cb = answerButtons[i].colors;
                    cb.normalColor = new Color32(60, 179, 113, 255); // Medium Sea Green
                    answerButtons[i].colors = cb;
                }
                else
                {
                    answerButtons[i].gameObject.SetActive(false); // Esconde botões não usados
                }
            }
            currentQuestionTime = TIME_PER_QUESTION;
        }
        else
        {
            // Todas as perguntas foram respondidas, mas não atingiu o número de acertos para a cutscene
            EndQuiz(false); // Quiz terminou sem vitória
        }
    }

    public void OnAnswerSelected(int selectedOptionIndex)

    {
        quizActive = false; // Pausa o timer
        DisableAnswerButtons();

        Question q = questions[currentQuestionIndex];

        if (selectedOptionIndex == q.correctAnswerIndex)
        {
            correctAnswersCount++;
            feedbackTextUI.color = new Color32(65, 105, 225, 255); // Royal Blue
            feedbackTextUI.text = "Correto!";
            HighlightCorrectAnswer(selectedOptionIndex);
            StartCoroutine(NextQuestionAfterDelay(2f));
        }
        else
        {
            incorrectAttempts++;
            errorTextUI.text = "Erros: " + incorrectAttempts + "/" + MAX_INCORRECT_ATTEMPTS;
            feedbackTextUI.color = new Color32(220, 20, 60, 255); // Crimson
            feedbackTextUI.text = "Incorreto!";
            HighlightIncorrectAnswer(selectedOptionIndex, q.correctAnswerIndex);

            if (incorrectAttempts >= MAX_INCORRECT_ATTEMPTS)
            {
                StartCoroutine(EndQuizAfterDelay(2f, false)); // Perdeu por erros
            }
            else
            {
                StartCoroutine(NextQuestionAfterDelay(2f));
            }
        }
    }

    void OnTimeUp()
    {
        quizActive = false;
        DisableAnswerButtons();
        incorrectAttempts++;
        errorTextUI.text = "Erros: " + incorrectAttempts + "/" + MAX_INCORRECT_ATTEMPTS;
        feedbackTextUI.color = new Color32(220, 20, 60, 255); // Crimson
        feedbackTextUI.text = "Tempo Esgotado!";

        if (incorrectAttempts >= MAX_INCORRECT_ATTEMPTS)
        {
            StartCoroutine(EndQuizAfterDelay(2f, false)); // Perdeu por tempo
        }
        else
        {
            StartCoroutine(NextQuestionAfterDelay(2f));
        }
    }

    void DisableAnswerButtons()
    {
        foreach (Button btn in answerButtons)
        {
            btn.interactable = false;
        }
    }

    void HighlightCorrectAnswer(int correctIndex)
    {
        ColorBlock cb = answerButtons[correctIndex].colors;
        cb.normalColor = new Color32(65, 105, 225, 255); // Royal Blue
        answerButtons[correctIndex].colors = cb;
    }

    void HighlightIncorrectAnswer(int incorrectIndex, int correctIndex)
    {
        ColorBlock cbIncorrect = answerButtons[incorrectIndex].colors;
        cbIncorrect.normalColor = new Color32(220, 20, 60, 255); // Crimson
        answerButtons[incorrectIndex].colors = cbIncorrect;

        ColorBlock cbCorrect = answerButtons[correctIndex].colors;
        cbCorrect.normalColor = new Color32(65, 105, 225, 255); // Royal Blue
        answerButtons[correctIndex].colors = cbCorrect;
    }

    IEnumerator NextQuestionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        feedbackTextUI.text = ""; // Limpa feedback
        currentQuestionIndex++;
        if (correctAnswersCount >= QUESTIONS_TO_WIN)
        {
            EndQuiz(true); // Venceu o quiz
        }
        else if (currentQuestionIndex < questions.Count)
        {
            quizActive = true; // Reativa o timer
            LoadQuestion();
        }
        else
        {
            EndQuiz(false); // Acabaram as perguntas sem atingir o número de acertos
        }
    }

    IEnumerator EndQuizAfterDelay(float delay, bool won)
    {
        yield return new WaitForSeconds(delay);
        EndQuiz(won);
    }

    void EndQuiz(bool won)
    {
        quizActive = false;
        quizPanel.SetActive(false); // Esconde o painel do quiz

        if (won)
        {
            Debug.Log("Quiz Vencido! Iniciando Cutscene...");
            // TODO: Chamar a função para iniciar a cutscene aqui
            // Exemplo: FindObjectOfType<CutsceneManager>().PlayCutscene();
            // Se você tiver um script para gerenciar cutscenes, chame-o aqui.
            // Exemplo: CutsceneManager.Instance.PlayCutscene("NomeDaSuaCutscene");
        }
        else
        {
            Debug.Log("Quiz Perdido! Tente novamente.");
            // Lógica para o jogador ter que interagir com o PC novamente
            if (pcInteractionScript != null)
            {
                pcInteractionScript.ReenablePCInteraction();
            }
        }
    }

    // Este método será chamado por outro script (ex: um script no PC no jogo)
    public void TriggerQuizStart()
    {
        StartQuiz();
    }
}


