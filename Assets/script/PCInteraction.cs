using UnityEngine;

public class PCInteraction : MonoBehaviour
{
    // Esta é a referência pública para o seu script QuizManager.
    // Você precisará arrastar o GameObject que contém o componente QuizManager
    // para este slot no Inspector do Unity, depois de anexar este script ao seu PC.
    public QuizManager quizManager;

    // Variável interna para rastrear se o jogador está dentro da área de interação do PC.
    private bool _playerInRange = false;

    // Variável interna para controlar se o quiz já está ativo.
    // Isso evita que o quiz seja iniciado várias vezes se o jogador spammar a tecla 'E'.
    private bool _quizActive = false;

    void Update()
    {
        // A cada frame, verifica se todas as condições para iniciar o quiz são atendidas:
        // 1. O jogador está dentro da área de interação (_playerInRange é verdadeiro).
        // 2. A tecla 'E' foi pressionada neste exato frame (Input.GetKeyDown(KeyCode.E)).
        // 3. O quiz não está ativo no momento (!_quizActive é verdadeiro).
        if (_playerInRange && Input.GetKeyDown(KeyCode.E) && !_quizActive)
        {
            Debug.Log("Tecla 'E' pressionada. Iniciando Quiz...");

            // Verifica se a referência 'quizManager' foi corretamente atribuída no Inspector.
            if (quizManager != null)
            {
                // Chama o método 'TriggerQuizStart()' no script QuizManager para iniciar o quiz.
                quizManager.TriggerQuizStart();
                // Define '_quizActive' como verdadeiro para indicar que o quiz está em andamento.
                _quizActive = true;
            }
            else
            {
                // Se a referência não foi atribuída, exibe um erro no console para ajudar na depuração.
                Debug.LogError("QuizManager não atribuído ao PCInteraction. Por favor, arraste o GameObject do QuizManager para o slot no Inspector.");
            }
        }
    }

    // Este método é chamado automaticamente pelo Unity quando outro Collider (marcado como Trigger)
    // entra na área de colisão (trigger) deste GameObject.
    void OnTriggerEnter(Collider other)
    {
        // Verifica se o GameObject que entrou na área tem a tag "Player".
        // (Lembre-se de que seu GameObject de jogador deve ter a tag "Player" atribuída no Inspector).
        if (other.CompareTag("Player"))
        {
            // Se for o jogador, define '_playerInRange' como verdadeiro.
            _playerInRange = true;
            Debug.Log("Jogador entrou na área de interação do PC. Pressione 'E' para iniciar o quiz.");
            // Opcional: Aqui você pode adicionar código para exibir uma mensagem na UI
            // para o jogador, como "Pressione E para interagir".
        }
    }

    // Este método é chamado automaticamente pelo Unity quando outro Collider (marcado como Trigger)
    // sai da área de colisão (trigger) deste GameObject.
    void OnTriggerExit(Collider other)
    {
        // Verifica se o GameObject que saiu da área tem a tag "Player".
        if (other.CompareTag("Player"))
        {
            // Se for o jogador, define '_playerInRange' como falso.
            _playerInRange = false;
            Debug.Log("Jogador saiu da área de interação do PC.");
            // Opcional: Aqui você pode adicionar código para esconder a mensagem da UI.
        }
    }

    // Este método é público e será chamado pelo script QuizManager
    // (especificamente na função EndQuiz) quando o quiz terminar.
    // Ele serve para "rearmar" a interação com o PC, permitindo que o jogador
    // inicie o quiz novamente se desejar (e se a lógica do jogo permitir).
    public void ReenablePCInteraction()
    {
        // Define '_quizActive' como falso, permitindo que o quiz seja iniciado novamente.
        _quizActive = false;
        Debug.Log("Interação com o PC reativada.");
        // Opcional: Se o jogador ainda estiver na área, você pode reativar o prompt "Pressione E".
        // if (_playerInRange) { /* Ativar prompt novamente */ }
    }
}
