using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RectangleView : MonoBehaviour {
    public TMP_Text centerLabel;
    public Image bg;

    public void SetValue(int v){
        if (centerLabel) centerLabel.text = v.ToString();
    }
    
    public void SetColor(Color c) {
        if (bg) bg.color = c;
    }
}