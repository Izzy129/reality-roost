using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ButtonPressVisual.cs handles moving the button as the user presses it. 
/// It sends RPCs across the network so all clients can another user pressing the button visually.
/// 
/// Collision is detected via OnTriggerEnter, OnTriggerExit, and OnTriggerStay Unity MonoBehaviour events.
/// </summary>
public class ButtonPressVisual : NetworkBehaviour
{
    private bool _onTriggerEnter;
    [SerializeField] private Transform _buttonMovingVisual; // The part of the button that moves
    private Vector3 _buttonVisualStartPos;
    private Vector3 _interactorStartPos; // i.e The 'Hand' collider
    private NetworkedInteractable _networkedInteractable;

    [SerializeField] private float _maxPress = 0.0529f; // Value that button presses in
    [SerializeField] private float _minPressChange = 0.002f;
    [SerializeField] private bool _stayPressedAfterInteraction = false; // Is the button able to be pressed many times, or is it only pressed once?

    private float _lastSentPress;
    private bool _hasBeenPressed;
    private bool _triggerRPCsOnce = true;

    private void Awake()
    {
        _networkedInteractable = GetComponent<NetworkedInteractable>();

        if (_buttonMovingVisual == null)
        {
            Debug.LogError("Button moving visual is not assigned.", this);
            enabled = false;
            return;
        }

        _buttonVisualStartPos = _buttonMovingVisual.localPosition;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Hand")) return;
        if (_stayPressedAfterInteraction && _hasBeenPressed) return;
        _onTriggerEnter = true;
        
        _interactorStartPos = transform.InverseTransformPoint(other.bounds.center);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Hand")) return;
        if (!_onTriggerEnter) return;
        if (_stayPressedAfterInteraction && _hasBeenPressed) return;

        // Get local position of the interactor
        Vector3 localInteractorPos = transform.InverseTransformPoint(other.bounds.center);
        // Find the difference between the interactor start pos & the current localInteractorPos 
        float movement = _interactorStartPos.y - localInteractorPos.y;
        float press = Mathf.Clamp(movement, 0f, _maxPress);

        // Send visual updates only when the press changes enough
        if (Mathf.Abs(press - _lastSentPress) >= _minPressChange || press >= _maxPress)
        {
            _lastSentPress = press;
            RequestVisualPress(press);
        }

        // Trigger selection once per press
        if (press >= _maxPress && _triggerRPCsOnce)
        {
            _triggerRPCsOnce = false;
            _hasBeenPressed = true;

            Debug.Log("Button fully pressed; calling RPCs");
            _networkedInteractable?.RequestSelect();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Hand")) return;
        _onTriggerEnter = false;

        // Ignore exits if the button has already completed its one-time interaction.
        if (_stayPressedAfterInteraction && _hasBeenPressed)
        {
            if (!_triggerRPCsOnce)
            {
                _networkedInteractable?.RequestDeselect();
                _triggerRPCsOnce = true;
            }
            return;
        }

        // Multiple-Press button: deselects after a successful press
        if (!_triggerRPCsOnce) _networkedInteractable?.RequestDeselect();

        // Multiple-Press button: reset so it can be pressed again
        if (!_stayPressedAfterInteraction)
        {
            RequestVisualPress(0f);
            _lastSentPress = 0f;
            _hasBeenPressed = false;
            _triggerRPCsOnce = true;
        }
    }

    #region Methods
    /// <summary>
    /// User requests a visual press across the network.
    /// If Host, they apply the visual press automatically. If Client, they will send a ServerRPC.
    /// </summary>
    /// <param name="press"></param>
    public void RequestVisualPress(float press)
    {
        press = Mathf.Clamp(press, 0f, _maxPress);

        if (IsServer)
        {
            ApplyVisualPress(press);
        }
        else
        {
            SetVisualPressRpc(press);
        }
    }
    /// <summary>
    /// Calculates and sets the button's local position
    /// </summary>
    /// <param name="press"></param>
    private void ApplyVisualPress(float press)
    {
        if (_buttonMovingVisual == null) return;
        
        press = Mathf.Clamp(press, 0f, _maxPress);
        // Calculate current position of the button
        Vector3 pos = _buttonVisualStartPos;
        pos.y -= press;
        // Set button local position to updated position
        _buttonMovingVisual.localPosition = pos;
    }
    public void ResetButton()
    {
        _onTriggerEnter = false;
        _triggerRPCsOnce = true;
        _hasBeenPressed = false;
        _lastSentPress = 0f;
        RequestVisualPress(0f);
    }

    #endregion

    #region RPCs
    /// <summary>
    /// Server RPC that shows the button press visual across the network.
    /// </summary>
    /// <param name="press"></param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetVisualPressRpc(float press)
    {
        ApplyVisualPress(Mathf.Clamp(press, 0f, _maxPress));
    }
    #endregion
}