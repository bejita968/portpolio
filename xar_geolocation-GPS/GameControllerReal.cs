using System;
using System.Collections;
using System.Collections.Generic;
using Math = System.Math;

using UnityEngine;
using UnityEngine.SceneManagement;

using TMPro;
using HighlightPlus;

public class GameControllerReal : MonoBehaviour
{
	WaitForSeconds oneSec = new WaitForSeconds(1);
	WaitForSeconds twoSec = new WaitForSeconds(2);
	WaitForSeconds threeSec = new WaitForSeconds(3);

	[SerializeField]
	float Ratio = 10000f;
	[SerializeField] 
	Vector3 _worldSize = new Vector3(10000f, 0, 10000f);
	[SerializeField] 
	Vector3 _worldOrigin = new Vector3(0, 0, 0);
	[SerializeField]
	Vector3 _originOffset = new Vector3(0, 0, 0);

	[SerializeField] 
	bool isGo = false;

	[SerializeField]
	public GameObject caveEntPrefab;

	[SerializeField] 
	List<GameObject> _caveList = new List<GameObject>();

	[SerializeField]
	public GameObject player;
	[SerializeField]
	public GameObject world;
	public WorldController wc;

	[SerializeField]
	public TMP_Text label;
	[SerializeField]
	public TMP_Text lat;
	[SerializeField]
	public TMP_Text lon;
	[SerializeField]
	public TMP_Text dist;

	[SerializeField]
	public TMP_Dropdown caveTDD;
	[SerializeField]
	List<string> optionList = new List<string>();
	[SerializeField]
	public int currentOption = 0;

	[SerializeField]
	public NaviController Navi;

	[SerializeField]
	public HighlightEffect hlEffect;

	public OnlineMaps oMap;
	public OnlineMapsMarkerManager markerManager;

	public Texture2D texGangguMarker;
	public Texture2D texPlayerMarker;

	OnlineMapsMarker playerMarker;

	/// <summary>
	/// temp! change to GameDataManager.Instance._currentXXX
	/// </summary>
	[SerializeField]
	GeoCoord _playerPosition = new GeoCoord { code = "", label = "����(���)", latitude = 37.064796099326657, longitude = 127.978222391122245 };
	[SerializeField]
	GeoCoord[] _coordinates = new GeoCoord[] {
        new GeoCoord{ code = "", label="2ȣ��" , latitude=37.065703140903999 , longitude=127.977923932619831 } ,
		new GeoCoord{ code = "", label="����" , latitude=37.064494632733748 , longitude=127.978150048807507 } ,
		new GeoCoord{ code = "", label="��2��" , latitude=37.064655565458281 , longitude=127.978055762854481 } ,
	};

	void Start()
	{
		InitScene();
	}

	void InitScene() {
		_playerPosition.latitude = GameDataManager.Instance.current_lat;
		_playerPosition.longitude = GameDataManager.Instance.current_lon;

		MoveOrigin(_playerPosition);

		GenerateCaveEnt();
		caveTDD.onValueChanged.AddListener(delegate { SelectValue(caveTDD); });

		label.text = "����: " + GameDataManager.Instance.net_currentGangguCoordinate.ToString();
		lat.text = "Lat: " + GameDataManager.Instance.net_currentGangguCoordinate.latitude.ToString();
		lon.text = "Lon: " + GameDataManager.Instance.net_currentGangguCoordinate.longitude.ToString();
		Location loc1 = new Location(_playerPosition.latitude, _playerPosition.longitude);
		Location loc2 = new Location(GameDataManager.Instance.net_currentGangguCoordinate.latitude, GameDataManager.Instance.net_currentGangguCoordinate.longitude);
		Distance dis = LocationDistance.distance(loc1, loc2);

		dist.text = "Dist: " + String.Format("{0:F6}", dis.getMeter()) + " m";

		Navi.RefreshTarget(_caveList[currentOption].gameObject);

		hlEffect = _caveList[currentOption].gameObject.GetComponentInChildren<HighlightEffect>();
		hlEffect.highlighted = true;

		isGo = true;
		StartCoroutine(CoordUpdate());
	}
	
