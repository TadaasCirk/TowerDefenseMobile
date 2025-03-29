using UnityEngine;
using UnityEngine.InputSystem;

public class TouchManager : MonoBehaviour
{
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private float _holdThreshold = 0.5f; // Time in seconds to consider a touch a "hold"
    
    private TouchControls _touchControls;
    private Vector2 _touchPosition;
    private bool _isTouching;
    private float _touchStartTime;
    private bool _isHolding;
    
    // Events for other systems to subscribe to
    public event System.Action<Vector2> OnTap;
    public event System.Action<Vector2> OnHoldStart;
    public event System.Action<Vector2> OnHoldEnd;
    public event System.Action<Vector2, Vector2> OnDrag; // (position, delta)
    
    private void Awake()
    {
        _touchControls = new TouchControls();
        
        if (_mainCamera == null)
            _mainCamera = Camera.main;
    }
    
    private void OnEnable()
    {
        _touchControls.Enable();
        
        // Subscribe to touch events
        _touchControls.Touch.PrimaryPosition.performed += ctx => _touchPosition = ctx.ReadValue<Vector2>();
        
        _touchControls.Touch.PrimaryPhase.performed += ctx => 
        {
            int phase = ctx.ReadValue<int>();
            
            // Phase 1 = Began
            if (phase == 1) 
            {
                _isTouching = true;
                _touchStartTime = Time.time;
                _isHolding = false;
            }
            // Phase 2 = Moved, Phase 3 = Stationary 
            else if ((phase == 2 || phase == 3) && _isTouching) 
            {
                // Check for hold threshold
                if (!_isHolding && Time.time - _touchStartTime >= _holdThreshold)
                {
                    _isHolding = true;
                    Vector2 worldPosition = GetWorldPosition(_touchPosition);
                    OnHoldStart?.Invoke(worldPosition);
                }
            }
            // Phase 4 = Ended, Phase 5 = Canceled
            else if ((phase == 4 || phase == 5) && _isTouching) 
            {
                _isTouching = false;
                
                if (_isHolding)
                {
                    // Was a hold, end it
                    _isHolding = false;
                    Vector2 worldPosition = GetWorldPosition(_touchPosition);
                    OnHoldEnd?.Invoke(worldPosition);
                }
                else if (Time.time - _touchStartTime < _holdThreshold)
                {
                    // Was a tap (short press)
                    Vector2 worldPosition = GetWorldPosition(_touchPosition);
                    OnTap?.Invoke(worldPosition);
                }
            }
        };
        
        // Handle drag if you've created a Drag action
        if (_touchControls.Touch.Drag != null)
        {
            _touchControls.Touch.Drag.performed += ctx => 
            {
                if (_isTouching)
                {
                    Vector2 delta = ctx.ReadValue<Vector2>();
                    Vector2 worldPosition = GetWorldPosition(_touchPosition);
                    OnDrag?.Invoke(worldPosition, delta);
                }
            };
        }
        
        // If you have a Tap action, use it
        if (_touchControls.Touch.Tap != null)
        {
            _touchControls.Touch.Tap.performed += ctx => 
            {
                // We'll still use our manual tap detection for consistency,
                // but you could replace it with this built-in event if preferred
            };
        }
    }
    
    private void OnDisable()
    {
        _touchControls.Disable();
    }
    
    private Vector2 GetWorldPosition(Vector2 screenPosition)
    {
        return _mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, _mainCamera.nearClipPlane));
    }
}