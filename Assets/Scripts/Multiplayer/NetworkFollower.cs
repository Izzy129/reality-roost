using Unity.Netcode;
using UnityEngine;

public class NetworkFollower : NetworkBehaviour
{
    // Prefab references
    public GameObject head;
    public GameObject leftController;
    public GameObject rightController;
    public GameObject leftAnkle;
    public GameObject rightAnkle;
    public GameObject waist;

    // Scene references
    private GameObject headRef;
    private GameObject leftControllerRef;
    private GameObject rightControllerRef;
    private GameObject leftAnkleRef;
    private GameObject rightAnkleRef;
    private GameObject waistRef;

    private void Start()
    {
        if (IsOwner)
        {
            headRef = GameObject.Find("Head Target");
            if (headRef == null)
            {
                Debug.LogError("Could not find Head Target in scene!");    
            }
            
            leftControllerRef = GameObject.Find("Left Hand Target");
            if (leftControllerRef == null)
            {
                Debug.LogError("Could not find Left Controller Target in scene!");    
            }

            rightControllerRef = GameObject.Find("Right Hand Target");

            if (rightControllerRef == null)
            {
                Debug.LogError("Could not find Right Controller Target in scene!");    
            }

            if (CalibrationState.Value == CalibrationMode.FullBody)
            {
                leftAnkleRef = GameObject.Find("Left Ankle Target");
                if (leftAnkleRef == null)
                {
                    Debug.LogError("Could not find Left Ankle Target in scene!");
                }
                rightAnkleRef = GameObject.Find("Right Ankle Target");
                if (rightAnkleRef == null)
                {
                    Debug.LogError("Could not find Right Ankle Target in scene!");
                }
                waistRef = GameObject.Find("Waist Target");
                if (waistRef == null)
                {
                    Debug.LogError("Could not find Waist Target in scene!");
                }
            }
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            FollowTransform(head, headRef);
            FollowTransform(leftController, leftControllerRef);
            FollowTransform(rightController, rightControllerRef);
            
            if (CalibrationState.Value == CalibrationMode.FullBody)
            {
                FollowTransform(leftAnkle, leftAnkleRef);
                FollowTransform (rightAnkle, rightAnkleRef);
                FollowTransform(waist, waistRef);
            }
        }
    }

    void FollowTransform(GameObject reference, GameObject objToFollow)
    {
        reference.transform.position = objToFollow.transform.position;
        reference.transform.rotation = objToFollow.transform.rotation;
    }
}
