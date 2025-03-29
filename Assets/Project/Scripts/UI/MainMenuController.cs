using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button levelSelectButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsCloseButton;

private void Start()
{
    // Set up button listeners with null checks
    if (playButton != null)
        playButton.onClick.AddListener(OnPlayButtonClicked);
    
    if (levelSelectButton != null)
        levelSelectButton.onClick.AddListener(OnLevelSelectButtonClicked);
    
    if (settingsButton != null)
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);
    
    if (settingsCloseButton != null)
        settingsCloseButton.onClick.AddListener(OnSettingsCloseButtonClicked);
    
    // Ensure settings panel is closed initially
    if (settingsPanel != null)
        settingsPanel.SetActive(false);
}

    private void OnPlayButtonClicked()
    {
        // For now, load the first level directly
        SceneManager.LoadScene("Level_01");
    }

    private void OnLevelSelectButtonClicked()
    {
        // TODO: Implement level selection screen
        Debug.Log("Level Select button clicked");
    }

    private void OnSettingsButtonClicked()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    private void OnSettingsCloseButtonClicked()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // Clean up listeners with null checks
        if (playButton != null)
            playButton.onClick.RemoveAllListeners();
        
        if (levelSelectButton != null)
            levelSelectButton.onClick.RemoveAllListeners();
        
        if (settingsButton != null)
            settingsButton.onClick.RemoveAllListeners();
    
        if (settingsCloseButton != null)
            settingsCloseButton.onClick.RemoveAllListeners();
    }
}