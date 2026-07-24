using UnityEngine;
using extOSC;

public class TileOSCSender : MonoBehaviour
{

    private float oscSendInterval = 1f / 60f; // send at 60hz 
    private float lastSendTime = 0f;

    // Called once per frame
    public void Update()
    {
        //Debug.Log($"FPS: {1.0f / Time.deltaTime:F1}");

        // only send OSC at 60hz
        if (Time.time - lastSendTime >= oscSendInterval)
        {
            //Debug.Log(Time.time - lastSendTime);
            lastSendTime = Time.time;
            SendIntensities(tileIntensities);
        }
    }

    [Header("OSC Transmitter")]
    public OSCTransmitter transmitter;

    [Header("Manual test values")]
    public float[] tileIntensities = new float[16];

    // TEST button in Inspector
    [ContextMenu("Test Send (Inspector)")]
    public void TestSendIntensities()
    {
        SendIntensities(tileIntensities);
    }

    public void SendIntensities(float[] intensities)
    {
        // Validate
        if (intensities == null)
        {
            Debug.LogError("Intensity array is null.");
            return;
        }

        if (intensities.Length != 16)
        {
            Debug.LogError("Intensity array must be length 16.");
            return;
        }

        foreach (float f in intensities)
        {
            if (f < 0f || f > 1f)
            {
                Debug.LogError("Intensity out of range (0–1).");
                return;
            }
        }

        // Build OSC message
        var message = new OSCMessage("/tile/intensities");

        for (int i = 0; i < 16; i++)
        {
            message.AddValue(OSCValue.Float(intensities[i]));
        }

        // Send with error handling
        try
        {
            transmitter.Send(message);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to send OSC message: {ex.Message}");
        }
    }
}