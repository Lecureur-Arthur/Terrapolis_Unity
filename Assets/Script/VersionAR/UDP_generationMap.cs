using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using UnityEngine.EventSystems;

public class UDP_generationMap : MonoBehaviour
{
    [Header("Réseau UDP")]
    public string pythonIP = "172.20.10.5"; 
    public int pythonPort = 5005;
    public int unityPort = 5006;

    [Header("UI & Etapes")]
    public TextMeshProUGUI instructionText; 
    public GameObject finalCanvas; 
    public GameObject itemMenuPanel;
    public GameObject validateButton;
    public TextMeshProUGUI feedbackText;

    [Header("Calibration Raycast")]
    public float selectionOffsetY = 0.0f; 
    public float selectionOffsetX = 0.0f;
    public Vector2Int indexCorrection = Vector2Int.zero;

    [Header("Bâtiments & Map")]
    public GameObject tilePrefab;
    public List<GameObject> buildingPrefabs; 
    public float anchorWidthMeters = 0.165f;
    public float gridWidthMeters = 1.01f;
    public float gridHeightMeters = 0.65f;
    public float gridLift = 0.0015f;
    public float depthOffset = -0.002f;

    [Header("Composants AR")]
    public ARTrackedImageManager imageManager;
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    // Variables internes
    private UdpClient udpClient;
    private ARAnchor currentAnchor;
    private GameObject mapContainer;
    private bool isMapped = false;
    private string receivedData = "";
    private bool groundDetected = false;

    // Variables Gameplay
    private GameObject currentGhost;
    private int currentBuildingIndex = -1;
    private Vector2Int currentGridPos;
    private bool isGhostActive = false;

    void OnEnable() { if (imageManager != null) imageManager.trackedImagesChanged += OnImageChanged; }
    void OnDisable() { if (imageManager != null) imageManager.trackedImagesChanged -= OnImageChanged; }

    void Start() {
        if (finalCanvas != null) finalCanvas.SetActive(false);
        if (itemMenuPanel != null) itemMenuPanel.SetActive(false);
        if (validateButton != null) validateButton.SetActive(false);
        if (instructionText != null) instructionText.text = "Étape 1 : Scannez le sol...";
        
        try {
            udpClient = new UdpClient(unityPort);
            udpClient.BeginReceive(new AsyncCallback(OnReceiveUDP), null);
        } catch (Exception e) { Debug.LogError("Erreur UDP: " + e.Message); }
    }

    void OnImageChanged(ARTrackedImagesChangedEventArgs args) {
        foreach (var img in args.added) HandleQRCodeDetection(img);
        foreach (var img in args.updated) HandleQRCodeDetection(img);
    }

    void HandleQRCodeDetection(ARTrackedImage trackedImage) {
        if (isMapped || trackedImage.trackingState != TrackingState.Tracking) return;

        DisableARPlanes();

        float finalY = trackedImage.transform.position.y;
        var hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(new Ray(trackedImage.transform.position + Vector3.up, Vector3.down), hits, TrackableType.PlaneWithinPolygon)) {
            finalY = hits[0].pose.position.y;
        }

        GameObject anchorObj = new GameObject("Anchor_Map");
        anchorObj.transform.position = new Vector3(trackedImage.transform.position.x, finalY, trackedImage.transform.position.z);
        Vector3 forward = Vector3.ProjectOnPlane(trackedImage.transform.forward, Vector3.up).normalized;
        anchorObj.transform.rotation = Quaternion.LookRotation(Vector3.up, -forward);
        currentAnchor = anchorObj.AddComponent<ARAnchor>();

        mapContainer = new GameObject("MapContainer");
        mapContainer.transform.SetParent(currentAnchor.transform, false);

