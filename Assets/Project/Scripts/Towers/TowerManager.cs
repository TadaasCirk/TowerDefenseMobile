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



        private GameObject debugCursor;
        
        
        
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
    
            // Use the actual tower prefab
            placementPreview = Instantiate(selectedTowerDefinition.towerPrefab);
            placementPreview.name = $"{selectedTowerDefinition.displayName}_Preview";
    
            // Disable any scripts on the preview
            MonoBehaviour[] scripts = placementPreview.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                script.enabled = false;
            }
    
            // Make it slightly transparent to indicate it's a preview
            Renderer[] renderers = placementPreview.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                // Make a copy of the material to avoid modifying the original
                Material mat = new Material(renderer.material);
                // Set transparency
                Color color = mat.color;
                color.a = 0.7f;
                mat.color = color;
                renderer.material = mat;
            }
    
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
        }
        
        
        /// <summary>
        /// Attempt to place the selected tower at the current position
        /// </summary>
        public bool TryPlaceTower()
        {
            if (!isPlacingTower || selectedTowerDefinition == null)
            {
                return false;
            }

            // Get the current grid position
            Vector2Int gridPos = currentGridPosition;

            // Check if player can afford the tower
            if (gameManager != null && !gameManager.CanAfford(selectedTowerDefinition.purchaseCost))
            {
                Debug.Log("Cannot afford tower");
                return false;
            }

            // Check if placement is valid
            if (!CanPlaceTowerAt(gridPos))
            {
                Debug.Log("Invalid placement location");
                return false;
            }

            // Place the tower
            Tower placedTower = PlaceTower(gridPos, selectedTowerDefinition);
            if (placedTower == null)
            {
                Debug.Log("Failed to place tower");
                return false;
            }

            // Deduct the cost - THIS IS THE KEY PART
            if (gameManager != null)
            {
                gameManager.SpendGold(selectedTowerDefinition.purchaseCost);
            }
    
            // Notify listeners
            OnTowerPlaced?.Invoke(placedTower);

            return true;
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

// In TowerManager.cs - Update method
        private void Update()
        {
            // Only process placement logic if we're in placement mode
            if (isPlacingTower)
            {
                Camera cam = Camera.main;
                if (cam == null)
                {
                    Debug.LogError("Main camera not found!");
                    return;
                }

                // Cast a ray from the camera through the mouse position
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        
                // Create a plane at the grid level (y=0)
                Plane gridPlane = new Plane(Vector3.up, Vector3.zero);
        
                float distance;
                // If the ray hits the grid plane
                if (gridPlane.Raycast(ray, out distance))
                {
                    // Get the exact point where the ray intersects the plane
                    Vector3 hitPoint = ray.GetPoint(distance);
            
                    // Get grid position - THIS IS THE CRITICAL PART
                    if (gridManager != null)
                    {
                        Vector2Int gridPosition = gridManager.GetGridPosition(hitPoint);
                        currentGridPosition = gridPosition;
                
                        // Highlight cell based on placement validity
                        bool canPlace = CanPlaceTowerAt(gridPosition);
                        gridManager.HighlightCell(gridPosition, canPlace);
                        canPlaceAtCurrentPosition = canPlace;
                
                        // Position the preview at the correct world position
                        if (placementPreview != null)
                        {
                            Vector3 worldPos = gridManager.GetWorldPosition(gridPosition);
                            placementPreview.transform.position = worldPos;
                        }
                    }
            
                    // Handle placement on click
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (TryPlaceTower())
                        {
                            ExitPlacementMode();
                        }
                    }
                }
        
                // Allow canceling with right-click or Escape
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