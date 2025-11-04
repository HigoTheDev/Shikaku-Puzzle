using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Dữ liệu 1 level Shikaku:
/// - Hệ tọa độ ô (x,y) với gốc TRÁI-DƯỚI (x tăng trái→phải, y tăng dưới→trên).
/// - Mỗi "clue" là 1 ô có số, số = diện tích hình chữ nhật phải bao nó.
/// - (Tuỳ chọn) "solutionRectangles" là lời giải cố định, dùng cho Hint/validate.
/// </summary>
[CreateAssetMenu(menuName = "Shikaku/Level Data", fileName = "ShikakuLevel")]
public class ShikakuLevelData : ScriptableObject
{
    public enum Difficulty { Easy, Medium, Hard }

    [Header("Grid size (cells)")]
    [Min(1)] public int width = 8;
    [Min(1)] public int height = 12;

    [Header("Meta")]
    public string displayName = "Level";
    public Difficulty difficulty = Difficulty.Easy;
    [TextArea] public string tipForThisLevel;

    [Serializable]
    public struct Clue
    {
        public int x;      // 0..width-1
        public int y;      // 0..height-1 (gốc trái-dưới)
        [Min(1)] public int value;  // diện tích yêu cầu
    }

    [Serializable]
    public struct RectDef
    {
        public int x;  // góc trái-dưới
        public int y;
        [Min(1)] public int w;
        [Min(1)] public int h;

        public RectInt ToRectInt() => new RectInt(x, y, w, h);
    }

    [Header("Given clues")]
    public List<Clue> clues = new();

    [Header("Optional: fixed solution (for hint/validation)")]
    public List<RectDef> solutionRectangles = new();

    /// <summary> Có khai báo lời giải cố định không? </summary>
    public bool HasFixedSolution => solutionRectangles != null && solutionRectangles.Count > 0;

    // =======================
    // TIỆN ÍCH CHO GAMEPLAY
    // =======================

    /// <summary> Kiểm tra (x,y) nằm trong lưới. </summary>
    public bool IsInside(int x, int y) => (uint)x < (uint)width && (uint)y < (uint)height;

    /// <summary> Kiểm tra RectInt nằm trong lưới. </summary>
    public bool IsInside(RectInt r) =>
        r.xMin >= 0 && r.yMin >= 0 && r.xMax < width && r.yMax < height;

    /// <summary> Trả về value của clue tại (x,y) nếu có đúng 1 clue, ngược lại false. </summary>
    public bool TryGetClueAt(int x, int y, out int value)
    {
        for (int i = 0; i < clues.Count; i++)
        {
            if (clues[i].x == x && clues[i].y == y)
            {
                value = clues[i].value;
                return true;
            }
        }
        value = -1;
        return false;
    }

    /// <summary> Liệt kê toàn bộ ô (x,y) nằm trong một RectInt. </summary>
    public IEnumerable<Vector2Int> CellsOf(RectInt r)
    {
        for (int yy = r.yMin; yy <= r.yMax; yy++)
            for (int xx = r.xMin; xx <= r.xMax; xx++)
                yield return new Vector2Int(xx, yy);
    }

