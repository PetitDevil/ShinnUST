﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using URG;

// Official Documents of URG Sensor
// http://sourceforge.net/p/urgnetwork/wiki/top_jp/
// https://www.hokuyo-aut.co.jp/02sensor/07scanner/download/pdf/URG_SCIP20.pdf

class UrgMesh
{
    public List<Vector3> VertexList { get; private set; }
    public List<Vector2> UVList { get; private set; }
    public List<int> IndexList { get; private set; }

    public UrgMesh()
    {
        VertexList = new List<Vector3>();
        UVList = new List<Vector2>();
        IndexList = new List<int>();
    }

    public void Clear()
    {
        VertexList.Clear();
        UVList.Clear();
        IndexList.Clear();
    }

    public void AddVertex(Vector3 pos)
    {
        VertexList.Add(pos);
    }

    public void AddUv(Vector2 uv)
    {
        UVList.Add(uv);
    }

    public void AddIndices(int[] indices)
    {
        IndexList.AddRange(indices);
    }
}

public class Urg : MonoBehaviour
{
    #region Device Config
    [SerializeField]
    URGDevice urg;

    [SerializeField]
    //string ipAddress = "192.168.0.10";
    string ipAddress = "10.0.0.172";

    [SerializeField]
    int portNumber = 10940;

    [SerializeField]
    string portName = "COM3";

    [SerializeField]
    int baudRate = 115200;

    [SerializeField]
    bool useEthernetTypeURG = true;

    int urgStartStep;
    int urgEndStep;

    public static int _startstep;
    public static int _endstep;
    public static int _stepCount;

    //   float detectRange=100;
    //public float DetectRange
    //{
    //	get { return detectRange; }
    //	set {detectRange = value; }
    //}

    #endregion

    #region Debug

    Vector3 _scale = new Vector3(0.05f, 0.05f, 1);
    public Vector3 _Scale
    {
        get { return _scale; }
        set
        {
            _scale = value;
        }
    }

    float rotate = 0;
    public float Rotate
    {
        get { return rotate; }
        set
        {
            //if (value > 0)
            rotate = value;
        }
    }

    Vector3 posOffset = Vector3.zero;
    public Vector3 PosOffset
    {
        get { return posOffset; }
        set { posOffset = value; }
    }

    bool drawMesh = true;
    public bool DrawMesh
    {
        get { return drawMesh; }
        set { drawMesh = value; }
    }

    #endregion

    #region Mesh
    UrgMesh urgMesh;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    Mesh mesh;
    #endregion

    [SerializeField]

    long[] distances;

    public Vector4[] DetectedObstacles { get; private set; }
    List<Vector3> DetectSolution = new List<Vector3>();

    public bool IsConnected { get { return urg.IsConnected; } }

    bool ScriptStart = false;
    bool workOnce = false;

    public GameObject UST_POS;
    public ShinnUSTManager main;
    public Camera cam;

    [Tooltip("選擇'牆'或'地板', 如果為地板, 請將UST Group, Rot x設為90")]
    public Type type;

    public enum Type
    {
        Wall,
        Floor
    }

    public Vector3 RaycaseOffset;
    public bool RaycastDebug = false;

    public void init()
    {
        if (main.Enable)
        {
            print("go");

            if (useEthernetTypeURG)
            {
                urg = new EthernetURG(ipAddress, portNumber);
            }
            else
            {
                urg = new SerialURG(portName, baudRate);
            }

            urg.StepCount360 = _stepCount;
            urg.Open();

            urgStartStep = _startstep;
            urgEndStep = _endstep;

            distances = new long[urgEndStep - urgStartStep + 1];

            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            mesh = new Mesh();
            urgMesh = new UrgMesh();
            DetectedObstacles = new Vector4[urgEndStep - urgStartStep + 1];

            ScriptStart = true;

        }

    }

