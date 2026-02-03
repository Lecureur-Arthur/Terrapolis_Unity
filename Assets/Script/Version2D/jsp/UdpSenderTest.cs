using UnityEngine;
using System.Net.Sockets;
using System.Text;

public class UdpSenderTest : MonoBehaviour
{
    public string ipAddress = "10.255.137.104";
    public int port = 5005;

    public void SendMessageToPC()
    {
        UdpClient udpClient = new UdpClient();

        try
        {
            udpClient.Connect(ipAddress, port);
            string message = "Hello from Unity!";
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length);
            Debug.Log("Message sent to " + ipAddress + ":" + port);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error sending UDP message: " + e.Message);
        }
        finally
        {
            udpClient.Close();
        }
    }
}
