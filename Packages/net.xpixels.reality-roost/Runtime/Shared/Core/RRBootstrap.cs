using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

namespace RealityRoost.Shared.Core
{
    // RR_Boot (build index 0) holds persistent networking managers and starts the netcode session.
    
    // On Start, rr_network.json decides the role (see RRNetworkConfig):
    //   host   - StartHost(), then loads the calibration scene through NGO's scene manager so
    //            every client that joins is synchronized into it automatically.
    //   client - StartClient() only. It does NOT load a scene locally; NGO synchronization hands
    //            the client whichever scene the host is currently in. Loading locally here would
    //            race that sync.    
    public class RRBootstrap : RRSubsystem
    {
        protected override string SubsystemName => "Network";

        [Tooltip("Build Settings index of the calibration scene the host loads on start (RR_Boot is 0).")]
        [SerializeField] private int calibrationSceneBuildIndex = 1;

        [Tooltip("Seconds to wait for a connection attempt before giving up and retrying.")]
        [SerializeField] private float connectTimeoutSeconds = 5f;

        [Tooltip("Seconds between client connection retries. Retries continue indefinitely.")]
        [SerializeField] private float retryIntervalSeconds = 3f;

        // Fired on this machine only. Wired to network status UI.
        public event Action<ulong> OnConnected;
        public event Action OnDisconnected;
        public event Action<int> OnRetryAttempt;
        public event Action<string> OnStatusChanged;

        // Human-readable connection state, for UIs.
        public string Status { get; private set; } = "Starting...";

        public RRNetworkConfig Config { get; private set; }

        public bool IsHost => Nm != null && Nm.IsListening && Nm.IsServer;

        private Coroutine _clientRoutine;

        private static NetworkManager Nm => NetworkManager.Singleton;

        // ---- Lifecycle ----

        private IEnumerator Start()
        {
            if (Nm == null)
            {
                LogError("No NetworkManager in the RR_Boot scene - add the Roost Network Manager " +
                         "prefab. Networking is disabled.");
                SetStatus("No NetworkManager");
                yield break;
            }

            SetStatus("Waiting for XR...");
            yield return WaitForXrAndRendering();

            Config = RRNetworkConfig.Load();
            LogDebug("RR Network Config Loaded!");
            if (Config.isHost)
            {
                LogDebug("Starting Netcode Session...");
                StartHosting();
            }
            else
            {
                LogDebug("Joining Netcode Session...");
                StartJoining(Config.hostIP);
            }
        }

        // Blocks Start() (before Network config loading and server start/join ) until OpenXR has initialized
        private IEnumerator WaitForXrAndRendering()
        {
            XRManagerSettings manager = XRGeneralSettings.Instance.Manager;

            LogDebug("Waiting for XR initialization...");
            while (!manager.isInitializationComplete)
            {
                LogDebug("XR Manager has not init yet");
                yield return null;
            }

            // Let render pipeline produce at least one full frame before we proceed
            yield return new WaitForEndOfFrame();

            // height check lol
            while (Camera.main == null || Camera.main.transform.localPosition.y < 0.1)
            {
                LogDebug($"Main camera is null or height {Camera.main.transform.localPosition.y} is too low");
                yield return null;
            }
            LogDebug($"height is good now at {Camera.main.transform.localPosition.y}");
            LogDebug("XR initialized, rendering ready.");
        }

        protected override void OnSubsystemStop()
        {
            StopClientRoutine();
            if (Nm != null)
            {
                Nm.OnServerStarted -= HandleServerStarted;
                Nm.OnClientDisconnectCallback -= HandleClientDisconnect;
            }
        }

        // ---- Public API  ----

        // Starts hosting on this machine. Shuts down any existing session first.
        public void HostNow()
        {
            if (Nm == null)
            {
                LogError("HostNow called with no NetworkManager.");
                return;
            }
            StartCoroutine(RestartAs(true, null));
        }

        // Connects to a host at the given IPv4 address. Retries until it succeeds.
        public void JoinNow(string ip)
        {
            if (Nm == null)
            {
                LogError("JoinNow called with no NetworkManager.");
                return;
            }
            if (string.IsNullOrWhiteSpace(ip))
            {
                LogError("JoinNow called with an empty address. Enter the host PC's IP, e.g. 192.168.50.67.");
                SetStatus("Enter a host IP");
                return;
            }
            StartCoroutine(RestartAs(false, ip.Trim()));
        }

        // Leaves the session and stops retrying.
        public void Disconnect()
        {
            StopClientRoutine();
            if (Nm != null && Nm.IsListening)
            {
                Nm.Shutdown();
            }
            SetStatus("Disconnected");
            LogInfo("Disconnected by request.");
            OnDisconnected?.Invoke();
        }

        // This machine's LAN IPv4 address.
        // Returns "unknown" if no non-loopback IPv4 interface is found.
        public string LocalIPAddress
        {
            get
            {
                try
                {
                    foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    {
                        if (address.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(address))
                        {
                            return address.ToString();
                        }
                    }
                }
                catch (Exception e)
                {
                    LogWarning($"Could not resolve the local IP address ({e.Message}).");
                }
                return "unknown";
            }
        }

        // ---- Host ----

