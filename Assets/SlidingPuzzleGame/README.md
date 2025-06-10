# Mini-Jogo de Puzzle de Deslizar para Unity

Este projeto contém um mini-jogo de puzzle de deslizar básico implementado em Unity, utilizando scripts C#.

## Estrutura do Projeto

O projeto está organizado nas seguintes pastas:

- `SlidingPuzzleGame/Assets/Scripts`: Contém todos os scripts C# do jogo (`PuzzleManager.cs`, `PuzzlePiece.cs`, `UIManager.cs`).
- `SlidingPuzzleGame/Assets/Scenes`: Para as cenas do Unity (a cena principal do jogo).
- `SlidingPuzzleGame/Assets/Prefabs`: Para prefabs de peças de puzzle ou outros elementos reutilizáveis.
- `SlidingPuzzleGame/Assets/Materials`: Para materiais usados nas peças do puzzle.
- `SlidingPuzzleGame/Assets/Sprites`: Para as imagens que serão usadas como texturas das peças do puzzle.

## Como Configurar e Executar o Projeto no Unity

1.  **Abrir o Projeto no Unity:**
    *   Abra o Unity Hub.
    *   Clique em `Add` e navegue até a pasta `SlidingPuzzleGame` (a pasta raiz do projeto que contém a pasta `Assets`).
    *   Selecione a pasta e clique em `Add Project`.
    *   Abra o projeto no Unity Editor.

2.  **Configurar a Cena:**
    *   Crie uma nova cena (File > New Scene) ou use a cena existente.
    *   Crie um GameObject vazio na hierarquia e renomeie-o para `PuzzleManager`.
    *   Anexe o script `PuzzleManager.cs` a este GameObject.

3.  **Configurar as Peças do Puzzle:**
    *   Crie um GameObject 2D (Sprite) que servirá como prefab para as peças do puzzle. Pode ser um quadrado simples ou uma imagem.
    *   Anexe o script `PuzzlePiece.cs` a este prefab.
    *   Arraste este prefab para a slot `Puzzle Piece Prefab` no componente `PuzzleManager` no inspetor.

4.  **Configurar o Parent do Puzzle:**
    *   Crie um GameObject vazio na hierarquia (pode ser um GameObject 2D para melhor visualização) e renomeie-o para `PuzzleParent`.
    *   Arraste este GameObject para a slot `Puzzle Parent` no componente `PuzzleManager` no inspetor. As peças serão instanciadas como filhas deste GameObject.

5.  **Configurar a UI (Interface do Utilizador):**
    *   Crie um Canvas (GameObject > UI > Canvas).
    *   Dentro do Canvas, crie um Painel (GameObject > UI > Panel) para a mensagem de vitória e renomeie-o para `WinPanel`.
    *   Adicione um TextMeshPro (ou Text Legacy) ao `WinPanel` com a mensagem "Você Venceu!".
    *   Adicione um Botão (GameObject > UI > Button) ao Canvas e renomeie-o para `RestartButton`.
    *   Crie um GameObject vazio na hierarquia e renomeie-o para `UIManager`.
    *   Anexe o script `UIManager.cs` a este GameObject.
    *   Arraste o `WinPanel` para a slot `Win Panel` no componente `UIManager`.
    *   Arraste o `RestartButton` para a slot `Restart Button` no componente `UIManager`.
    *   Arraste o GameObject `PuzzleManager` para a slot `Puzzle Manager` no componente `UIManager`.

6.  **Executar o Jogo:**
    *   Pressione o botão Play no Unity Editor para iniciar o jogo.

## Scripts C#

### `PuzzleManager.cs`

Gerencia a lógica principal do puzzle:

-   **`gridSize`**: Define o tamanho da grelha do puzzle (ex: 3 para 3x3).
-   **`puzzlePiecePrefab`**: O prefab da peça do puzzle a ser instanciada.
-   **`puzzleParent`**: O transform pai onde as peças do puzzle serão colocadas.
-   **`uiManager`**: Referência ao script `UIManager` para controlar a UI.
-   **`InitializePuzzle()`**: Cria e posiciona as peças do puzzle, definindo o espaço vazio.
-   **`ShufflePuzzle()`**: Embaralha as peças do puzzle.
-   **`TryMovePiece(GameObject pieceObject)`**: Tenta mover uma peça clicada se for adjacente ao espaço vazio.
-   **`CheckWinCondition()`**: Verifica se o puzzle foi resolvido.

### `PuzzlePiece.cs`

Controla o comportamento individual de cada peça do puzzle:

-   **`SetInitialPosition(int x, int y)`**: Define a posição inicial correta da peça na grelha.
-   **`SetManager(PuzzleManager mgr)`**: Define a referência ao `PuzzleManager`.
-   **`OnMouseDown()`**: Deteta o clique do rato na peça e notifica o `PuzzleManager` para tentar mover a peça.

### `UIManager.cs`

Gerencia a interface do utilizador:

-   **`winPanel`**: O GameObject do painel de vitória.
-   **`restartButton`**: O botão para reiniciar o jogo.
-   **`puzzleManager`**: Referência ao script `PuzzleManager` para reiniciar o jogo.
-   **`ShowWinPanel()`**: Ativa o painel de vitória.
-   **`HideWinPanel()`**: Desativa o painel de vitória.
-   **`OnRestartButtonClick()`**: Chamado quando o botão de reiniciar é clicado, reinicia o puzzle.

## Próximos Passos e Melhorias

-   **Embaralhamento Solucionável**: Implementar um algoritmo de embaralhamento que garanta que o puzzle é sempre solucionável.
-   **Animações**: Adicionar animações suaves para o movimento das peças.
-   **Imagens de Fundo**: Permitir que o utilizador escolha uma imagem de fundo para o puzzle.
-   **Contador de Movimentos/Tempo**: Adicionar um contador de movimentos ou um temporizador.
-   **Diferentes Tamanhos de Grelha**: Permitir que o utilizador escolha o tamanho da grelha (ex: 4x4, 5x5).


