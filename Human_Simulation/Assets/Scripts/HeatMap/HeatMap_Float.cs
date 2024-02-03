using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HeatMap_Float : MonoBehaviour
{
    [SerializeField] private GameObject visualPrefab;
    private GameObject[] visual;
    private HeatMapVisual_Float[] heatMapVisual;
    private Grid_Float[] grid;
    public GameObject colorbar;

    //for screen shot
    [SerializeField] private GameObject heatmapCamera;

    static int defaultSize = 100;
    public bool debugMode = true;
    public float maxLimit = 0, max = 0;
    
    void OnEnable()
    {
        if(debugMode == true)
        {
            DebugHeatMap();
            //TakeScreenShotDebug();
            return;
        }
        if (dynamicSystem.instance.heatmapMode == "realtime") // for debug
        {
            Init();
        }
        else //static
        {
            StaticHeatmap();
            gameObject.SetActive(false);
        }
    }
    void OnDisable()
    {
        /*
        if (dynamicSystem.instance.heatmapMode == "realtime")
        {
            DestroyVisualPrefab();
        }
        */
        DestroyVisualPrefab();
    }
    

    // Start is called before the first frame update
    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {
        if (dynamicSystem.instance.heatmapMode == "realtime")
        {
            UpdateHeatmap();
        }  
    }

    void TakeScreenShot()
    {
        /*
        GameObject heatmapCamera = GameObject.Find("heatmapCamera_" + UIController.instance.currentScene);
        */
        heatmapCamera.SetActive(true);
        int resWidth = 1600, resHeight = 1080;
        Camera camera = heatmapCamera.GetComponent<Camera>();
        SetCameraCullingMask(camera, UIController.instance.currentScene);
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
        //string filename = ScreenShotName(resWidth, resHeight);
        string filename = dynamicSystem.instance.heatmapFilename;
        System.IO.File.WriteAllBytes(filename, bytes);
        //DestroyVisualPrefab();
        heatmapCamera.SetActive(false);
    }

    void TakeScreenShotDebug()
    {
        /*
        GameObject heatmapCamera = GameObject.Find("heatmapCamera_" + UIController.instance.currentScene);
        */
        heatmapCamera.SetActive(true);
        int resWidth = 1600, resHeight = 1080;
        Camera camera = heatmapCamera.GetComponent<Camera>();
        SetCameraCullingMask(camera, UIController.instance.currentScene);
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
        //string filename = dynamicSystem.instance.heatmapFilename;
        System.IO.File.WriteAllBytes(filename, bytes);
        //DestroyVisualPrefab();
        heatmapCamera.SetActive(false);
    }
    void SetCameraCullingMask(Camera cam, string sceneHeadName)
    {
        int defaultLayer = LayerMask.NameToLayer("Default");
        int scene = LayerMask.NameToLayer(sceneHeadName);
        cam.cullingMask = (1 << defaultLayer) | (1 << scene);
    }
    public static string ScreenShotName(int width, int height)
    {
        return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }

    public void DestroyVisualPrefab()
    {
        for(int i = 0; i < heatMapVisual.Length; i++)
        {
            Destroy(visual[i]);
        }
    }

    public void Init()
    {
        int size = dynamicSystem.instance.matrixSize;
        float sceneSize = dynamicSystem.instance.sceneSize;

        string input = File.ReadAllText(@"E:\ChengHao\Lab707\thesisCode\ChengHao\thesisCode\Human_Simulation\Assets\StreamingAssets\Simulation_Result\test33\space_usage.txt");

        float[,] matrix = new float[size, size];
        int i = 0, j = 0;
        float max = -1;
        /*
        foreach (var row in input.Split('\n'))
        {
            j = 0;
            foreach (var col in row.Split(' '))
            {
                if (col == "") break;

                float value = float.Parse(col);
                if (value > max) max = value;

                matrix[i, j] = value;

                j++;
            }
            i++;
        }
        */
        matrix = dynamicSystem.instance.matrix;
        max = 1;
        /*
        for (i = 0; i < size; i++)
        {
            for (j = 0; j < size; j++)
            {
                if (matrix[i, j] == 0) matrix[i, j] = max + 1;
            }
        }
        */

        float[,] matrixAfterRotation = new float[size, size];

        /*counterclockwise 90*/
        for (i = 0; i < size; i++)
        {
            for (j = 0; j < size; j++)
            {
                matrixAfterRotation[j, size - 1 - i] = matrix[i, j];
            }
        }

        /*gaussian filter*/
        /*
        int gaussian_filter_size = 10;
        float[,] gaussian_matrix = new float[gaussian_filter_size, gaussian_filter_size];
        float sum_gaussian_matrix = 0;

        int half_gaussian_filter_size = gaussian_filter_size / 2;
        int x, y;
        for (i = 0; i < gaussian_filter_size; i++)
        {
            for (j = 0; j < gaussian_filter_size; j++)
            {
                x = i - half_gaussian_filter_size;
                y = j - half_gaussian_filter_size;
                gaussian_matrix[i, j] = Mathf.Exp(-(x * x + y * y));
                sum_gaussian_matrix += gaussian_matrix[i, j];
            }
        }

        for (i = 0; i < gaussian_filter_size; i++) for (j = 0; j < gaussian_filter_size; j++) gaussian_matrix[i, j] /= sum_gaussian_matrix;


        for (i = 0; i < size; i++)
        {

            for (j = 0; j < size; j++)
            {
                float tmpValue = 0;
                sum_gaussian_matrix = 0;
                for (int k = 0; k < gaussian_filter_size; k++)
                {
                    for (int l = 0; l < gaussian_filter_size; l++)
                    {
                        x = k - half_gaussian_filter_size;
                        y = l - half_gaussian_filter_size;
                        if (x < 0 && i + x < 0) continue;
                        if (x > 0 && i + x >= size) continue;
                        if (y < 0 && j + y < 0) continue;
                        if (y > 0 && j + y >= size) continue;
                        tmpValue += matrixAfterRotation[i + x, j + y] * gaussian_matrix[k, l];
                        sum_gaussian_matrix += gaussian_matrix[k, l];
                    }
                }
                matrix[i, j] = tmpValue / sum_gaussian_matrix;
            }
        }

        max = -1;
        for (i = 0; i < size; i++)
        {
            for (j = 0; j < size; j++)
            {
                if (matrix[i, j] > max) max = matrix[i, j];
            }
        }
        */

        //set grid
        int edgeMeshCount = size / defaultSize;
        int totalMeshCount = edgeMeshCount * edgeMeshCount;
        float cellSize = sceneSize / size;

        grid = new Grid_Float[totalMeshCount];
        heatMapVisual = new HeatMapVisual_Float[totalMeshCount];
        visual = new GameObject[totalMeshCount];
        Vector3 offset = Vector3.zero;

        switch (UIController.instance.currentScene)
        {
            case "119":
                offset += new Vector3(0, 0, 0);
                break;
            case "120":
                offset += new Vector3(50, 0, 0);
                break;
            case "225":
                offset += new Vector3(100, 0, 4);
                break;
        }

        if (UIController.instance.curOption.Contains("A"))
        {
            offset += new Vector3(0, 0, 50);
        }
        else if (UIController.instance.curOption.Contains("B"))
        {
            offset += new Vector3(0, 0, 100);
        }
        else
        {
            offset += new Vector3(0, 0, 0);
        }
        for (i = 0; i < edgeMeshCount; i++)
        {
            for (j = 0; j < edgeMeshCount; j++)
            {
                int index = i * edgeMeshCount + j;
                Vector3 generatePos = new Vector3(sceneSize / 2.0f, 0, sceneSize / 2.0f);
                Vector3 originPos = new Vector3(defaultSize * cellSize * i, 0.3f, defaultSize * cellSize * j);
                visual[index] = Instantiate(visualPrefab, originPos - generatePos + offset, Quaternion.Euler(90, 0, 0));

                heatMapVisual[index] = visual[index].GetComponent<HeatMapVisual_Float>();

                grid[index] = new Grid_Float(defaultSize, defaultSize, cellSize, Vector3.zero);

                grid[index].HEAT_MAP_MAX_VALUE = max;

                /*handle part of array*/
                float[,] partArray = new float[defaultSize, defaultSize];

                int startRow = i * defaultSize;
                int startCol = j * defaultSize;

                for (int k = 0; k < defaultSize; k++)
                {
                    for (int l = 0; l < defaultSize; l++)
                    {
                        partArray[k, l] = matrixAfterRotation[startRow + k, startCol + l];
                        //partArray[k, l] = matrix[startRow + k, startCol + l];
                    }
                }

                grid[index].gridArray = partArray;
                heatMapVisual[index].SetGrid(grid[index]);
            }
        }
    }

    public void UpdateHeatmap()
    {
        if (dynamicSystem.instance.deltaTimeCounter - dynamicSystem.instance.updateHeatmapTime >= 5.0f)
        {
            int size = dynamicSystem.instance.matrixSize;
            float sceneSize = dynamicSystem.instance.sceneSize;

            //string input = File.ReadAllText(@"E:\ChengHao\Lab707\thesisCode\ChengHao\thesisCode\Human_Simulation\Assets\StreamingAssets\Simulation_Result\test33\space_usage.txt");

            float[,] matrix = new float[size, size];
            int i = 0, j = 0;
            float max = -1;
            /*
            foreach (var row in input.Split('\n'))
            {
                j = 0;
                foreach (var col in row.Split(' '))
                {
                    if (col == "") break;

                    float value = float.Parse(col);
                    if (value > max) max = value;

                    matrix[i, j] = value;

                    j++;
                }
                i++;
            }
            */
            matrix = dynamicSystem.instance.matrix;
            float[,] matrixAfterRotation = new float[size, size];

            //counterclockwise 90
            for (i = 0; i < size; i++)
            {
                for (j = 0; j < size; j++)
                {
                    matrixAfterRotation[j, size - 1 - i] = matrix[i, j];
                }
            }
            /*
            //gaussian filter
            int gaussian_filter_size = 10;
            float[,] gaussian_matrix = new float[gaussian_filter_size, gaussian_filter_size];
            float sum_gaussian_matrix = 0;

            int half_gaussian_filter_size = gaussian_filter_size / 2;
            int x, y;
            for (i = 0; i < gaussian_filter_size; i++)
            {
                for (j = 0; j < gaussian_filter_size; j++)
                {
                    x = i - half_gaussian_filter_size;
                    y = j - half_gaussian_filter_size;
                    gaussian_matrix[i, j] = Mathf.Exp(-(x * x + y * y));
                    sum_gaussian_matrix += gaussian_matrix[i, j];
                }
            }

            for (i = 0; i < gaussian_filter_size; i++) for (j = 0; j < gaussian_filter_size; j++) gaussian_matrix[i, j] /= sum_gaussian_matrix;


            for (i = 0; i < size; i++)
            {

                for (j = 0; j < size; j++)
                {
                    float tmpValue = 0;
                    sum_gaussian_matrix = 0;
                    for (int k = 0; k < gaussian_filter_size; k++)
                    {
                        for (int l = 0; l < gaussian_filter_size; l++)
                        {
                            x = k - half_gaussian_filter_size;
                            y = l - half_gaussian_filter_size;
                            if (x < 0 && i + x < 0) continue;
                            if (x > 0 && i + x >= size) continue;
                            if (y < 0 && j + y < 0) continue;
                            if (y > 0 && j + y >= size) continue;
                            tmpValue += matrixAfterRotation[i + x, j + y] * gaussian_matrix[k, l];
                            sum_gaussian_matrix += gaussian_matrix[k, l];
                        }
                    }
                    matrix[i, j] = tmpValue / sum_gaussian_matrix;
                }
            }


            */
            max = -1;
            for (i = 0; i < size; i++)
            {
                for (j = 0; j < size; j++)
                {
                    //if (matrix[i, j] > max) max = matrix[i, j];
                    if (matrixAfterRotation[i, j] > max) max = matrixAfterRotation[i, j];
                }
            }

            if (max == 0) max = 1;

            //set grid
            int edgeMeshCount = size / defaultSize;
            int totalMeshCount = edgeMeshCount * edgeMeshCount;
            float cellSize = sceneSize / size;

            for (i = 0; i < edgeMeshCount; i++)
            {
                for (j = 0; j < edgeMeshCount; j++)
                {
                    int index = i * edgeMeshCount + j;
                    heatMapVisual[index] = visual[index].GetComponent<HeatMapVisual_Float>();
                    grid[index].HEAT_MAP_MAX_VALUE = max;

                    //handle part of array
                    float[,] partArray = new float[defaultSize, defaultSize];

                    int startRow = i * defaultSize;
                    int startCol = j * defaultSize;

                    for (int k = 0; k < defaultSize; k++)
                    {
                        for (int l = 0; l < defaultSize; l++)
                        {
                            partArray[k, l] = matrixAfterRotation[startRow + k, startCol + l];
                            //partArray[k, l] = matrix[startRow + k, startCol + l];
                        }
                    }

                    grid[index].gridArray = partArray;
                    heatMapVisual[index].SetGrid(grid[index]);
                }
            }
            dynamicSystem.instance.updateHeatmapTime = dynamicSystem.instance.deltaTimeCounter;
        }
    }

    public void StaticHeatmap()
    {
        int size = dynamicSystem.instance.staticMatrix.GetLength(0);
        float sceneSize = dynamicSystem.instance.sceneSize;
        if (dynamicSystem.instance.heatmapFilename.Contains("move")) maxLimit = dynamicSystem.instance.moveMaxLimit;
        else maxLimit = dynamicSystem.instance.stayMaxLimit;
        
        float[,] matrix = dynamicSystem.instance.staticMatrix;
        int i = 0, j = 0;
        float[,] matrixAfterRotation = new float[size, size];

        //counterclockwise 90
        for (i = 0; i < size; i++)
        {
            for (j = 0; j < size; j++)
            {
                matrixAfterRotation[j, size - 1 - i] = matrix[i, j];
            }
        }

        //gaussian filter
        //int gaussian_filter_size = 10;
        bool use_Gaussian_filter = dynamicSystem.instance.useGaussian;
        int gaussian_filter_size = dynamicSystem.instance.gaussianFilterSize;
        if (use_Gaussian_filter)
        {
            float[,] gaussian_matrix = new float[gaussian_filter_size, gaussian_filter_size];
            float sum_gaussian_matrix = 0;

            int half_gaussian_filter_size = gaussian_filter_size / 2;
            int x, y;
            for (i = 0; i < gaussian_filter_size; i++)
            {
                for (j = 0; j < gaussian_filter_size; j++)
                {
                    x = i - half_gaussian_filter_size;
                    y = j - half_gaussian_filter_size;
                    gaussian_matrix[i, j] = Mathf.Exp(-(x * x + y * y));
                    sum_gaussian_matrix += gaussian_matrix[i, j];
                }
            }

            for (i = 0; i < gaussian_filter_size; i++) for (j = 0; j < gaussian_filter_size; j++) gaussian_matrix[i, j] /= sum_gaussian_matrix;


            for (i = 0; i < size; i++)
            {

                for (j = 0; j < size; j++)
                {
                    float tmpValue = 0;
                    sum_gaussian_matrix = 0;
                    for (int k = 0; k < gaussian_filter_size; k++)
                    {
                        for (int l = 0; l < gaussian_filter_size; l++)
                        {
                            x = k - half_gaussian_filter_size;
                            y = l - half_gaussian_filter_size;
                            if (x < 0 && i + x < 0) continue;
                            if (x > 0 && i + x >= size) continue;
                            if (y < 0 && j + y < 0) continue;
                            if (y > 0 && j + y >= size) continue;
                            tmpValue += matrixAfterRotation[i + x, j + y] * gaussian_matrix[k, l];
                            sum_gaussian_matrix += gaussian_matrix[k, l];
                        }
                    }
                    matrix[i, j] = tmpValue / sum_gaussian_matrix;
                }
            }
        }

        max = -1;
        for (i = 0; i < size; i++)
        {
            for (j = 0; j < size; j++)
            {
                if (matrix[i, j] > max && matrix[i, j] <= maxLimit) max = matrix[i, j];
                if (matrix[i, j] > maxLimit)
                {
                    max = maxLimit;
                    matrix[i, j] = maxLimit;
                }
            }
        }

        Debug.Log("Max: " + max);
        if (maxLimit > max && !dynamicSystem.instance.firstGenerateHeatmap)
        {
            max = maxLimit;
        }

        //change Heatmap Setting UI Max Value
        if (dynamicSystem.instance.heatmapFilename.Contains("move"))
        {
            if (dynamicSystem.instance.firstGenerateHeatmap)
            {
                HeatmapSetting.instance.originalMoveHeatmapValue.text = max.ToString("f2");
            }
            HeatmapSetting.instance.moveMaxValueInput.text = max.ToString("f2");
        }
        else
        {
            if (dynamicSystem.instance.firstGenerateHeatmap)
            {
                HeatmapSetting.instance.originalStayHeatmapValue.text = max.ToString("f2");
            }
            HeatmapSetting.instance.stayMaxValueInput.text = max.ToString("f2");
        }

        //set grid
        int edgeMeshCount = size / defaultSize;
        int totalMeshCount = edgeMeshCount * edgeMeshCount;
        float cellSize = sceneSize / size;

        grid = new Grid_Float[totalMeshCount];
        heatMapVisual = new HeatMapVisual_Float[totalMeshCount];
        visual = new GameObject[totalMeshCount];
        Vector3 offset = Vector3.zero;

        switch (UIController.instance.currentScene) 
        {
            case "119":
                offset += new Vector3(0, 0, 0);
                break;
            case "120":
                offset += new Vector3(50, 0, 0);
                break;
            case "225":
                offset += new Vector3(101.2f, 0, 3.75f);
                break;
        }

        if (UIController.instance.curOption.Contains("A"))
        {
            offset += new Vector3(0, 0, 50);
        }
        else if (UIController.instance.curOption.Contains("B"))
        {
            offset += new Vector3(0, 0, 100);
        }
        else
        {
            offset += new Vector3(0, 0, 0);
        }


        for (i = 0; i < edgeMeshCount; i++)
        {
            for (j = 0; j < edgeMeshCount; j++)
            {
                int index = i * edgeMeshCount + j;
                Vector3 generatePos = new Vector3(sceneSize / 2.0f, 0, sceneSize / 2.0f);
                Vector3 originPos = new Vector3(defaultSize * cellSize * i, 0.5f, defaultSize * cellSize * j);
                visual[index] = Instantiate(visualPrefab, originPos - generatePos + offset, Quaternion.Euler(90, 0, 0));

                heatMapVisual[index] = visual[index].GetComponent<HeatMapVisual_Float>();

                grid[index] = new Grid_Float(defaultSize, defaultSize, cellSize, Vector3.zero);

                grid[index].HEAT_MAP_MAX_VALUE = max;

                //handle part of array
                float[,] partArray = new float[defaultSize, defaultSize];

                int startRow = i * defaultSize;
                int startCol = j * defaultSize;

                for (int k = 0; k < defaultSize; k++)
                {
                    for (int l = 0; l < defaultSize; l++)
                    {
                        partArray[k, l] = matrixAfterRotation[startRow + k, startCol + l];
                        //partArray[k, l] = matrix[startRow + k, startCol + l];
                    }
                }

                grid[index].gridArray = partArray;
                heatMapVisual[index].SetGrid(grid[index]);
            }
        }

        //adjust camera position
        Vector3 heatmapCameraOriginPos = Vector3.zero;

        switch (UIController.instance.currentScene)
        {
            case "119":
                heatmapCameraOriginPos = new Vector3(-0.5f, 15, 0f);
                heatmapCamera.GetComponent<Camera>().orthographicSize = 10.48f;
                break;
            case "120":
                heatmapCameraOriginPos = new Vector3(-0.5f, 15, 0);
                heatmapCamera.GetComponent<Camera>().orthographicSize = 13f;
                break;
            case "225":
                heatmapCameraOriginPos = new Vector3(-0.5f, 15, 0f);
                heatmapCamera.GetComponent<Camera>().orthographicSize = 10.48f;
                break;
        }

        heatmapCameraOriginPos += offset;
        heatmapCamera.transform.position = heatmapCameraOriginPos;

        if (dynamicSystem.instance.heatmapFilename.Contains("stay"))
        {
            colorbar.SetActive(true);
            colorbar.transform.Rotate(0, 180, 0);
            colorbar.transform.position = new Vector3(-23.5f, 0, 0);
            TakeScreenShot();
            colorbar.transform.Rotate(0, -180, 0);
            colorbar.transform.position = Vector3.zero;
            colorbar.SetActive(false);
        }
        else
        {
            colorbar.SetActive(true);
            TakeScreenShot();
            colorbar.SetActive(false);
        }


        //do it once for original text
        if (dynamicSystem.instance.heatmapFilename.Contains("stay") && dynamicSystem.instance.firstGenerateHeatmap) dynamicSystem.instance.firstGenerateHeatmap = false;
    }

    public void DebugHeatMap()
    {
        int size = 500;
        float sceneSize = dynamicSystem.instance.sceneSize;
        string path = "D:\\ChengHao\\thesisCode\\Human_Simulation\\Assets\\StreamingAssets\\Simulation_Result\\sample14\\";
        string input = File.ReadAllText(@"D:\ChengHao\thesisCode\Human_Simulation\Assets\StreamingAssets\Simulation_Result\colorbar120\moveHeatMap.txt");

        float[,] matrix = new float[size, size];
        int i = 0, j = 0;
        max = -1;
        
        foreach (var row in input.Split('\n'))
        {
            j = 0;
            foreach (var col in row.Split(' '))
            {
                if (col == "") break;

                float value = float.Parse(col);
                /*
                if (value > max && value <= maxLimit) max = value;
                if (value > maxLimit)
                {
                    value = maxLimit;
                    max = maxLimit;
                }
                */
                if (value > max)
                {
                    max = value;
                }
                matrix[i, j] = value;

                j++;
            }
            i++;
        }
        
        float[,] matrixAfterRotation = new float[size, size];

        //counterclockwise 90
        for (i = 0; i < size; i++)
        {
            for (j = 0; j < size; j++)
            {
                matrixAfterRotation[j, size - 1 - i] = matrix[i, j];
            }
        }

        //gaussian filter
        //int gaussian_filter_size = 10;
        bool use_Gaussian_filter = dynamicSystem.instance.useGaussian;
        int gaussian_filter_size = dynamicSystem.instance.gaussianFilterSize;
        if (use_Gaussian_filter)
        {
            float[,] gaussian_matrix = new float[gaussian_filter_size, gaussian_filter_size];
            float sum_gaussian_matrix = 0;

            int half_gaussian_filter_size = gaussian_filter_size / 2;
            int x, y;
            for (i = 0; i < gaussian_filter_size; i++)
            {
                for (j = 0; j < gaussian_filter_size; j++)
                {
                    x = i - half_gaussian_filter_size;
                    y = j - half_gaussian_filter_size;
                    gaussian_matrix[i, j] = Mathf.Exp(-(x * x + y * y));
                    sum_gaussian_matrix += gaussian_matrix[i, j];
                }
            }

            for (i = 0; i < gaussian_filter_size; i++) for (j = 0; j < gaussian_filter_size; j++) gaussian_matrix[i, j] /= sum_gaussian_matrix;


            for (i = 0; i < size; i++)
            {

                for (j = 0; j < size; j++)
                {
                    float tmpValue = 0;
                    sum_gaussian_matrix = 0;
                    for (int k = 0; k < gaussian_filter_size; k++)
                    {
                        for (int l = 0; l < gaussian_filter_size; l++)
                        {
                            x = k - half_gaussian_filter_size;
                            y = l - half_gaussian_filter_size;
                            if (x < 0 && i + x < 0) continue;
                            if (x > 0 && i + x >= size) continue;
                            if (y < 0 && j + y < 0) continue;
                            if (y > 0 && j + y >= size) continue;
                            tmpValue += matrixAfterRotation[i + x, j + y] * gaussian_matrix[k, l];
                            sum_gaussian_matrix += gaussian_matrix[k, l];
                        }
                    }
                    matrix[i, j] = tmpValue / sum_gaussian_matrix;
                }
            }
        }
        /*
       max = -1;
       for (i = 0; i < size; i++)
       {
           for (j = 0; j < size; j++)
           {
               if (matrix[i, j] > max) max = matrix[i, j];
           }
       }
       */
        /*
        max = -1;
        for (i = 0; i < size; i++)
        {
            for (j = 0; j < size; j++)
            {
                if (matrix[i, j] > max && matrix[i, j] <= maxLimit) max = matrix[i, j];
                if (matrix[i, j] > maxLimit)
                {
                    max = maxLimit;
                    matrix[i, j] = maxLimit;
                }
            }
        }
        */
        //set grid
        int edgeMeshCount = size / defaultSize;
        int totalMeshCount = edgeMeshCount * edgeMeshCount;
        float cellSize = sceneSize / size;

        grid = new Grid_Float[totalMeshCount];
        heatMapVisual = new HeatMapVisual_Float[totalMeshCount];
        visual = new GameObject[totalMeshCount];
        Vector3 offset = Vector3.zero;

        switch (UIController.instance.currentScene)
        {
            case "119":
                offset += new Vector3(0, 0, 0);
                break;
            case "120":
                offset += new Vector3(50, 0, 0);
                break;
            case "225":
                offset += new Vector3(101.2f, 0, 3.75f);
                break;
        }

        if (UIController.instance.curOption.Contains("A"))
        {
            offset += new Vector3(0, 0, 50);
        }
        else if (UIController.instance.curOption.Contains("B"))
        {
            offset += new Vector3(0, 0, 100);
        }
        else
        {
            offset += new Vector3(0, 0, 0);
        }

        Debug.Log("Max: " + max);
        if (maxLimit > max) max = maxLimit;
        for (i = 0; i < edgeMeshCount; i++)
        {
            for (j = 0; j < edgeMeshCount; j++)
            {
                int index = i * edgeMeshCount + j;
                Vector3 generatePos = new Vector3(sceneSize / 2.0f, 0, sceneSize / 2.0f);
                Vector3 originPos = new Vector3(defaultSize * cellSize * i, 0.5f, defaultSize * cellSize * j);
                visual[index] = Instantiate(visualPrefab, originPos - generatePos + offset, Quaternion.Euler(90, 0, 0));

                heatMapVisual[index] = visual[index].GetComponent<HeatMapVisual_Float>();

                grid[index] = new Grid_Float(defaultSize, defaultSize, cellSize, Vector3.zero);

                grid[index].HEAT_MAP_MAX_VALUE = max;

                //handle part of array
                float[,] partArray = new float[defaultSize, defaultSize];

                int startRow = i * defaultSize;
                int startCol = j * defaultSize;

                for (int k = 0; k < defaultSize; k++)
                {
                    for (int l = 0; l < defaultSize; l++)
                    {
                        partArray[k, l] = matrixAfterRotation[startRow + k, startCol + l];
                        //partArray[k, l] = matrix[startRow + k, startCol + l];
                    }
                }

                grid[index].gridArray = partArray;
                heatMapVisual[index].SetGrid(grid[index]);
            }
        }

        //adjust camera position
        Vector3 heatmapCameraOriginPos = Vector3.zero;

        switch (UIController.instance.currentScene)
        {
            case "119":
                heatmapCameraOriginPos = new Vector3(-0.5f, 15, 0f);
                heatmapCamera.GetComponent<Camera>().orthographicSize = 10.48f;
                break;
            case "120":
                heatmapCameraOriginPos = new Vector3(-0.5f, 15, 0);
                heatmapCamera.GetComponent<Camera>().orthographicSize = 13f;
                break;
            case "225":
                heatmapCameraOriginPos = new Vector3(-0.5f, 15, 0f);
                heatmapCamera.GetComponent<Camera>().orthographicSize = 10.48f;
                break;
        }

        heatmapCameraOriginPos += offset;
        heatmapCamera.transform.position = heatmapCameraOriginPos;
        colorbar.SetActive(true);
        heatmapCamera.SetActive(true);
        int resWidth = 1600, resHeight = 1080;
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
        //string filename = dynamicSystem.instance.heatmapFilename;
        System.IO.File.WriteAllBytes(filename, bytes);
        //DestroyVisualPrefab();
        heatmapCamera.SetActive(false);
        colorbar.SetActive(false);
    }
}
