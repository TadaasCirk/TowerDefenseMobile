using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerDefense.Core;
using TowerDefense.Towers;

namespace TowerDefense.UI
{
    /// <summary>
    /// Manages the tower selection and placement UI.
    /// </summary>
    public class TowerPlacementUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Parent container for tower buttons")]
        [SerializeField] private RectTransform towerButtonContainer;
        
        [Tooltip("Prefab for tower selection buttons")]
        [SerializeField] private GameObject towerButtonPrefab;

        [Header("Tower Info Panel")]
        [Tooltip("Panel showing selected tower info")]
        [SerializeField] private GameObject towerInfoPanel;
        
        [Tooltip("Text displaying tower name")]
        [SerializeField] private TextMeshProUGUI towerNameText;
        
        [Tooltip("Text displaying tower description")]
        [SerializeField] private TextMeshProUGUI towerDescriptionText;
        
        [Tooltip("Text displaying tower cost")]
        [SerializeField] private TextMeshProUGUI towerCostText;
        
        [Tooltip("Image displaying tower icon")]
        [SerializeField] private Image towerIconImage;
        
        [Tooltip("Button to confirm tower placement")]
        [SerializeField] private Button placeTowerButton;
        
        [Tooltip("Button to cancel tower placement")]
        [SerializeField] private Button cancelButton;

        [Header("UI Feedback")]
        [Tooltip("Color for when player can afford the tower")]
        [SerializeField] private Color affordableColor = Color.white;
        
        [Tooltip("Color for when player cannot afford the tower")]
        [SerializeField] private Color unaffordableColor = Color.red;

        // References to managers
        private TowerManager towerManager;
        private GameManager gameManager;

        // Runtime state
        private List<TowerButtonData> towerButtons = new List<TowerButtonData>();
        private TowerDefinition selectedTowerDefinition;

        // Helper class to store button data
        private class TowerButtonData
        {
            public GameObject ButtonObject;
            public Button Button;
            public Image IconImage;
            public TextMeshProUGUI NameText;
            public TextMeshProUGUI CostText;
            public TowerDefinition Definition;
        }

        private void Awake()
        {
            // Hide the info panel initially
            if (towerInfoPanel != null)
            {
                towerInfoPanel.SetActive(false);
            }
        }
        public void TestButtonClick()
        {
            Debug.Log("Button clicked through inspector event!");
        }

        private void Start()
        {
            // Find the managers
            towerManager = TowerManager.Instance;
            gameManager = GameManager.Instance; 

            if (towerManager == null)
            {
                Debug.LogError("TowerPlacementUI: TowerManager not found!");
                return;
            }

            Debug.Log("TowerPlacementUI Start");
            Debug.Log($"TowerManager found: {towerManager != null}");

            // Set up cancel button
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }

            // Set up place tower button
            if (placeTowerButton != null)
            {
                placeTowerButton.onClick.AddListener(OnPlaceTowerButtonClicked);
            }

            // Subscribe to tower manager events
            towerManager.OnTowerSelected += OnTowerSelected;
            towerManager.OnTowerPlaced += OnTowerPlaced;
            towerManager.OnTowerUnlocked += OnTowerUnlocked;

            // Subscribe to game manager events
            if (gameManager != null)
            {
                gameManager.OnGoldChanged += OnGoldChanged;
            }

            // Generate the tower buttons
            GenerateTowerButtons();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (towerManager != null)
            {
                towerManager.OnTowerSelected -= OnTowerSelected;
                towerManager.OnTowerPlaced -= OnTowerPlaced;
                towerManager.OnTowerUnlocked -= OnTowerUnlocked;
            }

            if (gameManager != null)
            {
                gameManager.OnGoldChanged -= OnGoldChanged;
            }

