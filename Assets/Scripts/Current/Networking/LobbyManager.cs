using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

namespace CardGame.Networking
{
    /// <summary>
    /// Manages lobby/room creation and joining for multiplayer matches
    /// </summary>
    public class LobbyManager : MonoBehaviourPunCallbacks
    {
        public static LobbyManager Instance { get; private set; }

        [Header("Room Settings")]
        [SerializeField] private string defaultRoomName = "CardGameRoom";
        [SerializeField] private byte maxPlayersPerRoom = 2;

        public bool IsInLobby => PhotonNetwork.InLobby;
        public bool IsInRoom => PhotonNetwork.InRoom;
        public Room CurrentRoom => PhotonNetwork.CurrentRoom;
        public List<RoomInfo> AvailableRooms { get; private set; } = new List<RoomInfo>();

        // Events
        public System.Action<List<RoomInfo>> OnRoomListUpdated;
        public System.Action<string> OnRoomCreated;
        public System.Action<string> OnRoomJoined;
        public System.Action OnRoomCreationFailed;
        public System.Action<string> OnRoomJoinFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Creates a new room with the specified name
        /// </summary>
        public void CreateRoom(string roomName = null)
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogError("[LobbyManager] Cannot create room - not connected to Photon");
                return;
            }

            if (PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[LobbyManager] Already in a room. Leaving current room first...");
                PhotonNetwork.LeaveRoom();
                return;
            }

            string finalRoomName = string.IsNullOrEmpty(roomName) ? $"{defaultRoomName}_{Random.Range(1000, 9999)}" : roomName;

            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = maxPlayersPerRoom,
                IsVisible = true,
                IsOpen = true
            };

            Debug.Log($"[LobbyManager] Creating room: {finalRoomName}");
            PhotonNetwork.CreateRoom(finalRoomName, roomOptions);
        }

        /// <summary>
        /// Joins a room by name
        /// </summary>
        public void JoinRoom(string roomName)
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogError("[LobbyManager] Cannot join room - not connected to Photon");
                return;
            }

            if (PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[LobbyManager] Already in a room. Leaving current room first...");
                PhotonNetwork.LeaveRoom();
                return;
            }

            Debug.Log($"[LobbyManager] Joining room: {roomName}");
            PhotonNetwork.JoinRoom(roomName);
        }

        /// <summary>
        /// Joins a random available room
        /// </summary>
        public void JoinRandomRoom()
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogError("[LobbyManager] Cannot join room - not connected to Photon");
                return;
            }

            if (PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[LobbyManager] Already in a room");
                return;
            }

            Debug.Log("[LobbyManager] Joining random room...");
            PhotonNetwork.JoinRandomRoom();
        }

        /// <summary>
        /// Leaves the current room
        /// </summary>
        public void LeaveRoom()
        {
            if (PhotonNetwork.InRoom)
            {
                Debug.Log("[LobbyManager] Leaving room...");
                PhotonNetwork.LeaveRoom();
            }
        }

        /// <summary>
        /// Joins the Photon lobby to see available rooms
        /// </summary>
        public void JoinLobby()
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogError("[LobbyManager] Cannot join lobby - not connected to Photon");
                return;
            }

            if (PhotonNetwork.InLobby)
            {
                Debug.Log("[LobbyManager] Already in lobby");
                return;
            }

            Debug.Log("[LobbyManager] Joining lobby...");
            PhotonNetwork.JoinLobby();
        }

        #region Photon Callbacks

        public override void OnJoinedLobby()
        {
            Debug.Log("[LobbyManager] Joined lobby");
            AvailableRooms.Clear();
        }

        public override void OnLeftLobby()
        {
            Debug.Log("[LobbyManager] Left lobby");
            AvailableRooms.Clear();
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            AvailableRooms.Clear();
            foreach (RoomInfo roomInfo in roomList)
            {
                if (roomInfo.RemovedFromList)
                {
                    continue;
                }
                AvailableRooms.Add(roomInfo);
            }

            Debug.Log($"[LobbyManager] Room list updated. {AvailableRooms.Count} rooms available");
            OnRoomListUpdated?.Invoke(AvailableRooms);
        }

        public override void OnCreatedRoom()
        {
            Debug.Log($"[LobbyManager] Room created: {PhotonNetwork.CurrentRoom.Name}");
            OnRoomCreated?.Invoke(PhotonNetwork.CurrentRoom.Name);
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[LobbyManager] Failed to create room. Code: {returnCode}, Message: {message}");
            OnRoomCreationFailed?.Invoke();
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"[LobbyManager] Joined room: {PhotonNetwork.CurrentRoom.Name}");
            Debug.Log($"[LobbyManager] Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
            OnRoomJoined?.Invoke(PhotonNetwork.CurrentRoom.Name);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[LobbyManager] Failed to join room. Code: {returnCode}, Message: {message}");
            OnRoomJoinFailed?.Invoke(message);
        }

        public override void OnLeftRoom()
        {
            Debug.Log("[LobbyManager] Left room");
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log($"[LobbyManager] No random room available. Creating new room...");
            CreateRoom();
        }

        #endregion
    }
}

