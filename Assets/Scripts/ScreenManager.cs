using UnityEngine;

public class ScreenManager : MonoBehaviour {
    [Header("Настройки экрана")]
    public int targetWidth = 1920;
    public int targetHeight = 1080;
    public bool fullscreen = true;
    public int targetFrameRate = 60;
    
    void Start() {
        SetScreenSettings();
    }
    
    void SetScreenSettings() {
        Screen.SetResolution(targetWidth, targetHeight, fullscreen);
        Application.targetFrameRate = targetFrameRate;
    }
    
    public void ToggleFullscreen() {
        fullscreen = !fullscreen;
        Screen.SetResolution(targetWidth, targetHeight, fullscreen);
    }
}