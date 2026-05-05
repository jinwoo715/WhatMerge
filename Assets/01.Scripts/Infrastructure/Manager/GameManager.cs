using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface INetworkReader
{
	PlayerData GetPlayerData();
}

public class NetworkDataManager : INetworkReader
{
	IPlayerDataLoader DataLoader;

	public void LoadData() 
	{
		DataLoader = new PlayerDataLoader();
	}

    public PlayerData GetPlayerData()
    {
		return DataLoader.LoadPlayerData();
	}
}


public class GameManager : MonoBehaviour
{
    public static bool Initialized { get; set; } = false;

    private static GameManager _instance;
    public static GameManager Instance { get { Init(); return _instance; } }

	public AddressableResourcesManager _resourcesLoader;

    public DataManager _data;
	public GamePayload _payload = new GamePayload();
	public NetworkDataManager _networkData = new NetworkDataManager();

	public static DataManager Data { get { return Instance?._data; } }
	public static GamePayload Payload { get { return Instance?._payload; } }

	public static IResourcesReader Resource { get { return Instance?._resourcesLoader; } }
	public static INetworkReader NetworkData { get { return Instance?._networkData; } }
	public static void Init()
    {
		if (_instance == null && Initialized == false)
		{
			Initialized = true;

			GameObject go = GameObject.Find("GameManager");
			if (go == null)
			{
				go = new GameObject { name = "GameManager" };
				go.AddComponent<GameManager>();
			}

			DontDestroyOnLoad(go);

			// √ ±‚»≠
			_instance = go.GetComponent<GameManager>();

			_instance?._networkData.LoadData();
			_instance?._resourcesLoader.LoadResources();

			Data.Init(Resource);
		}
	}
    private void Awake()
    {
        Init();
    }
}
