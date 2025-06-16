using UnityEngine;

public class ArrowPointerUI : MonoBehaviour
{
    public Transform target3D;
    public RectTransform arrowImageUI;
    public Camera mainCamera;

    [Tooltip("Distância da seta em relação ao centro da tela.")]
    public float screenRadius = 200f;

    [Tooltip("Distância mínima para considerar que chegou ao objetivo.")]
    public float hideDistance = 3f;

    void Update()
    {
        if (target3D == null || arrowImageUI == null || mainCamera == null) return;

        float distanceToTarget = Vector3.Distance(mainCamera.transform.position, target3D.position);

        // ?? Oculta a seta se estiver próximo do alvo
        if (distanceToTarget <= hideDistance)
        {
            arrowImageUI.gameObject.SetActive(false);
            return;
        }
        else
        {
            arrowImageUI.gameObject.SetActive(true);
        }

        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 screenPos = mainCamera.WorldToScreenPoint(target3D.position);

        if (screenPos.z < 0)
        {
            screenPos *= -1;
        }

        Vector3 dir = (screenPos - screenCenter).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        arrowImageUI.rotation = Quaternion.Euler(0, 0, angle);
        Vector2 canvasPos = dir * screenRadius;
        arrowImageUI.anchoredPosition = canvasPos;
    }
}