        SendData("GET_MAP");
        isMapped = true;
    }

    void Update() {
        if (!groundDetected && planeManager != null && planeManager.trackables.count > 0) {
            groundDetected = true;
            instructionText.text = "Sol détecté ! Étape 2 : Scannez le QR Code.";
        }

        if (!string.IsNullOrEmpty(receivedData)) {
            SpawnMap(receivedData);
            receivedData = "";
        }

        if (isMapped && isGhostActive && currentGhost != null)
        {
            UpdateGhostPosition();
        }
    }

    void UpdateGhostPosition()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Plane mapPlane = new Plane(mapContainer.transform.forward, mapContainer.transform.position + mapContainer.transform.forward * gridLift);

        if (mapPlane.Raycast(ray, out float enter))
        {
            Vector3 worldHit = ray.GetPoint(enter);
            Vector3 localHit = mapContainer.transform.InverseTransformPoint(worldHit);

            float tX = gridWidthMeters / 15f;
            float tY = gridHeightMeters / 10f;
            
            float xZero = (anchorWidthMeters / 2f);
            float yZero = -(gridHeightMeters / 2f) + depthOffset;

            float relativeX = localHit.x - xZero + selectionOffsetX;
            float relativeY = localHit.y - yZero + selectionOffsetY;

            int c = Mathf.RoundToInt((relativeX / tX) - 0.5f);
            int r = Mathf.RoundToInt((relativeY / tY) - 0.5f);

            c += indexCorrection.x;
            r += indexCorrection.y;

            if (c >= 0 && c < 15 && r >= 0 && r < 10)
            {
                currentGridPos = new Vector2Int(c, r);

                float xCenter = xZero + (c * tX) + (tX / 2f);
                float yCenter = yZero + (r * tY) + (tY / 2f);

                Vector3 snappedLocalPos = new Vector3(xCenter, yCenter, gridLift);
                currentGhost.transform.localPosition = snappedLocalPos;
                
                if (!currentGhost.activeSelf) currentGhost.SetActive(true);
            }
            else
            {
                if (currentGhost.activeSelf) currentGhost.SetActive(false);
            }
        }
    }

    public void SelectBuilding(int index)
    {
        if (currentGhost != null) Destroy(currentGhost);

        if (index >= 0 && index < buildingPrefabs.Count)
        {
            currentBuildingIndex = index;
            currentGhost = Instantiate(buildingPrefabs[index], mapContainer.transform);
            
            Collider[] colliders = currentGhost.GetComponentsInChildren<Collider>();
            foreach(var col in colliders) Destroy(col);

            isGhostActive = true;
            
            CloseItemMenu();
            if (validateButton != null) validateButton.SetActive(true);
            if (feedbackText != null) feedbackText.text = "Positionnez le bâtiment...";
        }
    }

    public void OnValidatePlacement()
    {
        if (!isGhostActive || currentGhost == null || currentBuildingIndex == -1)
        {
            Debug.LogWarning("Placement invalide.");
            return;
        }

        // Placement visuel définitif
        GameObject finalBuilding = Instantiate(buildingPrefabs[currentBuildingIndex], mapContainer.transform);
        finalBuilding.transform.position = currentGhost.transform.position;
        finalBuilding.transform.rotation = currentGhost.transform.rotation;
        finalBuilding.transform.localScale = currentGhost.transform.localScale;

        // --- CORRECTION DU CALCUL D'INDEX ---
        // Problème précédent : Mobile(4,0) envoyait un index qui donnait PC(14,4).
        // Solution : Inverser la logique pour compenser l'interprétation du PC.
        // Formule déduite : (14 - Col) * 10 + Row
        // Exemple : Pour (4,0) -> (14 - 4) * 10 + 0 = 100.
        // Si le PC reçoit 100, il décode (4, 0).
        int indexToSend = (14 - currentGridPos.x) * 10 + currentGridPos.y;

        int serverBuildingID = currentBuildingIndex + 5; 
        
        string command = $"BUILD,{indexToSend},{serverBuildingID}";
        SendData(command);

        if(feedbackText != null)
            feedbackText.text = $"Bâtiment {serverBuildingID} placé en ({currentGridPos.x}, {currentGridPos.y})";

        Destroy(currentGhost);
        isGhostActive = false;
        currentGhost = null;
        
        if (validateButton != null) validateButton.SetActive(false);
    }

    public void OpenItemMenu() { if (itemMenuPanel != null) itemMenuPanel.SetActive(true); }
    public void CloseItemMenu() { if (itemMenuPanel != null) itemMenuPanel.SetActive(false); }

    void DisableARPlanes() {
        if (planeManager == null) return;
        planeManager.enabled = false;
        foreach (var plane in planeManager.trackables) plane.gameObject.SetActive(false);
    }

    void SpawnMap(string data) {
        if (instructionText != null) instructionText.gameObject.SetActive(false);
        if (finalCanvas != null) finalCanvas.SetActive(true);

        string[] values = data.Split(',');
        float tX = gridWidthMeters / 15f; float tY = gridHeightMeters / 10f;
        float xStart = anchorWidthMeters / 2f; float yHalf = gridHeightMeters / 2f;

        for (int i = 0; i < values.Length; i++) {
            if (!int.TryParse(values[i], out int type) || type == 0) continue;
            
            int r = i / 15; int c = i % 15;
            
            // Calcul de position standard pour l'affichage initial
            Vector3 localPos = new Vector3(xStart + (c * tX) + (tX / 2f), (r * tY) - yHalf + (tY / 2f) + depthOffset, gridLift);
            GameObject tile = Instantiate(tilePrefab, mapContainer.transform);
            tile.transform.localPosition = localPos;
            tile.transform.localScale = new Vector3(tX * 0.95f, tY * 0.95f, 0.005f);
            ApplyColor(tile, type);
        }
    }

    void SendData(string msg) {
        try {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            udpClient.Send(data, data.Length, pythonIP, pythonPort);
        } catch (Exception e) { Debug.LogError("Send UDP Error: " + e.Message); }
    }

    private void OnReceiveUDP(IAsyncResult ar) {
        try {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, unityPort);
            byte[] bytes = udpClient.EndReceive(ar, ref ip);
            receivedData = Encoding.UTF8.GetString(bytes);
            udpClient.BeginReceive(new AsyncCallback(OnReceiveUDP), null);
        } catch {}
    }

    void ApplyColor(GameObject tile, int type) {
        Color col = type switch {
            1 => new Color(0.45f, 0.85f, 0.45f), 2 => new Color(0.5f, 0.5f, 0.5f),
            3 => new Color(0.15f, 0.55f, 0.15f), 4 => new Color(0.25f, 0.45f, 0.95f), _ => Color.white
        };
        tile.GetComponent<Renderer>().material.color = col;
    }
    
    private void OnDestroy() { if (udpClient != null) udpClient.Close(); }
}