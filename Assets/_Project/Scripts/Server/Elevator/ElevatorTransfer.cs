using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct SceneMessage : NetworkMessage
{
    public string sceneName;
    public LoadSceneMode loadMode;
}

public class ElevatorTransfer : NetworkBehaviour
{
    public const string GameSceneName = "Game";

    [SerializeField] private Elevator _elevator;
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private LoadSceneMode _sceneMode;

    private readonly List<Scene> _loadedScenes = new();
    private List<NetworkConnectionToClient> _playersInElevator;
    private readonly Queue<(List<NetworkConnectionToClient> players, Scene scene)> _pendingGroups = new();

    private void OnEnable() =>
        _elevator.PlayersReadyToTransfer += OnPlayersReadyToTransfer;

    private void OnDisable() =>
        _elevator.PlayersReadyToTransfer -= OnPlayersReadyToTransfer;

    [Server]
    private void OnPlayersReadyToTransfer(List<NetworkConnectionToClient> players)
    {
        _playersInElevator = new(players);
        StartCoroutine(ServerLoadSceneAdditive());
    }

    [Server]
    private IEnumerator ServerLoadSceneAdditive()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(GameSceneName, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
            yield return null;

        //Scene serverLoaded = SceneManager.GetSceneByName(GameSceneName);
        Scene serverLoaded = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

        if (serverLoaded.IsValid() && serverLoaded.isLoaded)
        {
            _loadedScenes.Add(serverLoaded);
            Debug.Log("ElevatorTransfer: сервер Ц сцена загружена (неактивна€)");
        }
        else
        {
            Debug.LogError("ElevatorTransfer: сервер Ц не удалось загрузить сцену");
            yield break;
        }

        _pendingGroups.Enqueue((new List<NetworkConnectionToClient>(_playersInElevator), serverLoaded));

        // сообщаем клиентам о необходимости загрузить и активировать сцену
        SceneMessage msg = new()
        {
            sceneName = GameSceneName,
            loadMode = _sceneMode
        };
        foreach (var conn in _playersInElevator)
            conn.Send(msg);

        yield return new WaitForSeconds(1f); // даЄм клиентам врем€

        // извлекаем именно эту группу и сцену из очереди
        var (players, scene) = _pendingGroups.Dequeue();
        foreach (var conn in players)
            MovePlayerToMatch(conn, scene);

        _playersInElevator.Clear();
    }

    public override void OnStartClient() =>
        NetworkClient.RegisterHandler<SceneMessage>(OnSceneMessageReceived);

    [Client]
    private void OnSceneMessageReceived(SceneMessage msg)
    {
        Debug.Log($"Elevator: Client received SceneMessage, sceneName: {msg.sceneName}");
        StartCoroutine(ClientLoadAndActivate(msg));
    }

    [Client]
    public IEnumerator ClientLoadAndActivate(SceneMessage msg)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(
            msg.sceneName,
            new LoadSceneParameters(msg.loadMode, LocalPhysicsMode.Physics3D)
        );

        while (!asyncLoad.isDone)
            yield return null;

        Scene clientLoaded = SceneManager.GetSceneByName(msg.sceneName);

        if (clientLoaded.IsValid() && clientLoaded.isLoaded)
        {
            _loadedScenes.Add(clientLoaded);
            SceneManager.SetActiveScene(clientLoaded);
            Debug.Log("ElevatorTransfer: клиент Ц сцена загружена и активирована");
        }
        else
        {
            Debug.LogError("ElevatorTransfer: клиент Ц не удалось загрузить сцену");
        }
    }

    [Server]
    private void MovePlayerToMatch(NetworkConnectionToClient connection, Scene targetScene)
    {
        GameObject oldPlayer = connection.identity.gameObject;
        GameObject newPlayer = Instantiate(_playerPrefab);

        if (newPlayer.scene != targetScene)
            SceneManager.MoveGameObjectToScene(newPlayer, targetScene);

        NetworkServer.ReplacePlayerForConnection(
            connection,
            newPlayer,
            ReplacePlayerOptions.KeepAuthority
        );

        NetworkServer.Destroy(oldPlayer);
    }
}