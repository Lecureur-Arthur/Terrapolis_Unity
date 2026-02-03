using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("Références")]
    public GameObject popupPanel; // Glissez votre Panel UI (le menu de construction) ici
    public UdpSender udpSender;   // Glissez votre objet NetworkManager ici

    // Variables pour retenir où on a cliqué
    private int tempX;
    private int tempY;

    void Start()
    {
        // On s'assure que le popup est caché au lancement
        if(popupPanel != null) 
            popupPanel.SetActive(false);
    }

    // Étape 1 : Appelée quand on clique sur la grille
    public void OpenBuildPopup(int x, int y)
    {
        tempX = x;
        tempY = y;
        
        if(popupPanel != null)
            popupPanel.SetActive(true); // Affiche le menu
            
        Debug.Log($"Sélection de la case : {x}, {y}");
    }

    // Étape 2 : Appelée par les boutons du Popup (Scierie, Eolienne, etc.)
    // Dans l'inspecteur du bouton, tapez le nom (ex: "Eolienne") dans le champ texte
    public void SelectBuilding(string typeBatiment)
    {
        // Création du paquet de données
        BuildingPacket packet = new BuildingPacket();
        packet.x = tempX;
        packet.y = tempY;
        packet.batiment = typeBatiment;

        // Conversion en JSON
        string json = JsonUtility.ToJson(packet);

        // Envoi au PC
        if (udpSender != null)
        {
            udpSender.SendString(json);
        }

        // Fermeture du menu
        ClosePopup();
    }

    public void ClosePopup()
    {
        if(popupPanel != null)
            popupPanel.SetActive(false);
    }
}