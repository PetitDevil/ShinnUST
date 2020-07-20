using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.WebSockets;
using System;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

public class Connection : MonoBehaviour
{

    [HideInInspector]
    public string wsAddress = "";
    //Uri u = new Uri("ws://127.0.0.1:1337");
    Uri u;
    ClientWebSocket cws = null;
    ArraySegment<byte> buf = new ArraySegment<byte>(new byte[1024]);

    string message = "";

    //Instance of JsonData for send data to server
    JsonData jd = new JsonData();

    string recv = "";

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Launch());
    }

    IEnumerator Launch()
    {
        Debug.Log(DateTime.Now + "Before loading" + GetComponent<LoadConfig>().isloaded);
        yield return new WaitWhile(() => { if (GetComponent<LoadConfig>().isloaded == true) { return true; } else { return false; } });
        Debug.Log(DateTime.Now + "After loading" + GetComponent<LoadConfig>().isloaded);
        u = new Uri(wsAddress);
        Init();
    }

    void Init()
    {
        jd.Init();
        Connect();
    }

    async void Connect()
    {

        cws = new ClientWebSocket();
        try
        {
            Debug.Log("in Connect");
            await cws.ConnectAsync(u, CancellationToken.None);
            if (cws.State == WebSocketState.Open) Debug.Log("connected");
            SayHello();
            GetStuff();
        }
        catch (Exception e)
        {
            Debug.Log("woe " + e.Message);
            Invoke("Connect", 3);
            Debug.Log("connect failed, try to connect in 3 seconds");
        }
    }

    // Update is called once per frame
    void Update()
    {

        // if (cws == null || GetComponent<LoadConfig>().isloaded == false)
        // {

        //     Debug.Log("cws is null or did't load config file. cws:" + cws + "load?" + GetComponent<LoadConfig>().isloaded);

        //     return;
        // }

    }

    /// <summary>
    /// First time send to Server for identifying
    /// </summary>
    async void SayHello()
    {
        Debug.Log("sayhello");
        jd.type = "Server";
        jd.cmd = "login";
        jd.parameter = "Radar";
        message = JsonUtility.ToJson(jd);

        ArraySegment<byte> b = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
        await cws.SendAsync(b, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async void SendData(Vector2 point)
    {
        jd.type = "Data";
        jd.cmd = "Forward";
        jd.parameter = point.x + "," + point.y;

        message = JsonUtility.ToJson(jd);
        ArraySegment<byte> b = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
        await cws.SendAsync(b, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    /// <summary>
    /// While(0) for getting order
    /// </summary>
    /// 
    WebSocketReceiveResult r;
    async void GetStuff()
    {

        try
        {
            r = await cws.ReceiveAsync(buf, CancellationToken.None);

            recv = Encoding.UTF8.GetString(buf.Array, 0, r.Count);

            Debug.Log("Got: " + recv + System.DateTime.Now + cws.State);

            GetStuff();
        }
        catch (Exception e)
        {
            Debug.Log("GetStuff:  " + e.Message);
        }


    }

    private void OnDisable()
    {
        CloseWebsocket();
    }

    async void CloseWebsocket()
    {
        try
        {
            Debug.Log("Close ws");
            await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "NormalClosure", CancellationToken.None);
            Debug.Log("status" + cws.CloseStatus);
        }
        catch (Exception e)
        {
            Debug.Log("CloseWebsocket:" + e.Message);
        }


    }

}
/// <summary>
/// Command json
/// </summary>
[Serializable]
public class JsonData
{
    public string type;
    public string cmd;
    public string parameter;

    public void Init(string type = "", string cmd = "", string parameter = "")
    {
        this.type = type; this.cmd = cmd; this.parameter = parameter;
    }
    public void Clear()
    {
        this.type = ""; this.cmd = ""; this.parameter = "";
    }
}