using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;

public class LoadConfig : MonoBehaviour
{

    // Start is called before the first frame update
    string path = "";
    string result;

    [HideInInspector]
    public bool isloaded = false;

    void Start()
    {
        //Add prefix for url
        path = "file://" + Application.streamingAssetsPath + "/config.json";
        isloaded = false;

        StartCoroutine(load());
    }

    IEnumerator load()
    {
        UnityWebRequest unityWebRequest = UnityWebRequest.Get(path);

        yield return unityWebRequest.SendWebRequest();

        result = unityWebRequest.downloadHandler.text;

        Debug.Log("result: " + result);

        var go = JsonUtility.FromJson<Config>(result);
        
        //WS Server address
        GetComponent<Connection>().wsAddress = go.Server;

        isloaded = true;

    }

    // Update is called once per frame
    void Update()
    {

    }
}


[Serializable]
public class Config
{
    public string Server;
}