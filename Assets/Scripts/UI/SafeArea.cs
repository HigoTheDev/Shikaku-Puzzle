using NUnit.Framework.Internal.Commands;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class SafeArea : MonoBehaviour
{
    private RectTransform rt;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    void OnEnable(){rt = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafeArea || 
            lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
        {
            Apply();
        }
    }
    
    void Apply()
    {
        if (rt == null) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        var sa = Screen.safeArea;
        lastSafeArea = sa;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        var size = new Vector2(sa.width/Screen.width, sa.height/Screen.height);
        var pos = new Vector2(sa.x / Screen.width, sa.y/Screen.height);
        rt.anchorMin = pos;
        rt.anchorMax = pos + size;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