        private void StartHosting()
        {
            string localIp = LocalIPAddress;
            string advertised = localIp == "unknown" ? "127.0.0.1" : localIp;

            // Listen on every interface so clients on any room subnet can reach us.
            ApplyConnectionData(advertised, Config.port, "0.0.0.0");

            Nm.OnServerStarted -= HandleServerStarted;
            Nm.OnServerStarted += HandleServerStarted;

            LogDebug("Calling Network Manager StartHost()...");
            if (!Nm.StartHost())
            {
                LogError($"StartHost failed on port {Config.port}. Another process may already be " +
                         "using that port. Networking is disabled.");
                SetStatus("Host failed");
                return;
            }
            LogDebug("Netcode Server successfully started!");
            SetStatus($"Hosting on {LocalIPAddress}:{Config.port}");
            LogInfo($"Hosting on {LocalIPAddress}:{Config.port}.");
        }

        private void HandleServerStarted()
        {
            Nm.OnServerStarted -= HandleServerStarted;
            LoadCalibrationScene();
        }

        // Host-only. Clients receive this scene through NGO synchronization instead.
        private void LoadCalibrationScene()
        {
            int sceneCount = SceneManager.sceneCountInBuildSettings;
            if (calibrationSceneBuildIndex <= 0 || calibrationSceneBuildIndex >= sceneCount)
            {
                LogError($"calibrationSceneBuildIndex {calibrationSceneBuildIndex} is invalid " +
                         $"(build has {sceneCount} scenes, RR_Boot must be 0). Check Build Settings.");
                return;
            }

            string path = SceneUtility.GetScenePathByBuildIndex(calibrationSceneBuildIndex);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);

            SceneEventProgressStatus status =
                Nm.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                LogError($"Loading calibration scene '{sceneName}' failed to start, NGO error: {status}.");
                return;
            }
            LogInfo($"Loading calibration scene '{sceneName}' (build index {calibrationSceneBuildIndex}).");
        }

        // ---- Client ----

        private void StartJoining(string ip)
        {
            StopClientRoutine();

            Nm.OnClientDisconnectCallback -= HandleClientDisconnect;
            Nm.OnClientDisconnectCallback += HandleClientDisconnect;

            _clientRoutine = StartCoroutine(ClientConnectLoop(ip));
        }

        // Retries indefinitely
        private IEnumerator ClientConnectLoop(string ip)
        {
            int attempt = 0;

            while (true)
            {
                attempt++;
                ApplyConnectionData(ip, Config.port);

                SetStatus($"Connecting to {ip}:{Config.port} (attempt {attempt})...");

                if (Nm.StartClient())
                {
                    float deadline = Time.realtimeSinceStartup + connectTimeoutSeconds;
                    while (Nm.IsClient && !Nm.IsConnectedClient && Time.realtimeSinceStartup < deadline)
                    {
                        yield return null;
                    }

                    if (Nm.IsConnectedClient)
                    {
                        _clientRoutine = null;
                        SetStatus($"Connected to {ip}:{Config.port}");
                        LogInfo($"Connected to {ip}:{Config.port} as client {Nm.LocalClientId}.");
                        OnConnected?.Invoke(Nm.LocalClientId);
                        yield break;
                    }
                }

                if (Nm.IsListening || Nm.IsClient)
                {
                    Nm.Shutdown();
                    while (Nm.ShutdownInProgress)
                    {
                        yield return null;
                    }
                }

                LogWarning($"Connect to {ip}:{Config.port} failed, retry {attempt} " +
                           $"(next attempt in {retryIntervalSeconds:0.#}s).");
                SetStatus($"Host unreachable at {ip}:{Config.port} - retrying...");
                OnRetryAttempt?.Invoke(attempt);

                yield return new WaitForSeconds(retryIntervalSeconds);
            }
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            // On the host this also fires for every remote client leaving
            if (Nm.IsServer || clientId != Nm.LocalClientId)
            {
                return;
            }

            LogWarning($"Lost connection to the host{FormatReason()}. Reconnecting...");
            SetStatus("Connection lost - reconnecting...");
            OnDisconnected?.Invoke();

            if (_clientRoutine == null)
            {
                StartJoining(Config.hostIP);
            }
        }

        private string FormatReason()
        {
            string reason = Nm.DisconnectReason;
            return string.IsNullOrEmpty(reason) ? string.Empty : $" ({reason})";
        }

        // ---- Internals ----

        private IEnumerator RestartAs(bool host, string ip)
        {
            StopClientRoutine();

            if (Nm.IsListening)
            {
                Nm.Shutdown();
                while (Nm.ShutdownInProgress)
                {
                    yield return null;
                }
            }

            if (host)
            {
                StartHosting();
            }
            else
            {
                Config.hostIP = ip;
                StartJoining(ip);
            }
        }

        private void ApplyConnectionData(string address, int port, string listenAddress = null)
        {
            if (Nm.NetworkConfig.NetworkTransport is not UnityTransport transport)
            {
                LogError("NetworkManager's transport is not UnityTransport - cannot set the address. " +
                         "Check the Roost Network Manager prefab.");
                return;
            }
            transport.SetConnectionData(address, (ushort)port, listenAddress);
        }

        private void StopClientRoutine()
        {
            if (_clientRoutine != null)
            {
                StopCoroutine(_clientRoutine);
                _clientRoutine = null;
            }
        }

        private void SetStatus(string status)
        {
            Status = status;
            OnStatusChanged?.Invoke(status);
        }
    }
}
