using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkedRoostCamera : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float baseRotationSpeed = 20.0f;
    public float baseMovementSpeed = 10.0f;
    //private Rigidbody rb;
    public InputActionAsset InputActions;
    public float thetaX, thetaY;
    private InputAction moveAction, speedAction;
    float movementUp, turnDirectionY, movementSpeed, rotationSpeed;

    //[SerializeField]
    //public GameObject dataVizPivot;
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
        moveAction = InputActions.FindAction("Up and Side");
        speedAction = InputActions.FindAction("Speed Control");
    }

    void Start()
    {
        movementSpeed = baseMovementSpeed;
        rotationSpeed = baseRotationSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;

        //moveInput = moveAction.ReadValue<Vector2>();
        //when right/left arrow keys pressed, rotate the y direction
        //when up/down arrow pressed, rotate the x direction
        
        movementUp = 0f;
        turnDirectionY = 0f;
        if (moveAction.IsPressed()){
            movementUp =  moveAction.ReadValue<Vector2>().y;
            turnDirectionY = -1*moveAction.ReadValue<Vector2>().x;
            //the vectors were acting weird... ):
            Debug.Log(movementUp + " " + turnDirectionY);
        }

        //dataVizPivot.transform;

        float moveUp = movementUp * movementSpeed * Time.fixedDeltaTime;
        float turnY = turnDirectionY * rotationSpeed * Time.fixedDeltaTime;

        //thetaX += turnX;
        thetaY += turnY;
        //thetaX = Mathf.Clamp(thetaX, -90f, 90f);
        //Vector3 turnRotation = new Vector3(turnX, turnY, 0f);
        //rb.transform.Rotate(rb.rotation * turnRotation,Space.World);
        transform.localEulerAngles = new Vector3 (0f, thetaY, 0f);
        transform.Translate(0, moveUp, 0);

        if (speedAction.IsPressed())
        {
            float multiplier = (speedAction.ReadValue<float>() * -1) + 1.01f;
            movementSpeed = baseMovementSpeed * multiplier;
            rotationSpeed = baseRotationSpeed * multiplier;
            Debug.Log(speedAction.ReadValue<float>());
        }
    }
}
