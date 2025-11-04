using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class Tile : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
{
    public int x, y;
    public TMP_Text clueLabel;
    public Image bg;
    private BoardManager board;

    public void Init(int x, int y, BoardManager bp)
    {
        this.x = x;
        this.y = y;
        board = bp;
        if (clueLabel) clueLabel.gameObject.SetActive(false);
    }

    public void SetClue(int value)
    {
        if (clueLabel)
        {
            clueLabel.text = value.ToString();
            clueLabel.gameObject.SetActive(true);
        }
    }

    public void OnPointerDown(PointerEventData e) => board.OnPointerDownTile(x, y);
    public void OnPointerEnter(PointerEventData e) => board.OnPointerEnterTile(x, y);
    public void OnPointerUp(PointerEventData e) => board.OnPointerUpTile(x, y);
}
