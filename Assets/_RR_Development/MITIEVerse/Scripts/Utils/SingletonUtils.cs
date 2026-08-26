using UnityEngine;

public static class SingletonUtils
{
    /// <summary>
    /// Sets up a standard singleton for a MonoBehaviour. This function should be called in Awake. Will destroy its GameObject if it is a duplicate.
    /// </summary>
    /// <typeparam name="T">The MonoBehaviour type of the singleton</typeparam>
    /// <param name="singleton">The static singleton property for the type</param>
    /// <param name="instance">The instance to assign as the singleton or destroy</param>
    /// <returns>A reference to the singleton</returns>
    public static T SingletonSetup<T>(T singleton, T instance) where T : MonoBehaviour
    {
        if (singleton != null && singleton != instance)
        {
            GameObject.Destroy(instance.gameObject);
            return singleton;
        }

        singleton = instance;
        GameObject.DontDestroyOnLoad(singleton.gameObject);
        return instance;
    }
}
