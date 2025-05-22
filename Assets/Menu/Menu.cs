using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class Menu : MonoBehaviour
{
    private bool creditosAtivos = false;
    public GameObject menuPrincipal;
    public GameObject painelCred;
    public GameObject painelConfig;
    public void Iniciar()
    {
        SceneManager.LoadScene("MapaLab");
    }

    public void config()
    {

    }

    public void MostrarCreditos()
    {
        menuPrincipal.SetActive(false);
        painelCred.SetActive(true);
        creditosAtivos = true;
    }

    void Update()
    {
        if (creditosAtivos && Input.anyKeyDown)
        {
            Debug.Log("Qualquer tecla para voltar");
            VoltarMenu();
        }
    }

    public void MostrarConfig()
    {
        painelConfig.SetActive(true);
        painelCred.SetActive(false);
        menuPrincipal.SetActive(false);
    }

    public void VoltarMenu()
    {
        painelCred.SetActive(false);
        painelConfig.SetActive(false);
        menuPrincipal.SetActive(true);
        creditosAtivos = false;
    }
    public void Sair()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
