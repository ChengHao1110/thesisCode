using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LitJson;
using System.Text;
using System.IO;

public class ModifyTaskRecord
{
    public int taskNo;
    public double modifyTimeCounter;
}

public class SystemTask : MonoBehaviour
{
    /*save tasks*/
    bool saveCurrentTaskInfo = false;
    List<TaskContent> tasksInfoList = new List<TaskContent>();


    // objective measure
    List<ModifyTaskRecord> recordList = new List<ModifyTaskRecord>();
    public float observeTimeCounter = 0f, modifyTimeCounter = 0f;

    // Task content
    int taskNo = 0;
    public int testIdx = 0;
    List<int> taskOrder = new List<int>();
    GameObject currentScene;
    List<GameObject> currentExhibitList = new List<GameObject>();
    Camera mCamera;
    List<GameObject> overlayExhibit = new List<GameObject>();

    /*starting*/
    public GameObject startPanel;
    public Button startPanelNextBtn;

    /*taskPanel*/
    public GameObject taskPanel;
    public TextMeshProUGUI taskNOText, taskModeText;
    public GameObject taskImg;
    public Button taskNextBtn;

    /*editting*/
    public TextMeshProUGUI editNOText, editModeText;
    public GameObject editImg;
    bool startModify = false;

    /*successful panel*/
    public GameObject successPanel;

    // Start is called before the first frame update
    void Start()
    {
        startPanelNextBtn.onClick.AddListener(StartPanelNextBtnOnclick);
        mCamera = Camera.main;
        LoadTaskJson();
        GenerateTaskOrder();
    }

    // Update is called once per frame
    void Update()
    {
        Check();
        if (Input.GetKeyDown(KeyCode.N))
        {
            OpenTaskPanel(taskOrder[testIdx]);
        }
    }

    public void StartPanelNextBtnOnclick()
    {
        startPanel.SetActive(false);
        OpenTaskPanel(taskOrder[testIdx]);
        //for (int i = 0; i < 5; i++) TakePicture(i);
        //SaveCurrentTaskInfo();
    }



    /*Editting function*/
    void Check()
    {
        if (startModify)
        {
            bool pass = true;
            
            for (int i = 0; i < currentExhibitList.Count; i++)
            {
                int sceneIdx = Mathf.CeilToInt((float)taskNo / 2);
                string sceneName = sceneIdx.ToString() + "b";
                GameObject correctScene = GameObject.Find("[EnvironmentsOfEachScene]/" + sceneName);
                foreach (Transform ex in correctScene.transform)
                {
                    if (currentExhibitList[i].name == ex.name)
                    {
                        Vector2 diffPos;
                        if (taskNo % 2 == 0)
                        {
                            diffPos.x = currentExhibitList[i].transform.position.x + 25 - ex.position.x;
                            diffPos.y = currentExhibitList[i].transform.position.z - ex.position.z;
                        }
                        else
                        {
                            diffPos.x = currentExhibitList[i].transform.position.x + 50 - ex.transform.position.x;
                            diffPos.y = currentExhibitList[i].transform.position.z - ex.transform.position.z;
                        }
                        float diffRotY = currentExhibitList[i].transform.eulerAngles.y - ex.eulerAngles.y;

                        //對稱性質的展品
                        diffRotY = Mathf.Abs(diffRotY);
                        //handle 360
                        if (diffRotY > 180) diffRotY -= 360;
                        //Debug.Log(diffRotY);
                        //change rotatino to degree
                        if (diffPos.magnitude > 0.1f || diffRotY > 5)
                        {
                            pass = false;
                            break;
                        }
                    }
                }
            }

            if (pass)
            {
                successPanel.SetActive(true);
                startModify = false;
                modifyTimeCounter = Time.time - modifyTimeCounter;
                Debug.Log("complete time: " + modifyTimeCounter);
                //record complete time
                ModifyTaskRecord mtr = new ModifyTaskRecord();
                mtr.taskNo = taskNo;
                mtr.modifyTimeCounter = modifyTimeCounter;
                recordList.Add(mtr);
                //successful isSelected status change to false
                if (Controller.instance.hasSelecetedExhibition)
                {
                    Controller.instance.boundingBox.GetComponent<DrawBoundingBox>().DeleteBoundingBox();
                    Controller.instance.selectedExhibition.GetComponent<ModifyExhibitForTask>().isSelected = false;
                    Controller.instance.hasSelecetedExhibition = false;
                }
                testIdx++;
                
            }
        }


    }

