using UnityEngine;

public class SireneRotator : MonoBehaviour
{
    public float rotationSpeed = 180f;

    public enum EixoRotacao { X, Y, Z }
    public EixoRotacao eixo = EixoRotacao.Y;

    void Update()
    {
        Vector3 direcao = Vector3.zero;

        switch (eixo)
        {
            case EixoRotacao.X:
                direcao = Vector3.right;
                break;
            case EixoRotacao.Y:
                direcao = Vector3.up;
                break;
            case EixoRotacao.Z:
                direcao = Vector3.forward;
                break;
        }

        transform.Rotate(direcao * rotationSpeed * Time.deltaTime);
    }
}
