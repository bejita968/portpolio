using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Android;

using TMPro;

public class GPSInfo : MonoBehaviour
{
	private static WaitForSeconds second;

	private static bool gpsStarted = false;
	private static LocationInfo location;

	public float latitude;
	public float longitude;
	public float altitude;

	public TMP_Text textLat;
	public TMP_Text textLon;
	public TMP_Text textAlt;

	void Awake() {
		second = new WaitForSeconds(1.0f);
	}

	void Start()
    {
#if UNITY_ANDROID
		if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
		{
			Permission.RequestUserPermission(Permission.FineLocation);
		}
#endif

		StartCoroutine(StartLocationService());
    }

	private IEnumerator StartLocationService() {
		if (!Input.location.isEnabledByUser)
		{
			Debug.Log("User has not enabled location");
			yield break;
		}

		Input.location.Start();

		int maxWait = 20;
		while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0) {
			yield return second;
			maxWait--;
		}
		if(maxWait <= 0) {
			Debug.Log("Timed out");
			yield break;
		}
		if(Input.location.status == LocationServiceStatus.Failed) {
			Debug.Log("Unable to determine device location");
			yield break;
		}
		else
		{
			location = Input.location.lastData;
			latitude = location.latitude;
			longitude = location.longitude;
			altitude = location.altitude;
			gpsStarted = true;

			while (gpsStarted)
			{
				location = Input.location.lastData;

				latitude = location.latitude;
				longitude = location.longitude;
				altitude = location.altitude;

				textLat.text = "Lat: " + latitude;
				textLon.text = "Lon: " + longitude;
				textAlt.text = "Alt: " + altitude;

				Debug.Log("Latitude : " + latitude);
				Debug.Log("Longitude : " + longitude);
				Debug.Log("Altitude : " + altitude);

				yield return second;
			}
		}
	}
}
