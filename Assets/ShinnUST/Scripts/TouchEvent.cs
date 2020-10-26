using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class TouchEvent : MonoBehaviour
{
    public GameObject wsClient;
    public int timersPerSecond = 30;
    public bool shouldSend = false;
    public Vector2 point = new Vector2();
    // Start is called before the first frame update
    void Start()
    {

    }

    float timer = 0;
    // Update is called once per frame
    void Update()
    {
        //Send point to touchscreen for 30 times per second;
        if (timer > 1.0f / timersPerSecond)
        {
            //send point to frontend
            if (shouldSend)
            {
                Debug.Log("point: " + point.x * 3000 + "," + point.y * 1200);
                shouldSend = false;
                point.x *= 3000;
                point.y *= 1200;
            }
            else
            {
                //Set point to illegal
                point = new Vector2(-1, -1);
            }
            //TODO: Send point to frontend. Send 0.0~1.0 data or matched data to screen
            wsClient.GetComponent<Connection>().SendData(point);
            timer = 0;
        }
        else
        {
            timer += Time.deltaTime;
        }

    }

}
