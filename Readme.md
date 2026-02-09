
---

# Projet Unity : Architecture AR & Réseau (Client UDP)

Ce projet est le **Client Mobile** développé sous **Unity 2022.3.37f1**. Il permet le placement de bâtiments en Réalité Augmentée (AR) et la visualisation tactique en 2D.

**Important :** Ce projet ne fonctionne pas seul. Il agit en tant que client et nécessite l'exécution d'un **Serveur Python** (pour la génération de map et la communication UDP) disponible sur un autre dépôt.

## Dépôt Serveur (Python)

Le backend (génération de la carte, logique serveur UDP) :
**[\[Repo Moteur de jeu ici\]](https://github.com/Lecureur-Arthur/Terrapolis_Python)**

**Assurez-vous de cloner et de lancer le script Python sur votre PC avant de tester l'application Unity.**

---

## Prérequis

* **Unity Version :** `2022.3.37f1`
* **Modules Unity :** Android Build Support + AR Foundation.
* **Backend :** Le script Python doit être lancé sur un PC.
* **Réseau :** Le PC (Serveur Python) et le mobile (Client Unity) doivent être connectés au **même réseau Wi-Fi**.

---

## Description des Scènes

L'application Unity est composée de 4 scènes :

### 1. MainMenu (Menu Principal)

Interface d'accueil.

* **Bouton "Lancer" :** Démarre l'expérience.
* **Bouton "Quitter" :** Ferme l'application.
* **Crédits :** Auteurs du projet.

### 2. GamePlayAR (Expérience AR)

Scène principale de Réalité Augmentée.

* **Étape 1 : Initialisation (AR Plane Manager)**
    * Scan de l'environnement pour détecter une zone de jeu.
    * **Impératif :** Vous devez scanner une **surface HORIZONTALE** (sol ou table).
    * *Note : La détection ne fonctionne pas sur les surfaces verticales (murs, écrans télé, etc.).*


* **Étape 2 : Synchronisation Carte (UDP)**
    * Scan d'un **QR Code** pour envoyer une requête au **Serveur Python**.
    * **Script :** `UDP_generationMap`.
    * Réception des données de la map et ancrage via **AR Anchors** (permettant une navigation stable sans rescan).


* **Étape 3 : Placement des Bâtiments**
    * Sélection via un menu déroulant (6 bâtiments disponibles).
    * Placement tactile sur la map et validation.


* **Navigation :** Transition possible vers les scènes *GamePlay2D* ou *VisualizationPrefab3D*.

### 3. GamePlay2D (Interface Tactique)

Visualisation et gestion de la carte en 2D.

* **Script `MapReceiver` :** Reçoit les données de la grille envoyées par le Python via UDP.
* **Script `GridGenerator` :** Construit visuellement la grille (boutons/tuiles).
* **Script `responsiveGrid` :** Adapte l'affichage à la taille de l'écran mobile.

### 4. VisualizationPrefab3D (Image Tracking)

Scène de visualisation d'objets.

* **Script :** `MultiImageTrackingHandler`.
* **Fonctionnalité :** Tracking simultané de plusieurs images pour afficher des modèles 3D.

---

## Configuration Réseau (À faire avant le Build)

Pour que le mobile puisse discuter avec le script Python, vous devez configurer les IPs.

### 1. Côté Python (PC)

1. Lancez votre terminal et faites `ipconfig` (Win).
2. Notez votre **Adresse IPv4** (C'est l'adresse du serveur).
3. Lancez le script Python.

### 2. Côté Unity (Scène "GamePlayAR")

L'application doit savoir où envoyer la requête de map.

1. Ouvrez la scène `GamePlayAR`.
2. Sélectionnez le GameObject **AR Session**.
3. Dans l'inspecteur, **entrez l'IP du PC** (notée à l'étape 1) dans le champ dédié.
4. Vérifiez que le port correspond à celui défini dans le script Python.

### 3. Côté Unity (Scène "GamePlay2D")

L'application doit savoir sur quel port écouter la réponse.

1. Ouvrez la scène `GamePlay2D`.
2. Chemin : `Canvas` -> `MainPanel` -> **GridPanel**.
3. Sur le GameObject **GridPanel**, vérifiez le **Port Réseau** (Défaut : `5005`).

---

## Installation & Lancement

1. **Cloner les dépôts :** Récupérez ce projet (Unity) ainsi que le dépôt du Serveur Python.
2. **Ouvrir le projet :** Lancez **Unity 2022.3.37f1**.
3. **Configuration de la Plateforme :**
    * Allez dans `File > Build Settings`.
    * Sélectionnez **Android** dans la liste des plateformes.
    * Cliquez sur le bouton **Switch Platform** (cela peut prendre quelques minutes).


4. **Préparation du Mobile :**
    * Activez le **Mode Développeur** sur votre téléphone/tablette.
    * Activez le **Débogage USB** dans les options de développement.
    * Connectez votre appareil au PC via un câble USB (acceptez la confirmation sur le téléphone si elle apparaît).


5. **Configuration Réseau :** Appliquez les IPs (voir section "Configuration Réseau" ci-dessus).
6. **Lancement du Serveur :** Démarrez le script Python sur votre PC.
7. **Build :** Dans `Build Settings`, assurez-vous que toutes les scènes sont cochées (MainMenu en premier), sélectionnez votre appareil dans "Run Device" puis cliquez sur **Build And Run**.

---

## Auteurs

* LECUREUR Arthur
* TOURNAY Clara
* PLATET Thibaut
