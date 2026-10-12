using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkManager))]
public sealed class NetworkGameStarter : MonoBehaviour
{
    [SerializeField] private NetworkSessionRuntime _runtime;
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private string _gameSceneName = "MainMap_Online";

    private NetworkManager _manager;
    private NetworkSceneManager _sceneManager;

    public bool IsStarting { get; private set; }

    private void Awake()
    {
        _manager = GetComponent<NetworkManager>();

        // NetworkSceneManager chỉ sẵn sàng sau khi mạng khởi động.
        _manager.OnServerStarted += RegisterSceneEvents;
    }

    private void RegisterSceneEvents()
    {
        if (_sceneManager != null)
            _sceneManager.OnLoadEventCompleted -= OnLoadCompleted;

        _sceneManager = _manager.SceneManager;
        _sceneManager.OnLoadEventCompleted += OnLoadCompleted;
    }

    public void StartGame()
    {
        if (!_manager.IsHost ||
            IsStarting ||
            _manager.ShutdownInProgress ||
            _manager.ConnectedClientsIds.Count != 2 ||
            _runtime.Roster.Members.Count != 2)
            return;

        if (_playerPrefab == null ||
            _playerPrefab.GetComponent<NetworkPlayerIdentity>() == null)
        {
            Debug.LogError(
                "Assign a player prefab with NetworkPlayerIdentity."
            );
            return;
        }

        IsStarting = true;
        _runtime.Roster.AcceptingPlayers = false;

        var result = _sceneManager.LoadScene(
            _gameSceneName,
            LoadSceneMode.Single
        );

        if (result != SceneEventProgressStatus.Started)
        {
            IsStarting = false;
            _runtime.Roster.AcceptingPlayers = true;
            Debug.LogError("Cannot load game scene: " + result);
        }
    }

    private void OnLoadCompleted(
        string sceneName,
        LoadSceneMode loadMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        if (!_manager.IsServer ||
            !IsStarting ||
            sceneName != _gameSceneName)
            return;

        // Không bắt đầu trận nếu một máy tải lỗi hoặc mất kết nối.
        bool allReady =
            clientsTimedOut.Count == 0 &&
            _manager.ConnectedClientsIds.Count == 2 &&
            _runtime.Roster.Members.Count == 2;

        foreach (ulong clientId in _manager.ConnectedClientsIds)
            allReady &= clientsCompleted.Contains(clientId);

        if (!allReady)
        {
            Debug.LogError(
                "A player disconnected or failed to load the map."
            );

            _manager.Shutdown();
            return;
        }

        Scene gameScene = SceneManager.GetSceneByName(sceneName);

        foreach (RoomMember member in _runtime.Roster.Members)
        {
            var client = _manager.ConnectedClients[member.ClientId];

            // Chống spawn trùng nếu callback bị xử lý lại.
            if (client.PlayerObject != null) continue;

            NetworkObject player = Instantiate(_playerPrefab);

            // Đặt player vào scene map trước khi spawn.
            SceneManager.MoveGameObjectToScene(
                player.gameObject,
                gameScene
            );

            player.SpawnAsPlayerObject(
                member.ClientId,
                destroyWithScene: true
            );

            player.GetComponent<NetworkPlayerIdentity>()
                .SetNameOnServer(member.Name);
        }

        // Giữ IsStarting = true để không bắt đầu trận lần thứ hai.
    }

    private void OnDestroy()
    {
        if (_manager != null)
            _manager.OnServerStarted -= RegisterSceneEvents;

        if (_sceneManager != null)
            _sceneManager.OnLoadEventCompleted -= OnLoadCompleted;
    }
}