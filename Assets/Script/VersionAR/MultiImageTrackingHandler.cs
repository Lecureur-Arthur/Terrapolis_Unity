using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MultiImageTrackingHandler : MonoBehaviour
{
    // Petite classe pour lier un nom d'image à un Prefab dans l'inspecteur
    [System.Serializable]
    public struct ImageEntry
    {
        [Tooltip("Le nom EXACT de l'image dans votre Reference Image Library")]
        public string imageName;
        public GameObject prefabToSpawn;
    }

    [Header("Configuration")]
    [SerializeField] private ARTrackedImageManager _aRTrackedImageManager;
    [Tooltip("Liste des correspondances Image <-> Prefab")]
    public List<ImageEntry> imageLibrarySettings = new List<ImageEntry>();

    // Dictionnaire pour se souvenir quel bâtiment a été créé pour quelle image.
    // Clé = nom de l'image, Valeur = le bâtiment instancié.
    private Dictionary<string, GameObject> spawnedObjectsDict = new Dictionary<string, GameObject>();

    void Awake()
    {
        // Sécurité : si on oublie de lier le manager, on essaie de le trouver
        if (_aRTrackedImageManager == null)
        {
            _aRTrackedImageManager = FindObjectOfType<ARTrackedImageManager>();
        }
    }

    void OnEnable()
    {
        if (_aRTrackedImageManager != null)
        {
            _aRTrackedImageManager.trackedImagesChanged += OnImageChanged;
        }
    }

    void OnDisable()
    {
        if (_aRTrackedImageManager != null)
        {
            _aRTrackedImageManager.trackedImagesChanged -= OnImageChanged;
        }
    }

    void OnImageChanged(ARTrackedImagesChangedEventArgs args)
    {
        // 1. Gérer les NOUVELLES images détectées
        foreach (var trackedImage in args.added)
        {
            SpawnObjectForImage(trackedImage);
        }

        // 2. Gérer les mises à jour (activer/désactiver si le tracking est perdu)
        foreach (var trackedImage in args.updated)
        {
            string imageName = trackedImage.referenceImage.name;
            
            // Si on a déjà fait spawn un objet pour cette image, on met à jour son état
            if (spawnedObjectsDict.ContainsKey(imageName))
            {
                GameObject spawnedObj = spawnedObjectsDict[imageName];
                // L'objet est actif seulement si l'état est "Tracking"
                bool shouldBeActive = trackedImage.trackingState == TrackingState.Tracking;
                
                if (spawnedObj.activeSelf != shouldBeActive)
                {
                     spawnedObj.SetActive(shouldBeActive);
                }
            }
        }
    }

    private void SpawnObjectForImage(ARTrackedImage trackedImage)
    {
        string detectedImageName = trackedImage.referenceImage.name;

        // Sécurité : si on a déjà un objet pour cette image, on ne fait rien.
        if (spawnedObjectsDict.ContainsKey(detectedImageName))
        {
            return;
        }

        // On cherche dans la liste configurée dans l'inspecteur si on a une correspondance
        GameObject prefabToUse = null;
        foreach (var entry in imageLibrarySettings)
        {
            // Comparaison des noms (ATTENTION : Sensible aux majuscules/minuscules)
            if (entry.imageName == detectedImageName)
            {
                prefabToUse = entry.prefabToSpawn;
                break; // On a trouvé, on sort de la boucle
            }
        }

        // Si on a trouvé un prefab correspondant
        if (prefabToUse != null)
        {
            // --- C'est ici que j'ai repris la méthode exacte de votre MapTrackingHandler ---
            
            // 1. Instancier à la position et rotation de l'image
            GameObject newObject = Instantiate(prefabToUse, trackedImage.transform.position, trackedImage.transform.rotation);
            
            // 2. Mettre l'objet en enfant de l'image pour qu'il la suive
            newObject.transform.SetParent(trackedImage.transform);
            // -----------------------------------------------------------------------------

            // 3. Ajouter au dictionnaire pour s'en souvenir
            spawnedObjectsDict.Add(detectedImageName, newObject);
            
            Debug.Log($"[Succès] Image '{detectedImageName}' détectée -> Prefab '{newObject.name}' instancié.");
        }
        else
        {
            Debug.LogWarning($"[Attention] L'image '{detectedImageName}' a été détectée, mais aucun prefab n'est configuré pour ce nom dans la liste du script.");
        }
    }
}