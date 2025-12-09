using ReadyPlayerMe.NetcodeSupport;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class RPM_RelayManager : MonoBehaviour
{
    [SerializeField] private int maxPlayers = 4;

    [SerializeField] private GameObject startPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private InputField urlField;

    private async void Start()
    {
        // Initialize Unity Services
        await UnityServices.InitializeAsync();

        // Sign in anonymously
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("✅ Signed in to Unity Services");
        }

        startButton.onClick.AddListener(() =>
        {
            NetworkPlayer.InputUrl = urlField.text;
            startPanel.SetActive(false);
            //connectionPanel.SetActive(true);
        });

    }

    public async Task<string> StartHostWithRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);


            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Join Code: {joinCode}");

            
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
                );

            // Start as host
            NetworkManager.Singleton.StartHost();
            Debug.Log("Started as HOST");

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e.Message);
            return null;
        }
    }

    public async Task<bool> JoinWithRelay(string joinCode)
    {
        try
        {
            // Join the Relay allocation
            JoinAllocation allocation;
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log(allocation.Region.ToString());

            Debug.Log($" Joined Relay Server: {allocation.RelayServer.IpV4}:{allocation.RelayServer.Port}");

            // Get UnityTransport component
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            // Set client Relay data
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
                );

            // Start as client
            NetworkManager.Singleton.StartClient();
            Debug.Log("✅ Started as CLIENT");

            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"❌ Relay Join Error: {e.Message}");
            return false;
        }
    }
}
