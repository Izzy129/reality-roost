using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRotater : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float rotationSpeed = 50.0f;
    //private Rigidbody rb;
    public InputActionAsset InputActions;
    public float thetaX, thetaY;
    private InputAction rotateAction;
    float turnDirectionX, turnDirectionY;
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
        rotateAction = InputSystem.actions.FindAction("Rotate");
    }

    void Start()
    {  
        //rb = GetComponent<Rigidbody>();
        
    }

    // Update is called once per frame
    void Update()
    {
        //moveInput = moveAction.ReadValue<Vector2>();
        //when right/left arrow keys pressed, rotate the y direction
        //when up/down arrow pressed, rotate the x direction
        //Vector2 moveInput = Vector2.zero;


        /*// up/down (rotation)
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            moveInput.x = 1f;
        }
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            moveInput.x = -1f;
        }
        
        turnDirectionX = moveInput.x;

        // Left/right (rotation)
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            moveInput.y = 1f;
        }
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            moveInput.y = -1f;
        }
        turnDirectionY = moveInput.y; */
        turnDirectionX = 0f;
        turnDirectionY = 0f;
        if (rotateAction.IsPressed()){
            turnDirectionX =  rotateAction.ReadValue<Vector2>().y;
            turnDirectionY = -1*rotateAction.ReadValue<Vector2>().x;
            //the vectors were acting weird... ):
            Debug.Log(turnDirectionX + " " + turnDirectionY);
        }

        float turnX = turnDirectionX * rotationSpeed * Time.fixedDeltaTime;
        float turnY = turnDirectionY * rotationSpeed * Time.fixedDeltaTime;

        thetaX += turnX;
        thetaY += turnY;
        thetaX = Mathf.Clamp(thetaX, -90f, 90f);
        //Vector3 turnRotation = new Vector3(turnX, turnY, 0f);
        //rb.transform.Rotate(rb.rotation * turnRotation,Space.World);
        transform.localEulerAngles = new Vector3 (thetaX, thetaY, 0f);  
    }
}
