using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{

    public int maxPlayersPerRoom = 20;

    private bool isHost; // 0:client, 1:host
    // getter and setter
    public bool IsHost
    {
        get { return isHost; }
        set { isHost = value; }
    }

    private string nickName; // player name
    // getter and setter
    public string NickName
    {
        get { return nickName; }
        set { nickName = value; }
    }

    private string classroomName; // class name
    // getter and setter
    public string ClassroomName
    {
        get { return classroomName; }
        set { classroomName = value; }
    }

    private int classroomIndex = 1; // class index
    // getter and setter
    public int ClassroomIndex
    {
        get { return classroomIndex; }
        set { classroomIndex = value; }
    }

    private int avatarIndex; // selected player avatar index (avatarPrefabs[])
    // getter and setter
    public int AvatarIndex
    {
        get { return avatarIndex; }
        set { avatarIndex = value; }
    }

    private int currentUserCount;
    // getter and setter
    public int CurrentUserCount
    {
        get { return currentUserCount; }
        set { currentUserCount = value; }
    }

    // 
    public GameObject[] avatarPrefabs;

    public GameObject playerPrefab;

    /// <summary>
    /// 
    /// </summary>
    private static DataManager instance;
    public static DataManager Instance
    {
        get
        {
            if (instance == null)
            {
                DataManager obj = FindFirstObjectByType<DataManager>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    DataManager newDataManager = new GameObject("DataManager").AddComponent<DataManager>();
                    instance = newDataManager;
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
        var objs = FindObjectsByType<DataManager>(FindObjectsSortMode.None);
        if (objs.Length != 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    public void ValueInitialization()
    {
        isHost = false;
        nickName = string.Empty;
        classroomName = string.Empty;
        avatarIndex = 0;
    }

}
