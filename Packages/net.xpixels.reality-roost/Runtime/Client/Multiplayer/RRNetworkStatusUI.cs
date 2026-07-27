using RealityRoost.Shared.Core;
using TMPro;
using UnityEngine;

namespace RealityRoost.Client.Multiplayer
{
    // Drives text fields on an authored connection UI (e.g. a body-locked panel on the rig).
    //
    // Drop this on your UI panel, assign the TMP fields you care about, and leave the rest empty -
    // every reference is optional. Buttons on the same panel should call RRBootstrap.HostNow(),
    // JoinNow(string) and Disconnect() directly through UnityEvents in the Inspector; this
    // component only displays state, it does not drive the connection.
    //
    // Lives in the Client assembly so the Shared assembly stays free of a TextMeshPro dependency.
    public class RRNetworkStatusUI : MonoBehaviour
    {
        [Tooltip("Leave empty to find the RRBootstrap in the RR_Boot scene automatically.")]
        [SerializeField] private RRBootstrap bootstrap;

        [Header("Optional display fields")]
        [Tooltip("Connection state, e.g. 'Connecting to 192.168.1.50:7777 (attempt 3)...'")]
        [SerializeField] private TMP_Text statusText;

        [Tooltip("This machine's LAN IP. Show it on the host so it can be typed into clients.")]
        [SerializeField] private TMP_Text localIpText;

        [Tooltip("Role of this machine: 'Host' or 'Client'.")]
        [SerializeField] private TMP_Text roleText;

        private void OnEnable()
        {
            if (bootstrap == null)
            {
                bootstrap = FindFirstObjectByType<RRBootstrap>();
            }
            if (bootstrap == null)
            {
                Debug.LogWarning("[RR][WARN] NetworkStatusUI: no RRBootstrap found - " +
                                 "connection status will not update. Assign it in the Inspector, " +
                                 "or make sure the RR_Boot scene loaded first.");
                return;
            }

            bootstrap.OnStatusChanged += HandleStatusChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (bootstrap != null)
            {
                bootstrap.OnStatusChanged -= HandleStatusChanged;
            }
        }

        private void HandleStatusChanged(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }
            Refresh();
        }

        private void Refresh()
        {
            if (statusText != null)
            {
                statusText.text = bootstrap.Status;
            }
            if (localIpText != null)
            {
                localIpText.text = bootstrap.LocalIPAddress;
            }
            if (roleText != null)
            {
                roleText.text = bootstrap.IsHost ? "Host" : "Client";
            }
        }
    }
}