    /// <summary>
    /// Nếu có solutionRectangles: kiểm tra mỗi rect chứa đúng 1 clue và diện tích = value,
    /// các rect không chồng lấn; (tuỳ chọn) kiểm tra tổng diện tích phủ kín bảng.
    /// </summary>
    public bool ValidateFixedSolution(out string report, bool requireFullCover = false)
    {
        var msgs = new List<string>();
        bool ok = true;

        if (!HasFixedSolution)
        {
            report = "No fixed solution provided.";
            return true; // không có lời giải cố định thì coi như hợp lệ
        }

        // Build map clue -> value
        var clueMap = clues.ToDictionary(c => new Vector2Int(c.x, c.y), c => c.value);
        var occupied = new HashSet<Vector2Int>();

        // 1) Kiểm tra từng rect
        for (int i = 0; i < solutionRectangles.Count; i++)
        {
            var rd = solutionRectangles[i];
            var r = rd.ToRectInt();

            if (!IsInside(r))
            {
                ok = false;
                msgs.Add($"Rect[{i}] out of bounds: {r}");
                continue;
            }

            // Đếm clue bên trong
            int count = 0; int clueVal = -1;
            foreach (var pos in CellsOf(r))
            {
                if (clueMap.TryGetValue(pos, out int v))
                {
                    count++; clueVal = v;
                    if (count > 1) break;
                }
            }
            if (count != 1)
            {
                ok = false;
                msgs.Add($"Rect[{i}] must contain exactly ONE clue, found {count}.");
            }
            else
            {
                // Diện tích khớp value?
                int area = r.width * r.height;
                if (area != clueVal)
                {
                    ok = false;
                    msgs.Add($"Rect[{i}] area {area} != clue value {clueVal}.");
                }
            }

            // Kiểm tra chồng lấn
            foreach (var cell in CellsOf(r))
            {
                if (!occupied.Add(cell))
                {
                    ok = false;
                    msgs.Add($"Rect[{i}] overlaps at cell {cell}.");
                    break;
                }
            }
        }

        // 2) (Tuỳ chọn) phủ kín bảng
        if (requireFullCover)
        {
            int covered = occupied.Count;
            int total = width * height;
            if (covered != total)
            {
                ok = false;
                msgs.Add($"Solution does not fully cover board. Covered={covered} / {total}.");
            }
        }

        report = msgs.Count == 0 ? "OK" : string.Join("\n", msgs);
        return ok;
    }

    /// <summary>
    /// Gợi ý: tính các hình chữ nhật ứng viên cho một clue (không xét chồng lấn với các khối đã đặt).
    /// Trả về các RectInt có diện tích = value và chứa ô clue, đồng thời nằm hoàn toàn trong lưới.
    /// </summary>
    public List<RectInt> GetCandidateRectsForClue(Clue c)
    {
        var candidates = new List<RectInt>();
        int v = Mathf.Max(1, c.value);

        for (int w = 1; w <= v; w++)
        {
            if (v % w != 0) continue;
            int h = v / w;

            // Duyệt mọi vị trí sao cho (c.x,c.y) nằm trong rect w×h
            for (int x0 = c.x - (w - 1); x0 <= c.x; x0++)
            {
                for (int y0 = c.y - (h - 1); y0 <= c.y; y0++)
                {
                    var r = new RectInt(x0, y0, w, h);
                    if (IsInside(r))
                        candidates.Add(r);
                }
            }
        }
        return candidates;
    }

    // =======================
    // ONVALIDATE: TỰ SỬA & CẢNH BÁO DỮ LIỆU
    // =======================
#if UNITY_EDITOR
    void OnValidate()
    {
        // Clamp size hợp lý
        width  = Mathf.Clamp(width,  1, 128);
        height = Mathf.Clamp(height, 1, 128);

        // Chuẩn hoá clues: bỏ cái ngoài biên, gộp trùng (giữ cái sau cùng)
        var map = new Dictionary<(int x,int y), Clue>();
        for (int i = 0; i < clues.Count; i++)
        {
            var c = clues[i];
            if (!IsInside(c.x, c.y))
            {
                Debug.LogWarning($"{name}: Clue out of bounds removed ({c.x},{c.y})");
                continue;
            }
            map[(c.x, c.y)] = new Clue { x = c.x, y = c.y, value = Mathf.Max(1, c.value) };
        }
        clues = map.Values.ToList();

        // Chuẩn hoá solutionRectangles: bỏ ngoài biên, w/h<1
        if (solutionRectangles == null) solutionRectangles = new List<RectDef>();
        var cleaned = new List<RectDef>();
        for (int i = 0; i < solutionRectangles.Count; i++)
        {
            var rd = solutionRectangles[i];
            rd.w = Mathf.Max(1, rd.w);
            rd.h = Mathf.Max(1, rd.h);

            if (!IsInside(new RectInt(rd.x, rd.y, rd.w, rd.h)))
            {
                Debug.LogWarning($"{name}: Solution rect out of bounds removed ({rd.x},{rd.y},{rd.w},{rd.h})");
                continue;
            }
            cleaned.Add(rd);
        }
        solutionRectangles = cleaned;

        // Nếu có lời giải: chạy validate nhẹ (không bắt buộc phủ kín)
        if (HasFixedSolution)
        {
            if (!ValidateFixedSolution(out string report, false))
                Debug.LogWarning($"{name}: Fixed solution issues:\n{report}");
        }
    }
#endif
}
