using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;

using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using M2MqttUnity;

public class MQTT_Pub : MonoBehaviour
{
    public MqttClient client;

    public bool isEncrypted = false;
    public string brokerAddress;// = "192.168.0.41";
    public int brokerPort;// = 1883;
    public string topic;// = "wbpos";
    public string username = "anonymous_pub";

    public List<TFData> tfDatas;

    // Start is called before the first frame update
    void Start()
    {
        tfDatas = new List<TFData>();
        StreamReader sr = new StreamReader(Application.streamingAssetsPath + "/tf_out_new.json");
        while (!sr.EndOfStream)
        {
            string inpLine = sr.ReadLine();
            TFData data = JsonUtility.FromJson<TFData>(inpLine);
            tfDatas.Add(data);
        }
        sr.Close();
    }

    public void Begin()
    {
        client = new MqttClient(brokerAddress, brokerPort, isEncrypted, null, null, isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
        client.Connect(username);

        StartCoroutine(DelayedSend());
    }

    public IEnumerator DelayedSend()
    {
        yield return null;
        foreach (TFData data in tfDatas)
        {
            client.Publish(topic, Encoding.UTF8.GetBytes(JsonUtility.ToJson(data)), 0, false);
            yield return new WaitForSeconds(0.02f);
        }
    }
}
