using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardGame.Networking;
using Photon.Realtime;
using System.Collections.Generic;

namespace CardGame.UI
{
    /// <summary>
    /// UI controller for lobby creation and joining in MainMenu scene
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private Button createLobbyButton;
        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private Button joinRandomButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_InputField roomNameInputField;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text connectionStatusText;
        [SerializeField] private Transform roomListContainer;
        [SerializeField] private GameObject roomListItemPrefab;

        [Header("Settings")]
        [SerializeField] private string battleSceneName = "BattleScreenMultiplayer";

        private List<GameObject> roomListItems = new List<GameObject>();
        private bool hasButtonListeners = false;

        private void Awake()
        {
            // Hide lobby panel initially
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(false);
            }
        }

        private void Start()
        {
            SetupButtons();
            UpdateConnectionStatus();
        }

        private void OnEnable()
        {
            // Subscribe to network events
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnectedToMasterEvent += OnConnectedToMaster;
                NetworkManager.Instance.OnDisconnectedEvent += OnDisconnected;
                NetworkManager.Instance.OnJoinedRoomEvent += OnJoinedRoom;
            }

            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.OnRoomListUpdated += OnRoomListUpdated;
                LobbyManager.Instance.OnRoomCreated += OnRoomCreated;
                LobbyManager.Instance.OnRoomJoined += OnRoomJoined;
                LobbyManager.Instance.OnRoomCreationFailed += OnRoomCreationFailed;
                LobbyManager.Instance.OnRoomJoinFailed += OnRoomJoinFailed;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from network events
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnectedToMasterEvent -= OnConnectedToMaster;
                NetworkManager.Instance.OnDisconnectedEvent -= OnDisconnected;
                NetworkManager.Instance.OnJoinedRoomEvent -= OnJoinedRoom;
            }

            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.OnRoomListUpdated -= OnRoomListUpdated;
                LobbyManager.Instance.OnRoomCreated -= OnRoomCreated;
                LobbyManager.Instance.OnRoomJoined -= OnRoomJoined;
                LobbyManager.Instance.OnRoomCreationFailed -= OnRoomCreationFailed;
                LobbyManager.Instance.OnRoomJoinFailed -= OnRoomJoinFailed;
            }
        }

        private void SetupButtons()
        {
            if (hasButtonListeners)
            {
                if (createLobbyButton != null) createLobbyButton.onClick.RemoveListener(OnCreateLobbyClicked);
                if (joinLobbyButton != null) joinLobbyButton.onClick.RemoveListener(OnJoinLobbyClicked);
                if (joinRandomButton != null) joinRandomButton.onClick.RemoveListener(OnJoinRandomClicked);
                if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
            }

            if (createLobbyButton != null)
            {
                createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
            }

            if (joinLobbyButton != null)
            {
                joinLobbyButton.onClick.AddListener(OnJoinLobbyClicked);
            }

            if (joinRandomButton != null)
            {
                joinRandomButton.onClick.AddListener(OnJoinRandomClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            hasButtonListeners = true;
        }

        /// <summary>
        /// Allows runtime configuration of UI references when the panel is created programmatically.
        /// </summary>
        public void ConfigureRuntimeReferences(
            GameObject panel,
            Button createButton,
            Button joinButton,
            Button joinRandomBtn,
            Button backBtn,
            TMP_InputField roomInput,
            TMP_Text statusLabel,
            TMP_Text connectionLabel,
            Transform roomContainer,
            GameObject roomItemTemplate)
        {
            lobbyPanel = panel;
            createLobbyButton = createButton;
            joinLobbyButton = joinButton;
            joinRandomButton = joinRandomBtn;
            backButton = backBtn;
            roomNameInputField = roomInput;
            statusText = statusLabel;
            connectionStatusText = connectionLabel;
            roomListContainer = roomContainer;
            roomListItemPrefab = roomItemTemplate;

            SetupButtons();
            UpdateConnectionStatus();
        }

        /// <summary>
        /// Shows the lobby UI panel
        /// </summary>
        public void ShowLobby()
        {
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(true);
            }

            UpdateConnectionStatus();
            UpdateStatus("Ready to create or join a lobby");

            // Join lobby to see available rooms
            if (LobbyManager.Instance != null && NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
            {
                LobbyManager.Instance.JoinLobby();
            }
        }

        /// <summary>
        /// Hides the lobby UI panel
        /// </summary>
        public void HideLobby()
        {
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(false);
            }
        }

        private void OnCreateLobbyClicked()
        {
            string roomName = roomNameInputField != null && !string.IsNullOrEmpty(roomNameInputField.text) 
                ? roomNameInputField.text 
                : null;

            if (LobbyManager.Instance != null)
            {
                UpdateStatus("Creating lobby...");
                LobbyManager.Instance.CreateRoom(roomName);
            }
            else
            {
                UpdateStatus("Error: LobbyManager not found");
            }
        }

        private void OnJoinLobbyClicked()
        {
            string roomName = roomNameInputField != null ? roomNameInputField.text : null;

            if (string.IsNullOrEmpty(roomName))
            {
                UpdateStatus("Please enter a room name");
                return;
            }

            if (LobbyManager.Instance != null)
            {
                UpdateStatus($"Joining lobby: {roomName}...");
                LobbyManager.Instance.JoinRoom(roomName);
            }
            else
            {
                UpdateStatus("Error: LobbyManager not found");
            }
        }

        private void OnJoinRandomClicked()
        {
            if (LobbyManager.Instance != null)
            {
                UpdateStatus("Joining random lobby...");
                LobbyManager.Instance.JoinRandomRoom();
            }
            else
            {
                UpdateStatus("Error: LobbyManager not found");
            }
        }

        private void OnBackClicked()
        {
            HideLobby();
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[LobbyUI] {message}");
        }

        private void UpdateConnectionStatus()
        {
            if (connectionStatusText != null)
            {
                if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
                {
                    connectionStatusText.text = "Connected";
                    connectionStatusText.color = Color.green;
                }
                else
                {
                    connectionStatusText.text = "Disconnected";
                    connectionStatusText.color = Color.red;
                }
            }
        }

        private void OnRoomListUpdated(List<RoomInfo> rooms)
        {
            // Clear existing room list items
            foreach (GameObject item in roomListItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            roomListItems.Clear();

            // Create new room list items
            if (roomListContainer != null && roomListItemPrefab != null)
            {
                foreach (RoomInfo room in rooms)
                {
                    GameObject roomItem = Instantiate(roomListItemPrefab, roomListContainer);
                    roomListItems.Add(roomItem);

                    // Set up room item (you may need to customize this based on your prefab structure)
                    Button roomButton = roomItem.GetComponent<Button>();
                    if (roomButton != null)
                    {
                        string roomName = room.Name;
                        roomButton.onClick.AddListener(() => OnRoomItemClicked(roomName));
                    }

                    // Update room item text if it has a TextMeshProUGUI component
                    TMP_Text roomText = roomItem.GetComponentInChildren<TMP_Text>();
                    if (roomText != null)
                    {
                        roomText.text = $"{room.Name} ({room.PlayerCount}/{room.MaxPlayers})";
                    }
                }
            }
        }

        private void OnRoomItemClicked(string roomName)
        {
            if (LobbyManager.Instance != null)
            {
                UpdateStatus($"Joining room: {roomName}...");
                LobbyManager.Instance.JoinRoom(roomName);
            }
        }

        #region Network Event Handlers

        private void OnConnectedToMaster()
        {
            UpdateConnectionStatus();
            UpdateStatus("Connected to Photon");
        }

        private void OnDisconnected()
        {
            UpdateConnectionStatus();
            UpdateStatus("Disconnected from Photon");
        }

        private void OnRoomCreated(string roomName)
        {
            UpdateStatus($"Room created: {roomName}. Waiting for players...");
        }

        private void OnRoomJoined(string roomName)
        {
            UpdateStatus($"Joined room: {roomName}");
            
            // Wait a moment for room to sync, then load battle scene
            StartCoroutine(LoadBattleSceneAfterJoin());
        }

        private void OnRoomCreationFailed()
        {
            UpdateStatus("Failed to create room. Please try again.");
        }

        private void OnRoomJoinFailed(string message)
        {
            UpdateStatus($"Failed to join room: {message}");
        }

        private void OnJoinedRoom()
        {
            // This is called when we successfully join a room
            // The scene will be loaded by the coroutine
        }

        private System.Collections.IEnumerator LoadBattleSceneAfterJoin()
        {
            yield return new WaitForSeconds(0.5f); // Wait for room to sync

            // Check if room is full (2 players)
            if (LobbyManager.Instance != null && LobbyManager.Instance.CurrentRoom != null)
            {
                if (LobbyManager.Instance.CurrentRoom.PlayerCount >= LobbyManager.Instance.CurrentRoom.MaxPlayers)
                {
                    UpdateStatus("Room is full! Starting game...");
                    yield return new WaitForSeconds(1f);
                    UnityEngine.SceneManagement.SceneManager.LoadScene(battleSceneName);
                }
                else
                {
                    UpdateStatus($"Waiting for players... ({LobbyManager.Instance.CurrentRoom.PlayerCount}/{LobbyManager.Instance.CurrentRoom.MaxPlayers})");
                }
            }
        }

        #endregion
    }
}

