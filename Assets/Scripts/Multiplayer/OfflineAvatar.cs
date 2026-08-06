using UnityEngine;
using XRMultiplayer;

public class OfflineAvatar : MonoBehaviour
{
    void OnEnable()
    {
        XRINetworkGameManager.Connected.Subscribe(connected =>
        {
            gameObject.SetActive(!connected);
        });
    }

    void OnDisable()
    {
        XRINetworkGameManager.Connected.Unsubscribe(connected =>
        {
            gameObject.SetActive(!connected);
        });
    }
}
