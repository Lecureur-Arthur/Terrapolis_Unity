using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
[RequireComponent(typeof(RectTransform))]
public class ResponsiveGrid : MonoBehaviour
{
    public int columns = 10;
    public int rows = 15;

    // Espace voulu entre les boutons (optionnel)
    public Vector2 spacing = new Vector2(5, 5);

    private GridLayoutGroup gridLayout;
    private RectTransform rectTransform;

    void Awake()
    {
        gridLayout = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        AjustGridSize();
    }

    // Appelé aussi quand l'écran est redimensionné
    void OnRectTransformDimensionsChange()
    {
        AjustGridSize();
    }

    public void AjustGridSize()
    {
        // 1. Appliquer l'espacement choisi
        gridLayout.spacing = spacing;

        // 2. Calculer la largeur et hauteur totales disponibles
        float containerWidth = rectTransform.rect.width;
        float containerHeight = rectTransform.rect.height;
        
        // 3. Soustraire le padding (marges intérieures) s'il y en a
        float availableWidth = containerWidth - gridLayout.padding.left - gridLayout.padding.right;
        float availableHeight = containerHeight - gridLayout.padding.top - gridLayout.padding.bottom;

        // 4. Soustraire l'espace total occupé par les espacements entre boutons 
        availableWidth -= spacing.x * (columns - 1);
        availableHeight -= spacing.y * (rows - 1);

        // 5. Calculer la taille d'une seule cellule
        float cellWidth = availableWidth / columns;
        float cellHeight = availableHeight / rows;

        //6. Appliquer la taille calculée
        gridLayout.cellSize = new Vector2(cellWidth, cellHeight);

        // 7. Forcer la contrainte pour s'assurer que c'est bien une grille fixe
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;
    }
}
