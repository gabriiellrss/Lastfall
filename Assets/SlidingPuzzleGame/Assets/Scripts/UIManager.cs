using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject winPanel;
    public Button restartButton;
    public PuzzleManager puzzleManager;

    void Start()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClick);
        }
    }

    public void ShowWinPanel()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
    }

    public void HideWinPanel()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
    }

    void OnRestartButtonClick()
    {
        if (puzzleManager != null)
        {
            puzzleManager.InitializePuzzle(); // Re-initialize and shuffle
            HideWinPanel();
        }
    }
}


