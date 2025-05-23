using UnityEngine;

public class LightPiscando : MonoBehaviour
{
    public Light luz;
    public float intensidadeMinima = 0f;
    public float intensidadeMaxima = 1f;
    public float velocidadePiscar = 2f; // Quanto maior, mais rápido

    private bool aumentando = true;

    void Start()
    {
        if (luz == null)
            luz = GetComponent<Light>();
    }

    void Update()
    {
        if (aumentando)
        {
            luz.intensity += velocidadePiscar * Time.deltaTime;
            if (luz.intensity >= intensidadeMaxima)
            {
                luz.intensity = intensidadeMaxima;
                aumentando = false;
            }
        }
        else
        {
            luz.intensity -= velocidadePiscar * Time.deltaTime;
            if (luz.intensity <= intensidadeMinima)
            {
                luz.intensity = intensidadeMinima;
                aumentando = true;
            }
        }
    }
}
