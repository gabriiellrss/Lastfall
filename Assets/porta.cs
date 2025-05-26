using UnityEngine;

public class Porta : MonoBehaviour
{
    public Transform portaLeft;
    public Transform portaRight;

    public Vector3 leftOpenOffset = new Vector3(0, 0, -2f);  // Ajuste conforme seu cenário
    public Vector3 rightOpenOffset = new Vector3(0, 0, 2f);  // Ajuste conforme seu cenário

    public float velocidade = 2f;

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;

    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    private bool abrir = false;

    void Start()
    {
        leftClosedPos = portaLeft.localPosition;
        rightClosedPos = portaRight.localPosition;

        leftOpenPos = leftClosedPos + leftOpenOffset;
        rightOpenPos = rightClosedPos + rightOpenOffset;
    }

    void Update()
    {
        if (abrir)
        {
            portaLeft.localPosition = Vector3.Lerp(portaLeft.localPosition, leftOpenPos, Time.deltaTime * velocidade);
            portaRight.localPosition = Vector3.Lerp(portaRight.localPosition, rightOpenPos, Time.deltaTime * velocidade);
        }
        else
        {
            portaLeft.localPosition = Vector3.Lerp(portaLeft.localPosition, leftClosedPos, Time.deltaTime * velocidade);
            portaRight.localPosition = Vector3.Lerp(portaRight.localPosition, rightClosedPos, Time.deltaTime * velocidade);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            abrir = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            abrir = false;
        }
    }
}
