using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using M2MqttUnity;

public class MQTT_Sub : MonoBehaviour
{
    public MqttClient client;

    public bool isEncrypted = false;
    public string brokerAddress;// = "192.168.0.41";
    public int brokerPort;// = 1883;
    public string topic;// = "wbpos";
    public string username = "anonymous_sub";

    int count = 0;

	private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
	{
        count++;
        Debug.Log(count + " > " + e.Topic + " : " + Encoding.UTF8.GetString(e.Message));

        TFData data = JsonUtility.FromJson<TFData>(Encoding.UTF8.GetString(e.Message));
        PostBox.Getinstance.PushData(data);
    }

    public void Begin() {
        client = new MqttClient(brokerAddress, brokerPort, isEncrypted, null, null, isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
        client.Connect(username);

        client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;

        client.Subscribe(new string[] { topic }, new byte[] { 0 });
    }
}
