// 유니티는 사용자 디바이스의 GPS를 기반으로 경도와 위도 또는 고도를 받아올 수 있는 라이브러리를 제공한다

// Unity - Scripting API : LocationService.Start

// 위치 서비스를 시작할 때 오버로딩이 총 세개가 있는데,
// 이 때 '원하는 미터 정확도'와 '미터 업데이트 거리'를 조절할 수 있다.
// '미터'로 표시되기 때문에 값이 낮을수록 더 정밀해진다.
// 기본은 둘 다 10m 로 되어 있다.

// LocationInfo 클래스에는 경도, 위도, 고도, 각각의 정확성에 관한 값들이 들어있다.
// 기본적으로 Input.location.Start() 로 위치 서비스를 시작하고, Input.location.Stop() 으로 중단할 수 있다.
// 위도와 경도 값은 float로 되어 있는데, 따라서 double (* 1.0) 자료형을 곱해준다면 더 정확한 수치가 나온다.
// 고도의 경우 location.altitude 으로 가져오면 된다.

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Android;

public class GPSManager : MonoBehaviour {
	public static GPSManager Instance = null;

	public static double first_Lat;
	public static double first_Lon;

	public static double current_Lat;
	public static double current_Lon;

	private static WaitForSeconds oneSecond;
	private static WaitForSeconds threeSecond;

	private static bool gpsStarted = false;

	private static LocationInfo location;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			if (Instance != this)
			{
				Destroy(this.gameObject);
			}
		}

		oneSecond = new WaitForSeconds(1);
		threeSecond = new WaitForSeconds(3);
	}

	IEnumerator Start() {
		#if UNITY_ANDROID
		if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
		{
			Permission.RequestUserPermission(Permission.FineLocation);
		}
		#endif
		if (!Input.location.isEnabledByUser) {
			Debug.Log("GPS is not enabled");
			yield break;
		}

		Input.location.Start();
		Debug.Log("Awaiting Initialization");

		int maxWait = 20;
		while(Input.location.status == LocationServiceStatus.Initializing && maxWait > 0) {
			yield return oneSecond;
			maxWait -= 1;
		}
		if(maxWait < 1) {
			Debug.Log("Timed out");
			yield break;
		}
		if(Input.location.status == LocationServiceStatus.Failed) {
			Debug.Log("Unable to determine device location");
			yield break;
		}
		else {
			location = Input.location.lastData;

			first_Lat = location.latitude * 1.0d;
			first_Lon = location.longitude * 1.0d;

			gpsStarted = true;

			while(gpsStarted) {
				location = Input.location.lastData;
				current_Lat = location.latitude * 1.0d;
				current_Lon = location.longitude * 1.0d;

				GameDataManager.Instance.current_lat = (float)current_Lat;
				GameDataManager.Instance.current_lon = (float)current_Lon;
				//GameDataManager.Instance._playerLocation.latitude = current_Lat;
				//GameDataManager.Instance._playerLocation.longitude = current_Lon;

				yield return oneSecond;
			}
		}
	}

	public static void StopGPS() {
		if(Input.location.isEnabledByUser) {
			gpsStarted = false;
			Input.location.Stop();
		}
	}

	public Vector2 GetCurrentGPSCoord() {
		Vector2 coord = new Vector2((float)current_Lat,  (float)current_Lon);
		return coord;
	}
}
