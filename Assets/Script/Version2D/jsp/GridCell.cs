using UnityEngine;

public class GridCell : MonoBehaviour
{
    [Header("Coordonnées de la case")]
    public int x; // À régler manuellement dans l'inspecteur pour chaque bouton (0 à 9)
    public int y; // À régler manuellement dans l'inspecteur pour chaque bouton (0 à 14)

    private BuildingManager manager;

    void Start()
    {
        // Trouve le manager automatiquement dans la scène
        manager = FindObjectOfType<BuildingManager>();
    }

    // Liez cette fonction au bouton "On Click" de la case
    public void OnClickCell()
    {
        if (manager != null)
        {
            manager.OpenBuildPopup(x, y);
        }
        else
        {
            Debug.LogError("BuildingManager introuvable dans la scène !");
        }
    }
}