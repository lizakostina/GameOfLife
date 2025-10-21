using UnityEngine;
using System.Collections;

public class Cell : MonoBehaviour {
    public enum CellColor { Dead, Blue, Red }
    
    public bool isAlive = false;
    public CellColor color = CellColor.Dead;
    private Renderer cellRenderer;
    
    [Header("Materials")]
    public Material aliveMaterial;
    public Material deadMaterial;
    public Material whiteMaterial;
    public Material blackMaterial;
    
    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public float scaleMultiplier = 1.2f;
    private Material currentMaterial;
    private Coroutine colorAnimationCoroutine;
    private Coroutine scaleAnimationCoroutine;
    private Vector3 originalScale;
    
    void Awake() {
        InitializeRenderer();
        originalScale = transform.localScale;
    }
    
    void Start() {
        InitializeRenderer();
        UpdateVisual();
    }
    
    private void InitializeRenderer() {
        if (cellRenderer == null) {
            cellRenderer = GetComponent<Renderer>();
        }
    }
    
    public void SetAlive(bool alive) {
        SetAlive(alive, CellColor.Dead);
    }
    
    public void SetAlive(bool alive, CellColor newColor) {
        bool stateChanged = (isAlive != alive) || (color != newColor);
        isAlive = alive;
        color = newColor;
        
        if (stateChanged) {
            UpdateVisual();
        }
    }
    
    void UpdateVisual() {
        InitializeRenderer();
        Material targetMaterial = GetTargetMaterial();
        StartColorAnimation(targetMaterial);
    }
    
    Material GetTargetMaterial() {
        if (!isAlive) {
            return deadMaterial ?? aliveMaterial;
        } else {
            switch (color) {
                case CellColor.Blue:
                    return whiteMaterial ?? aliveMaterial;
                case CellColor.Red:
                    return blackMaterial ?? aliveMaterial;
                case CellColor.Dead:
                default:
                    return aliveMaterial;
            }
        }
    }
    
    void StartColorAnimation(Material targetMaterial) {
        if (colorAnimationCoroutine != null) {
            StopCoroutine(colorAnimationCoroutine);
        }
        
        colorAnimationCoroutine = StartCoroutine(AnimateColor(targetMaterial));
    }

    IEnumerator AnimateColor(Material targetMaterial)
    {
        Material startMaterial = cellRenderer.material;
        float elapsed = 0f;

        Material tempMaterial = new Material(startMaterial);
        cellRenderer.material = tempMaterial;

        Color startColor = startMaterial.color;
        Color targetColor = targetMaterial.color;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            tempMaterial.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        cellRenderer.material = targetMaterial;
        Destroy(tempMaterial);
    }
    
    public void StopAnimations() {
        if (colorAnimationCoroutine != null) {
            StopCoroutine(colorAnimationCoroutine);
            colorAnimationCoroutine = null;
        }
        if (scaleAnimationCoroutine != null) {
            StopCoroutine(scaleAnimationCoroutine);
            scaleAnimationCoroutine = null;
        }
    }
    
    public void ForceResetState() {
        StopAnimations();
        InitializeRenderer();
        cellRenderer.material = GetTargetMaterial();
    }
}