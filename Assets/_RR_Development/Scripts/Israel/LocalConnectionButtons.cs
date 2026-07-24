using TMPro;
using UnityEngine;
using XRMultiplayer;

public class LocalConnectionButtons : MonoBehaviour
{
    [SerializeField] TMP_InputField m_HostIPInputField;
    [SerializeField] LobbyUI m_LobbyUI;

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

    public void JoinWithIP()
    {
        string hostIP = m_HostIPInputField.text;
        if (string.IsNullOrWhiteSpace(hostIP))
        {
            Debug.LogError("[LocalConnectionButtons] Host IP field is empty.");
            return;
        }

        m_LobbyUI.SetIP(hostIP);
        Join();
    }
}
