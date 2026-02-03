using UnityEngine;
using System.Net.Sockets;
using System.Text;

public class UdpSender : MonoBehaviour
{
    [Header("Configuration Réseau")]
    // REMPLACEZ CECI PAR L'IP DE VOTRE PC (ex: 192.168.1.15)
    public string pcIpAddress = "172.23.163.104"; 
    public int port = 5005;

    // Cette fonction reçoit le texte JSON et l'envoie au PC
    public void SendString(string messageToSend)
    {
        UdpClient client = new UdpClient();
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(messageToSend);
            
            // Envoi du paquet
            client.Send(data, data.Length, pcIpAddress, port);
            Debug.Log($"Données envoyées : {messageToSend}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur d'envoi UDP : {e.Message}");
        }
        finally
        {
            client.Close();
        }
    }
}