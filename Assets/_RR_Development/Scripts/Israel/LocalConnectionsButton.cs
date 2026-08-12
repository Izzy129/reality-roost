using UnityEngine;
using XRMultiplayer;

public class LocalConnectionButtons : MonoBehaviour
{
    public void Host()
    {
        bool started = XRINetworkGameManager.Instance.HostLocalConnection();
        if (!started)
        {
            Debug.LogError("[LocalConnectionButtons] HostLocalConnection() failed to start.");
        }
    }

    public void Join()
    {
        bool started = XRINetworkGameManager.Instance.JoinLocalConnection();
        if (!started)
        {
            Debug.LogError("[LocalConnectionButtons] JoinLocalConnection() failed to start.");
        }
    }
}