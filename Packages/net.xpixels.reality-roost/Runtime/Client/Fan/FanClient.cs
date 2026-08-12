using RealityRoost.Shared.Fan;
using Unity.Netcode;
using UnityEngine;

namespace RealityRoost.Client.Fan
{
    public class FanClient : NetworkBehaviour
    {

        // Scene-placed NetworkObject, one per scene
        public static FanClient Instance { get; private set; }
        private void Start()
        {
        //     Debug.Log(
        //         $"FanClient Start: IsSpawned={NetworkObject.IsSpawned}, " +
        //         $"IsServer={IsServer}, IsClient={IsClient}, IsOwner={IsOwner}"
        //     );
        }
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[RR][WARN] FanClient: Another instance already exists in this scene, only one is supported.");
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        //Sets the specified fan to the listed fan speed, pitch, and yaw
        public void SetFan(int nodeIndex, float fanSpeed, float pitch, float yaw){
                if(!FanUtils.IsValidFanIndex(nodeIndex, nameof(SetFan))){
                    return;
                }
                SetFanServerRpc(nodeIndex, fanSpeed, pitch, yaw);
        }

        //Sets the specified group of fans to the listed fan speed, pitch, and yaw
        public void SetFanGroup(int[] nodeIndices, float fanSpeed, float pitch, float yaw){
            if(nodeIndices.Length <= 0){
                return;
            }
            SetFanGroupServerRpc(nodeIndices, fanSpeed, pitch, yaw);
        }
        
        //Responsible for relaying the command to the host computer
        [ServerRpc(RequireOwnership = false)]
        private void SetFanServerRpc(int nodeIndex, float fanSpeed, float pitch, float yaw){
            Debug.Log("[FanClient] ServerRpc called");
            FanEvents.RaiseSetFanRequested(nodeIndex, fanSpeed, pitch, yaw);
        }
        [ServerRpc(RequireOwnership = false)]
        private void SetFanGroupServerRpc(int[] nodeIndices, float fanSpeed, float pitch, float yaw){
            FanEvents.RaiseSetFanGroupRequested(nodeIndices, fanSpeed, pitch, yaw);
        }

        
    }
}