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
    //beginner mode
    public bool beginnerMode = false;
    public GameObject beginnerPanel;
    public TextMeshProUGUI beginnerModeText;

    /*save tasks*/
    bool saveCurrentTaskInfo = false;
    List<TaskContent> tasksInfoList = new List<TaskContent>();

    // objective measure
    List<ModifyTaskRecord> recordList = new List<ModifyTaskRecord>();
    public float observeTimeCounter = 0f, modifyTimeCounter = 0f;
    public int userTestListNo = 0; // 0 -> 1 2 4 3 5 6 8 7 9 10, 1 -> 2 1 3 4 6 5 7 8 10 9

    // Task content
    int taskNo = 0;
    public int testIdx = 0;
    List<int> taskOrder = new List<int>();
    GameObject currentScene;
    List<GameObject> currentExhibitList = new List<GameObject>();
    Camera mCamera;
    List<GameObject> overlayExhibit = new List<GameObject>();
    List<GameObject> idObject = new List<GameObject>();
    public GameObject markPrefab;
    public TextMeshProUGUI taskMsg;

    /*starting*/
    public GameObject startPanel;
    public Button startPanelNextBtn;

    /*taskPanel*/
    public GameObject taskPanel;
    public TextMeshProUGUI taskNOText, taskModeText;
    public Button taskNextBtn;

    /*editting*/
    public GameObject taskEditPanel;
    public TextMeshProUGUI editNOText, editModeText;
    public GameObject editImg;
    bool startModify = false;

    /*successful panel*/
    public GameObject successPanel;

    /*finish panel*/
    public GameObject finishPanel;

    // Start is called before the first frame update
    void Start()
    {
        mCamera = Camera.main;
        if (beginnerMode)
        {
            beginnerPanel.SetActive(true);
            mCamera.transform.position = new Vector3(-50, 15, 0);
            mCamera.rect = new Rect(0, 0, 1, 1);
        }
        else
        {
            startPanel.SetActive(true);
            taskEditPanel.SetActive(true);
            startPanelNextBtn.onClick.AddListener(StartPanelNextBtnOnclick);
            //LoadTaskJson();
            GenerateTaskOrder();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (beginnerMode) return;
        Check();

        //test
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

            if (Controller.instance.mode1 && Controller.instance.hasSelecetedExhibition) return;
            taskMsg.text = "Hint:\n";
            for (int i = 0; i < currentExhibitList.Count; i++)
            {
                taskMsg.text += "[" + currentExhibitList[i].name + "] ";
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
                        //Debug.Log(currentExhibitList[i].transform.eulerAngles.y); // eulerAngles: 0 ~ 360

                        diffRotY = Mathf.Abs(diffRotY);
                        //Debug.Log("before: " + diffRotY);
                        //handle 360
                        if (Mathf.Abs(diffRotY - 360) < 5) diffRotY = 0;

                        if (taskNo == 3 || taskNo == 4 || taskNo == 5 || taskNo == 6) // symmetry ex
                        {
                            if (Mathf.Abs(diffRotY - 180) < 5) diffRotY = 0;  
                        }
                        //Debug.Log("after: " + diffRotY);
                        //change rotatino to degree
                        if (diffPos.magnitude > 0.1f)
                        {
                            pass = false;
                            taskMsg.text += "Pos: X ";
                        }
                        else
                        {
                            taskMsg.text += "Pos: V ";
                        }

                        if (diffRotY > 5)
                        {
                            pass = false;
                            taskMsg.text += "Rot: X ";
                        }
                        else 
                        {
                            taskMsg.text += "Rot: V\n";
                        }
                        /*
                        if (diffPos.magnitude > 0.1f || diffRotY > 5)
                        {
                            pass = false;
                            break;
                        }
                        */
                    }
                }
            }

            if (pass)
            {
                startModify = false;
                modifyTimeCounter = Time.time - modifyTimeCounter;
                Debug.Log("complete time: " + modifyTimeCounter);
                //record complete time
                ModifyTaskRecord mtr = new ModifyTaskRecord();
                mtr.taskNo = taskNo;
                mtr.modifyTimeCounter = modifyTimeCounter;
                recordList.Add(mtr);

                testIdx++;
                if (testIdx == 10)
                {
                    WriteRecordJson();
                    finishPanel.SetActive(true);
                    return; // open finish panel
                }
                else
                {
                    successPanel.SetActive(true);
                }
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
        
        taskPanel.SetActive(true);

        currentScene = GameObject.Find("[EnvironmentsOfEachScene]/" + sceneName);
        currentExhibitList.Clear();
        foreach (GameObject mark in idObject)
        {
            Destroy(mark);
        }
        idObject.Clear();
        foreach (Transform child in currentScene.transform)
        {
            if (child.name.Contains("119"))
            {
                currentExhibitList.Add(child.gameObject);
                // add idObejct 
                Vector3 pos = new Vector3(child.position.x - 0.5f, child.position.y + 5, child.position.z + 0.5f);
                GameObject mark = Instantiate(markPrefab, pos, Quaternion.identity);
                mark.transform.SetParent(child);
                mark.transform.Find("Text").GetComponent<TextMesh>().text = child.name;
                mark.transform.Find("Text").rotation = Quaternion.Euler(90, 0, 0);
                mark.transform.Find("Text").GetComponent<TextMesh>().fontSize = 20;
                idObject.Add(mark);
            }
        }

        /*edit UI*/
        editNOText.text = taskNOText.text;
        editModeText.text = taskModeText.text;
        RawImage img = editImg.GetComponent<RawImage>();
        Texture texture = Resources.Load<Texture>("ModifySceneMaterial/modifyTask" + sceneIdx.ToString());
        img.texture = texture;

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
        // remove all selection 
        //successful isSelected status change to false
        if (Controller.instance.hasSelecetedExhibition)
        {
            Controller.instance.boundingBox.GetComponent<DrawBoundingBox>().DeleteBoundingBox();
            Controller.instance.selectedExhibition.GetComponent<ModifyExhibitForTask>().isSelected = false;
            Controller.instance.hasSelecetedExhibition = false;
        }
        taskPanel.SetActive(false);
        startModify = true;
    }
    #endregion

    #region Success panel
    public void SuccessPanelNextOnClick()
    {
        successPanel.SetActive(false);
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
        // 0 -> 1 2 4 3 5 6 8 7 9 10, 1 -> 2 1 3 4 6 5 7 8 10 9
        int mode = 0;
        if (userTestListNo == 0) mode = 0;
        else mode = 1;
        for (int i = 1; i < 6; i++)
        {
            if(mode == 0)
            {
                taskOrder.Add(2 * i - 1);
                taskOrder.Add(2 * i);
                mode = 1;
            }
            else
            {
                taskOrder.Add(2 * i);
                taskOrder.Add(2 * i - 1);
                mode = 0;
            }
        }
    }
    #endregion

    public void BeginnerMode1Btn()
    {
        Controller.instance.mode1 = true;
        Controller.instance.mode2 = false;
        beginnerModeText.text = "Mode 1";
    }

    public void BeginnerMode2Btn()
    {
        Controller.instance.mode1 = false;
        Controller.instance.mode2 = true;
        beginnerModeText.text = "Mode 2";
    }
}
