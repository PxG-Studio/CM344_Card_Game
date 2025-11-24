using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using CardGame.Networking;

namespace CardGame.Networking
{
    /// <summary>
    /// Manages room synchronization and player assignment in the battle scene
    /// </summary>
    public class RoomSyncManager : MonoBehaviourPunCallbacks
    {
        public static RoomSyncManager Instance { get; private set; }

        [Header("Player Assignment")]
        [SerializeField] private bool assignPlayerNumbersOnJoin = true;

        public int LocalPlayerNumber { get; private set; } = -1;
        public bool IsRoomReady { get; private set; } = false;

        // Events
        public System.Action<int> OnPlayerNumberAssigned;
        public System.Action OnRoomReady;
        public System.Action OnRoomNotReady;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (PhotonNetwork.InRoom)
            {
                AssignPlayerNumber();
                CheckRoomReady();
            }
            else
            {
                Debug.LogWarning("[RoomSyncManager] Not in a Photon room. This scene should only be loaded when in a room.");
            }
        }

        private void AssignPlayerNumber()
        {
            if (!assignPlayerNumbersOnJoin) return;

            // Assign player number based on ActorNumber (first player = 1, second player = 2)
            // Photon ActorNumbers start at 1
            LocalPlayerNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            
            Debug.Log($"[RoomSyncManager] Assigned player number: {LocalPlayerNumber}");
            OnPlayerNumberAssigned?.Invoke(LocalPlayerNumber);
        }

        private void CheckRoomReady()
        {
            if (PhotonNetwork.CurrentRoom == null)
            {
                IsRoomReady = false;
                OnRoomNotReady?.Invoke();
                return;
            }

            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;

            if (playerCount >= maxPlayers)
            {
                IsRoomReady = true;
                OnRoomReady?.Invoke();
                Debug.Log($"[RoomSyncManager] Room is ready! Players: {playerCount}/{maxPlayers}");
            }
            else
            {
                IsRoomReady = false;
                OnRoomNotReady?.Invoke();
                Debug.Log($"[RoomSyncManager] Waiting for players... {playerCount}/{maxPlayers}");
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"[RoomSyncManager] Player entered: {newPlayer.NickName} (Actor {newPlayer.ActorNumber})");
            CheckRoomReady();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"[RoomSyncManager] Player left: {otherPlayer.NickName} (Actor {otherPlayer.ActorNumber})");
            CheckRoomReady();
        }

        /// <summary>
        /// Gets whether the local player is Player 1 (first to join)
        /// </summary>
        public bool IsLocalPlayerP1()
        {
            if (PhotonNetwork.CurrentRoom == null) return false;
            
            // Player 1 is the one with the lowest ActorNumber
            foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                if (player.ActorNumber < LocalPlayerNumber)
                {
                    return false; // Someone joined before us
                }
            }
            return true; // We have the lowest ActorNumber
        }

        /// <summary>
        /// Gets whether the local player is Player 2 (second to join)
        /// </summary>
        public bool IsLocalPlayerP2()
        {
            return !IsLocalPlayerP1() && LocalPlayerNumber > 0;
        }
    }
}