            // Clean up button listeners
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
            }

            if (placeTowerButton != null)
            {
                placeTowerButton.onClick.RemoveListener(OnPlaceTowerButtonClicked);
            }

            // Clean up tower buttons
            foreach (var buttonData in towerButtons)
            {
                if (buttonData.Button != null)
                {
                    buttonData.Button.onClick.RemoveAllListeners();
                }
            }
        }

        /// <summary>
        /// Creates buttons for each unlocked tower
        /// </summary>
        private void GenerateTowerButtons()
        {
            // Clear any existing buttons
            foreach (var buttonData in towerButtons)
            {
                if (buttonData.ButtonObject != null)
                {
                    Destroy(buttonData.ButtonObject);
                }
            }
            towerButtons.Clear();


            Debug.Log("GenerateTowerButtons called");
            if (towerManager != null) {
                var unlockedTowers1 = towerManager.GetUnlockedTowerDefinitions();
                Debug.Log($"Unlocked towers count: {unlockedTowers1?.Count ?? 0}");
            }

            // Get unlocked tower definitions
            List<TowerDefinition> unlockedTowers = towerManager.GetUnlockedTowerDefinitions();

            if (towerButtonContainer == null || towerButtonPrefab == null)
            {
                Debug.LogError("TowerPlacementUI: Missing tower button container or prefab!");
                return;
            }

            // Create a button for each unlocked tower
            foreach (var towerDef in unlockedTowers)
            {
                CreateTowerButton(towerDef);
            }

            UpdateButtonInteractability();
        }

        /// <summary>
        /// Creates a tower selection button
        /// </summary>
        private void CreateTowerButton(TowerDefinition towerDef)
        {
            // Instantiate button prefab
            GameObject buttonObj = Instantiate(towerButtonPrefab, towerButtonContainer);

            // Set up the button data
            TowerButtonData buttonData = new TowerButtonData
            {
                ButtonObject = buttonObj,
                Button = buttonObj.GetComponent<Button>(),
                Definition = towerDef
            };

            // Find UI components
            buttonData.IconImage = buttonObj.transform.Find("IconImage")?.GetComponent<Image>();
            buttonData.NameText = buttonObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            buttonData.CostText = buttonObj.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();

            // Set up UI elements
            if (buttonData.IconImage != null && towerDef.icon != null)
            {
                buttonData.IconImage.sprite = towerDef.icon;
            }

            if (buttonData.NameText != null)
            {
                buttonData.NameText.text = towerDef.displayName;
            }

            if (buttonData.CostText != null)
            {
                buttonData.CostText.text = towerDef.purchaseCost.ToString();
            }

            // Set up button click handler
            buttonData.Button.onClick.AddListener(() => OnTowerButtonClicked(towerDef));

            // Add to the list
            towerButtons.Add(buttonData);
        }

        /// <summary>
        /// Updates button interactability based on gold availability
        /// </summary>
        private void UpdateButtonInteractability()
        {
            if (gameManager == null) return;

            int currentGold = gameManager.GetGold();

            foreach (var buttonData in towerButtons)
            {
                bool canAfford = currentGold >= buttonData.Definition.purchaseCost;
                buttonData.Button.interactable = canAfford;

                // Update cost text color
                if (buttonData.CostText != null)
                {
                    buttonData.CostText.color = canAfford ? affordableColor : unaffordableColor;
                }
            }

            // Update place tower button if we have a selected tower
            if (placeTowerButton != null && selectedTowerDefinition != null)
            {
                placeTowerButton.interactable = currentGold >= selectedTowerDefinition.purchaseCost;

                // Update cost text color in info panel
                if (towerCostText != null)
                {
                    bool canAffordSelected = currentGold >= selectedTowerDefinition.purchaseCost;
                    towerCostText.color = canAffordSelected ? affordableColor : unaffordableColor;
                }
            }
        }

        /// <summary>
        /// Shows the tower info panel with details about the selected tower
        /// </summary>
        private void ShowTowerInfo(TowerDefinition towerDef)
        {
            if (towerInfoPanel == null || towerDef == null) return;

            // Update UI elements
            if (towerNameText != null)
            {
                towerNameText.text = towerDef.displayName;
            }

            if (towerDescriptionText != null)
            {
                towerDescriptionText.text = towerDef.description;
            }

            if (towerCostText != null)
            {
                towerCostText.text = $"Cost: {towerDef.purchaseCost}";

                // Update color based on affordability
                if (gameManager != null)
                {
                    bool canAfford = gameManager.GetGold() >= towerDef.purchaseCost;
                    towerCostText.color = canAfford ? affordableColor : unaffordableColor;
                }
            }

            if (towerIconImage != null && towerDef.icon != null)
            {
                towerIconImage.sprite = towerDef.icon;
            }

            // Update place button interactability
            if (placeTowerButton != null && gameManager != null)
            {
                placeTowerButton.interactable = gameManager.GetGold() >= towerDef.purchaseCost;
            }

            // Show the panel
            towerInfoPanel.SetActive(true);
        }

        /// <summary>
        /// Hides the tower info panel
        /// </summary>
        private void HideTowerInfo()
        {
            if (towerInfoPanel != null)
            {
                towerInfoPanel.SetActive(false);
            }
        }

        #region Event Handlers

        /// <summary>
        /// Handles a tower button being clicked
        /// </summary>
        private void OnTowerButtonClicked(TowerDefinition towerDef)
        {
            selectedTowerDefinition = towerDef;
            ShowTowerInfo(towerDef);
        }

        /// <summary>
        /// Handles the place tower button being clicked
        /// </summary>
        public void OnPlaceTowerButtonClicked()
        {
            Debug.Log("Place tower button clicked");
    
            // Make sure we have a selected tower and a tower manager
            if (selectedTowerDefinition == null || towerManager == null)
            {
                Debug.LogWarning("Cannot place tower: No tower selected or towerManager not found");
                return;
            }
    
            // Tell the TowerManager to start placement mode, but DON'T place the tower yet
            towerManager.SelectTowerForPlacement(selectedTowerDefinition.towerID);
    
            // Hide the tower info panel after starting placement
            towerInfoPanel.SetActive(false);
        }

        /// <summary>
        /// Handles the cancel button being clicked
        /// </summary>
        private void OnCancelButtonClicked()
        {
            // Clear selection
            selectedTowerDefinition = null;

            // Hide info panel
            HideTowerInfo();

            // Exit placement mode if active
            if (towerManager != null)
            {
                towerManager.ExitPlacementMode();
            }
        }

        /// <summary>
        /// Handles a tower being selected for placement
        /// </summary>
        private void OnTowerSelected(TowerDefinition towerDef)
        {
            // Update UI to show the tower is being placed
            selectedTowerDefinition = towerDef;
            ShowTowerInfo(towerDef);
        }

        /// <summary>
        /// Handles a tower being placed
        /// </summary>
        private void OnTowerPlaced(Tower tower)
        {
            // Update button interactability after spending gold
            UpdateButtonInteractability();
        }

        /// <summary>
        /// Handles a tower being unlocked
        /// </summary>
        private void OnTowerUnlocked(string towerID)
        {
            // Refresh all buttons to show the newly unlocked tower
            GenerateTowerButtons();
        }

        /// <summary>
        /// Handles player's gold changing
        /// </summary>
        private void OnGoldChanged(int newGold)
        {
            // Update button interactability
            UpdateButtonInteractability();
        }

        #endregion
    }
}