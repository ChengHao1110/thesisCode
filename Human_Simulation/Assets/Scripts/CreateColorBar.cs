using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class CreateColorBar : MonoBehaviour
{
    // Start is called before the first frame update

    // rectangle
    //          (-startX, startZ) 0----1 (-startX + width, startZ)           green
    //                            |    |                                       |
    //                            |    |                                       |
    //                            |    |                                       |
    //                            |    |                                       |
    // (-startX, startZ - length) 3----2 (-startX + width, startZ - length)   red

    VertexHelper vh = new VertexHelper();
    float colorbarLength = 0f, colorbarWidth = 0f;

    // colorbar rectangle                
    int count;
    float colorStep = 0f, colorValue = 0f;

    // position
    Vector3 startPos = Vector3.zero;
    Vector3 offset = Vector3.zero;

    // text
    public float textMaxValue, textStep; //set from heatmap_float.cs
    float textOffset = 0;
    public HeatMap_Float heatMap_Float;
    public GameObject textPrefab;
    public GameObject textParent;


    private void OnEnable()
    {
        InitializeParameters();
        SetColorBar();
    }

    private void OnDisable()
    {
        DestoryColorbar();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void InitializeParameters()
    {
        colorbarLength = 15.0f; // z axis
        colorbarWidth = 0.5f; // x axis
        count = 10;
        colorValue = 1.0f / 3.0f;
        colorStep = colorValue / count;
        textMaxValue = heatMap_Float.max;
        textStep = textMaxValue / count;
        SetPosition();
    }

    void SetPosition()
    {
        offset = Vector3.zero;
        switch (UIController.instance.currentScene)
        {
            case "119":
                startPos = new Vector3(-12f, 7.5f, colorbarLength / 2f);
                textOffset = 4f;
                offset += new Vector3(0, 0, 0);
                break;
            case "120":
                startPos = new Vector3(-14f, 7.5f, colorbarLength / 2f);
                textOffset = 5.0f;
                offset += new Vector3(50, 0, 0);
                break;
            case "225":
                startPos = new Vector3(-12f, 7.5f, colorbarLength / 2f);
                textOffset = 4f;
                offset += new Vector3(101.2f, 0, 3.75f);
                break;
        }

        if (UIController.instance.curOption.Contains("A")) offset += new Vector3(0, 0, 50);
        else if (UIController.instance.curOption.Contains("B")) offset += new Vector3(0, 0, 100);
        else offset += new Vector3(0, 0, 0);
    }
    void SetColorBar()
    {
        vh.Clear();
        for (int i = 0; i < count; i++)
        {
            CreateRectangle(i);
        }
        CreateText(startPos + offset - new Vector3(0, 0, colorbarLength) - textOffset * new Vector3(colorbarWidth, 0, 0), textMaxValue);
        var meshFilter = this.GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        vh.FillMesh(mesh);
        meshFilter.mesh = mesh;
    }

    void CreateRectangle(int idx)
    {
        UIVertex[] verts = new UIVertex[4];
        float posStep = colorbarLength / count;
        Vector3 posOffset = idx * new Vector3(0, 0, -posStep);
        verts[0].position = startPos + offset + posOffset;
        verts[0].color = Color.HSVToRGB(colorValue - idx * colorStep, 1, 1);
        verts[0].uv0 = Vector2.zero;

        posOffset = idx * new Vector3(0, 0, -posStep) + new Vector3(colorbarWidth, 0, 0);
        verts[1].position = startPos + offset + posOffset;
        verts[1].color = Color.HSVToRGB(colorValue - idx * colorStep, 1, 1);
        verts[1].uv0 = Vector2.zero;

        posOffset = (idx + 1) * new Vector3(0, 0, -posStep) + new Vector3(colorbarWidth, 0, 0);
        verts[2].position = startPos + offset + posOffset;
        verts[2].color = Color.HSVToRGB(colorValue - (idx + 1) * colorStep, 1, 1);
        verts[2].uv0 = Vector2.zero;

        posOffset = (idx + 1) * new Vector3(0, 0, -posStep);
        verts[3].position = startPos + offset + posOffset;
        verts[3].color = Color.HSVToRGB(colorValue - (idx + 1) * colorStep, 1, 1);
        verts[3].uv0 = Vector2.zero;

        posOffset = idx * new Vector3(0, 0, -posStep) - textOffset * new Vector3(colorbarWidth, 0, 0);
        CreateText(startPos + offset + posOffset, idx * textStep);

        vh.AddUIVertexQuad(verts);
    }

    void CreateText(Vector3 pos, float value)
    {
        GameObject markText = Instantiate(textPrefab);
        markText.transform.SetParent(textParent.transform);
        markText.transform.position = pos;
        markText.transform.rotation = Quaternion.Euler(90, 180, 0);
        markText.transform.localScale = new Vector3(1, 1, 1);
        markText.GetComponent<TextMeshProUGUI>().text = "-" + value.ToString("f2");
    }

    void DestoryColorbar()
    {
        vh.Clear();
        foreach(Transform text in textParent.transform)
        {
            Destroy(text.gameObject);
        }
    }
}
