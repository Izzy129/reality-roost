using UnityEngine;
using System.Net.Sockets;
using RealityRoost.Shared.Fan;
using RealityRoost.Shared.Core;

namespace RealityRoost.Host.Fans
{
    //drives fans!
    public class FanDriver : RRSubsystem
    {
        protected override string SubsystemName => "FanDriver";

        // Network settings
        private UdpClient udpClient;
        private const int udpPort = 8888;
    
        // Fan node setup
        private readonly FanNode[] nodes = {
            new FanNode("192.168.50.100")
        };

        protected override void OnSubsystemStart()
        {
            udpClient = new UdpClient();

            FanEvents.OnSetFanRequested += HandleSetFan;
            FanEvents.OnSetFanGroupRequested += HandleSetFanGroup;

            ForceSendState();
        }
        protected override void OnSubsystemStop()
        {
            FanEvents.OnSetFanRequested -= HandleSetFan;
            FanEvents.OnSetFanGroupRequested    -= HandleSetFanGroup;

            if (udpClient != null) 
            {
                udpClient.Close();
            }
        }

        // Updated to accept the specific IP address alongside the values
        private void SendPWM(FanNode node)
        {
            // Convert fan speed, pitch, and yaw to their raw values
            byte FanSpeed = (byte)(node.FanSpeed * 255f / 100f);
            //need to test these
            byte Pitch = (byte)(node.Pitch * 255f / 90f);
            byte Yaw = (byte)(node.Yaw * 255f / 90f);
            byte[] payload = {FanSpeed, Pitch, Yaw};

            try 
            {
                Debug.Log($"We got here, so we did send something Speed: {FanSpeed}, Pitch: {Pitch}, Yaw: {Yaw}. IP: {node.NodeIp}");
                
                udpClient.Send(payload, payload.Length, node.NodeIp, udpPort);
            } 
            catch (System.Exception e) 
            {
                Debug.LogError("Failed to send UDP to " + node.NodeIp + ": " + e.Message);
            }
        }

        //Given a valid node index, alter the speed, pitch, and yaw of a fan.
        private void SetFan(int nodeIndex, float fanSpeed, float pitch, float yaw){
            if(!FanUtils.IsValidFanIndex(nodeIndex, nameof(SetFan))){
                return;
            }

            FanNode node = nodes[nodeIndex];

            node.FanSpeed = FanUtils.ClampSpeed(fanSpeed, nameof(SetFan));
            node.Pitch = FanUtils.ClampAngle(pitch, nameof(SetFan));
            node.Yaw = FanUtils.ClampAngle(yaw, nameof(SetFan));

            Debug.Log($"[FanDriver] Physical fans should be updated at this point. Speed: {node.FanSpeed}, Pitch: {node.Pitch}, Yaw: {node.Yaw}. IP: {node.NodeIp}");
            SendPWM(node);
        }

        //Handlers for SetFan
        private void HandleSetFan(int nodeIndex, float fanSpeed, float pitch, float yaw){
            Debug.Log("[FanDriver] HandleSetFan called");
            SetFan(nodeIndex, fanSpeed, pitch, yaw);
        }

        //Handler for SetFanGroup
        private void HandleSetFanGroup(int[] nodeIndices, float fanSpeed, float pitch, float yaw){
            foreach (int nodeIndex in nodeIndices){
                SetFan(nodeIndex, fanSpeed, pitch, yaw);
            }
        }

        //Handler for PointFanAt
        private void HandlePointFanAt(int nodeIndex, float fanSpeed, Vector3 location){
            float x_length = (location.x - FanConstants.FAN_POSITIONS[nodeIndex].x);
            float y_length = (location.y - FanConstants.FAN_POSITIONS[nodeIndex].y);
            float z_length = (location.z - FanConstants.FAN_POSITIONS[nodeIndex].z);
            float z_angle_as_percent = Mathf.Atan(z_length/x_length) / (2*Mathf.PI);
            float x_angle_as_percent = Mathf.Atan(x_length/y_length) / (2*Mathf.PI);
            SetFan(nodeIndex, fanSpeed, z_angle, x_angle);
        }
        // Bypasses the delta-check to send the baseline payload immediately on start
        private void ForceSendState()
        {
            foreach (FanNode node in nodes){
                SendPWM(node);
            }
        }

        
    }
}
