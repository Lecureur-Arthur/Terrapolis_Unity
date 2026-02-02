using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class MapReceiver : MonoBehaviour
{
    [Header("Réseau")]
    public int port = 5005;

    [Header("UI - Construction")]
    public GameObject buildPopup; // Le menu avec les boutons de batiments
    public GameObject destroyPopup; // Le menu de confirmation de destruction
    public GameObject BgCloser;   // Le fond global pour fermer les popups

    [Header("UI - Infos & Alertes")]
    public GameObject infoPopup;      // Le panel "InfoPopup"
    public TMP_Text infoTitleText;    // Le texte du Titre
    public TMP_Text infoBodyText;     // Le texte du Message

    private Color[] terrainColors;
    private UdpClient client;
    private IPEndPoint serverEndPoint;
    private Thread receiveThread;
    private bool isRunning = true;
    private Button[] gridButtons;
    private int currentSelectedTileIndex = -1;

    private int[] currentTileTypes;
    
    // File d'attente pour exécuter le code sur le Thread principal d'Unity
    private Queue<System.Action> mainThreadActions = new Queue<System.Action>();

    void Start()
    {
        // --- DÉFINITION DES COULEURS ---
        // ID 0=Void, 1=Plaine, 2=Montagne, 3=Foret, 4=Rivière, 5+=Bâtiments
        terrainColors = new Color[] { 
            Color.black,                    // 0: Void
            new Color(0.45f, 0.85f, 0.45f), // 1: Plaine (Vert clair)
            new Color(0.5f, 0.5f, 0.5f),    // 2: Montagne (Gris)
            new Color(0.15f, 0.55f, 0.15f), // 3: Forêt (Vert fonce)
            new Color(0.25f, 0.45f, 0.95f), // 4: Rivière (Bleu)
            new Color(0.6f, 0.6f, 0.6f),    // 5: Carrière
            new Color(0.6f, 0.3f, 0.1f),    // 6: Scierie
            new Color(0.2f, 0.2f, 0.2f),    // 7: Usine Charbon
            Color.white,                    // 8: Eolienne / Nucleaire
            Color.white,                    // 9: (Reserve)
            new Color(1f, 0.6f, 0f)         // 10: Residence (Orange)
        };

        // Récupération des boutons de la grille (assurez-vous qu'ils sont dans l'ordre)
        gridButtons = GetComponentsInChildren<Button>();

        currentTileTypes = new int[gridButtons.Length];

        for (int i = 0; i < gridButtons.Length; i++)
        {
            int index = i;
            gridButtons[i].onClick.AddListener(() => OnTileClicked(index));
        }

        // Masquer les interfaces au démarrage
        CloseAllPopups();

        StartUDP();
        SendHandshake(); // Dire "READY" au PC
    }

    // --- INTERFACE ---
    void OnTileClicked(int index)
    {
        currentSelectedTileIndex = index;
        if(BgCloser != null) BgCloser.SetActive(true);

        // --- MODIFICATION : Choix entre Construire ou Détruire ---
        int type = currentTileTypes[index];

        // Si ID >= 5 (Bâtiment) et != 99 (pas inondé)
        if (type >= 5 && type != 99)
        {
            // C'est un bâtiment -> On ouvre le menu Détruire
            if (destroyPopup != null) destroyPopup.SetActive(true);
        }
        else
        {
            // C'est vide -> On ouvre le menu Construire
            if (buildPopup != null) buildPopup.SetActive(true);
        }
    }

    public void RequestDestroy()
    {
        if (serverEndPoint == null) return;
        
        // Format : DESTROY,index
        string message = $"DESTROY,{currentSelectedTileIndex}";
        byte[] data = Encoding.UTF8.GetBytes(message);
        
        try {
            client.Send(data, data.Length, serverEndPoint);
        } catch { }

        CloseAllPopups();
    }

    public void RequestBuild(int buildingID)
    {
        SendBuildRequest(buildingID);
    }

    public void CloseAllPopups()
    {
        if(BgCloser != null) BgCloser.SetActive(false);
        if (buildPopup != null) buildPopup.SetActive(false);
        if (infoPopup != null) infoPopup.SetActive(false);
        if (destroyPopup != null) destroyPopup.SetActive(false);
    }

    // --- AFFICHAGE DES INFOS (POPUP) ---
    public void ShowInfo(string type, string title, string message)
    {
        if (infoPopup != null && infoTitleText != null && infoBodyText != null)
        {
            if(BgCloser != null) BgCloser.SetActive(true);
            
            // Couleur du titre selon le type
            if (type == "ERROR") infoTitleText.color = new Color(1f, 0.3f, 0.3f); // Rouge
            else if (type == "CONFIRM") infoTitleText.color = new Color(0.3f, 1f, 0.3f); // Vert
            else infoTitleText.color = Color.white;

            infoTitleText.text = title;
            infoBodyText.text = message;
            
            infoPopup.SetActive(true);
            
            // Fermer le menu construction s'il est ouvert
            if(buildPopup != null) buildPopup.SetActive(false);
        }
    }

    // --- RÉSEAU ---
    void SendBuildRequest(int buildingType)
    {
        if (serverEndPoint == null) return;
        // Envoi au format : BUILD,index,typeID
        string message = $"BUILD,{currentSelectedTileIndex},{buildingType}";
        byte[] data = Encoding.UTF8.GetBytes(message);
        
        try {
            client.Send(data, data.Length, serverEndPoint);
        } catch (System.Exception e) { Debug.LogError("Erreur Envoi: " + e.Message); }

        // On ferme l'interface locale en attendant la réponse du serveur
        CloseAllPopups();
    }

    void SendHandshake()
    {
        try {
            string message = "READY";
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, port));
            Debug.Log("Handshake READY envoyé.");
        } catch { }
    }

    void StartUDP()
    {
        try {
            client = new UdpClient(port);
            client.EnableBroadcast = true;
            receiveThread = new Thread(new ThreadStart(ReceiveLoop));
            receiveThread.IsBackground = true;
            receiveThread.Start();
            Debug.Log("UDP Démarré sur le port " + port);
        } catch (System.Exception e) {
            Debug.LogError("Impossible de démarrer UDP: " + e.Message);
        }
    }

    private void ReceiveLoop()
    {
        while (isRunning)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);

                // Sauvegarde l'adresse du serveur au premier message reçu (sauf si c'est notre propre broadcast)
                if (serverEndPoint == null && text != "READY") 
                {
                    serverEndPoint = anyIP;
                    Debug.Log("Serveur trouvé : " + anyIP.ToString());
                }

                // On renvoie le traitement sur le Thread Principal d'Unity
                lock (mainThreadActions)
                {
                    mainThreadActions.Enqueue(() => ProcessMessage(text));
                }
            }
            catch (System.Exception e) 
            { 
                // Ignorer les erreurs de fermeture de socket
                if(isRunning) Debug.LogWarning("Erreur Réseau: " + e.Message);
            }
        }
    }

    void Update()
    {
        // Exécuter les actions en attente (Mise à jour UI, etc.)
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0) mainThreadActions.Dequeue().Invoke();
        }
    }

    // --- TRAITEMENT DES MESSAGES ---
    void ProcessMessage(string message)
    {
        // 1. POPUP
        // Format Python : POPUP,TYPE,TITRE,MESSAGE (avec | pour les sauts de ligne)
        if (message.StartsWith("POPUP"))
        {
            string[] parts = message.Split(',');
            if(parts.Length >= 4)
            {
                string type = parts[1]; // ERROR ou CONFIRM
                string title = parts[2];
                
                // On recolle le reste du message (au cas où il y aurait des virgules dans le texte)
                string rawBody = string.Join(",", parts, 3, parts.Length - 3);
                
                // IMPORTANT : Remplacer les '|' par des vrais sauts de ligne '\n'
                string cleanBody = rawBody.Replace("|", "\n");

                ShowInfo(type, title, cleanBody);
            }
        }
        // 2. RESULT (Confirmation technique)
        else if (message.StartsWith("RESULT"))
        {
            // Optionnel : Jouer un son ici selon OK ou ERROR
        }
        // 3. CARTE (Suite de nombres séparés par des virgules)
        else 
        {
            UpdateGridColors(message);
        }
    }

    void UpdateGridColors(string dataString)
    {
        string[] values = dataString.Split(',');
        int limit = Mathf.Min(values.Length, gridButtons.Length);

        for (int i = 0; i < limit; i++)
        {
            if (int.TryParse(values[i], out int type))
            {
                // --- NOUVEAU : On sauvegarde le type pour le clic futur ---
                currentTileTypes[i] = type;
                
                if (type == 99) 
                    gridButtons[i].image.color = new Color(0.5f, 0.35f, 0.1f);
                else if (type >= 0 && type < terrainColors.Length)
                    gridButtons[i].image.color = terrainColors[type];
            }
        }
    }

    void OnDestroy()
    {
        isRunning = false;
        if (client != null) client.Close();
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
    }
}