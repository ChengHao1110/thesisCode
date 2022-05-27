using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HeatMap : MonoBehaviour
{
    /*
    [SerializeField] private HeatMapVisual heatMapVisual;
    [SerializeField] private GameObject showingObject;
    private Grid grid;
    */

    [SerializeField] private GameObject visualPrefab;
    private GameObject[] visual;
    private HeatMapVisual[] heatMapVisual;
    private Grid[] grid;

    static int defaultSize = 100;

    // Start is called before the first frame update
    void Start()
    {   
        int size = 1000;
        float sceneSize = 22.0f;

        string input = File.ReadAllText(@"E:\ChengHao\Lab707\thesisCode\ChengHao\thesisCode\Human_Simulation\Assets\StreamingAssets\Simulation_Result\test29\space_usage.txt");

        int[,] matrix = new int[size, size];
        int i = 0, j = 0, max = -1;

        foreach (var row in input.Split('\n'))
        {
            j = 0;
            foreach (var col in row.Split(' '))
            {
                if (col == "") break;
                int value = int.Parse(col);
                if (value > max) max = value;
                matrix[i, j] = value;
                j++;
            }
            i++;
        }
        /*
        for (i = 0; i < size; i++)
        {
            for (j = 0; j < size; j++)
            {
                if (matrix[i, j] == 0) matrix[i, j] = max + 1;
            }
        }
        */

        int[,] matrixAfterRotation = new int[size, size];

        /*counterclockwise 90*/
        for (i = 0; i < size; i++)
        {
            for (j = 0; j < size; j++)
            {
                matrixAfterRotation[j, size - 1 - i] = matrix[i, j];
            }
        }

        //set grid
        int edgeMeshCount = size / defaultSize;
        int totalMeshCount = edgeMeshCount * edgeMeshCount;
        float cellSize = sceneSize / size;

        grid = new Grid[totalMeshCount];
        heatMapVisual = new HeatMapVisual[totalMeshCount];
        visual = new GameObject[totalMeshCount];

        for (i = 0; i < edgeMeshCount; i++)
        {
            for (j = 0; j < edgeMeshCount; j++)
            {
                int index = i * edgeMeshCount + j;
                Vector3 generatePos = new Vector3(sceneSize/2.0f, 0, sceneSize/2.0f);
                Vector3 originPos = new Vector3(defaultSize * cellSize * i, 0.2f, defaultSize * cellSize * j);
                visual[index] = Instantiate(visualPrefab, originPos - generatePos, Quaternion.Euler(90, 0, 0));

                heatMapVisual[index] = visual[index].GetComponent<HeatMapVisual>();

                grid[index] = new Grid(defaultSize, defaultSize, cellSize, Vector3.zero);

                grid[index].HEAT_MAP_MAX_VALUE = max;

                /*handle part of array*/
                int[,] partArray = new int[defaultSize, defaultSize];

                int startRow = i * defaultSize;
                int startCol = j * defaultSize;

                for (int k = 0; k < defaultSize; k++)
                {
                    for (int l = 0; l < defaultSize; l++)
                    {
                        partArray[k, l] = matrixAfterRotation[startRow + k, startCol + l];
                    }
                }

                grid[index].gridArray = partArray;
                heatMapVisual[index].SetGrid(grid[index]);
            }
        }

        /*
        int size = 110;
        grid = new Grid(size, size, 22.0f / size, new Vector3(0, 0, 0) );
        showingObject.transform.position = new Vector3(-11, 0.4f, -11);
        string input = File.ReadAllText(@"E:\ChengHao\Lab707\thesisCode\ChengHao\thesisCode\Human_Simulation\Assets\StreamingAssets\Simulation_Result\test28\space_usage.txt");

        //Debug.Log(input);

        int i = 0, j = 0;
        int max = -1;
        int[,] result = new int[size, size];
        foreach (var row in input.Split('\n'))
        {
            //Debug.Log(row);
            j = 0;
            foreach (var col in row.Split(' '))
            {
                if (col == "") break;

                int value = int.Parse(col);
                if (value > max) max = value;

                result[i, j] = value;
                
                j++;
            }
            i++;
        }

        for (i = 0; i < size; i++)
        {
            for (j = 0; j < size; j++)
            {
                if (result[i, j] == 0) result[i, j] = max + 1;
            }
        }

        grid.HEAT_MAP_MAX_VALUE = max + 1;

        int[,] tmp = new int[size, size];
        //counterclockwise 90
        for (i = 0; i < size; i++)
        {
            for (j = 0; j < size; j++)
            {
                tmp[j, size- 1 - i] = result[i, j];
            }
        }

        grid.gridArray = tmp;

        heatMapVisual.SetGrid(grid);
        TakeScreenShot();
        */
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void TakeScreenShot()
    {
        GameObject heatmapCamera = GameObject.Find("heatmapCamera_" + UIController.instance.currentScene);
        heatmapCamera.SetActive(true);
        int resWidth = 1200, resHeight = 1200;
        Camera camera = heatmapCamera.GetComponent<Camera>();
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        camera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        camera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        camera.targetTexture = null;
        RenderTexture.active = null; // JC: added to avoid errors
        Destroy(rt);
        byte[] bytes = screenShot.EncodeToPNG();
        string filename = ScreenShotName(resWidth, resHeight);
        System.IO.File.WriteAllBytes(filename, bytes);
        heatmapCamera.SetActive(false);
    }

    public static string ScreenShotName(int width, int height)
    {
        return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }
}
