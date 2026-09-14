using UnityEngine;

namespace RealityRoost.Shared
{
    public class RoostOrigin : MonoBehaviour
    {
        public static RoostOrigin Singleton;
        void Start()
        {
            if (Singleton == null) Singleton = this;
            else Destroy(gameObject);

            DontDestroyOnLoad(gameObject);
        }
    }
}
