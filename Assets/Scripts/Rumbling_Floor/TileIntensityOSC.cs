using UnityEngine;
using extOSC;

public class TileIntensityOSC : MonoBehaviour
{
    [Header("OSC")]
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

        // ✅ extOSC uses Send(message)
        transmitter.Send(message);

        Debug.Log("Sent intensity array via OSC.");
    }
}