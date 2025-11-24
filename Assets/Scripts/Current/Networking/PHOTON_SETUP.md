# Photon Multiplayer Setup Guide

## Overview
This project uses Photon PUN2 (Photon Unity Networking 2) for multiplayer functionality. The lobby system allows players to create or join rooms and play matches together.

## Setup Steps

### 1. Install Photon PUN2
**Option A: Via Unity Package Manager (Recommended)**
1. In Unity, go to **Window > Package Manager**
2. Click the **+** button and select **Add package from git URL**
3. Enter: `https://github.com/ExitGames/PhotonUnityNetworking.git?path=/PhotonUnityNetworking/Assets`
4. Click **Add**

**Option B: Via Asset Store**
1. Open the Unity Asset Store
2. Search for "Photon PUN 2"
3. Download and import the package

### 2. Get Photon App ID
1. Go to https://dashboard.photonengine.com/
2. Sign up or log in
3. Create a new app (choose "Photon PUN" as the type)
4. Copy your **App ID**

### 3. Configure Photon Settings in Unity
1. In Unity, go to **Window > Photon Unity Networking > PUN Wizard**
2. Enter your App ID in the wizard
3. Click "Setup Project"
4. This will create the necessary Photon settings files

### 4. Scene Setup

#### MainMenu Scene
1. Add a `NetworkManager` component to a GameObject (or it will be created automatically)
2. Add a `LobbyManager` component to a GameObject (or it will be created automatically)
3. Create UI for the lobby:
   - Create a panel for the lobby UI
   - Add a `LobbyUI` component to the panel
   - Assign UI references:
     - Lobby Panel (the panel itself)
     - Create Lobby Button
     - Join Lobby Button
     - Join Random Button
     - Back Button
     - Room Name Input Field
     - Status Text
     - Connection Status Text
     - Room List Container (for displaying available rooms)
     - Room List Item Prefab (prefab for each room in the list)
4. Update the MainMenu script reference to point to the LobbyUI component

#### BattleScreenMultiplayer Scene
1. Add a `RoomSyncManager` component to a GameObject
2. This will handle player assignment and room synchronization

### 5. UI Setup Example

Create a lobby panel with the following structure:
```
LobbyPanel
├── Header (Text: "Multiplayer Lobby")
├── ConnectionStatus (Text: Shows connection status)
├── StatusText (Text: Shows current status messages)
├── RoomNameInput (InputField: For entering/creating room names)
├── ButtonsContainer
│   ├── CreateLobbyButton
│   ├── JoinLobbyButton
│   ├── JoinRandomButton
│   └── BackButton
└── RoomListContainer (ScrollView)
    └── RoomListItemPrefab (Template for room list items)
```

### 6. Testing
1. Build the game or run two instances in the Unity Editor
2. In the first instance, click "Create Lobby" or "Join Random"
3. In the second instance, join the same room
4. Once both players are in the room, the battle scene should load automatically

## Scripts Overview

### NetworkManager
- Handles Photon connection
- Manages connection state
- Provides connection events

### LobbyManager
- Creates and joins rooms
- Manages room list
- Handles room events

### LobbyUI
- UI controller for lobby interface
- Displays room list
- Handles user input for creating/joining rooms

### RoomSyncManager
- Manages player assignment in battle scene
- Tracks room readiness
- Assigns player numbers (P1/P2)

## Notes
- The system automatically connects to Photon when NetworkManager is created
- Rooms are limited to 2 players (configurable in LobbyManager)
- The battle scene loads automatically when 2 players join a room
- Make sure to set up your Photon App ID before testing

