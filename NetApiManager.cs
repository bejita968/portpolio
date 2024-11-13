using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Ocsp;

public class NetApiManager : MonoBehaviour
{
	public static NetApiManager instance;

	public DataManager dm;

	private string patientlistApiUrl = "http://192.168.0.177:3000/api/patientinfolist";
	private string patientInfoApiUrl = "http://192.168.0.177:3000/api/patientinfo/(:id)";

	private string patientUpdateApiUrl = "http://192.168.0.177:3000/api/patientupdate";
	private string patientAddApiUrl = "http://192.168.0.177:3000/api/patientadd";
	private string patientDeleteApiUrl = "http://192.168.0.177:3000/api/patientdelete";

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			Destroy(gameObject);
		}
		else
		{
			instance = this;
		}
	}

	public IEnumerator StartDataDownload()
	{
		dm.Init();

		StartCoroutine(PatientListInfoFromWeb(patientlistApiUrl));
		yield return null;
	}

	IEnumerator PatientListInfoFromWeb(string uri) {
		using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
		{
			yield return webRequest.SendWebRequest();

			string jsonString = webRequest.downloadHandler.text;
			dm.patientRoot = JsonUtility.FromJson<PatientInfoRoot>(jsonString);
		}
	}

	// post data to server
	public IEnumerator UpdatePatientInfoToWeb(PatientInfoDatum pi) {
		yield return null;

		WWWForm form = new WWWForm();
		form.AddField("uid", pi.uid);
		form.AddField("name", pi.Name);
		form.AddField("age", pi.Age);
		form.AddField("gender", pi.Gender);
		form.AddField("grade", pi.Grade);
		form.AddField("gcsscore", pi.GCSScore);
		form.AddField("e", pi.E);
		form.AddField("m", pi.M);
		form.AddField("v", pi.V);
		form.AddField("avpu", pi.AVPU);

		form.AddField("contractbloodpressure", pi.ContractBloodPressure);
		form.AddField("relaxbloodpressure", pi.RelaxBloodPressure);
		form.AddField("pulse", pi.Pulse);
		form.AddField("bodytemperature", pi.BodyTemperature.ToString()); // ���� float
		form.AddField("oxygen", pi.Oxygen);
		form.AddField("breathcount", pi.BreathCount);

		form.AddField("pollution", pi.Pollution);
		form.AddField("pollutionpoint", pi.PollutionPoint);
		form.AddField("pollutionlevelcps", pi.PollutionLevelCPS);
		form.AddField("innerpollution", pi.InnerPollution);
		form.AddField("wound", pi.Wound);
		form.AddField("woundpoint", pi.WoundPoint);
		form.AddField("woundtype", pi.WoundType);
		form.AddField("woundpoint2", pi.WoundPoint2);
		form.AddField("woundtype2", pi.WoundType2);

		form.AddField("walkable", pi.Walkable);
		form.AddField("walktype", pi.WalkType.ToString());  // ���� float
		form.AddField("posetype", pi.PoseType);
		form.AddField("pose_idx", pi.pose_idx);
		form.AddField("bodyposevalue", pi.bodyposeValue);
		form.AddField("head_idx", pi.head_idx);
		form.AddField("face_idx", pi.face_idx);
		form.AddField("shirt_idx", pi.shirt_idx);
		form.AddField("pants_idx", pi.pants_idx);
		form.AddField("shoes_idx", pi.shoes_idx);
		form.AddField("glasses", pi.glasses);

		using (UnityWebRequest webRequest = UnityWebRequest.Post(patientUpdateApiUrl, form))
		{
			yield return webRequest.SendWebRequest();

			// refresh data
			StartCoroutine(StartDataDownload());
		}
	}

	public IEnumerator AddPatientInfoToWeb(PatientInfoDatum pi) {
		yield return null;

		WWWForm form = new WWWForm();
		form.AddField("name", pi.Name);
		form.AddField("age", pi.Age);
		form.AddField("gender", pi.Gender);
		form.AddField("grade", pi.Grade);
		form.AddField("gcsscore", pi.GCSScore);
		form.AddField("e", pi.E);
		form.AddField("m", pi.M);
		form.AddField("v", pi.V);
		form.AddField("avpu", pi.AVPU);

		form.AddField("contractbloodpressure", pi.ContractBloodPressure);
		form.AddField("relaxbloodpressure", pi.RelaxBloodPressure);
		form.AddField("pulse", pi.Pulse);
		form.AddField("bodytemperature", pi.BodyTemperature.ToString()); // ���� float
		form.AddField("oxygen", pi.Oxygen);
		form.AddField("breathcount", pi.BreathCount);

		form.AddField("pollution", pi.Pollution);
		form.AddField("pollutionpoint", pi.PollutionPoint);
		form.AddField("pollutionlevelcps", pi.PollutionLevelCPS);
		form.AddField("innerpollution", pi.InnerPollution);
		form.AddField("wound", pi.Wound);
		form.AddField("woundpoint", pi.WoundPoint);
		form.AddField("woundtype", pi.WoundType);
		form.AddField("woundpoint2", pi.WoundPoint2);
		form.AddField("woundtype2", pi.WoundType2);

		form.AddField("walkable", pi.Walkable);
		form.AddField("walktype", pi.WalkType.ToString());  // ���� float
		form.AddField("posetype", pi.PoseType);
		form.AddField("pose_idx", pi.pose_idx);
		form.AddField("bodyposevalue", pi.bodyposeValue);
		form.AddField("head_idx", pi.head_idx);
		form.AddField("face_idx", pi.face_idx);
		form.AddField("shirt_idx", pi.shirt_idx);
		form.AddField("pants_idx", pi.pants_idx);
		form.AddField("shoes_idx", pi.shoes_idx);
		form.AddField("glasses", pi.glasses);

		using (UnityWebRequest webRequest = UnityWebRequest.Post(patientAddApiUrl, form))
		{
			yield return webRequest.SendWebRequest();

			// refresh data
			StartCoroutine(StartDataDownload());
		}
	}

	public IEnumerator DeletePatientInfoToWeb(int id)
	{
		yield return null;

		WWWForm form = new WWWForm();
		form.AddField("id", id);

		using (UnityWebRequest webRequest = UnityWebRequest.Post(patientDeleteApiUrl, form))
		{
			yield return webRequest.SendWebRequest();

			// refresh data
			StartCoroutine(StartDataDownload());
		}
	}
}
