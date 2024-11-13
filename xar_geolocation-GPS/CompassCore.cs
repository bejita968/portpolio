using System.Collections;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class CompassCore : MonoBehaviour
{
	private bool gyroEnabled;

	private Gyroscope gyro;
	private Compass compass;

	public GameObject Compass;

	//	public Text RotText;
	public TMP_Text rotateText;

	private float CompassAngle;
	private float AngleFromN;

	private static WaitForSeconds second;

	private void Awake()
	{
		second = new WaitForSeconds(1.0f);
	}

	private void Start()
	{
		rotateText.text = "Compass : 0";

		gyroEnabled = EnableGyro();
	}

	private bool EnableGyro()
	{
		if (SystemInfo.supportsGyroscope)
		{
			gyro = Input.gyro;
			gyro.enabled = true;

			compass = Input.compass;
			compass.enabled = true;

			return true;
		}
		else
		{
			return false;
		}
	}

	private void Update()
	{
		if (gyroEnabled)
		{
			CompassAngle = gyro.attitude.eulerAngles.z;
			Compass.transform.localRotation = Quaternion.Euler(-90f, CompassAngle, 0f);

			AngleFromN = -CompassAngle;
			if (AngleFromN < 0)
				AngleFromN += 360;
			rotateText.text = "Compass : " + ((int)AngleFromN).ToString();
			GameDataManager.Instance.current_compass = (int)AngleFromN;
		}
	}

	IEnumerator GyroUpdate() {
		while(gyroEnabled) {
			CompassAngle = gyro.attitude.eulerAngles.z;
			Compass.transform.localRotation = Quaternion.Euler(-90f, CompassAngle, 0f);

			AngleFromN = -CompassAngle;
			if (AngleFromN < 0)
				AngleFromN += 360;
			rotateText.text = "Compass : " + ((int)AngleFromN).ToString();
			GameDataManager.Instance.current_compass = (int)AngleFromN;

			yield return second;
		}

		yield return null;
	}
}