    void Update()
    {

        if (main.Enable)
        {

            if (distances.Length == 0 && !workOnce)
            {
                workOnce = true;
                Reset();
                StartCoroutine(ResetFuct(5));
            }


            if (ScriptStart)
            {
                if (urg.Distances.Count() == distances.Length)
                {

                    try
                    {
                        distances = urg.Distances.ToArray();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, this);
                    }

                }

                UpdateObstacleData();
                meshRenderer.enabled = drawMesh;

                if (drawMesh)
                {
                    CreateMesh();
                    ApplyMesh();
                    UST_POS.transform.position = new Vector3(PosOffset.x, PosOffset.y, -20);
                }
            }

        }
    }

    void UpdateObstacleData()
    {

        for (int i = 0; i < distances.Length; i++)
        {

            if (type == Type.Floor)
            {
                Vector3 position = new Vector3(_scale.x * Index2Position(i).x + PosOffset.x, 1, _scale.y * Index2Position(i).y + PosOffset.y);
                DetectedObstacles[i] = new Vector4(position.x, position.y, position.z, distances[i]);

                int index = i + 1;
                if (index > distances.Length - 1)
                    index = distances.Length - 1;

                if (Mathf.Abs(distances[i] - distances[index]) < 10)
                {

                    if (position.x > ShinnUSTManager.touchArea_value.x && position.x < ShinnUSTManager.touchArea_value.y)
                        DetectSolution.Add(position + RaycaseOffset);
                }


            }

            else
            {
                Vector3 position = new Vector3(_scale.x * Index2Position(i).x + PosOffset.x, _scale.y * Index2Position(i).y + PosOffset.y, 1);
                DetectedObstacles[i] = new Vector4(position.x, position.y, position.z, distances[i]);

                int index = i + 1;
                if (index > distances.Length - 1)
                    index = distances.Length - 1;

                if (Mathf.Abs(distances[i] - distances[index]) < 20)
                {
                    if (position.x > ShinnUSTManager.touchArea_value.x && position.x < ShinnUSTManager.touchArea_value.y && position.y < ShinnUSTManager.touchArea_value.z && position.y > ShinnUSTManager.touchArea_value.w)
                    {
                        DetectSolution.Add(position + RaycaseOffset);
                        //Debug.Log("add point to detectSolution" + (position + RaycaseOffset));
                    }

                }

            }


        }

        //Debug.Log("DetectSolution count: " + DetectSolution.Count);

        for (int i = 0; i < DetectSolution.Count; i++)
        {

            //Debug.Log("DetectSolution " + i + ": " + DetectSolution[i]);

            RaycastHit hit;
            Vector3 _screenPos = cam.WorldToScreenPoint(DetectSolution[i]);
            Ray ray = cam.ScreenPointToRay(_screenPos);


            if (Physics.Raycast(ray, out hit, 100))
            {
                if (RaycastDebug)
                    // Debug.DrawLine(cam.transform.position, hit.transform.position, Color.red, .1f, true);

                    if (RaycastDebug)
                    {
                        var go = new Vector3(_screenPos.x, _screenPos.y, 0);
                        Debug.DrawLine(cam.transform.position, go, Color.red, .1f, true);
                    }

                // if (hit.collider.gameObject.transform.tag == "trigger")
                //     hit.transform.gameObject.SendMessage("CubeStart");

                if (hit.collider.gameObject.name == "toucharea")
                {
                    //Debug.Log("hit point: "+ hit.point);
                    //Change point in TouchEvent
                    var go = ShinnUSTManager.touchArea_value;
                    var x = (hit.point.x - go.x) / (go.y - go.x);
                    var y = (hit.point.y - go.z) / (go.w - go.z);
                    // hit.collider.gameObject.GetComponent<TouchEvent>().point = new Vector2(hit.point.x, hit.point.y);
                    hit.collider.gameObject.GetComponent<TouchEvent>().point = new Vector2(x, y);
                    // hit.collider.gameObject.GetComponent<TouchEvent>().point = new Vector2(DetectSolution[i].x, DetectSolution[i].y);
                    hit.collider.gameObject.GetComponent<TouchEvent>().shouldSend = true;
                }

            }
            else
            {
                //Reset point 
            }
        }
        DetectSolution.Clear();
    }

    static bool IsValidDistance(long distance)
    {
        return distance >= 21 && distance <= 30000;
    }

    bool IsOffScreen(Vector3 worldPosition)
    {
        Vector3 viewPos = cam.WorldToViewportPoint(worldPosition);
        return (viewPos.x < 0 || viewPos.x > 1 || viewPos.y < 0 || viewPos.y > 1);
    }

    float Index2Rad(int index)
    {
        float step = 2 * Mathf.PI / urg.StepCount360;
        float offset = step * (urg.EndStep + urg.StartStep) / 2;
        return step * index + offset;
    }

    Vector3 Index2Position(int index)
    {
        float radius = rotate * Mathf.Deg2Rad;
        return new Vector3(distances[index] * Mathf.Cos(Index2Rad(index + urgStartStep) + radius) - distances[index] * Mathf.Sin(Index2Rad(index + urgStartStep) + radius),
                            distances[index] * Mathf.Sin(Index2Rad(index + urgStartStep) + radius) + distances[index] * Mathf.Cos(Index2Rad(index + urgStartStep) + radius), 0);
    }

    void CreateMesh()
    {
        urgMesh.Clear();
        urgMesh.AddVertex(PosOffset);
        urgMesh.AddUv(cam.WorldToViewportPoint(PosOffset));

        for (int i = distances.Length - 1; i >= 0; i--)
        {

            //urgMesh.AddVertex(scale * Index2Position(i) + PosOffset);
            //urgMesh.AddUv(Camera.main.WorldToViewportPoint(scale * Index2Position(i) + PosOffset));

            urgMesh.AddVertex(new Vector3(_scale.x * Index2Position(i).x + PosOffset.x, _scale.y * Index2Position(i).y + PosOffset.y, 1));
            urgMesh.AddUv(cam.WorldToViewportPoint(new Vector3(_scale.x * Index2Position(i).x + PosOffset.x, _scale.y * Index2Position(i).y + PosOffset.y, 1)));
        }
        for (int i = 0; i < distances.Length - 1; i++)
        {
            urgMesh.AddIndices(new int[] { 0, i + 1, i + 2 });

        }
    }

    void ApplyMesh()
    {
        mesh.Clear();
        mesh.name = "URG Data";
        mesh.vertices = urgMesh.VertexList.ToArray();
        mesh.uv = urgMesh.UVList.ToArray();
        mesh.triangles = urgMesh.IndexList.ToArray();
        meshFilter.sharedMesh = mesh;
    }


    void Reset()
    {
        Disconnect();
        urg.Close();


        init();
        Connect();
    }


    IEnumerator ResetFuct(float delay)
    {
        yield return new WaitForSeconds(delay);
        workOnce = false;
    }

    public void Connect()
    {
        urg.Write(SCIP_library.SCIP_Writer.MD(urgStartStep, urgEndStep, 1, 0, 0));
    }

    public void Disconnect()
    {
        urg.Write(SCIP_library.SCIP_Writer.QT());
    }

    void OnApplicationQuit()
    {

        if (main.Enable)
        {
            urg.Write(SCIP_library.SCIP_Writer.QT());
            urg.Close();
        }

    }

    void OnDestroy()
    {

        if (main.Enable)
        {
            urg.Write(SCIP_library.SCIP_Writer.QT());
            urg.Close();
        }

    }


}