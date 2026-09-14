using System;
using System.IO;
using UnityEngine;

namespace RealityRoost.Shared.Core
{
    // network settings read from rr_network.json next to build executable
    //
    // The file is authored per machine is NOT inside the build,
    // This means that the same client.exe can be a host on one PC and a client on another. 
    // TODO: this violates the RR_HOST and RR_CLIENT separate build architecture, but works for now...
    //
    //   {
    //     "isHost": false,
    //     "hostIP": "192.168.1.50",
    //     "port": 7777
    //   }
    //
    // If the file is missing/corrupt, the below defaults apply.
    // This lets a solo build (or the Editor) start as a solo host and is immediately playable without any setup
    [Serializable]
    public class RRNetworkConfig
    {
        [Tooltip("True: this machine hosts the session. False: it connects to hostIP.")]
        public bool isHost = true;

        [Tooltip("IPv4 address of the host machine. Ignored when isHost is true.")]
        public string hostIP = "127.0.0.1";

        [Tooltip("UDP port the host listens on. Must match on every machine in the room.")]
        public int port = 7777;

        // Loads config from RRConfig.NetworkFilePath.
        // Falls back to defaults (solo host) and warns.
        //  hostValue = if the user is a Host or Client
        public static RRNetworkConfig Load(bool hostValue)
        {
            string path = RRConfig.NetworkFilePath;

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[RR][WARN] Network: no config at '{path}' - " +
                                 "starting as a solo host. Create rr_network.json next to the " +
                                 "executable to join a running experience.");
                return new RRNetworkConfig();
            }

            try
            {
                string json = File.ReadAllText(path);
                RRNetworkConfig config = JsonUtility.FromJson<RRNetworkConfig>(json);
                config.RestoreToDefault();

                config.isHost = hostValue;
                if (config == null)
                {
                    Debug.LogError($"[RR][ERROR] Network: '{path}' is not valid JSON - " +
                                   "starting as a solo host. Fix the file and relaunch.");
                    return new RRNetworkConfig();
                }

                if (config.isHost == false)
                {
                    config.hostIP = "192.168.50.193"; // change to using RRBoostrap "LocalIPAddress"
                    Debug.Log("Config isHost = false");
                }
                Debug.Log("host IP config: " + config.hostIP);

                if (string.IsNullOrWhiteSpace(config.hostIP))
                {
                    Debug.LogWarning("[RR][WARN] Network: hostIP is missing from JSON, setting hostIP to localhost.");
                    config.hostIP = "127.0.0.1";
                }
                if (config.port <= 0 || config.port > 65535)
                {
                    Debug.LogWarning($"[RR][WARN] Network: port {config.port} in '{path}' is out of " +
                                     "range - falling back to 7777.");
                    config.port = 7777;
                }

                Debug.Log($"[RR][INFO] Network: config loaded from '{path}' " +
                          $"(isHost={config.isHost}, hostIP={config.hostIP}, port={config.port}).");
                return config;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RR][ERROR] Network: could not read '{path}' ({e.Message}) - " +
                               "starting as a solo host.");
                return new RRNetworkConfig();
            }
        }

        // Writes the current values to RRConfig.NetworkFilePath.
        // Used by editor tooling; the runtime only ever reads.
        public void Save()
        {
            string path = RRConfig.NetworkFilePath;
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(this, true));
                Debug.Log($"[RR][INFO] Network: config written to '{path}'.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[RR][ERROR] Network: could not write '{path}' ({e.Message}).");
            }
        }
        public void RestoreToDefault()
        {
            isHost = true;
            hostIP = "127.0.0.1";
            port = 7777;
            Save();
        }
    }
}
