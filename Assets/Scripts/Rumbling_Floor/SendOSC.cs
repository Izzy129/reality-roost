using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class SendOSC : MonoBehaviour
{
    public string ip = "127.0.0.1";
    public int port = 9000;

    UdpClient udp;
    IPEndPoint endpoint;

    void Start()
    {
        udp = new UdpClient();
        endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
        Debug.Log("OSC Sender initialized");
    }

    void Update()
    {
        float value = Mathf.Abs(Mathf.Sin(Time.time));
        byte[] message = BuildOSCMessage("/rumble", value);
        udp.Send(message, message.Length, endpoint);
        Debug.Log("Sending OSC value: " + value);
    }

    byte[] BuildOSCMessage(string address, float value)
    {
        var addrBytes = Encoding.ASCII.GetBytes(address + "\0");
        while (addrBytes.Length % 4 != 0)
            addrBytes = AddByte(addrBytes, 0);

        var typeBytes = Encoding.ASCII.GetBytes(",f\0");
        while (typeBytes.Length % 4 != 0)
            typeBytes = AddByte(typeBytes, 0);

        byte[] floatBytes = System.BitConverter.GetBytes(value);
        if (System.BitConverter.IsLittleEndian)
            System.Array.Reverse(floatBytes);

        byte[] full = new byte[addrBytes.Length + typeBytes.Length + floatBytes.Length];
        System.Buffer.BlockCopy(addrBytes, 0, full, 0, addrBytes.Length);
        System.Buffer.BlockCopy(typeBytes, 0, full, addrBytes.Length, typeBytes.Length);
        System.Buffer.BlockCopy(floatBytes, 0, full, addrBytes.Length + typeBytes.Length, floatBytes.Length);

        return full;
    }

    byte[] AddByte(byte[] source, byte b)
    {
        byte[] output = new byte[source.Length + 1];
        System.Buffer.BlockCopy(source, 0, output, 0, source.Length);
        output[output.Length - 1] = b;
        return output;
    }
}