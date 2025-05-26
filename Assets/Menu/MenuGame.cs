using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject configMenu;

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (configMenu.activeSelf)
            {
                configMenu.SetActive(false);
                pauseMenu.SetActive(true);
                return;
            }

            isPaused = !isPaused;

            if (isPaused)
                Pause();
            else
                Resume();
        }
    }

    public void Pause()
    {
        Time.timeScale = 0f;
        pauseMenu.SetActive(true);
        isPaused = true;
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);
        configMenu.SetActive(false);
        isPaused = false;
    }

    public void OpenConfig()
    {
        pauseMenu.SetActive(false);
        configMenu.SetActive(true);
    }
    public void VoltarMenu()
    {
        pauseMenu.SetActive(true);
        configMenu.SetActive(false);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu"); // substitua pelo nome real da cena
    }
}