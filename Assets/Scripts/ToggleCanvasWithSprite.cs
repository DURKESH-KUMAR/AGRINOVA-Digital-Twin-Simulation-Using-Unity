using UnityEngine;
using UnityEngine.UI;

public class ToggleCanvasWithSprite : MonoBehaviour
{
    [Header("Canvas Panels")]
    
    public GameObject canvasB; // Target panel

    [Header("Toggle Button")]
    public Image toggleImage;   // Button image component
    public Sprite onSprite;     // ON sprite
    public Sprite offSprite;    // OFF sprite

    [Header("Settings")]
    public bool isOn = false;   // Current state

    void Start()
    {
        UpdateUI();
    }

    // 🔁 Called by Button
    public void Toggle()
    {
        isOn = !isOn;
        UpdateUI();
    }

    void UpdateUI()
    {
        // 🎨 Change sprite
        if (toggleImage != null)
        {
            toggleImage.sprite = isOn ? onSprite : offSprite;
        }

        // 🔀 Switch canvases
        if ( canvasB != null)
        {
            
            canvasB.SetActive(isOn);
        }
    }
}