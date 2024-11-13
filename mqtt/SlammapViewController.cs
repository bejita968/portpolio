using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlammapViewController : MonoBehaviour
{
    public GameObject robot;

    Vector2 robotPos;

    Quaternion rot_qt;

    public float input_x = 0f;//10.201604273768218f;
    public float input_y = 0f;//8.81151373720791f;

    public float map_pos_x = 0f;
    public float map_pos_y = 0f;

    float map_res = 0.05f;

    float offset_x = 5.707087f;
    float offset_y = 9.954433f;

    // Start is called before the first frame update
    void Start()
    {
        rot_qt.x = 0f;
        rot_qt.y = 0f;
        rot_qt.z = 0f;// -0.49000647774689443f;
        rot_qt.w = 0f;// 0.8717187916788773f;

        map_pos_x = (input_x + offset_x) / map_res;
        map_pos_y = (input_y + offset_y) / map_res;
        robotPos = new Vector2(map_pos_x, map_pos_y);

        StartCoroutine(CheckQueue()); // coroutine update data to map-robot-pos
    }

    public void CalcPosition(TFData data) {
        rot_qt = new Quaternion();
        rot_qt.x = data.transform.rotation.x;
        rot_qt.y = data.transform.rotation.y;
        rot_qt.z = data.transform.rotation.z;
        rot_qt.w = data.transform.rotation.w;

		map_pos_x = (data.transform.translation.x + offset_x) / map_res;
		map_pos_y = (data.transform.translation.y + offset_y) / map_res;
		robotPos = new Vector2(map_pos_x, map_pos_y);

		robot.GetComponent<RectTransform>().anchoredPosition = robotPos;
        robot.GetComponent<RectTransform>().rotation = rot_qt;
    }

    public IEnumerator CheckQueue()
    {
        while (true)
        {
            TFData data = PostBox.Getinstance.GetData();
            if (data != null)
            {
                CalcPosition(data);
            }
            yield return new WaitForSeconds(0.02f);
        }
    }
}
