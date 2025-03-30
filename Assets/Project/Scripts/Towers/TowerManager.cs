using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;

namespace TowerDefense.Towers
{
    /// <summary>
    /// Manages tower definitions, instantiation, placement, and global tower operations.
    /// Acts as a central point for tower-related functionality.
    /// </summary>
    public class TowerManager : SingletonBehaviour<TowerManager>
    {
        [Header("Tower Definitions")]
        [Tooltip("All available tower definitions")]
        [SerializeField] private List<TowerDefinition> availableTowers = new List<TowerDefinition>();
        
        [Tooltip("Initial unlocked tower IDs")]
        [SerializeField] private List<string> initialUnlockedTowerIDs = new List<string>();

        [Header("Placement")]
        [Tooltip("Layer mask for tower placement raycast")]
        [SerializeField] private LayerMask placementLayerMask;
        
        [Tooltip("Material for valid placement preview")]
        [SerializeField] private Material validPlacementMaterial;
        
        [Tooltip("Material for invalid placement preview")]
        [SerializeField] private Material invalidPlacementMaterial;
        
        [Tooltip("Y position offset for placement preview")]
        [SerializeField] private float placementPreviewYOffset = 0.1f;

        // References to other managers
        private GridManager gridManager;
        private GameManager gameManager;
        private PathfindingManager pathfindingManager;

        // Runtime state
        private List<Tower> activeTowers = new List<Tower>();
        private Dictionary<string, TowerDefinition> towerDefinitionsMap = new Dictionary<string, TowerDefinition>();
        private HashSet<string> unlockedTowerIDs = new HashSet<string>();
        
        // Placement state
        private TowerDefinition selectedTowerDefinition;
        private GameObject placementPreview;
        private bool isPlacingTower = false;
        private bool canPlaceAtCurrentPosition = false;
        private Vector2Int currentGridPosition;
        
        // Events
        public System.Action<TowerDefinition> OnTowerSelected;
        public System.Action<Tower> OnTowerPlaced;
        public System.Action<Tower> OnTowerSold;
        public System.Action<Tower, int> OnTowerUpgraded;
        public System.Action<string> OnTowerUnlocked;

        #region Initialization

        /// <summary>
        /// Called when singleton is created
        /// </summary>
        protected override void OnSingletonAwake()
        {
            InitializeTowerDefinitionsMap();
            InitializeUnlockedTowers();
        }

        private void Start()
        {
            // Find required managers
            ResolveDependencies();
        }
        
        /// <summary>
        /// Initializes the map of tower definitions for quick lookup
        /// </summary>
        private void InitializeTowerDefinitionsMap()
        {
            towerDefinitionsMap.Clear();
            
            foreach (var definition in availableTowers)
            {
                if (definition != null && !string.IsNullOrEmpty(definition.towerID))
                {
                    towerDefinitionsMap[definition.towerID] = definition;
                }
                else
                {
                    Debug.LogWarning("TowerManager: Found null or invalid tower definition");
                }
            }
            
            Debug.Log($"TowerManager: Initialized {towerDefinitionsMap.Count} tower definitions");
        }
        
        /// <summary>
        /// Sets up initially unlocked towers
        /// </summary>
        private void InitializeUnlockedTowers()
        {
            unlockedTowerIDs.Clear();
            
            foreach (var id in initialUnlockedTowerIDs)
            {
                if (!string.IsNullOrEmpty(id) && towerDefinitionsMap.ContainsKey(id))
                {
                    unlockedTowerIDs.Add(id);
                }
            }
            
            Debug.Log($"TowerManager: Initialized with {unlockedTowerIDs.Count} unlocked towers");
        }
        