	private void SelectValue(TMP_Dropdown dd)
	{
		hlEffect = _caveList[currentOption].gameObject.GetComponentInChildren<HighlightEffect>();
		hlEffect.highlighted = false;

		currentOption = dd.value;
		Debug.Log("select value -> " + dd.value);

		GameDataManager.Instance.current_ganggu_idx = currentOption;
		GameDataManager.Instance.net_currentGangguCoordinate = GameDataManager.Instance.netGangguList[currentOption];

		Navi.RefreshTarget(_caveList[currentOption].gameObject);

		hlEffect = _caveList[currentOption].gameObject.GetComponentInChildren<HighlightEffect>();
		hlEffect.highlighted = true;
	}

	void MoveOrigin(GeoCoord coord)
	{
		_originOffset = GPSEncoder.GPSToUCS(new Vector2((float)coord.latitude, (float)coord.longitude));

		player.transform.position = _worldOrigin;
		world.transform.position = _worldOrigin;

		oMap.SetPositionAndZoom(coord.longitude, coord.latitude, 17);
	}

	public void GenerateCaveEnt()
	{
		caveTDD.onValueChanged.RemoveAllListeners();
		caveTDD.ClearOptions();

		for (int i = 0; i < GameDataManager.Instance.netGangguList.Count; i++)
		{
			GeoCoord _coord = GameDataManager.Instance.netGangguList[i];
			optionList.Add(_coord.ToString());

			Vector3 v3point = GPSEncoder.GPSToUCS(new Vector2((float)_coord.latitude, (float)_coord.longitude));
			Vector3 offset = v3point - _originOffset;
			Vector3 worldPos = _worldOrigin + offset;

			GameObject go = Instantiate(caveEntPrefab);
			go.transform.SetParent(world.transform, false);
			go.transform.position = worldPos;

			_caveList.Add(go);

			markerManager.Create(_coord.longitude, _coord.latitude, texGangguMarker);
		}
		caveTDD.AddOptions(optionList);

		currentOption = GameDataManager.Instance.net_current_ganggu_idx;

		caveTDD.value = currentOption;

		GameDataManager.Instance.net_current_ganggu_idx = currentOption;
		GameDataManager.Instance.net_currentGangguCoordinate = GameDataManager.Instance.netGangguList[currentOption];

		playerMarker = markerManager.Create(_playerPosition.longitude, _playerPosition.latitude, texPlayerMarker);
	}

	IEnumerator CoordUpdate()
	{
		while (isGo)
		{
			// refresh player position by walk(moving)
			RefreshLocation();
			// ...

			label.text = "����: " + GameDataManager.Instance.net_currentGangguCoordinate.ToString();
			lat.text = "Lat: " + GameDataManager.Instance.net_currentGangguCoordinate.latitude.ToString();
			lon.text = "Lon: " + GameDataManager.Instance.net_currentGangguCoordinate.longitude.ToString();
			Location loc1 = new Location(_playerPosition.latitude, _playerPosition.longitude);
			Location loc2 = new Location(GameDataManager.Instance.net_currentGangguCoordinate.latitude, GameDataManager.Instance.net_currentGangguCoordinate.longitude);
			Distance dis = LocationDistance.distance(loc1, loc2);
//			dist.text = "Dist: " + dis.getMeter().ToString() + " m";
			dist.text = "Dist: " + String.Format("{0:F6}", dis.getMeter()) + " m";
			Navi.RefreshTarget(_caveList[currentOption].gameObject);

			yield return oneSec;
		}
	}

//	IEnumerator RefreshLocation() { 
	void RefreshLocation() {
		_playerPosition.latitude = GameDataManager.Instance.current_lat;
		_playerPosition.longitude = GameDataManager.Instance.current_lon;

		MoveOrigin(_playerPosition);

		for (int i = 0; i < GameDataManager.Instance.netGangguList.Count; i++)
		{
			GeoCoord _coord = GameDataManager.Instance.netGangguList[i];

			Vector3 v3point = GPSEncoder.GPSToUCS(new Vector2((float)_coord.latitude, (float)_coord.longitude));
			Vector3 offset = v3point - _originOffset;
			Vector3 worldPos = _worldOrigin + offset;

			GameObject go = _caveList[i];
			go.transform.position = worldPos;
		}

		playerMarker.SetPosition(_playerPosition.longitude, _playerPosition.latitude);
	}

	public void ReturnToMenu()
	{
		SceneManager.LoadScene("MenuScene");
		UIManager.Instance.UITopMenuOn();
	}
}
