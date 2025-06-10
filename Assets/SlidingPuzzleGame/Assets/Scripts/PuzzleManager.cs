using UnityEngine;
using System.Collections.Generic;

public class PuzzleManager : MonoBehaviour
{
    public int gridSize = 3; // For a 3x3 puzzle
    public GameObject puzzlePiecePrefab;
    public Transform puzzleParent;
    public UIManager uiManager; // Reference to UIManager

    private GameObject[,] puzzleGrid;
    private List<GameObject> puzzlePieces;
    private int emptySlotX, emptySlotY;

    void Start()
    {
        InitializePuzzle();
    }

    public void InitializePuzzle()
    {
        // Clear existing pieces if any
        foreach (GameObject piece in puzzlePieces)
        {
            Destroy(piece);
        }
        puzzlePieces.Clear();

        puzzleGrid = new GameObject[gridSize, gridSize];
        puzzlePieces = new List<GameObject>();

        // Create pieces
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                // The last piece will be the empty slot
                if (x == gridSize - 1 && y == gridSize - 1)
                {
                    emptySlotX = x;
                    emptySlotY = y;
                    puzzleGrid[x, y] = null; // Mark as empty
                    continue;
                }

                GameObject piece = Instantiate(puzzlePiecePrefab, puzzleParent);
                piece.name = "PuzzlePiece_" + (y * gridSize + x);
                piece.transform.localPosition = new Vector3(x, -y, 0); // Adjust position based on grid

                PuzzlePiece pieceScript = piece.GetComponent<PuzzlePiece>();
                if (pieceScript != null)
                {
                    pieceScript.SetInitialPosition(x, y);
                    pieceScript.SetManager(this);
                }

                puzzleGrid[x, y] = piece;
                puzzlePieces.Add(piece);
            }
        }
        ShufflePuzzle();
    }

    void ShufflePuzzle()
    {
        // Simple shuffle for now, will improve later
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                positions.Add(new Vector2Int(x, y));
            }
        }

        // Remove the empty slot position from the list of positions to shuffle
        positions.Remove(new Vector2Int(emptySlotX, emptySlotY));

        // Shuffle the pieces
        for (int i = 0; i < puzzlePieces.Count; i++)
        {
            GameObject tempPiece = puzzlePieces[i];
            int randomIndex = Random.Range(i, puzzlePieces.Count);
            puzzlePieces[i] = puzzlePieces[randomIndex];
            puzzlePieces[randomIndex] = tempPiece;
        }

        // Reassign positions based on shuffled list
        int pieceIndex = 0;
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                if (x == emptySlotX && y == emptySlotY)
                {
                    puzzleGrid[x, y] = null;
                }
                else
                {
                    GameObject piece = puzzlePieces[pieceIndex];
                    piece.transform.localPosition = new Vector3(x, -y, 0);
                    puzzleGrid[x, y] = piece;
                    pieceIndex++;
                }
            }
        }
    }

    public void TryMovePiece(GameObject pieceObject)
    {
        int pieceX = -1, pieceY = -1;

        // Find the clicked piece's current position in the grid
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                if (puzzleGrid[x, y] == pieceObject)
                {
                    pieceX = x;
                    pieceY = y;
                    break;
                }
            }
            if (pieceX != -1) break;
        }

        if (pieceX == -1) return; // Piece not found in grid

        // Check if the piece is adjacent to the empty slot
        if ((Mathf.Abs(pieceX - emptySlotX) == 1 && pieceY == emptySlotY) ||
            (Mathf.Abs(pieceY - emptySlotY) == 1 && pieceX == emptySlotX))
        {
            // Move the piece
            Vector3 emptySlotPos = new Vector3(emptySlotX, -emptySlotY, 0);
            pieceObject.transform.localPosition = emptySlotPos;

            // Update grid and empty slot position
            puzzleGrid[emptySlotX, emptySlotY] = pieceObject;
            puzzleGrid[pieceX, pieceY] = null;

            emptySlotX = pieceX;
            emptySlotY = pieceY;

            if (CheckWinCondition())
            {
                Debug.Log("You won!");
                if (uiManager != null)
                {
                    uiManager.ShowWinPanel();
                }
            }
        }
    }

    bool CheckWinCondition()
    {
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                if (x == gridSize - 1 && y == gridSize - 1)
                {
                    if (puzzleGrid[x, y] != null) return false; // Empty slot must be in the last position
                }
                else
                {
                    PuzzlePiece pieceScript = puzzleGrid[x, y]?.GetComponent<PuzzlePiece>();
                    if (pieceScript == null || pieceScript.GetInitialX() != x || pieceScript.GetInitialY() != y)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
}


