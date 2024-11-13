using System.Collections;
using System.Collections.Generic;
using UMP;
using UnityEngine;

public class MainManager : MonoBehaviour
{
    public UniversalMediaPlayer ump;
    public MQTT_Sub mqttSub;
    public MQTT_Pub mqttPub;

    Dictionary<string, string> rtspIniData;
    Dictionary<string, string> mqttIniData;

    // Start is called before the first frame update
    void Start()
    {
        Initialize();
    }

    void Initialize() {
        rtspIniData = ConfigFileAccess.ReadINIFile(Application.streamingAssetsPath + "/wb_rtsp.ini");
        foreach (var tmp in rtspIniData)
        {
            Debug.Log(tmp.Key + " : " + tmp.Value);
        }
        Debug.Log(rtspIniData["RTSP_URL"]);

        ump.OnPathPrepared(rtspIniData["RTSP_URL"], true);

        mqttIniData = ConfigFileAccess.ReadINIFile(Application.streamingAssetsPath + "/wb_mqtt.ini");
        foreach (var tmp in mqttIniData)
        {
            Debug.Log(tmp.Key + " : " + tmp.Value);
        }
        Debug.Log(mqttIniData["MQTT_IP"]);
        Debug.Log(mqttIniData["MQTT_PORT"]);
        Debug.Log(mqttIniData["MQTT_TOPIC"]);

        mqttSub.brokerAddress = mqttIniData["MQTT_IP"];
        mqttSub.brokerPort = int.Parse(mqttIniData["MQTT_PORT"]);
        mqttSub.topic = mqttIniData["MQTT_TOPIC"];
        mqttSub.Begin();

        if(mqttIniData["MQTT_PUB"] == "true") {
            mqttPub.brokerAddress = mqttIniData["MQTT_IP"];
            mqttPub.brokerPort = int.Parse(mqttIniData["MQTT_PORT"]);
            mqttPub.topic = mqttIniData["MQTT_TOPIC"];
            mqttPub.Begin();
        } else {
            mqttPub.enabled = false;
        }
    }
}