    #region Task panel
    void OpenTaskPanel(int order)
    {
        /*fill up task panel*/
        taskNo = order;
        taskNOText.text = "Task " + order.ToString();
        /*Scene*/
        int sceneIdx = Mathf.CeilToInt((float)order / 2);
        string sceneName = sceneIdx.ToString() + "a";

        Controller.instance.firstEdit = false;
        if (order % 2 == 0)
        {
            taskModeText.text = "Mode: only mouse";
            /*Controller*/
            Controller.instance.mode1 = false;
            Controller.instance.mode2 = true;
            sceneName += "a";
            /*Camera*/
            mCamera.transform.position = new Vector3(25, 15, (sceneIdx - 1) * 50);
        }
        else
        {
            taskModeText.text = "Mode: mouse + keyboard";
            /*Controller*/
            Controller.instance.mode1 = true;
            Controller.instance.mode2 = false;
            /*Camera*/
            mCamera.transform.position = new Vector3(0, 15, (sceneIdx - 1) * 50);
        }
        RawImage img = taskImg.GetComponent<RawImage>();
        
        Texture texture = Resources.Load<Texture>("ModifySceneMaterial/modifyTask" + sceneIdx.ToString());
        img.texture = texture;
        taskPanel.SetActive(true);


        
        currentScene = GameObject.Find("[EnvironmentsOfEachScene]/" + sceneName);
        currentExhibitList.Clear();
        foreach (Transform child in currentScene.transform)
        {
            if (child.name.Contains("119"))
            {
                currentExhibitList.Add(child.gameObject);
            }
        }

        /*edit UI*/
        editNOText.text = taskNOText.text;
        editModeText.text = taskModeText.text;
        img = editImg.GetComponent<RawImage>();
        img.texture = texture;

        /*measure*/
        observeTimeCounter = Time.time;

        /*overlay exhibit*/
        sceneName = sceneIdx.ToString() + "b";
        currentScene = GameObject.Find("[EnvironmentsOfEachScene]/" + sceneName);
        foreach(GameObject ex in overlayExhibit)
        {
            Destroy(ex);
        }
        overlayExhibit.Clear();
        foreach (Transform child in currentScene.transform)
        {
            if (child.name.Contains("119"))
            {
                GameObject overlay = Instantiate(child.gameObject);
                Vector3 pos = new Vector3();
                if (order % 2 == 0)
                {
                    pos.x = child.position.x - 25;
                    pos.y = child.position.y;
                    pos.z = child.position.z;
                }
                else
                {
                    pos.x = child.position.x - 50;
                    pos.y = child.position.y;
                    pos.z = child.position.z;
                }


                overlay.transform.position = pos;
                //remove overlay modify script
                overlay.transform.tag = "Untagged";
                Destroy(overlay.GetComponent<ModifyExhibitForTask>());
                foreach (Transform c in overlay.transform)
                {
                    //Debug.Log(c.name);
                    Collider collider = c.GetComponent <Collider>();
                    if (collider != null) collider.enabled = false;
                    if (c.name == "center" || c.name == "range" || c.name == "BoundingBoxCube" || c.name == "ViewPoint")
                    {
                        continue;
                    }
                    Color oriColor = new Color();
                    if (c.childCount > 0)
                    {
                        foreach (Transform cc in c.transform)
                        {
                            collider = cc.GetComponent<Collider>();
                            if (collider != null) collider.enabled = false;
                            if (cc.name == "BoundingBoxCube")
                            {
                                continue;
                            }
                            oriColor = cc.GetComponent<MeshRenderer>().material.color;
                            cc.GetComponent<MeshRenderer>().material.color = new Color(oriColor.r, oriColor.g, oriColor.b, 0.05f);
                        }
                    }
                    else
                    {
                        oriColor = c.GetComponent<MeshRenderer>().material.color;
                        c.GetComponent<MeshRenderer>().material.color = new Color(oriColor.r, oriColor.g, oriColor.b, 0.05f);
                    }
                }
                overlayExhibit.Add(overlay);
            }
        }
    }

