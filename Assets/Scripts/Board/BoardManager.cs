using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
public class BoardManager : MonoBehaviour
{
    [Header("Scene Refs")] 
    public RectTransform boardRoot;
    public GridLayoutGroup grid;
    public RectTransform placedLayer;
    public RectTransform previewLayer;
    public GameObject tilePrefab;
    public GameObject rectangleViewPrefab;
    public ShikakuLevelData level;
    
    [Header("Grid Style")]
    public int paddingLeft = 20, paddingRight = 20, paddingTop = 20, paddingBottom = 20;
    public Vector2 spacing = new Vector2(12, 12);
    public bool fitByWidth = true;

    [Header("Preview")] public Color previewColor = new Color(1, 1, 1, 0.25f);

    private Tile[,] tiles;
    private Vector2Int dragStart;
    private bool dragging;
    private RectInt previewRect;
    private Image previewImg;
    private List<RectInt> placedRects = new();
    private List<GameObject> placedViews = new();

    private Vector2 cellSize;
    private Vector2Int gridSize;
    private bool built;

    void Awake()
    {
        if (level != null)
            gridSize = new Vector2Int(level.width, level.height);
        else
            gridSize = new Vector2Int(5, 5); // fallback nhỏ để test

        ApplyGridLayoutSettings();
    }


    void Start()
    {
        BuildGrid();
        BuildPreviewImage();
        built = true;
    }

    void OnRectTransformDimensionsChange()
    {
        if (built) Reflow();
    }

    void ApplyGridLayoutSettings()
    {
        grid.padding.left = paddingLeft;
        grid.padding.right = paddingRight;
        grid.padding.top = paddingTop;
        grid.padding.bottom = paddingBottom;
        grid.spacing = spacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = gridSize.x;
        grid.childAlignment = TextAnchor.UpperLeft;
    }

    void ComputeCellSize()
    {
        var rt = boardRoot;
        float totalW = rt.rect.width - (grid.padding.left + grid.padding.right) - spacing.x * (gridSize.x - 1);
        float totalH = rt.rect.height - (grid.padding.top + grid.padding.bottom) - spacing.y * (gridSize.y - 1);
        float sizeW = Mathf.Floor(totalW / gridSize.x);
        float sizeH = Mathf.Floor(totalH / gridSize.y);
        float size = fitByWidth ? sizeW : sizeH;
        size = Mathf.Max(8, size);
        cellSize = new Vector2(size, size);
        grid.cellSize = cellSize;
    }

