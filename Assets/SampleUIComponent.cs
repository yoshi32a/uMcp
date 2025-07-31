using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class UISettings
{
    public Color primaryColor = Color.blue;
    public float animationSpeed = 1.0f;
    public bool enableAutoHide = true;
}

public class SampleUIComponent : MonoBehaviour
{
    [Header("UI References")]
    public Button actionButton;
    public TextMeshProUGUI statusText;
    public Image backgroundImage;
    public Slider progressSlider;
    
    [Header("Settings")]
    public UISettings settings = new UISettings();
    
    [Header("Animation")]
    public AnimationCurve fadeAnimation = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float fadeDuration = 0.5f;
    
    [Header("Data")]
    [SerializeField] private int clickCount = 0;
    [SerializeField] private bool isActive = true;
    [Range(0f, 100f)]
    public float progress = 0f;
    
    // Private fields
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;
    
    void Start()
    {
        InitializeComponent();
        SetupEventListeners();
    }
    
    void Update()
    {
        UpdateProgress();
        CheckAutoHide();
    }
    
    private void InitializeComponent()
    {
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        
        if (statusText != null)
        {
            statusText.text = "Ready";
            statusText.color = settings.primaryColor;
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = settings.primaryColor;
        }
        
        if (progressSlider != null)
        {
            progressSlider.value = progress / 100f;
        }
        
        Debug.Log($"[SampleUIComponent] Initialized with settings: Color={settings.primaryColor}, Speed={settings.animationSpeed}");
    }
    
    private void SetupEventListeners()
    {
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionButtonClick);
        }
    }
    
    private void OnActionButtonClick()
    {
        clickCount++;
        UpdateStatus($"Clicked {clickCount} times");
        
        // Trigger some visual feedback
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(PulseEffect());
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[SampleUIComponent] Status: {message}");
    }
    
    private void UpdateProgress()
    {
        if (isActive && progress < 100f)
        {
            progress += Time.deltaTime * settings.animationSpeed * 10f;
            progress = Mathf.Clamp(progress, 0f, 100f);
            
            if (progressSlider != null)
            {
                progressSlider.value = progress / 100f;
            }
        }
    }
    
    private void CheckAutoHide()
    {
        if (settings.enableAutoHide && progress >= 100f && canvasGroup.alpha > 0.1f)
        {
            StartFade(0f);
        }
    }
    
    private System.Collections.IEnumerator PulseEffect()
    {
        float originalAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / fadeDuration;
            float curveValue = fadeAnimation.Evaluate(normalizedTime);
            
            canvasGroup.alpha = Mathf.Lerp(originalAlpha, 0.5f, curveValue);
            yield return null;
        }
        
        // Reset to original alpha
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / fadeDuration;
            float curveValue = fadeAnimation.Evaluate(normalizedTime);
            
            canvasGroup.alpha = Mathf.Lerp(0.5f, originalAlpha, curveValue);
            yield return null;
        }
        
        canvasGroup.alpha = originalAlpha;
        fadeCoroutine = null;
    }
    
    private void StartFade(float targetAlpha)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeToAlpha(targetAlpha));
    }
    
    private System.Collections.IEnumerator FadeToAlpha(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / fadeDuration;
            float curveValue = fadeAnimation.Evaluate(normalizedTime);
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
            yield return null;
        }
        
        canvasGroup.alpha = targetAlpha;
        fadeCoroutine = null;
    }
    
    // Public methods for external control
    public void SetActive(bool active)
    {
        isActive = active;
        UpdateStatus(active ? "Activated" : "Deactivated");
    }
    
    public void ResetProgress()
    {
        progress = 0f;
        if (progressSlider != null)
        {
            progressSlider.value = 0f;
        }
        UpdateStatus("Progress Reset");
    }
    
    public void SetProgress(float value)
    {
        progress = Mathf.Clamp(value, 0f, 100f);
        if (progressSlider != null)
        {
            progressSlider.value = progress / 100f;
        }
        UpdateStatus($"Progress: {progress:F1}%");
    }
    
    void OnDestroy()
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(OnActionButtonClick);
        }
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
    }
}
