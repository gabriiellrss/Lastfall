using UnityEngine;

public class CursorScript : MonoBehaviour
{
    public bool IsVisible;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            IsVisible = !IsVisible;
        }

        Cursor.visible = IsVisible;
    }
}