    void BuildGrid()
    {
        ComputeCellSize();
        for(int i = grid.transform.childCount - 1; i >= 0; i--) DestroyImmediate(grid.transform.GetChild(i).gameObject);
        tiles = new Tile[gridSize.x, gridSize.y];
        for (int y = gridSize.y - 1; y >= 0; y--)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                var go = Instantiate(tilePrefab, grid.transform);
                var t = go.GetComponent<Tile>();
                t.Init(x, y, this);
                tiles[x, y] = t;
            }
        }

        foreach (var c in level.clues)
        {
            tiles[c.x, c.y].SetClue(c.value);
        }
    }

    void BuildPreviewImage()
    {
        var go = new GameObject("PreviewRect", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(previewLayer, false);
        previewImg = go.GetComponent<Image>();
        previewImg.color = previewColor;
        previewImg.raycastTarget = false;
        previewImg.enabled = false;
        
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
    }

    void Reflow()
    {
        ComputeCellSize();
        for (int i = 0; i < placedRects.Count; i++)
        {
            var rt = placedViews[i].GetComponent<RectTransform>();
            PositionRectTransform(rt, placedRects[i]);
        }

        if (previewImg && previewImg.enabled)
        {
            PositionRectTransform(previewImg.rectTransform, previewRect);
        }
    }

    public void OnPointerDownTile(int x, int y)
    {
        dragging = true;
        dragStart = new Vector2Int(x, y);
        ShowPreview (MakeRect(dragStart, new Vector2Int(x, y)));
    }

    public void OnPointerEnterTile(int x, int y)
    {
        if(!dragging) return;
        ShowPreview (MakeRect(dragStart, new Vector2Int(x, y)));
    }

    public void OnPointerUpTile(int x, int y)
    {
        if (!dragging) return;
        dragging = false;
        var r = MakeRect(dragStart, new Vector2Int(x, y));
        HidePreview();
        TryPlace(r);
    }

    RectInt MakeRect(Vector2Int a, Vector2Int b)
    {
        int minX = Mathf.Min(a.x, b.x);
        int maxX = Mathf.Max(a.x, b.x);
        int minY = Mathf.Min(a.y, b.y);
        int maxY = Mathf.Max(a.y, b.y);
        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    void ShowPreview(RectInt r)
    {
        previewRect = r;
        PositionRectTransform(previewImg.rectTransform, r);
        previewImg.enabled = true;
    }

    void HidePreview()
    {
        if (previewImg) previewImg.enabled = false;
    }

    bool ContainsExactlyOneClue(RectInt r, out int value) {
        int count = 0; value = -1;
        foreach (var c in level.clues) {
            if (r.Contains(new Vector2Int(c.x, c.y))) {
                count++; value = c.value;
                if (count > 1) break;
            }
        }
        return count == 1;
    }

    public bool IsInside(RectInt r)
    {
        return r.xMin >= 0 && r.yMin >= 0 && r.xMax < gridSize.x && r.yMax < gridSize.y;
    }
    
    public bool OverlapsPlaced(RectInt r) {
        foreach (var p in placedRects)
            if (p.Overlaps(r)) return true;
        return false;
    }
    
    void TryPlace(RectInt r) {
        if (!IsInside(r) || OverlapsPlaced(r)) { FeedbackInvalid(); return; }
        if (!ContainsExactlyOneClue(r, out int val)) { FeedbackInvalid(); return; }
        if (r.width * r.height != val) { FeedbackInvalid(); return; }

        placedRects.Add(r);
        CreatePlacedView(r, val);
        if (IsBoardComplete()) OnWin();
    }
    
    void CreatePlacedView(RectInt r, int value) {
        var go = Instantiate(rectangleViewPrefab, placedLayer);
        var rv = go.GetComponent<RectangleView>();
        rv.SetValue(value);

        // Gán màu (xoay vòng)
        if (palette != null && palette.Length > 0) {
            rv.SetColor(palette[colorIndex % palette.Length]);
            colorIndex++;
        }

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1); // top-left
        rt.pivot = new Vector2(0, 1);

        PositionRectTransform(rt, r);
        placedViews.Add(go);
    }

    
    bool IsBoardComplete() {
        int covered = 0;
        foreach (var r in placedRects) covered += r.width * r.height;
        return covered == gridSize.x * gridSize.y;
    }
    
    void FeedbackInvalid() {
        // TODO: âm thanh/flash; tạm thời log
        Debug.LogWarning("Invalid placement");
    }
    
    void OnWin() {
        Debug.Log("WIN!");
        // TODO: popup + unlock
    }
    
    void PositionRectTransform(RectTransform rt, RectInt r) {
        // bước giữa các cột/hàng (ô + spacing)
        float stepX = cellSize.x + grid.spacing.x;
        float stepY = cellSize.y + grid.spacing.y;

        // offset theo padding
        float offX = grid.padding.left;
        float offY = grid.padding.top;

        // Do anchor top-left + Grid fill từ trên xuống:
        // hàng "topRow" là (height - yMax - 1)
        int topRow = gridSize.y - r.yMax - 1;

        Vector2 pos = new Vector2(
            offX + r.xMin * stepX,
            -(offY + topRow * stepY)
        );

        Vector2 size = new Vector2(
            r.width * cellSize.x + (r.width - 1) * grid.spacing.x,
            r.height * cellSize.y + (r.height - 1) * grid.spacing.y
        );

        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
    
    // (tuỳ chọn) Chuyển ngược lại: pixel → ô (ví dụ cho Eraser chọn ô theo tap)
    public Vector2Int AnchoredToGrid(Vector2 anchoredPos) {
        // anchoredPos tính theo top-left của Grid (âm ở trục Y)
        float stepX = cellSize.x + grid.spacing.x;
        float stepY = cellSize.y + grid.spacing.y;
        float offX = grid.padding.left;
        float offY = grid.padding.top;

        float px = anchoredPos.x - offX;
        float py = -anchoredPos.y - offY;

        int col = Mathf.FloorToInt(px / stepX);
        int rowTopDown = Mathf.FloorToInt(py / stepY);
        int y = gridSize.y - 1 - rowTopDown;

        col = Mathf.Clamp(col, 0, gridSize.x - 1);
        y   = Mathf.Clamp(y,   0, gridSize.y - 1);
        return new Vector2Int(col, y);
    }

    // API Erase: xoá khối chứa ô (x,y)
    public void EraseAt(int x, int y) {
        for (int i = placedRects.Count - 1; i >= 0; i--) {
            if (placedRects[i].Contains(new Vector2Int(x, y))) {
                Destroy(placedViews[i]);
                placedViews.RemoveAt(i);
                placedRects.RemoveAt(i);
                break;
            }
        }
    }

    // Hỗ trợ UI test Eraser theo vị trí chuột (nếu muốn)
    public void EraseAtPointerInBoard(Vector2 screenPoint) {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            grid.GetComponent<RectTransform>(), screenPoint, null, out var local);
        var cell = AnchoredToGrid(local);
        EraseAt(cell.x, cell.y);
    }
    
    [Header("Colors for placed rectangles")]
    public Color[] palette = new Color[] {
        new Color(0.4f, 0.8f, 1f),   // xanh dương nhạt
        new Color(0.6f, 0.9f, 0.6f), // xanh lá nhạt
        new Color(1f, 0.7f, 0.5f),   // cam nhạt
        new Color(0.9f, 0.5f, 0.5f), // đỏ nhạt
        new Color(0.8f, 0.6f, 1f)    // tím nhạt
    };
    int colorIndex = 0;

    public List<RectInt> GetPlacedRects() => placedRects;

    public void ForcePlace(RectInt r) {
        // giống TryPlace nhưng bỏ qua kiểm tra
        placedRects.Add(r);
        CreatePlacedView(r, r.width * r.height);
    }

    public void EraseAll() {
        for (int i = placedViews.Count - 1; i >= 0; i--) {
            Destroy(placedViews[i]);
        }
        placedViews.Clear();
        placedRects.Clear();
    }

}
