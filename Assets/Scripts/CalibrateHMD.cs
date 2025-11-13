using UnityEngine;
using System.Collections;
using Unity.XR.CoreUtils;
public class CalibrateHMD : MonoBehaviour
{
    public XROrigin xrOrigin;

    public GameObject headset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    private void OnEnable()
    {
        xrOrigin.transform.position = headset.transform.position;
        Debug.Log("hello world");
    }

    // Update is called once per frame
    void Update()
    {
    }
}
