using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawBoundingBox : MonoBehaviour
{
    private Vector3 v3FrontTopLeft;
    private Vector3 v3FrontTopRight;
    private Vector3 v3FrontBottomLeft;
    private Vector3 v3FrontBottomRight;
    private Vector3 v3BackTopLeft;
    private Vector3 v3BackTopRight;
    private Vector3 v3BackBottomLeft;
    private Vector3 v3BackBottomRight;
    private GameObject[] lines = new GameObject[4];
    private LineRenderer[] lrs = new LineRenderer[4];
    // Start is called before the first frame update
    void Start()
    {   
   
    }

    // Update is called once per frame
    void Update()
    {
        //DrawBox();
    }

    void CalcPositons()
    {
        Bounds bounds = GetComponent<MeshFilter>().mesh.bounds;

        Vector3 v3Center = bounds.center;
        Vector3 v3Extents = bounds.extents;

        v3FrontTopLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top left corner
        v3FrontTopRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top right corner
        v3FrontBottomLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom left corner
        v3FrontBottomRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom right corner
        v3BackTopLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top left corner
        v3BackTopRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top right corner
        v3BackBottomLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom left corner
        v3BackBottomRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom right corner

        v3FrontTopLeft = transform.TransformPoint(v3FrontTopLeft);
        v3FrontTopRight = transform.TransformPoint(v3FrontTopRight);
        v3FrontBottomLeft = transform.TransformPoint(v3FrontBottomLeft);
        v3FrontBottomRight = transform.TransformPoint(v3FrontBottomRight);
        v3BackTopLeft = transform.TransformPoint(v3BackTopLeft);
        v3BackTopRight = transform.TransformPoint(v3BackTopRight);
        v3BackBottomLeft = transform.TransformPoint(v3BackBottomLeft);
        v3BackBottomRight = transform.TransformPoint(v3BackBottomRight);
    }

    public void DrawBox(Color color)
    {
        //Transform[] child = this.GetComponentsInChildren<Transform>();
        if(this.transform.childCount != 4)
        {
            Initialize();
        }

        CalcPositons();
        DrawLine(lrs[0], v3FrontTopLeft, v3FrontTopRight, v3FrontBottomRight, v3FrontBottomLeft, color);
        DrawLine(lrs[1], v3BackTopLeft, v3BackTopRight, v3BackBottomRight, v3BackBottomLeft, color);
        DrawLine(lrs[2], v3FrontTopLeft, v3FrontTopRight, v3BackTopRight, v3BackTopLeft, color);
        DrawLine(lrs[3], v3FrontBottomLeft, v3FrontBottomRight, v3BackBottomRight, v3BackBottomLeft, color);
    }

    void DrawLine(LineRenderer lr, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Color color)
    {
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        lr.startColor = color;
        lr.endColor = color;
        lr.positionCount = 24;
        lr.loop = true;

        //Vector3[] points = {p1, p2, p3, p4};
        Vector3[] inputPoints = { p1, p2, p3, p4 };
        Vector3[] points = new Vector3[24]; // each edge add 5 points
        int inputNum = -1, smoothNum = 1, smoothTotal = 5;
        for(int i = 0; i < 24; i++)
        {
            if(i % 6 == 0)
            {
                inputNum++;
                points[i] = inputPoints[inputNum];
                smoothNum = 1;
            }
            else
            {
                int nextNum = inputNum + 1;
                if (nextNum == 4) nextNum = 0;
                Vector3 v1 = inputPoints[inputNum], v2 = inputPoints[nextNum];
                Vector3 smoothPoint = Vector3.Lerp(v1, v2, (float)smoothNum / smoothTotal);
                smoothNum++;
                points[i] = smoothPoint;
            }
        }

        lr.SetPositions(points);
    }

    void Initialize()
    {
        //initial lines and lrs
        for (int i = 0; i < 4; i++)
        {
            lines[i] = new GameObject("side" + i.ToString());
            lines[i].transform.SetParent(this.transform);
            lrs[i] = lines[i].AddComponent<LineRenderer>();
            lrs[i].positionCount = 0;
        }
    }

    public void DeleteBoundingBox()
    {
        foreach(Transform child in this.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
}
