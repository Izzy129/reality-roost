using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkedRoostZoom : NetworkBehaviour
{
    public InputActionAsset InputActions;
    private float baseZoomSpeed = 0.1f;
    private float scrollAmount = 0f;
    private float zoomSpeed;
    private InputAction zoomAction, speedAction;
    //private Vector3 initialPosition = new Vector3(0, 0, -10); // Initial position of the camera
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void OnEnable()
    {
        InputActions.FindActionMap("Testing").Enable();
    }

    private void OnDisable()
    {
        InputActions.FindActionMap("Testing").Disable();
    }

    private void Awake()
    {
        zoomAction = InputActions.FindAction("In and Out");
        speedAction = InputActions.FindAction("Speed Control");
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        zoomSpeed = baseZoomSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;

        if (zoomAction.IsPressed())
        {
            scrollAmount = zoomAction.ReadValue<float>() *zoomSpeed;
            Debug.Log(zoomAction.ReadValue<float>());
            transform.Translate(0, 0, scrollAmount * -1);
        }

        if (speedAction.IsPressed())
        {
            float multiplier = (speedAction.ReadValue<float>() * -1) + 1.01f;
            zoomSpeed = baseZoomSpeed * multiplier;
            Debug.Log(speedAction.ReadValue<float>());
        }
    }
}
