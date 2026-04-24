using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static bool Initialized { get; set; } = false;

    private static GameManager _instance;
    public static GameManager Instance { get { Init(); return _instance; } }

    public DataManager _data;
	public GamePayload _payload = new GamePayload();
	
    public static DataManager Data { get { return Instance?._data; } }
	public static GamePayload Payload { get { return Instance?._payload; } }

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

			Data.Init();
		}
	}
    private void Awake()
    {
        Init();
    }
}
