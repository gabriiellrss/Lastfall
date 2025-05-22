using UnityEngine;

public class CreditosRolar : MonoBehaviour
{
    public RectTransform creditosTransform;
    public float velocidade = 50f;

    private bool rolando = false;
    private Vector2 posInicial;

    void Start()
    {
        posInicial = creditosTransform.anchoredPosition;
    }

    void OnEnable()
    {
        creditosTransform.anchoredPosition = posInicial;
        rolando = true;
    }

    void Update()
    {
        if (rolando)
        {
            creditosTransform.anchoredPosition += Vector2.up * velocidade * Time.deltaTime;
        }

        if (Input.anyKeyDown)
        {
            rolando = false;
            gameObject.SetActive(false);
        }
    }
}