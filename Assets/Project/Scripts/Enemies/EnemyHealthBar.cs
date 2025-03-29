using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the display of an enemy's health bar.
/// Attaches to an enemy and manages a UI health bar that follows the enemy.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Tooltip("Reference to the UI slider used as the health bar")]
    [SerializeField] private Slider healthSlider;
    
    [Tooltip("Canvas group to control fade-in/out")]
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Display Settings")]
    [Tooltip("Should the health bar always be shown?")]
    [SerializeField] private bool alwaysVisible = false;
    
    [Tooltip("Show health bar only when damaged?")]
    [SerializeField] private bool showWhenDamaged = true;
    
    [Tooltip("Duration to show health bar after taking damage")]
    [SerializeField] private float showDuration = 2f;
    
    [Tooltip("Speed to fade in the health bar")]
    [SerializeField] private float fadeInSpeed = 3f;
    
    [Tooltip("Speed to fade out the health bar")]
    [SerializeField] private float fadeOutSpeed = 1f;
    
    [Header("Look At Camera")]
    [Tooltip("Should the health bar always face the camera?")]
    [SerializeField] private bool lookAtCamera = true;
    
    // Internal state
    private float maxHealth;
    private float displayTimer;
    private bool shouldBeVisible = false;
    
    // Cached camera reference
    private Camera mainCamera;
    
    private void Awake()
    {
        // Initialize canvas group
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        
        // Try to find health slider if not assigned
        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>();
        }
        
        // Set initial alpha
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alwaysVisible ? 1f : 0f;
        }
        
        // Cache camera reference
        mainCamera = Camera.main;
    }
    
    private void Update()
    {
        // Handle health bar visibility
        UpdateVisibility();
        
        // Make health bar face camera if needed
        if (lookAtCamera && mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
    }
    
    /// <summary>
    /// Updates health bar visibility based on settings
    /// </summary>
    private void UpdateVisibility()
    {
        if (canvasGroup == null)
            return;
        
        if (alwaysVisible)
        {
            // Always visible - ensure alpha is 1
            canvasGroup.alpha = 1f;
            return;
        }
        
        if (shouldBeVisible)
        {
            // Fade in
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, fadeInSpeed * Time.deltaTime);
            
            // Update timer
            displayTimer -= Time.deltaTime;
            
            // Check if timer expired
            if (displayTimer <= 0f)
            {
                shouldBeVisible = false;
            }
        }
        else
        {
            // Fade out
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, fadeOutSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Sets the maximum health value
    /// </summary>
    public void SetMaxHealth(float max)
    {
        maxHealth = max;
        
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
        }
    }
    
    /// <summary>
    /// Updates the current health display
    /// </summary>
    public void SetHealth(float current)
    {
        if (healthSlider != null)
        {
            healthSlider.value = current;
        }
        
        // Show health bar if damaged and set to show when damaged
        if (showWhenDamaged && current < maxHealth)
        {
            shouldBeVisible = true;
            displayTimer = showDuration;
        }
    }
    
    /// <summary>
    /// Forces the health bar to be visible for the specified duration
    /// </summary>
    public void Show(float duration = 2f)
    {
        shouldBeVisible = true;
        displayTimer = duration;
    }
    
    /// <summary>
    /// Forces the health bar to hide
    /// </summary>
    public void Hide()
    {
        shouldBeVisible = false;
        displayTimer = 0f;
    }
}