        /// <summary>
        /// Resolves dependencies to other managers
        /// </summary>
        private void ResolveDependencies()
        {
            // Try to get from ServiceLocator first, fallback to FindObjectOfType
            gridManager = ServiceLocator.Get<GridManager>(true);
            if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
            
            gameManager = ServiceLocator.Get<GameManager>(true);
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
            
            pathfindingManager = ServiceLocator.Get<PathfindingManager>(true);
            if (pathfindingManager == null) pathfindingManager = FindObjectOfType<PathfindingManager>();
            
            // Log warning if any dependency is missing
            if (gridManager == null) Debug.LogWarning("TowerManager: GridManager not found!");
            if (gameManager == null) Debug.LogWarning("TowerManager: GameManager not found!");
            if (pathfindingManager == null) Debug.LogWarning("TowerManager: PathfindingManager not found!");
        }

        #endregion

        #region Tower Definitions

        /// <summary>
        /// Gets a tower definition by ID
        /// </summary>
        public TowerDefinition GetTowerDefinition(string towerID)
        {
            if (towerDefinitionsMap.TryGetValue(towerID, out TowerDefinition definition))
            {
                return definition;
            }
            return null;
        }
        
        /// <summary>
        /// Gets all available tower definitions
        /// </summary>
        public List<TowerDefinition> GetAllTowerDefinitions()
        {
            return new List<TowerDefinition>(availableTowers);
        }
        
        /// <summary>
        /// Gets all unlocked tower definitions
        /// </summary>
        public List<TowerDefinition> GetUnlockedTowerDefinitions()
        {
            List<TowerDefinition> unlockedTowers = new List<TowerDefinition>();
            
            foreach (var id in unlockedTowerIDs)
            {
                if (towerDefinitionsMap.TryGetValue(id, out TowerDefinition definition))
                {
                    unlockedTowers.Add(definition);
                }
            }
            
            return unlockedTowers;
        }
        
        /// <summary>
        /// Checks if a tower type is unlocked
        /// </summary>
        public bool IsTowerUnlocked(string towerID)
        {
            return unlockedTowerIDs.Contains(towerID);
        }
        
        /// <summary>
        /// Unlocks a tower type
        /// </summary>
        public bool UnlockTower(string towerID)
        {
            if (IsTowerUnlocked(towerID))
            {
                return false;
            }
            
            if (!towerDefinitionsMap.ContainsKey(towerID))
            {
                Debug.LogWarning($"TowerManager: Attempted to unlock unknown tower ID: {towerID}");
                return false;
            }
            
            unlockedTowerIDs.Add(towerID);
            OnTowerUnlocked?.Invoke(towerID);
            Debug.Log($"TowerManager: Unlocked tower {towerID}");
            
            return true;
        }

        #endregion

        #region Tower Placement

        /// <summary>
        /// Selects a tower for placement
        /// </summary>
        public void SelectTowerForPlacement(string towerID)
        {
            // Check if the tower is unlocked
            if (!IsTowerUnlocked(towerID))
            {
                Debug.LogWarning($"TowerManager: Attempted to select a locked tower: {towerID}");
                return;
            }
            
            // Get the tower definition
            TowerDefinition definition = GetTowerDefinition(towerID);
            if (definition == null)
            {
                Debug.LogWarning($"TowerManager: Tower definition not found: {towerID}");
                return;
            }
            
            // Set as selected tower
            selectedTowerDefinition = definition;
            
            // Enter placement mode
            EnterPlacementMode();
            
            // Notify listeners
            OnTowerSelected?.Invoke(selectedTowerDefinition);
        }
        
        /// <summary>
        /// Enters tower placement mode
        /// </summary>
        private void EnterPlacementMode()
        {
            if (selectedTowerDefinition == null || selectedTowerDefinition.towerPrefab == null)
            {
                Debug.LogWarning("TowerManager: Cannot enter placement mode without valid tower definition");
                return;
            }
            
            isPlacingTower = true;
            
            // Create placement preview
            if (placementPreview != null)
            {
                Destroy(placementPreview);
            }
            
            placementPreview = Instantiate(selectedTowerDefinition.towerPrefab);
            
            // Disable any components on the preview
            MonoBehaviour[] components = placementPreview.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                component.enabled = false;
            }
            
            // Rename for clarity
            placementPreview.name = $"{selectedTowerDefinition.displayName}_Preview";
            
