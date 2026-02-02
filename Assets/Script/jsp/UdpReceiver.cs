using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro; // Important pour TextMeshPro

public class UdpReceiver : MonoBehaviour
{
    [Header("Configuration")]
    public int listenPort = 5006; // Le port que Python va viser
    public TextMeshProUGUI statusText; // Glissez votre zone de texte ici

    private Thread receiveThread;
    private UdpClient client;
    private bool isRunning = true;

    // Variable tampon pour passer l'info du Thread Réseau au Thread Unity
    private string lastReceivedMessage = ""; 
    private bool hasNewMessage = false;

    void Start()
    {
        // On démarre l'écoute dans un thread séparé
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        
        UpdateText("En attente du serveur...");
    }

    // Cette fonction tourne en boucle en arrière-plan
    private void ReceiveData()
    {
        client = new UdpClient(listenPort);
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                // Bloque jusqu'à réception d'un message
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);

                // On stocke le message pour que Update() le lise
                lastReceivedMessage = text;
                hasNewMessage = true;
            }
            catch (System.Exception e)
            {
                // Erreurs normales lors de la fermeture du socket
                Debug.LogWarning(e.Message);
            }
        }
    }

    // Update tourne sur le "Main Thread" (seul endroit où on peut toucher à l'UI)
    void Update()
    {
        if (hasNewMessage)
        {
            statusText.text = lastReceivedMessage;
            hasNewMessage = false; // On reset le drapeau
            
            // Optionnel : Faire disparaître le texte après 3 secondes
            CancelInvoke("ClearText");
            Invoke("ClearText", 3.0f);
        }
    }

    void ClearText()
    {
        statusText.text = "";
    }
    
    // Fonction utilitaire pour mettre à jour le texte manuellement si besoin
    public void UpdateText(string msg)
    {
        statusText.text = msg;
    }

    void OnDisable()
    {
        // Nettoyage propre quand on quitte le jeu
        isRunning = false;
        if (client != null) client.Close();
        if (receiveThread != null) receiveThread.Abort();
    }
}