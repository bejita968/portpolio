using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using Fusion.Addons.Physics;
using UnityEditor;
using static Unity.Collections.Unicode;
using Unity.VisualScripting;

public class NetManager : MonoBehaviour, INetworkRunnerCallbacks
{

    [SerializeField] private NetworkRunner networkRunnerPrefab;

    public NetworkRunner runnerInstance;
    private NetworkRunner _runner;

    public GameObject sessionListEntryPrefab;

    public Transform sessionListContentParent;

    public Dictionary<string, GameObject> sessionListUiDictionary = new Dictionary<string, GameObject>();

    // <summary>
    // 
    // </summary>
    private static NetManager instance;
    public static NetManager Instance
    {
        get
        {
            if (instance == null)
            {
                NetManager obj = FindFirstObjectByType<NetManager>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    NetManager newNet = new GameObject("NetManager").AddComponent<NetManager>();
                    instance = newNet;
                }
            }
            return instance;
        }

        private set
        {
            instance = value;
        }
    }

    private void Awake()
    {
        var objs = FindObjectsByType<NetManager>(FindObjectsSortMode.None);
        if (objs.Length != 1)
        {
            Destroy(gameObject);
            return;
        }
        //DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if(_runner != null) { Destroy(_runner); _runner = null; }

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.AddCallbacks(this);
        _runner.ProvideInput = true;
    }

    public async void StartConnectLobby()
    {
        // ���� �κ� ����
        await _runner.JoinSessionLobby(SessionLobby.Shared, GlobalVariables.lobbyName);
    }

    // shared mode
    public async void SharedModeStartGame(string roomName)
    {
        var sessionProperties = new Dictionary<string, SessionProperty>();
        if(DataManager.Instance.IsHost)
        {
            sessionProperties.Add("TUTOR", DataManager.Instance.NickName);
            sessionProperties.Add("DATE", DateTime.Now.ToString("yyyy/MM/dd"));
        }

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionProperties = sessionProperties,
            SessionName = roomName,
            PlayerCount = DataManager.Instance.maxPlayersPerRoom,
            Scene = SceneRef.FromIndex(DataManager.Instance.ClassroomIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            // GameScene
            //SceneManager.LoadScene("Classroom1");
        }
        else
        {
            await _runner.JoinSessionLobby(SessionLobby.Shared, GlobalVariables.lobbyName);
        }
    }

    //
    // interface implement
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log("SessionList updated");

        DeleteOldSessionsFromUI(sessionList);

        CompareLists(sessionList);
    }

    private void DeleteOldSessionsFromUI(List<SessionInfo> sessionList)
    {
        bool isContained = false;
        GameObject uiToDelete = null;

        foreach(KeyValuePair<string, GameObject> kvp in sessionListUiDictionary)
        {
            string sessionKey = kvp.Key;

            foreach(SessionInfo sessionInfo in sessionList)
            {
                if(sessionInfo.Name == sessionKey)
                {
                    isContained = true;
                    break;
                }
            }
            
            if(!isContained)
            {
                uiToDelete = kvp.Value;
                sessionListUiDictionary.Remove(sessionKey);
                Destroy(uiToDelete);
            }
        }
    }

    private void CompareLists(List<SessionInfo> sessionList)
    {
        int number = 1;
        foreach(SessionInfo session in sessionList)
        {
            if(sessionListUiDictionary.ContainsKey(session.Name))
            {
                UpdateEntryUI(session, number);
            }
            else
            {
                CreateEntryUI(session, number);
            }
            number++;
        }
    }
    private void CreateEntryUI(SessionInfo session, int num)
    {
        GameObject newEntry = GameObject.Instantiate(sessionListEntryPrefab);
        newEntry.transform.parent = sessionListContentParent;
        SessionListEntry entryScript = newEntry.GetComponent<SessionListEntry>();
        sessionListUiDictionary.Add(session.Name, newEntry);

        entryScript.sessionNameText.text = session.Name;
        entryScript.playerCountText.text = session.PlayerCount.ToString() + "/" + session.MaxPlayers.ToString();
        entryScript.joinButton.interactable = session.IsOpen;

        entryScript.numberText.text = num.ToString();

        SessionProperty textData;
        session.Properties.TryGetValue("TUTOR", out textData);
        entryScript.tutorNameText.text = textData;//.PropertyValue.ToString();

        session.Properties.TryGetValue("DATE", out textData);
        entryScript.dateText.text = textData;// textData.PropertyValue.ToString();

        newEntry.SetActive(session.IsVisible);
    }
    private void UpdateEntryUI(SessionInfo session, int num)
    {
        sessionListUiDictionary.TryGetValue(session.Name, out GameObject newEntry);

        SessionListEntry entryScript = newEntry.GetComponent<SessionListEntry>();

        entryScript.sessionNameText.text = session.Name;
        entryScript.playerCountText.text = session.PlayerCount.ToString() + "/" + session.MaxPlayers.ToString();
        entryScript.joinButton.interactable = session.IsOpen;

        entryScript.numberText.text = num.ToString();

        SessionProperty textData;
        session.Properties.TryGetValue("TUTOR", out textData);
        entryScript.tutorNameText.text = textData;//.ToString();

        session.Properties.TryGetValue("DATE", out textData);
        entryScript.dateText.text = textData;//.ToString();

        newEntry.SetActive(session.IsVisible);
    }
    public void CreateRandomSession()
    {
        int randomInt = UnityEngine.Random.Range(1000, 9999);
        string randomSessionName = "Class-" + randomInt.ToString();
        _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = randomSessionName,
            Scene = SceneRef.FromIndex(1),//GetSceneIndex(gameScene.name)),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }
    public void ReturnToLobby()
    {
        _runner.Despawn(_runner.GetPlayerObject(_runner.LocalPlayer));
        _runner.Shutdown(true, ShutdownReason.Ok);

        SceneManager.LoadScene("Main");
    }
    public void RefreshSessionListUI()
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        SceneManager.LoadScene("Main");
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

}
