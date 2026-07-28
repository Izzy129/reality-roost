using UnityEngine;
using UnityEngine.InputSystem;

public class ZoomInAndOut : MonoBehaviour
{
    //private Rigidbody rb;
    public InputActionAsset InputActions;
    private float zoomSpeed = 0.1f;
    private float scrollAmount = 0f;
    private InputAction zoomAction;
    //private Vector3 initialPosition = new Vector3(0, 0, -10); // Initial position of the camera
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void OnEnable()
    {
        InputActions.FindActionMap("Viewing").Enable();
    }

    private void OnDisable()
    {
        InputActions.FindActionMap("Viewing").Disable();
    }

    private void Awake()
    {
        zoomAction = InputSystem.actions.FindAction("Zoom");
    }
    void Start()
    {
        
    //rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        //keep x and y at 0, only change z when scrolling
        //Input.mouseScrollDelta.y is either 1 or -1 when scrolling(?)
        //why does nothing work...
        if (zoomAction.IsPressed())
        {
            scrollAmount = zoomAction.ReadValue<Vector2>().y * zoomSpeed;
            transform.Translate(0, 0, scrollAmount * -1);
        }
        
    }
}