            // Set a semi-transparent material on all renderers
            Renderer[] renderers = placementPreview.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                // Store original materials to restore them
                Material[] originalMaterials = renderer.materials;
                
                // Create new materials array
                Material[] newMaterials = new Material[originalMaterials.Length];
                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    newMaterials[i] = validPlacementMaterial;
                }
                
                // Apply new materials
                renderer.materials = newMaterials;
            }
            
            // Position off-screen initially
            placementPreview.transform.position = new Vector3(-1000, -1000, -1000);
            
            Debug.Log($"TowerManager: Entered placement mode for {selectedTowerDefinition.displayName}");
        }
        
        /// <summary>
        /// Exits tower placement mode
        /// </summary>
        public void ExitPlacementMode()
        {
            isPlacingTower = false;
            selectedTowerDefinition = null;
            
            if (placementPreview != null)
            {
                Destroy(placementPreview);
                placementPreview = null;
            }
            
            // Clear any grid highlights
            if (gridManager != null)
            {
                gridManager.ResetCellHighlight(currentGridPosition);
            }
            
            Debug.Log("TowerManager: Exited placement mode");
        }
        
        /// <summary>
        /// Updates the placement preview position and state
        /// </summary>
        public void UpdatePlacementPreview(Vector3 worldPosition)
        {
            if (!isPlacingTower || placementPreview == null || gridManager == null)
            {
                return;
            }
            
            // Convert world position to grid position
            Vector2Int gridPosition = gridManager.GetGridPosition(worldPosition);
            
            // Check if position changed
            if (gridPosition != currentGridPosition)
            {
                // Reset previous highlight
                gridManager.ResetCellHighlight(currentGridPosition);
                currentGridPosition = gridPosition;
            }
            
            // Check if placement is valid
            canPlaceAtCurrentPosition = CanPlaceTowerAt(gridPosition);
            
            // Highlight grid cell
            gridManager.HighlightCell(gridPosition, canPlaceAtCurrentPosition);
            
            // Update preview position
            Vector3 previewPosition = gridManager.GetWorldPosition(gridPosition);
            previewPosition.y += placementPreviewYOffset;
            placementPreview.transform.position = previewPosition;
            
            // Update preview materials based on placement validity
            Material previewMaterial = canPlaceAtCurrentPosition ? validPlacementMaterial : invalidPlacementMaterial;
            
            Renderer[] renderers = placementPreview.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = previewMaterial;
                }
                renderer.materials = materials;
            }
        }
        
        /// <summary>
        /// Attempt to place the selected tower at the current position
        /// </summary>
        public bool TryPlaceTower()
        {
            if (!isPlacingTower || selectedTowerDefinition == null || !canPlaceAtCurrentPosition)
            {
                return false;
            }
            
            // Check if player has enough gold
            if (gameManager != null && !gameManager.CanAfford(selectedTowerDefinition.purchaseCost))
            {
                Debug.Log("TowerManager: Cannot afford tower");
                return false;
            }
            
            // Place the tower
            Tower placedTower = PlaceTower(currentGridPosition, selectedTowerDefinition);
            
            if (placedTower != null)
            {
                // Deduct cost
                if (gameManager != null)
                {
                    gameManager.SpendGold(selectedTowerDefinition.purchaseCost);
                }
                
                // Notify listeners
                OnTowerPlaced?.Invoke(placedTower);
                
                Debug.Log($"TowerManager: Placed {selectedTowerDefinition.displayName} at {currentGridPosition}");
                
                // Continue placing the same tower type if there's enough gold
                if (gameManager != null && gameManager.CanAfford(selectedTowerDefinition.purchaseCost))
                {
                    // Just reset the placement preview to continue placing
                    Destroy(placementPreview);
                    EnterPlacementMode();
                }
                else
                {
                    // Exit placement mode if can't afford more
                    ExitPlacementMode();
                }
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Places a tower at the specified grid position
        /// </summary>
        private Tower PlaceTower(Vector2Int gridPosition, TowerDefinition definition)
        {
            if (gridManager == null || definition == null || definition.towerPrefab == null)
            {
                return null;
            }
            
            // Get world position from grid
            Vector3 worldPosition = gridManager.GetWorldPosition(gridPosition);
            
            // Final validation check
            if (!CanPlaceTowerAt(gridPosition))
            {
                return null;
            }
            
            // Spawn the tower
            GameObject towerObject = Instantiate(definition.towerPrefab, worldPosition, Quaternion.identity);
            Tower tower = towerObject.GetComponent<Tower>();
            
            if (tower == null)
            {
                Debug.LogError("TowerManager: Tower prefab does not have a Tower component");
                Destroy(towerObject);
                return null;
            }
            
            // Initialize the tower with its definition
            //tower.Initialize(definition);  // You'll need to add this method to the Tower class
            
            // Set the tower as occupying the grid cell
            gridManager.PlaceTower(gridPosition, towerObject, out GameObject _);
            
            // Subscribe to tower events
            SubscribeToTowerEvents(tower);
            
            // Add to active towers
            activeTowers.Add(tower);
            
            return tower;
        }
        
        /// <summary>
        /// Checks if a tower can be placed at the specified grid position
        /// </summary>
        private bool CanPlaceTowerAt(Vector2Int gridPosition)
        {
            if (gridManager == null)
            {
                return false;
            }
            
            return gridManager.CanPlaceTower(gridPosition);
        }
        
        /// <summary>
        /// Subscribes to events from a tower
        /// </summary>
        private void SubscribeToTowerEvents(Tower tower)
        {
            if (tower == null) return;
            
            tower.OnTowerSold += HandleTowerSold;
            tower.OnTowerUpgraded += HandleTowerUpgraded;
        }
        
        /// <summary>
        /// Unsubscribes from events from a tower
        /// </summary>
        private void UnsubscribeFromTowerEvents(Tower tower)
        {
            if (tower == null) return;
            
            tower.OnTowerSold -= HandleTowerSold;
            tower.OnTowerUpgraded -= HandleTowerUpgraded;
        }
        
        /// <summary>
        /// Handles a tower being sold
        /// </summary>
        private void HandleTowerSold(Tower tower)
        {
            if (tower == null) return;
            
            // Remove from active towers
            activeTowers.Remove(tower);
            
            // Unsubscribe from events
            UnsubscribeFromTowerEvents(tower);
            
            // Forward the event
            OnTowerSold?.Invoke(tower);
        }
        
        /// <summary>
        /// Handles a tower being upgraded
        /// </summary>
        private void HandleTowerUpgraded(Tower tower, int newLevel)
        {
            // Forward the event
            OnTowerUpgraded?.Invoke(tower, newLevel);
        }

        #endregion

        #region Updates and Input

        private void Update()
        {
            // Handle tower placement preview
            if (isPlacingTower)
            {
                // Get mouse position and convert to world position
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementLayerMask))
                {
                    UpdatePlacementPreview(hit.point);
                }
                
                // Handle placement on click
                if (Input.GetMouseButtonDown(0))
                {
                    TryPlaceTower();
                }
                
                // Handle cancel on right click or escape
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                {
                    ExitPlacementMode();
                }
            }
        }

        #endregion

        #region Tower Management

        /// <summary>
        /// Gets all active towers
        /// </summary>
        public List<Tower> GetActiveTowers()
        {
            return new List<Tower>(activeTowers);
        }
        
        /// <summary>
        /// Gets active towers of a specific type
        /// </summary>
        public List<Tower> GetActiveTowersOfType(string towerID)
        {
            List<Tower> result = new List<Tower>();
            
            foreach (var tower in activeTowers)
            {
                if (tower.GetTowerID() == towerID)
                {
                    result.Add(tower);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Clean up when the object is destroyed
        /// </summary>
        protected override void OnDestroy()
        {
            // Clean up events
            foreach (var tower in activeTowers)
            {
                UnsubscribeFromTowerEvents(tower);
            }
            
            // Call base implementation
            base.OnDestroy();
        }

        #endregion
    }
}