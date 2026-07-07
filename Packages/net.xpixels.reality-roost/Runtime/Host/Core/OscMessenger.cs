using System;
using extOSC;
using UnityEngine;

namespace RealityRoost.Host.Core
{
    public class OscMessenger : MonoBehaviour
    {
        [SerializeField] private OSCTransmitter transmitter;

        public void Send(OSCMessage message)
        {
            if (transmitter == null)
            {
                Debug.LogError("[RR][ERROR] OscMessenger: OSCTransmitter reference is not assigned in Inspector!");
                return;
            }

            try
            {
                transmitter.Send(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RR][ERROR] OscMessenger: Failed to send OSC message: {ex.Message}");
            }
        }
    }
}