    public void TaskPanelNextOnClick()
    {
        observeTimeCounter = Time.time - observeTimeCounter;
        taskPanel.SetActive(false);
        startModify = true;
    }
    #endregion

    #region Success panel
    public void SuccessPanelNextOnClick()
    {
        successPanel.SetActive(false);
        if (testIdx == 10)
        {
            WriteRecordJson(); 
            return; // open finish panel
        }
        OpenTaskPanel(taskOrder[testIdx]);
    }
    #endregion

    #region Task prepare
    void SaveCurrentTaskInfo()
    {
        for (int i = 0; i < 5; i++)
        {
            SaveExhibitInfo(i + 1, 1);
            SaveExhibitInfo(i + 1, 2);
        }
        WriteTasksJson();
    }

    void TakePicture(int i)
    {
        int resWidth = 1080, resHeight = 1080;
        mCamera.transform.position = new Vector3(50, 15, i * 50);
        Camera camera = mCamera.GetComponent<Camera>();
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
        string filename = Application.dataPath + "/screenshots/" + "modifyTask" + (i + 1).ToString() + ".png";
        System.IO.File.WriteAllBytes(filename, bytes);
    }

    void SaveExhibitInfo(int i, int mode)
    {
        // i start from 1
        //mode1
        TaskContent tc = new TaskContent();
        tc.taskNo = 2 * (i - 1) + mode;
        tc.taskImg = "modifyTask" + i.ToString() + ".png";
        tc.mode = mode;
        string sceneName = i.ToString() + "b";
        GameObject scene = GameObject.Find("[EnvironmentsOfEachScene]/" + sceneName); 
        foreach (Transform child in scene.transform)
        {
            if (child.name.Contains("119"))
            {
                ExhibitInfo exInfo = new ExhibitInfo();
                exInfo.exName = child.name;
                exInfo.posX = child.position.x;
                exInfo.posY = child.position.y;
                exInfo.posZ = child.position.z;
                exInfo.rotX = child.eulerAngles.x;
                exInfo.rotY = child.eulerAngles.y;
                exInfo.rotZ = child.eulerAngles.z;
                tc.exInfoList.Add(exInfo);
            }
        }
        tasksInfoList.Add(tc);
    }

    void WriteTasksJson()
    {
        string path = Application.dataPath + "/modifyTasks.json";
        StringBuilder sb = new StringBuilder();
        JsonWriter writer = new JsonWriter(sb);
        writer.PrettyPrint = true;
        JsonMapper.ToJson(tasksInfoList, writer);
        Debug.Log(sb.ToString());
        string outputJsonStr = sb.ToString();
        System.IO.File.WriteAllText(path, outputJsonStr);
    }

    void LoadTaskJson()
    {
        string path = Application.streamingAssetsPath + "/ModifyTasks/modifyTasks.json";
        string tmpJsonDataStr = File.ReadAllText(path);
        tasksInfoList = JsonMapper.ToObject<List<TaskContent>>(tmpJsonDataStr);
    }

    void WriteRecordJson()
    {
        //date
        System.DateTime dt = System.DateTime.Now;
        string date = dt.Year + "-" + dt.Month + "-" + dt.Day + "-" + dt.Hour + "-" + dt.Minute + "-" + dt.Second;
        string path = Application.streamingAssetsPath + "/ModifyTasks/test_" + date + ".json";
        StringBuilder sb = new StringBuilder();
        JsonWriter writer = new JsonWriter(sb);
        writer.PrettyPrint = true;
        JsonMapper.ToJson(recordList, writer);
        string outputJsonStr = sb.ToString();
        System.IO.File.WriteAllText(path, outputJsonStr);
    }

    void GenerateTaskOrder()
    {
        int ran = Random.Range(0, 2);
        for (int i = 1; i < 6; i++)
        {
            if(ran == 1)
            {
                taskOrder.Add(2 * i - 1);
                taskOrder.Add(2 * i);
            }
            else
            {
                taskOrder.Add(2 * i);
                taskOrder.Add(2 * i - 1);
            }
        }
    }
    #endregion
}
