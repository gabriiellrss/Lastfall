using UnityEngine;
using UnityEngine.UI;

public class OpcoesController : MonoBehaviour
{
    public Slider volumeSlider;
    public Slider sensibilidadeSlider;
    public GameObject menuPrincipal;
    public GameObject painelCred;
    public GameObject painelConfig;

    void Start()
    {
        volumeSlider.value = PlayerPrefs.GetFloat("volume", 1f);
        sensibilidadeSlider.value = PlayerPrefs.GetFloat("sensibilidade", 1f);
    }

    public void AlterarVolume(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat("volume", valor);
    }

    public void AlterarSensibilidade(float valor)
    {
        PlayerPrefs.SetFloat("sensibilidade", valor);
    }

    public void Voltar()
    {
        painelConfig.SetActive(false);
        menuPrincipal.SetActive(true);
    }
}