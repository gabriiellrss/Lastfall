using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    private int initialX, initialY;
    private PuzzleManager manager;

    public void SetInitialPosition(int x, int y)
    {
        initialX = x;
        initialY = y;
    }

    public void SetManager(PuzzleManager mgr)
    {
        manager = mgr;
    }

    void OnMouseDown()
    {
        if (manager != null)
        {
            manager.TryMovePiece(gameObject);
        }
    }

    public int GetInitialX() { return initialX; }
    public int GetInitialY() { return initialY; }
}


