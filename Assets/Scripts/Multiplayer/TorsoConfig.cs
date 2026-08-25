using UnityEngine;
using RootMotion.FinalIK;

public class TorsoConfig : MonoBehaviour
{
    [SerializeField] private GameObject head;
    [SerializeField] private GameObject chest;
    [SerializeField] private GameObject pelvis;
    [SerializeField] private GameObject leftFoot, rightFoot;

    public float chestVariation = 0.05f;
    public float pelvisVariation = 0.05f;
    public float footVariation = 0.05f;
    private Vector3 headPos;

    void Update()
    {
        headPos = head.transform.position;

        chest.transform.position = headPos - chestVariation*Vector3.up; 
        chest.transform.localEulerAngles = new Vector3(0,head.transform.localEulerAngles.y,0);

        pelvis.transform.position = headPos - pelvisVariation*Vector3.up;
        pelvis.transform.localEulerAngles = new Vector3(0,head.transform.localEulerAngles.y,0);
    }
}
