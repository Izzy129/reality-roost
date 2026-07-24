using Unity.Netcode;
using UnityEngine;

public class NetworkFollower : NetworkBehaviour
{
    [SerializeField] private bool _isInFBT = false;
    // Prefab references
    public GameObject head;
    public GameObject leftController;
    public GameObject rightController;
    public GameObject leftAnkle;
    public GameObject rightAnkle;
    //public GameObject waist;

    // Scene references
    private GameObject headRef;
    private GameObject leftControllerRef;
    private GameObject rightControllerRef;
    private GameObject leftAnkleRef;
    private GameObject rightAnkleRef;
    //private GameObject waistRef;

    private void Start()
    {
        if (IsOwner)
        {
            headRef = GameObject.Find("Head Target");
            leftControllerRef = GameObject.Find("Left Hand Target");
            rightControllerRef = GameObject.Find("Right Hand Target");

            if(_isInFBT)
            {
                leftAnkleRef = GameObject.Find("Left Ankle Target");
                rightAnkleRef = GameObject.Find("Right Ankle Target");
                //waistRef = GameObject.Find("Waist Target");
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
            
            if (_isInFBT)
            {
                FollowTransform(leftAnkle, leftAnkleRef);
                FollowTransform (rightAnkle, rightAnkleRef);
                //FollowTransform(waist, waistRef);
            }
        }
    }

    void FollowTransform(GameObject reference, GameObject objToFollow)
    {
        reference.transform.position = objToFollow.transform.position;
        reference.transform.rotation = objToFollow.transform.rotation;
    }
}
