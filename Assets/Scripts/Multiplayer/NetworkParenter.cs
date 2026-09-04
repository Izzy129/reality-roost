using UnityEngine;

public class NetworkParenter : MonoBehaviour
{
    void Start()
    {
        var obj = GameObject.Find("Networked Players");
        if(obj != null)
        {
            gameObject.transform.SetParent(obj.transform);
        }
    }
}
