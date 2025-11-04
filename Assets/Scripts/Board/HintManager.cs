using UnityEngine;

public class HintManager : MonoBehaviour {
    public ShikakuLevelData level;
    public BoardManager board;

    public void ShowHint() {
        if (level == null || !level.HasFixedSolution) {
            Debug.LogWarning("No fixed solution for this level!");
            return;
        }

        // Lấy 1 rect trong solution chưa đặt
        foreach (var rd in level.solutionRectangles) {
            var r = rd.ToRectInt();
            bool alreadyPlaced = false;
            foreach (var pr in board.GetPlacedRects())
                if (pr == r) { alreadyPlaced = true; break; }

            if (!alreadyPlaced) {
                board.ForcePlace(r);
                Debug.Log("Hint placed!");
                return;
            }
        }
    }
}