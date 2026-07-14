using Unity.Netcode;
using UnityEngine;

public class NetworkFollower : NetworkBehaviour
{
    // Prefab references
    public GameObject head;
    public GameObject leftController;
    public GameObject rightController;

    // Scene references
    [SerializeField] private GameObject headRef;
    [SerializeField] private GameObject leftControllerRef;
    [SerializeField] private GameObject rightControllerRef;

    private void Start()
    {
        if (IsOwner)
        {
            headRef = GameObject.Find("Head Target");
            leftControllerRef = GameObject.Find("Left Hand Target");
            rightControllerRef = GameObject.Find("Right Hand Target");
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            FollowTransform(head, headRef);
            FollowTransform(leftController, leftControllerRef);
            FollowTransform(rightController, rightControllerRef);
        }
    }

    void FollowTransform(GameObject reference, GameObject objToFollow)
    {
        reference.transform.position = objToFollow.transform.position;
        reference.transform.rotation = objToFollow.transform.rotation;
    }
}
