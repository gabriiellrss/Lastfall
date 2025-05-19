using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    public Image img;
    public Vector2 normalSize = new Vector2(32, 32);
    public Vector2 expandedSize = new Vector2(60, 60);
    public float speed = 10f;

    void Update()
    {
        // exemplo: se o jogador estiver correndo (Input.GetKey(KeyCode.LeftShift))
        bool running = Input.GetKey(KeyCode.LeftShift);

        Vector2 target = running ? expandedSize : normalSize;
        img.rectTransform.sizeDelta = Vector2.Lerp(img.rectTransform.sizeDelta, target, Time.deltaTime * speed);
    }
}
