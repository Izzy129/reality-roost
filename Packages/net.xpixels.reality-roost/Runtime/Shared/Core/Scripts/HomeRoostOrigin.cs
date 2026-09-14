using UnityEngine;

namespace RealityRoost.Shared
{
    public class HomeRoostOrigin : MonoBehaviour
    {
        RoostOrigin roostOrigin;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            roostOrigin = RoostOrigin.Singleton;
        }

        // Update is called once per frame
        void Update()
        {
            if(roostOrigin == null) roostOrigin = RoostOrigin.Singleton;

            roostOrigin.transform.position = transform.position;
            roostOrigin.transform.rotation = transform.rotation;
        }
    }
}
