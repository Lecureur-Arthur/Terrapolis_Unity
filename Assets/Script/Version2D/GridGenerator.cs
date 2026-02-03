using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GridGenerator : MonoBehaviour
{
    [Header("Parametre")]
    public GameObject buttonPrefab;
    public int columns = 10;
    public int rows = 15;

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        // On s'assure que le prefabe est bien assigné
        if (buttonPrefab == null)
        {
            Debug.LogError("Button Prefab is not assigned!");
            return;
        }

        // Boucle pour créer les boutons
        int totalButtons = columns * rows;

        for (int i = 0; i < totalButtons; i++)
        {
            // 1. Créer le bouton en tant qu"enfant de cet objet (le GridPanel)
            GameObject newButton = Instantiate(buttonPrefab, this.transform);

            // 2. Rennomer l'objet pour s'y retrouver dans l'éditeur (ex: "Button_0", "Button_1", ...)
            newButton.name = $"Button_{i}";

            // 3. (Optionnel) Changer le texte à l'intérieur pour afficher le numéro
            // Si vous utilisez le Text standar d'Unity
            TMP_Text     buttonText = newButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = i.ToString();
            }

           //   
        }
    }
}
