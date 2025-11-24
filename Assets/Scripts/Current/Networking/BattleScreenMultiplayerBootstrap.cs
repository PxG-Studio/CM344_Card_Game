using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardGame.Networking
{
    /// <summary>
    /// Ensures the Photon networking stack is available whenever the BattleScreenMultiplayer scene loads.
    /// This allows Play Online flows (or direct scene testing) to always have the required managers alive.
    /// </summary>
    internal static class BattleScreenMultiplayerBootstrap
    {
        private const string TargetSceneName = "BattleScreenMultiplayer";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.name.Equals(TargetSceneName, StringComparison.Ordinal))
            {
                return;
            }

            EnsureNetworkStack();
        }

        private static void EnsureNetworkStack()
        {
            NetworkManager networkManager = EnsureSingleton<NetworkManager>("NetworkManager");
            LobbyManager lobbyManager = EnsureSingleton<LobbyManager>("LobbyManager");

            EnsureRoomSyncManager();

            if (networkManager != null && !networkManager.IsConnected)
            {
                networkManager.ConnectToPhoton();
            }
        }

        private static T EnsureSingleton<T>(string name) where T : MonoBehaviour
        {
            T existing = UnityEngine.Object.FindObjectOfType<T>();
            if (existing != null)
            {
                return existing;
            }

            GameObject managerObject = new GameObject(name);
            UnityEngine.Object.DontDestroyOnLoad(managerObject);
            return managerObject.AddComponent<T>();
        }

        private static void EnsureRoomSyncManager()
        {
            if (RoomSyncManager.Instance != null)
            {
                return;
            }

            GameObject syncObject = new GameObject("RoomSyncManager");
            UnityEngine.Object.DontDestroyOnLoad(syncObject);
            syncObject.AddComponent<RoomSyncManager>();
        }
    }
}

