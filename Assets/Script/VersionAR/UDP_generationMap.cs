using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class UDP_generationMap : MonoBehaviour
{
    [Header("Réseau UDP")]
    public string pythonIP = "172.20.10.5"; 
    public int pythonPort = 5005;
    public int unityPort = 5006;

    [Header("UI & Etapes")]
    public TextMeshProUGUI instructionText; 
    public GameObject finalCanvas; 

    [Header("Composants AR (À remplir dans l'inspecteur)")]
    public ARTrackedImageManager imageManager; // <--- Nouveau : on surveille l'image ici
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public GameObject tilePrefab; 

    [Header("Dimensions Map")]
    public float anchorWidthMeters = 0.165f;
    public float gridWidthMeters = 1.01f;
    public float gridHeightMeters = 0.65f;
    public float gridLift = 0.0015f;
    public float depthOffset = -0.002f;

    private UdpClient udpClient;
    private ARAnchor currentAnchor;
    private GameObject mapContainer;
    private bool isMapped = false;
    private string receivedData = "";
    private bool groundDetected = false;

    // --- GESTION DE L'ABONNEMENT ---
    void OnEnable() {
        if (imageManager != null) imageManager.trackedImagesChanged += OnImageChanged;
    }

    void OnDisable() {
        if (imageManager != null) imageManager.trackedImagesChanged -= OnImageChanged;
    }

    void Start() {
        if (finalCanvas != null) finalCanvas.SetActive(false);
        if (instructionText != null) instructionText.text = "Étape 1 : Scannez le sol...";
        
        try {
            udpClient = new UdpClient(unityPort);
            udpClient.BeginReceive(new AsyncCallback(OnReceiveUDP), null);
        } catch (Exception e) { Debug.LogError("Erreur UDP: " + e.Message); }
    }

    // Cette fonction remplace le MapTrackingHandler
    void OnImageChanged(ARTrackedImagesChangedEventArgs args) {
        foreach (var trackedImage in args.added) {
            HandleQRCodeDetection(trackedImage);
        }
        foreach (var trackedImage in args.updated) {
            HandleQRCodeDetection(trackedImage);
        }
    }

    void HandleQRCodeDetection(ARTrackedImage trackedImage) {
        if (isMapped || trackedImage.trackingState != TrackingState.Tracking) return;

        // Désactivation du sol (Nettoyage)
        DisableARPlanes();

        // Calcul position sol
        float finalY = trackedImage.transform.position.y;
        var hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(new Ray(trackedImage.transform.position + Vector3.up, Vector3.down), hits, TrackableType.PlaneWithinPolygon)) {
            finalY = hits[0].pose.position.y;
        }

        // Création ancre
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
    }

    void DisableARPlanes() {
        if (planeManager == null) return;
        planeManager.enabled = false;
        foreach (var plane in planeManager.trackables) plane.gameObject.SetActive(false);
    }

    // ... (SpawnMap, SendData, OnReceiveUDP, ApplyColor restent pareils)
    void SpawnMap(string data) {
        if (instructionText != null) instructionText.gameObject.SetActive(false);
        if (finalCanvas != null) finalCanvas.SetActive(true);

        string[] values = data.Split(',');
        float tX = gridWidthMeters / 15f; float tY = gridHeightMeters / 10f;
        float xStart = anchorWidthMeters / 2f; float yHalf = gridHeightMeters / 2f;

        for (int i = 0; i < values.Length; i++) {
            int type = int.Parse(values[i]);
            if (type == 0) continue;
            int r = i / 15; int c = i % 15;
            Vector3 localPos = new Vector3(xStart + (c * tX) + (tX / 2f), (r * tY) - yHalf + (tY / 2f) + depthOffset, gridLift);
            GameObject tile = Instantiate(tilePrefab, mapContainer.transform);
            tile.transform.localPosition = localPos;
            tile.transform.localScale = new Vector3(tX * 0.95f, tY * 0.95f, 0.005f);
            ApplyColor(tile, type);
        }
    }

    void SendData(string msg) {
        byte[] data = Encoding.UTF8.GetBytes(msg);
        udpClient.Send(data, data.Length, pythonIP, pythonPort);
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
            1 => new Color(0.45f, 0.85f, 0.45f), // Plaine
            2 => new Color(0.5f, 0.5f, 0.5f),    // Montagne
            3 => new Color(0.15f, 0.55f, 0.15f), // Forêt
            4 => new Color(0.25f, 0.45f, 0.95f), // Rivière
            _ => Color.white
        };
        tile.GetComponent<Renderer>().material.color = col;
    }

    private void OnDestroy() { if (udpClient != null) udpClient.Close(); }
}