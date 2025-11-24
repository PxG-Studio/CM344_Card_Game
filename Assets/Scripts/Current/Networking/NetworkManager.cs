using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

namespace CardGame.Networking
{
    /// <summary>
    /// Manages Photon network connection and basic networking functionality
    /// </summary>
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Connection Settings")]
        [SerializeField] private string gameVersion = "1.0";
        [SerializeField] private byte maxPlayersPerRoom = 2;

        public bool IsConnected => PhotonNetwork.IsConnected;
        public bool IsInRoom => PhotonNetwork.InRoom;
        public int PlayerCount => PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;
        public int MaxPlayers => maxPlayersPerRoom;

        // Events
        public System.Action OnConnectedToMasterEvent;
        public System.Action OnDisconnectedEvent;
        public System.Action OnJoinedRoomEvent;
        public System.Action OnLeftRoomEvent;
        public System.Action<Player> OnPlayerEnteredRoomEvent;
        public System.Action<Player> OnPlayerLeftRoomEvent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Connect to Photon on start
            ConnectToPhoton();
        }

        /// <summary>
        /// Connects to Photon servers
        /// </summary>
        public void ConnectToPhoton()
        {
            if (PhotonNetwork.IsConnected)
            {
                Debug.Log("[NetworkManager] Already connected to Photon");
                OnConnectedToMasterEvent?.Invoke();
                return;
            }

            Debug.Log("[NetworkManager] Connecting to Photon...");
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }

        /// <summary>
        /// Disconnects from Photon
        /// </summary>
        public void Disconnect()
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
        }

        #region Photon Callbacks

        public override void OnConnectedToMaster()
        {
            Debug.Log("[NetworkManager] Connected to Photon Master Server");
            OnConnectedToMasterEvent?.Invoke();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.Log($"[NetworkManager] Disconnected from Photon. Cause: {cause}");
            OnDisconnectedEvent?.Invoke();
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"[NetworkManager] Joined room: {PhotonNetwork.CurrentRoom.Name}");
            Debug.Log($"[NetworkManager] Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
            OnJoinedRoomEvent?.Invoke();
        }

        public override void OnLeftRoom()
        {
            Debug.Log("[NetworkManager] Left room");
            OnLeftRoomEvent?.Invoke();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"[NetworkManager] Player entered room: {newPlayer.NickName} (Player {newPlayer.ActorNumber})");
            OnPlayerEnteredRoomEvent?.Invoke(newPlayer);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"[NetworkManager] Player left room: {otherPlayer.NickName} (Player {otherPlayer.ActorNumber})");
            OnPlayerLeftRoomEvent?.Invoke(otherPlayer);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[NetworkManager] Failed to join room. Code: {returnCode}, Message: {message}");
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[NetworkManager] Failed to create room. Code: {returnCode}, Message: {message}");
        }

        #endregion
    }
}

