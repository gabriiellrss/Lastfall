using UnityEngine;
using System.Collections.Generic;

public class ControlaLuzesEnergia : MonoBehaviour
{
    [Header("Controle")]
    public bool reduzirImpacto = false;

    [Header("Luzes para controlar")]
    public List<Light> luzes;

    [Header("Cores")]
    public Color corVermelha = Color.red;
    public Color corVerde = new Color(0.714f, 1f, 0.4f); // Hex: #B6FF66

    [Header("Velocidade da transição")]
    public float velocidade = 2f;

    void Update()
    {
        Color alvo = reduzirImpacto ? corVerde : corVermelha;

        foreach (Light luz in luzes)
        {
            if (luz != null)
            {
                luz.color = Color.Lerp(luz.color, alvo, Time.deltaTime * velocidade);
            }
        }
    }
}
