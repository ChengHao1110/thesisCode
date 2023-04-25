using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using UnityEditor;
using TMPro;
using AnotherFileBrowser.Windows;

public class cameraPerScene
{
    public GameObject sceneButton;
    public GameObject mainCamera;
    public GameObject minimapCamera;
}

public class cameraSetting 
{
    //main camera
    public Vector3 originalMainPos;
    public float originalMainFOV;

    //minimap camera
    public Vector3 originalMiniPos;
    public float originalMiniSize;

}

//exhibition info class
public class exhibitionInfo
{
    public string name;
    public double posX, posY, posZ;
    public double rotX, rotY, rotZ;
    public double sclX, sclY, sclZ;
    public double capacityMax, capacityMean, capacityMedian;
    public double stayTimeMax, stayTimeMin, stayTimeMean, stayTimeStd;
    public double chooseProbability, repeatChooseProbability;
}

public class exhibitionsInScene 
{
    public string sceneName;
    public List<exhibitionInfo> exhibitionsInfo;
}


public partial class UIController : PersistentSingleton<UIController>
{
    /*Message Panel*/
    public GameObject msgPanel;
    public TextMeshProUGUI msgType, msgContent;

    /* top left dashBoard */
    public GameObject EnvironmentsRoot;
    public GameObject realDataModeUI, simulationModeUI;  // for switching mode
    public GameObject modeButtonReal, modeButtonSimulate;
    public GameObject sceneButton119, sceneButton120, sceneButton225;
    public GameObject cameraPos_119, cameraPos_120, cameraPos_225;
    public GameObject miniCameraPos_119, miniCameraPos_120, miniCameraPos_225;
    public GameObject DashBoard;
    public GameObject DashBoardSwitch;

    /*right panel in simulation mode UI*/
    public GameObject settingUIBoard;
    public GameObject modifyController;
    public GameObject saveLoadPanel;
    public GameObject heatmapSettingPanel;
    public GameObject simpleUISettingPanel;
    public GameObject menuPanel;
    public GameObject replayPanel;
    public GameObject replayMode;
    public Dictionary<string, bool> isPanelUsing = new Dictionary<string, bool>();
    public bool isOriginalScene;

    public List<string> allScene;
    public Dictionary<string, cameraPerScene> cameras;
    public string currentScene = "119";
    public string currentMode = "DynamicSimulation"; // "RealDataVisualization"; // 
    public List<string> curSceneOptions;
    public string curOption;
    public List<Vector3> cameraPos;
    public List<Vector3> miniCameraPos;

    /* real data mode UI */
    public GameObject realVideoSelector_content;  // content of a scroll view
    public Button realVideoSelector_contentPrefab;  // prefab to creating in content

    /* Others */
    Color32 selectedColor = new Color32(120, 194, 196, 200);
    Color32 unSelectedColor = new Color32(255, 255, 255, 255);

    /*camera detail setting info*/
    public Dictionary<string, cameraSetting> camerasSetting;
    public RenderTexture minimapRT;

    /*buttom board buttons*/
    public Button oriButton, aButton, bButton;

    List<GameObject> cameraOptionsBtns = new List<GameObject>();
    public GameObject cameraOptionsBtnPrefab;
    public GameObject cameraOptionsBtnsParent;

    
    void Start()
    {
        //ui setting
        isPanelUsing.Add("modifyScene", false);
        isPanelUsing.Add("saveload", false);
        isPanelUsing.Add("heatmapSetting", false);
        isPanelUsing.Add("simpleUISetting", true); //basic Setting
        isPanelUsing.Add("replayMode", false);

        //camera setting
        cameras = new Dictionary<string, cameraPerScene>
        {
            ["119"] = new cameraPerScene { sceneButton = sceneButton119, mainCamera = cameraPos_119, minimapCamera = miniCameraPos_119 },
            ["120"] = new cameraPerScene { sceneButton = sceneButton120, mainCamera = cameraPos_120, minimapCamera = miniCameraPos_120 },
            ["225"] = new cameraPerScene { sceneButton = sceneButton225, mainCamera = cameraPos_225, minimapCamera = miniCameraPos_225 }
        };

        allScene = new List<string> { "119", "120", "225" };

        camerasSetting = new Dictionary<string, cameraSetting>();
        for (int i = 0; i < allScene.Count; i++)
        {
            cameraSetting cs = new cameraSetting();

            GameObject main = cameras[allScene[i]].mainCamera;
            cs.originalMainPos = main.transform.position;
            cs.originalMainFOV = main.GetComponent<Camera>().fieldOfView;

            GameObject mini = cameras[allScene[i]].minimapCamera;
            cs.originalMiniPos = mini.transform.position;
            cs.originalMiniSize = mini.GetComponent<Camera>().orthographicSize;

            camerasSetting.Add(allScene[i], cs);
        }

        foreach (string scene in allScene)
        {
            loadSettingsFromJson(scene);
        }

        setRealVideosSelector();
        switchMode(currentMode);
        setScene(currentScene);        
    }

    private void Update()
    {
        
        if(countDownTime > 0)
        {
            countDownTime -= Time.deltaTime;
        }
        else
        {
            //successLog.text = "";
        }
        
    }

    //modifyScene Button Function
    #region modifySceneButton Functions
    /*modifyscene button function*/
    /*main camera and mini camera swap*/
    #region Panel Open Function
    public void ModifySceneButton()
    {
        isPanelUsing["modifyScene"] = !isPanelUsing["modifyScene"];
        if (isPanelUsing["modifyScene"])
        {
            if (!curOption.Contains("A") && !curOption.Contains("B"))
            {
                isOriginalScene = true;
                ShowMsgPanel("Notice", "You cannot edit the exhibitions in the original scene, but you can edit the information of the exhibitions.");
                //ShowMsgPanel("Warning", "Please choose A or B option of the scene.");
                //return;
            }
            else
            {
                isOriginalScene = false;
            }
            int childCount = simulationModeUI.transform.childCount;
            modifyController.transform.SetSiblingIndex(childCount - 1);
            CloseOtherPanelOperation("modifyScene");
        }
        else
        {
            BackToSetting();

        }
        menuPanel.SetActive(false);
        ModifySceneCameraSwap();
    }

    void Swap(string sceneHeadName, int idx, Vector3 minimapCameraPos, float miniCameraSize, float mainCameraFOV)
    {
        Camera mainCamera;
        Camera minimapCamera;
        //RenderTexture minimapRT = (RenderTexture)AssetDatabase.LoadAssetAtPath("Assets/Resources/Materials/miniMapRenderTexture.renderTexture", typeof(RenderTexture));
        Vector3 zOffset = new Vector3(0, 0, 50 * idx);
        mainCamera = cameras[sceneHeadName].mainCamera.GetComponent<Camera>();
        minimapCamera = cameras[sceneHeadName].minimapCamera.GetComponent<Camera>();
        Debug.Log(idx);
        if (isPanelUsing["modifyScene"])
        {
            //DashBoard.SetActive(false);
            DashBoardSwitch.GetComponent<switchToggle>().toggle.isOn = false;
            DashBoardSwitch.GetComponent<switchToggle>().SwitchOn(false);
            cameras[sceneHeadName].mainCamera.tag = "Untagged";
            cameras[sceneHeadName].minimapCamera.tag = "MainCamera";
            mainCamera.targetTexture = minimapRT;
            minimapCamera.targetTexture = null;
            //change minimap camera
            Debug.Log(minimapCameraPos);
            Debug.Log(zOffset);
            cameras[sceneHeadName].minimapCamera.transform.position = minimapCameraPos + zOffset;
            minimapCamera.orthographicSize = miniCameraSize;
            cameras[sceneHeadName].minimapCamera.GetComponent<minimapCameraController>().Initial();
            //change main camera
            mainCamera.fieldOfView = mainCameraFOV;       
        }
        else
        {
            //DashBoard.SetActive(true);
            DashBoardSwitch.GetComponent<switchToggle>().toggle.isOn = true;
            DashBoardSwitch.GetComponent<switchToggle>().SwitchOn(true);
            //reset to default
            cameras[sceneHeadName].mainCamera.tag = "MainCamera";
            cameras[sceneHeadName].minimapCamera.tag = "Untagged";
            mainCamera.targetTexture = null;
            minimapCamera.targetTexture = minimapRT;
            //change minimap camera
            cameras[sceneHeadName].minimapCamera.transform.position = camerasSetting[sceneHeadName].originalMiniPos + zOffset;
            minimapCamera.orthographicSize = camerasSetting[sceneHeadName].originalMiniSize;
            //change main camera
            mainCamera.fieldOfView = camerasSetting[sceneHeadName].originalMainFOV;
        }
        //save information for each camera
        //main
        Rect mainVPRect = mainCamera.rect;
        float mainDepth = mainCamera.depth;
        //minimap
        Rect miniVPRect = minimapCamera.rect;
        float miniDepth = minimapCamera.depth;
        //swap
        minimapCamera.rect = mainVPRect;
        minimapCamera.depth = mainDepth;
        mainCamera.rect = miniVPRect;
        mainCamera.depth = miniDepth;
    }

    void ModifySceneCameraSwap()
    {
        string sceneHeadName = "119"; //default 119
        int idx = 0;
        for (int i = 0; i < allScene.Count; i++)
        {
            if (curOption.Contains(allScene[i]))
            {
                sceneHeadName = allScene[i];
            }
            if (curOption.Contains("A")) idx = 1;
            if (curOption.Contains("B")) idx = 2;
        }

        switch (sceneHeadName)
        {
            case "119":
                {
                    Swap(sceneHeadName, idx, new Vector3(-0.4f, 15, -0.2f), 7.71f, 70);
                }
                break;
            case "120":
                {
                    Swap(sceneHeadName, idx, new Vector3(50.89f, 15, -0.3f), 10.5f, 85);
                }
                break;
            case "225":
                {
                    Swap(sceneHeadName, idx, new Vector3(101.7f, 15, 3.56f), 8.5f, 80);
                }
                break;
        }
    }
    #endregion

    //Save/Load Button Functions
    public void SaveLoadButton()
    {
        isPanelUsing["saveload"] = !isPanelUsing["saveload"];
        if (isPanelUsing["saveload"])
        {
            int childCount = simulationModeUI.transform.childCount;
            saveLoadPanel.transform.SetSiblingIndex(childCount - 1);
            CloseOtherPanelOperation("saveload");
            DashBoardSwitch.GetComponent<switchToggle>().toggle.isOn = true;
            DashBoardSwitch.GetComponent<switchToggle>().SwitchOn(true);
        }
        else
        {
            BackToSetting();
        }
        menuPanel.SetActive(false);
    }

    //Heatmap Setting Panel
    public void HeatmapSettningPanel()
    {
        isPanelUsing["heatmapSetting"] = !isPanelUsing["heatmapSetting"];
        if (isPanelUsing["heatmapSetting"])
        {
            int childCount = simulationModeUI.transform.childCount;
            heatmapSettingPanel.transform.SetSiblingIndex(childCount - 1);
            CloseOtherPanelOperation("heatmapSetting");
            DashBoardSwitch.GetComponent<switchToggle>().toggle.isOn = true;
            DashBoardSwitch.GetComponent<switchToggle>().SwitchOn(true);
        }
        else
        {
            BackToSetting();
        }
        menuPanel.SetActive(false);
    }

    //replay panel
    public void ReplayPanel()
    {
        isPanelUsing["replayMode"] = !isPanelUsing["replayMode"];
        if (isPanelUsing["replayMode"])
        {
            int childCount = simulationModeUI.transform.childCount;
            replayPanel.transform.SetSiblingIndex(childCount - 1);
            CloseOtherPanelOperation("replayMode");
            //DashBoard.SetActive(false);
            DashBoardSwitch.GetComponent<switchToggle>().toggle.isOn = false;
            DashBoardSwitch.GetComponent<switchToggle>().SwitchOn(false);
            replayMode.SetActive(true);
        }
        else
        {
            BackToSetting();
        }
        menuPanel.SetActive(false);
    }
    public void BackToSetting()
    {
        CloseOtherPanelOperation("UISetting");
        //DashBoard.SetActive(true);
        if (isPanelUsing["simpleUISetting"])
        {
            GoToBasicSetting();
        }
        else
        {
            GoToAdvancedSetting();
        }
    }

    //Go To Advanced Setting & Go To Basic Setting
    public void GoToAdvancedSetting() 
    {
        isPanelUsing["simpleUISetting"] = false;
        int childCount = simulationModeUI.transform.childCount;
        settingUIBoard.transform.SetSiblingIndex(childCount - 1);
    }

    public void GoToBasicSetting()
    {
        isPanelUsing["simpleUISetting"] = true;
        int childCount = simulationModeUI.transform.childCount;
        simpleUISettingPanel.transform.SetSiblingIndex(childCount - 1);
    }

    public void OpenMenuPanel()
    {
        menuPanel.SetActive(true);
    }

    public void CloseMenuPanel()
    {
        menuPanel.SetActive(false);
    }

    public void CloseOtherPanelOperation(string currentMode)
    {
        for (int i = 0; i < isPanelUsing.Count; i++)
        {
            if (isPanelUsing.ElementAt(i).Key != currentMode) PanelCloseOperation(isPanelUsing.ElementAt(i).Key);
        }
    }

    public void PanelCloseOperation(string panelName)
    {
        if (panelName == "simpleUISetting") return;
        if (panelName == "modifyScene" && isPanelUsing["modifyScene"])
        {
            isPanelUsing["modifyScene"] = false;
            ModifySceneCameraSwap();
        }
        if(panelName == "replayMode")
        {
            replayPanel.transform.SetSiblingIndex(0);
            replayMode.SetActive(false);
        }
        isPanelUsing[panelName] = false;
    }
    #endregion
    /* update when mode or scene change*/
    public void setScene(string sceneName)
    {
        if (isPanelUsing["modifyScene"])
        {
            ShowMsgPanel("Warning", "Cannot change mode & scene in modify scene operation!");
            return;
        }
        /* single selection of UI */
        Vector3 cameraPosition = Vector3.zero, miniCameraPosition = Vector3.zero;
        foreach (KeyValuePair<string, cameraPerScene> camera in cameras)
        {
            if (camera.Key == sceneName)
            {
                camera.Value.sceneButton.GetComponent<Image>().color = selectedColor;
                camera.Value.mainCamera.SetActive(true);
                camera.Value.minimapCamera.SetActive(true);
                cameraPosition = camera.Value.mainCamera.transform.position;
                miniCameraPosition = camera.Value.minimapCamera.transform.position;
                dynamicSystem.instance.halfW = allSceneSettings[sceneName].screenSize_w / 2;
                dynamicSystem.instance.halfH = allSceneSettings[sceneName].screenSize_h / 2;
                realDataDrawTest.instance.halfW = allSceneSettings[sceneName].screenSize_w / 2;
                realDataDrawTest.instance.halfH = allSceneSettings[sceneName].screenSize_h / 2;
            }
            else
            {
                camera.Value.sceneButton.GetComponent<Image>().color = unSelectedColor;
                camera.Value.mainCamera.SetActive(false);
                camera.Value.minimapCamera.SetActive(false);
            }
        }               
        currentScene = sceneName;        
        setRealVideosSelector();
        changeSettings();
        dynamicSystem.instance.currentSceneSettings = allSceneSettings[currentScene];
        dynamicSystem.instance.setVisibleAllRange_exhibit(dynamicSystem.instance.exhibitRangeToggle.isOn);

        curSceneOptions.Clear();
        curSceneOptions = new List<string> { currentScene, currentScene + "_A", currentScene + "_B" };

        cameraPos = new List<Vector3>();
        miniCameraPos = new List<Vector3>();
        for (int i = 0; i < 3; i++)
        {
            Vector3 tmpPos = cameraPosition;
            tmpPos.z += i * 50;
            cameraPos.Add(tmpPos);

            Vector3 tmpPos2 = miniCameraPosition;
            tmpPos2.z += i * 50;
            miniCameraPos.Add(tmpPos2);
        }

        //set realtime heatmap
        switch (sceneName)
        {
            case "119":
                dynamicSystem.instance.matrixSize = 500;
                dynamicSystem.instance.gaussianFilterSize = 10;
                dynamicSystem.instance.sceneSize = 22;
                break;
            case "120":
                dynamicSystem.instance.matrixSize = 500;
                dynamicSystem.instance.gaussianFilterSize = 10;
                dynamicSystem.instance.sceneSize = 26;
                break;
            case "225":
                dynamicSystem.instance.matrixSize = 500;
                dynamicSystem.instance.gaussianFilterSize = 10;
                dynamicSystem.instance.sceneSize = 20;
                break;
        }
        
        changeOption(0);
    }

    public void changeOption(int index)
    {
        if (!isPanelUsing["modifyScene"])
        {
            // original : 0, A: 1, B: 2
            curOption = curSceneOptions[index];
            //cameras[currentScene].mainCamera.transform.position = cameraPos[index];
            CameraController cc = Camera.main.GetComponent<CameraController>();
            cc.SetCamera(cc.cameras[currentScene][0], index);
            cameras[currentScene].minimapCamera.transform.position = miniCameraPos[index];
            

            switch (index) 
            {
                case 0:
                    oriButton.GetComponent<Image>().color = selectedColor;
                    aButton.GetComponent<Image>().color = unSelectedColor;
                    bButton.GetComponent<Image>().color = unSelectedColor;
                    break;
                case 1:
                    oriButton.GetComponent<Image>().color = unSelectedColor;
                    aButton.GetComponent<Image>().color = selectedColor;
                    bButton.GetComponent<Image>().color = unSelectedColor;
                    break;
                case 2:
                    oriButton.GetComponent<Image>().color = unSelectedColor;
                    aButton.GetComponent<Image>().color = unSelectedColor;
                    bButton.GetComponent<Image>().color = selectedColor;
                    break;
            }
            SetCameraOptionsButtons(index);
            ChangeCameraOptionBtnColor(0);
            if (currentMode == "RealDataVisualization")
            {
                realDataDrawTest.instance.cleanBeforeRead();
            }
            else  // DynamicSimulation
            {
                dynamicSystem.instance.cleanPeopleBeforeGenerate();
            }
        }
        else
        {
            ShowMsgPanel("Warning", "Cannot change layout in modify scene function!");
        }
    }

    public void SetCameraOptionsButtons(int index)
    {
        if (cameraOptionsBtns.Count > 0)
        {
            for(int i = 0; i < cameraOptionsBtns.Count; i++)
            {
                Destroy(cameraOptionsBtns[i]);
            }
            cameraOptionsBtns.Clear();
        }
        CameraController cc = Camera.main.GetComponent<CameraController>();
        for (int i = 0; i < cc.cameraList.Count; i++)
        {
            GameObject go = (GameObject)Instantiate(cameraOptionsBtnPrefab);
            go.transform.SetParent(cameraOptionsBtnsParent.transform);
            Button btn = go.GetComponent<Button>();
            TextMeshProUGUI text = go.GetComponentInChildren<TextMeshProUGUI>();
            text.text = (i + 1).ToString();
            int id = i;
            btn.onClick.AddListener(() => { cc.SetCameraByBtn(cc.cameras[currentScene][id], index, id);
                                            ChangeCameraOptionBtnColor(id);
                                          });
            cameraOptionsBtns.Add(go);
        }
    }
    void ChangeCameraOptionBtnColor(int id)
    {
        for(int i = 0; i < cameraOptionsBtns.Count; i++)
        {
            Button btn = cameraOptionsBtns[i].GetComponent<Button>();
            if (i == id) btn.GetComponent<Image>().color = selectedColor;
            else btn.GetComponent<Image>().color = unSelectedColor;
        }
    }
    public void changeSettings()
    {
        tmpSaveUISettings = allSceneSettings[currentScene].customUI.copy();
        loadSettingToUI(tmpSaveUISettings);
    }
    
    public void switchMode(string mode)
    {
        if (isPanelUsing["modifyScene"])
        {
            ShowMsgPanel("Warning", "Cannot change mode & scene in modify scene operation!");
            return;
        }
        if (mode == "RealDataVisualization")
        {
            changeOption(0);
            setState(realDataModeUI, true);
            modeButtonReal.GetComponent<Image>().color = selectedColor;

            setState(simulationModeUI, false);
            modeButtonSimulate.GetComponent<Image>().color = unSelectedColor;
            dynamicSystem.instance.cleanPeopleBeforeGenerate();
        }
        else  // DynamicSimulation
        {
            setState(simulationModeUI, true);
            modeButtonSimulate.GetComponent<Image>().color = selectedColor;
            dynamicSystem.instance.setVisibleAllRange_exhibit(dynamicSystem.instance.exhibitRangeToggle.isOn);

            setState(realDataModeUI, false);
            modeButtonReal.GetComponent<Image>().color = unSelectedColor;
            realDataDrawTest.instance.cleanBeforeRead();
        }
    }
        
    void setState(GameObject obj, bool state)
    {
        obj.SetActive(state);
        foreach (Transform child in obj.transform)
        {
            // Debug.Log(child.name);
            child.gameObject.SetActive(state);
        }
    }
           
    /* save and load Setting (interact with UIs) */
    public void loadSettingsFromUI() 
    {
        string error = checkSettings();
        if(error == "")
        {
            //successLog.text = "Saved!";
            countDownTime = 5;
            allSceneSettings[currentScene].customUI = tmpSaveUISettings.copy();

            string path = Application.streamingAssetsPath + "/SettingsJson/";
            DateTime now = DateTime.Now;
            String dateStr = now.Year.ToString() +
                                 now.Month.ToString("D2") +
                                 now.Day.ToString("D2") + "_" +
                                 now.Hour.ToString("D2") +
                                 now.Minute.ToString("D2") +
                                 now.Second.ToString("D2");
            string outputFileName = "unitySettings_" + currentScene + "_" + dateStr + ".json";
            StringBuilder sb = new StringBuilder();
            JsonWriter writer = new JsonWriter(sb);
            writer.PrettyPrint = true;
            JsonMapper.ToJson(tmpSaveUISettings, writer);
            string outputJsonStr = sb.ToString();
            // Debug.Log(outputJsonStr);
            System.IO.File.WriteAllText(path + outputFileName, outputJsonStr);

            dynamicSystem.instance.currentSceneSettings = allSceneSettings[currentScene];
        }
        else
        {
            //successLog.text = "";
        }
    }

    public void changeSettingsInit()  // change setting back to init setting
    {
        /*
        tmpSaveUISettings = allSceneSettings[currentScene].oriJson.copy();
        loadSettingToUI(tmpSaveUISettings);
        allSceneSettings[currentScene].customUI = tmpSaveUISettings.copy();
        */
        string dirPath = Application.streamingAssetsPath + "/SettingsJson/Default Settings";
        string[] file = Directory.GetFiles(dirPath, "unitySettings_" + currentScene + "*.json");
        tmpSaveUISettings = JsonMapper.ToObject<UISettings>(File.ReadAllText(file[0]));
        loadSettingToUI(tmpSaveUISettings);
        allSceneSettings[currentScene].customUI = tmpSaveUISettings.copy();

        ShowMsgPanel("Success", "UI setting is reset.");
    }

    void loadSettingToUI(UISettings inputSetting) // show setting data on UI
    {
        /* Global */        
        adultPercentSlider.value = (float)inputSetting.UI_Global.adultPercentage * 100;  // 0.xx -> %
        changeAdultPercent();

        //handle load ui problem
        if(inputSetting.UI_Global.addAgentCount > addAgentCountSlider.maxValue)
        {
            addAgentCountSlider.maxValue = (float)inputSetting.UI_Global.agentCount;
        }
        addAgentCountSlider.value = (float)inputSetting.UI_Global.addAgentCount;


        changeAddAgentCount();
        agentCountSlider.value = (float)inputSetting.UI_Global.agentCount;
        changeChosenAgentCount();
        
        startAddAgentMinInput.text = inputSetting.UI_Global.startAddAgentMin.ToString();
        changeStartAddAgentMin();
        startAddAgentMaxInput.text = inputSetting.UI_Global.startAddAgentMax.ToString();
        changeStartAddAgentMax();
        updateRateGatherSlider.value = (float)inputSetting.UI_Global.UpdateRate["gathers"];
        changeUpdateRateGather();
        updateRateStatusSlider.value = (float)inputSetting.UI_Global.UpdateRate["stopWalkStatus"];
        changeUpdateRateStatus();
        updateRateMapSlider.value = (float)inputSetting.UI_Global.UpdateRate["influenceMap"];
        changeUpdateRateMap();
        /* Human */
        walkSpeedMinInput.text = inputSetting.UI_Human.walkSpeedMin.ToString();
        changeWalkSpeedMin();        
        walkSpeedMaxInput.text = inputSetting.UI_Human.walkSpeedMax.ToString();
        changeWalkSpeedMax();
        freeTimeMinInput.text = inputSetting.UI_Human.freeTimeMin.ToString();
        changeFreeTimeMin();
        freeTimeMaxInput.text = inputSetting.UI_Human.freeTimeMax.ToString();
        changeFreeTimeMax();
        gatherProbabilityMeanSlider.value = (float)inputSetting.UI_Human.gatherProbability.mean * 100;
        changeGatherMeanProbability();
        gatherProbabilityStdSlider.value = (float)inputSetting.UI_Human.gatherProbability.std * 100;
        changeGatherStdProbability();
        joinProbabilitySlider.value = (float)inputSetting.UI_Human.behaviorProbability["join"].mean * 100;
        changeJoinProbability();
        keepAloneProbabilitySlider.value = (float)inputSetting.UI_Human.behaviorProbability["keepAlone"].mean * 100;
        changeKeepAloneProbability();
        keepGatherSameGroupProbabilitySlider.value = (float)inputSetting.UI_Human.behaviorProbability["keepGather_sameGroup"].mean * 100;
        changeKeepGatherSameGroupProbability();
        keepGatherDifGroupProbabilitySlider.value = (float)inputSetting.UI_Human.behaviorProbability["keepGather_difGroup"].mean * 100;
        changeKeepGatherDifGroupProbability();
        leaveProbabilitySlider.value = (float)inputSetting.UI_Human.behaviorProbability["leave"].mean * 100;
        changeLeaveProbability();
        /* Exhibit */
        capacityTimesSlider.value = (float)inputSetting.UI_Exhibit.capacityLimitTimes;
        changeCapacityLimitTime();
        popularThresholdSlider.value = (float)inputSetting.UI_Exhibit.popularThreshold * 100;
        changePopularThreshold();
        crowdedThresholdSlider.value = (float)inputSetting.UI_Exhibit.crowdedThreshold * 100;
        changeCrowdedThreshold();
        crowdedTimeLimitSlider.value = (float)inputSetting.UI_Exhibit.crowdedTimeLimit * 100;
        changeCrowdedTimeLimit();
        /* Influence Map */
        weightHumanSlider.value = (float)inputSetting.UI_InfluenceMap.weightHuman;
        changeHumanInfluenceWeight();
        weightExhibitSlider.value = (float)inputSetting.UI_InfluenceMap.weightExhibit;
        changeExhibitInfluenceWeight();
        humanFollowDesireInput.text = (inputSetting.UI_InfluenceMap.humanInflence["followDesire"] * 100).ToString();
        changeHumanInfluence_followDesire();
        humanTakeTimeInput.text = (inputSetting.UI_InfluenceMap.humanInflence["takeTime"] * 100).ToString();
        changeHumanInfluence_takeTime();
        humanGatherDesireInput.text = (inputSetting.UI_InfluenceMap.humanInflence["gatherDesire"] * 100).ToString();
        changeHumanInfluence_gatherDesire();
        humanTypeAttractInput.text = (inputSetting.UI_InfluenceMap.humanInflence["humanTypeAttraction"] * 100).ToString();
        changeHumanInfluence_humanTypeAttraction();
        humanBehaviorAttractInput.text = (inputSetting.UI_InfluenceMap.humanInflence["behaviorAttraction"] * 100).ToString();
        changeHumanInfluence_behaviorAttraction();
        exhibitCapacityInput.text = (inputSetting.UI_InfluenceMap.exhibitInflence["capactiy"] * 100).ToString();
        changeExhibitInfluence_capactiy();
        exhibitTakeTimeInput.text = (inputSetting.UI_InfluenceMap.exhibitInflence["takeTime"] * 100).ToString();
        changeExhibitInfluence_takeTime();
        exhibitPopularLvInput.text = (inputSetting.UI_InfluenceMap.exhibitInflence["popularLevel"] * 100).ToString();
        changeExhibitInfluence_popularLevel();
        exhibitHumanPreferInput.text = (inputSetting.UI_InfluenceMap.exhibitInflence["humanPreference"] * 100).ToString();
        changeExhibitInfluence_humanPreference();
        exhibitCloseBestInput.text = (inputSetting.UI_InfluenceMap.exhibitInflence["closeToBestViewDirection"] * 100).ToString();
        changeExhibitInfluence_closeToBestViewDirection();
        /* walk Stage*/
        StageRadiusGoToSlider.value = (float)inputSetting.walkStage["GoTo"].radius;
        changeStageRadius_GoTo();
        StageSpeedGoToSlider.value = (float)inputSetting.walkStage["GoTo"].speed;
        changeStageSpeed_GoTo();
        StageRadiusCloseSlider.value = (float)inputSetting.walkStage["Close"].radius;
        changeStageRadius_Close();
        StageSpeedCloseSlider.value = (float)inputSetting.walkStage["Close"].speed;
        changeStageSpeed_Close();
        StageRadiusAtSlider.value = (float)inputSetting.walkStage["At"].radius;
        changeStageRadius_At();
        StageSpeedAtSlider.value = (float)inputSetting.walkStage["At"].speed;
        changeStageSpeed_At();
        InitialOperationCount();
        SimpleUISetting.instance.GetValueFromUIController();
        //heatmap.maxLimit = (tmpSaveUISettings.UI_Global.agentCount * tmpSaveUISettings.UI_Human.freeTimeMax) / dynamicSystem.instance.trajectoryRecordTime;
        //heatmapMaxValueInput.text = heatmap.maxLimit.ToString();
    }

    void loadSettingsFromJson(string scene)  // only do at start, load local file
    {
        settingsClass newSceneSetting = new settingsClass();
        string dirPath = Application.streamingAssetsPath + "/SettingsJson/Default Settings";
        string jsonPath = dirPath + "/statisticOutput_" + scene + ".json";
        string tmpJsonDataStr = File.ReadAllText(jsonPath);
        JsonData tmpJsonData = new JsonData();
        tmpJsonData = JsonMapper.ToObject(tmpJsonDataStr);

        newSceneSetting.screenSize_w = int.Parse(tmpJsonData["screenSize"][0].ToJson());
        newSceneSetting.screenSize_h = int.Parse(tmpJsonData["screenSize"][1].ToJson());
        // floats in settingsClass
        newSceneSetting.oriJson.UI_Global.adultPercentage = double.Parse(tmpJsonData["adultPercentage"].ToJson());
        newSceneSetting.oriJson.UI_Human.gatherProbability.mean = float.Parse(tmpJsonData["gatherProbability"]["mean"].ToJson());
        newSceneSetting.oriJson.UI_Human.gatherProbability.std = float.Parse(tmpJsonData["gatherProbability"]["std"].ToJson());
        newSceneSetting.oriJson.UI_Human.gatherProbability.min = float.Parse(tmpJsonData["gatherProbability"]["min"].ToJson());
        newSceneSetting.oriJson.UI_Human.gatherProbability.max = float.Parse(tmpJsonData["gatherProbability"]["max"].ToJson());
        newSceneSetting.exhibitionStateMap = JsonMapper.ToObject<Dictionary<string, Dictionary<string, double>>>(tmpJsonData["exhibitionStateMap"].ToJson());
        
        // behavior
        foreach (KeyValuePair<string, JsonData> behaviorP in JsonMapper.ToObject<Dictionary<string, JsonData>>(tmpJsonData["behaviorProbability"].ToJson()))
        {
            statisticParameters newMeanAndStd = new statisticParameters();
            newMeanAndStd.mean = float.Parse(behaviorP.Value["mean"].ToString());
            newMeanAndStd.std = float.Parse(behaviorP.Value["std"].ToString());
            newSceneSetting.oriJson.UI_Human.behaviorProbability[behaviorP.Key.ToString()] = newMeanAndStd;
        }

        // exhibitions
        newSceneSetting.Exhibitions = new Dictionary<string, settings_exhibition>();
        foreach (JsonData k in tmpJsonData["exhibitions"])
        {
            settings_exhibition newExhibitionSetting = new settings_exhibition();
            newExhibitionSetting.color = JsonMapper.ToObject<List<int>>(k["color"].ToJson());
            newExhibitionSetting.capacity.max = int.Parse(k["capacity_max"].ToJson());
            newExhibitionSetting.capacity.mean = int.Parse(k["capacity_avg"].ToJson());
            newExhibitionSetting.capacity.median = int.Parse(k["capacity_median"].ToJson());
            newExhibitionSetting.stayTime.min = float.Parse(k["stayTime_min"].ToJson());
            newExhibitionSetting.stayTime.max = float.Parse(k["stayTime_max"].ToJson());
            newExhibitionSetting.stayTime.mean = float.Parse(k["stayTime_avg"].ToJson());
            newExhibitionSetting.stayTime.std = float.Parse(k["stayTime_std"].ToJson());
            newExhibitionSetting.bestViewDirection = JsonMapper.ToObject<List<int>>(k["bestViewDirection"].ToJson());
            newExhibitionSetting.bestViewDistance = JsonMapper.ToObject<List<List<double>>>(k["bestViewDistance"].ToJson());
            newExhibitionSetting.frontSide = JsonMapper.ToObject<List<int>>(k["frontSide"].ToJson());
            List<int> tmp = JsonMapper.ToObject<List<int>>(k["centerPos"].ToJson());
            newExhibitionSetting.centerPos = new Vector2(tmp[0], tmp[1]);
            newExhibitionSetting.chosenProbabilty = float.Parse(k["chosenProbabilty"].ToJson());
            newExhibitionSetting.repeatChosenProbabilty = float.Parse(k["repeatChosenProbabilty"].ToJson());

            newSceneSetting.Exhibitions.Add(k["name"].ToString(), newExhibitionSetting);
        }

        // humanTypes
        newSceneSetting.humanTypes = new Dictionary<string, settings_humanType>();
        foreach (JsonData k in tmpJsonData["humanTypes"])
        {
            settings_humanType newHumanType = new settings_humanType();
            newHumanType.walkSpeed.min = float.Parse(k["walkSpeed_min"].ToJson());
            newHumanType.walkSpeed.max = float.Parse(k["walkSpeed_max"].ToJson());
            newHumanType.walkSpeed.mean = float.Parse(k["walkSpeed_avg"].ToJson());
            newHumanType.walkSpeed.std = float.Parse(k["walkSpeed_std"].ToJson());
            newHumanType.walkSpeed.median = float.Parse(k["walkSpeed_median"].ToJson());
            newHumanType.walkToStopRate = float.Parse(k["walkToStopRate"].ToJson());
            newHumanType.stopToWalkRate = float.Parse(k["stopToWalkRate"].ToJson());
            foreach (KeyValuePair<string, double> exhibitInterest in JsonMapper.ToObject<Dictionary<string, double>>(k["interestForEachExhibition"].ToJson()))
            {
                newHumanType.interestForEachExhibition.Add(exhibitInterest.Key.ToString(), float.Parse(exhibitInterest.Value.ToString()));
            }

            newSceneSetting.humanTypes.Add(k["type"].ToString(), newHumanType);
        }

        /* get custom UI settings*/
        //need to update
        //dirPath += "/Default Settings";
        /*
        string[] allFiles = Directory.GetFiles(dirPath, "unitySettings_" + scene + "*.json"); 
        if (allFiles.Length != 0)
        {
            string newestJsonDataFileNames = allFiles.OrderByDescending(f => f.Substring(f.Substring(0, f.LastIndexOf("_")).LastIndexOf('_') + 1)).ToList()[0];
            Debug.Log("Load json: " + newestJsonDataFileNames);
            newSceneSetting.customUI = JsonMapper.ToObject<UISettings>(File.ReadAllText(newestJsonDataFileNames));
        }
        else
            newSceneSetting.customUI = newSceneSetting.oriJson.copy();
        */
        string[] file = Directory.GetFiles(dirPath, "unitySettings_" + scene + "*.json");
        newSceneSetting.customUI = JsonMapper.ToObject<UISettings>(File.ReadAllText(file[0]));

        allSceneSettings.Add(scene, newSceneSetting);  // save
    }
    
    public string checkSettings()
    {
        string errorMessage = "";
        /** Global **/
        if (tmpSaveUISettings.UI_Global.addAgentCount > tmpSaveUISettings.UI_Global.agentCount)
        {
            errorMessage += "- addAgentCount should be <= to agentCount\n";
        }

        if (tmpSaveUISettings.UI_Global.startAddAgentMin >= tmpSaveUISettings.UI_Global.startAddAgentMax)
        {
            errorMessage += "- startAddAgent Min should be <= to Max\n";
        }        

        /** Human **/
        if (tmpSaveUISettings.UI_Human.walkSpeedMin != -1 && tmpSaveUISettings.UI_Human.walkSpeedMax != -1)
        {
            if (tmpSaveUISettings.UI_Human.walkSpeedMin >= tmpSaveUISettings.UI_Human.walkSpeedMax)
            {
                errorMessage += "- walkSpeed Min should be <= to Max\n";
            }
        }
        if (tmpSaveUISettings.UI_Human.freeTimeMin != -1 && tmpSaveUISettings.UI_Human.freeTimeMax != -1)
        {
            if (tmpSaveUISettings.UI_Human.freeTimeMin > tmpSaveUISettings.UI_Human.freeTimeMax)
            {
                errorMessage += "- freeTime Min should be <= to Max\n";
            }
        }

        /** Exhibition **/
        if (tmpSaveUISettings.UI_Exhibit.popularThreshold >= tmpSaveUISettings.UI_Exhibit.crowdedThreshold)
            errorMessage += "- popular Threshold should be <= to crowded Threshold\n";

        /** influence Map **/
        /* check exhibit influence total */
        double total = 0f;
        foreach (double value in tmpSaveUISettings.UI_InfluenceMap.exhibitInflence.Values)
        {
            total += value;
        }
        if (total != 1) errorMessage += "- exhibit Influence total is "+ total + " not 100\n";
        /* check human influence total */
        total = 0f;
        foreach (double value in tmpSaveUISettings.UI_InfluenceMap.exhibitInflence.Values)
        {
            total += value;
        }
        if (total != 1) errorMessage += "- human Influence total is " + total + " not 100\n";

        if (errorMessage != "")
        {
            errorMessage = "Fix these errors first: \n" + errorMessage;
        }

        //errorDebuger.text = errorMessage;
        return errorMessage;
    }
      
    /* real data mode initialize*/
    void setRealVideosSelector()
    {
        /* Kill child before create new */
        for (int i = realVideoSelector_content.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = realVideoSelector_content.transform.GetChild(i).gameObject;
            Destroy(child);
        }

        foreach (string videoDir in Directory.GetDirectories(Application.streamingAssetsPath + "/Jsons/", currentScene + "*", SearchOption.TopDirectoryOnly))
        {
            var dirName = new DirectoryInfo(videoDir).Name;
            // Debug.Log(dirName);
            Button item = Instantiate(realVideoSelector_contentPrefab);
            item.GetComponentInChildren<Text>().text = dirName;
            item.onClick.AddListener(delegate { realDataDrawTest.instance.changeData(dirName); });
            item.transform.SetParent(realVideoSelector_content.transform, false);
        }
    }

    #region UISetting save/load
    public void SaveUISettings()
    {
        string error = checkSettings();
        if (error == "")
        {
            //successLog.text = "Saved!";
            countDownTime = 5;
            allSceneSettings[currentScene].customUI = tmpSaveUISettings.copy();

            string defaultFolder = "", defaultFileName = "";
            defaultFolder = Application.streamingAssetsPath + "/UISetting";
            System.DateTime dt = System.DateTime.Now;
            string date = dt.Year + "-" + dt.Month + "-" + dt.Day + "_" + dt.Hour + "-" + dt.Minute + "-" + dt.Second;
            defaultFileName = "UISetting_" + currentScene + "_" + date;
            /*
            var path = EditorUtility.SaveFilePanel("Save UI setting as JSON",
                                                    defaultFolder,
                                                    defaultFileName + ".json",
                                                    "json");
            */
            string path = "";
            var bp = new BrowserProperties();
            bp.title = "Save UI Setting";
            bp.initialDir = defaultFolder;
            bp.filter = "json files (*.json)|*.json";
            bp.filterIndex = 0;

            new FileBrowser().SaveFileBrowser(bp, defaultFileName, ".json", filepath =>
            {
                //Do something with path(string)
                Debug.Log(filepath);
                path = filepath;
            });


            if (path.Length != 0)
            {
                StringBuilder sb = new StringBuilder();
                JsonWriter writer = new JsonWriter(sb);
                writer.PrettyPrint = true;
                JsonMapper.ToJson(tmpSaveUISettings, writer);
                string outputJsonStr = sb.ToString();
                // Debug.Log(outputJsonStr);
                System.IO.File.WriteAllText(path, outputJsonStr);
                dynamicSystem.instance.currentSceneSettings = allSceneSettings[currentScene];
                string[] filename = path.Split('/');
                ShowMsgPanel("Success", "Save current simulation UI setting.\n" + 
                                        "filename: " + filename[filename.Length - 1]);
            }
            else
            {
                //ShowMsgPanel("Warning", "Current simulation UI setting is not saved.");
            }
        }
        else
        {
            //successLog.text = "";
            ShowMsgPanel("Warning", error);
        }
    }

    public void LoadUISettings()
    {
        /*
        string defaultFolder = Application.streamingAssetsPath + "/UISetting";
        var path = EditorUtility.OpenFilePanel("Load UI Setting",
                                                defaultFolder,
                                               "json");
        */
        string path = "";
        var bp = new BrowserProperties();
        bp.title = "Load UI Setting File";
        bp.initialDir = Application.streamingAssetsPath + "/UISetting";
        bp.filter = "json files (*.json)|*.json";
        bp.filterIndex = 0;

        new FileBrowser().OpenFileBrowser(bp, filepath =>
        {
            //Do something with path(string)
            Debug.Log(filepath);
            path = filepath;
        });

        if (path.Length != 0)
        {
            string tmpJsonDataStr = File.ReadAllText(path);
            UISettings uiSettings = new UISettings();
            uiSettings = JsonMapper.ToObject<UISettings>(tmpJsonDataStr);
            loadSettingToUI(uiSettings);
            tmpSaveUISettings = uiSettings;
            allSceneSettings[currentScene].customUI = tmpSaveUISettings.copy();
            dynamicSystem.instance.currentSceneSettings = allSceneSettings[currentScene];
            string[] filename = path.Split('/');
            ShowMsgPanel("Success", filename[filename.Length - 1] + " is loaded.");
            ShowMsgPanel("Success", "Load UI setting.\n" +
                        "filename: " + filename[filename.Length - 1]);
        }
        else
        {
            //ShowMsgPanel("Warning", "UI setting is not loaded.");
        }
    }
    #endregion

    #region exhibition save/load
    public void SaveExhibitionInfo()
    {
        exhibitionsInScene exhibitionsInScene = new exhibitionsInScene();
        GameObject scene = GameObject.Find("/[EnvironmentsOfEachScene]/" + curOption);
        string sceneNum = curOption.Substring(0, 3);

        string defaultFolder = "", defaultFileName = "";
        defaultFolder = Application.streamingAssetsPath + "/ExhibitionSetting";
        System.DateTime dt = System.DateTime.Now;
        string date = dt.Year + "-" + dt.Month + "-" + dt.Day + "_" + dt.Hour + "-" + dt.Minute + "-" + dt.Second;
        defaultFileName = "ExSetting_" + curOption + "_" + date;
        /*
        var path = EditorUtility.SaveFilePanel("Save exhibitions information as JSON",
                                                defaultFolder,
                                                defaultFileName + ".json",
                                                "json");
        */
        string path = "";
        var bp = new BrowserProperties();
        bp.title = "Save Exhibition Setting";
        bp.initialDir = defaultFolder;
        bp.filter = "json files (*.json)|*.json";
        bp.filterIndex = 0;

        new FileBrowser().SaveFileBrowser(bp, defaultFileName, ".json", filepath =>
        {
            //Do something with path(string)
            Debug.Log(filepath);
            path = filepath;
        });


        if (path.Length != 0)
        {
            //get each exhibition
            List<exhibitionInfo> exhibitionsInfo = new List<exhibitionInfo>();
            foreach (Transform child in scene.transform)
            {
                if (child.name.Contains(sceneNum) && !child.name.Contains("ExitDoor"))
                {
                    exhibitionInfo exInfo = new exhibitionInfo();
                    exInfo.name = child.name;
                    exInfo.posX = child.transform.position.x;
                    exInfo.posY = child.transform.position.y;
                    exInfo.posZ = child.transform.position.z;
                    exInfo.rotX = child.transform.rotation.eulerAngles.x;
                    exInfo.rotY = child.transform.rotation.eulerAngles.y;
                    exInfo.rotZ = child.transform.rotation.eulerAngles.z;
                    exInfo.sclX = child.transform.localScale.x;
                    exInfo.sclY = child.transform.localScale.y;
                    exInfo.sclZ = child.transform.localScale.z;

                    string key = child.name.Replace(UIController.instance.currentScene + "_", "p");
                    if (dynamicSystem.instance.currentSceneSettings.Exhibitions.ContainsKey(key))
                    {
                        settings_exhibition info = dynamicSystem.instance.currentSceneSettings.Exhibitions[key];
                        exInfo.capacityMax = info.capacity.max;
                        exInfo.capacityMean = info.capacity.mean;
                        exInfo.capacityMedian = info.capacity.median;
                        exInfo.stayTimeMax = info.stayTime.max;
                        exInfo.stayTimeMin = info.stayTime.min;
                        exInfo.stayTimeMean = info.stayTime.mean;
                        exInfo.stayTimeStd = info.stayTime.std;
                        exInfo.chooseProbability = info.chosenProbabilty;
                        exInfo.repeatChooseProbability = info.repeatChosenProbabilty;
                    }
                    else
                    {
                        exInfo.capacityMax = 0;
                        exInfo.capacityMean = 0;
                        exInfo.capacityMedian = 0;
                        exInfo.stayTimeMax = 0;
                        exInfo.stayTimeMin = 0;
                        exInfo.stayTimeMean = 0;
                        exInfo.stayTimeStd = 0;
                        exInfo.chooseProbability = 0;
                        exInfo.repeatChooseProbability = 0;
                    }
                    exhibitionsInfo.Add(exInfo);
                }
            }
            exhibitionsInScene.sceneName = curOption;
            exhibitionsInScene.exhibitionsInfo = exhibitionsInfo;

            StringBuilder sb = new StringBuilder();
            JsonWriter writer = new JsonWriter(sb);
            writer.PrettyPrint = true;
            JsonMapper.ToJson(exhibitionsInScene, writer);
            string outputJsonStr = sb.ToString();
            System.IO.File.WriteAllText(path, outputJsonStr);
            Debug.Log("Save!");
            string[] filename = path.Split('/');
            ShowMsgPanel("Success", "Save exhibition setting in current scene option (" + curOption + ")\n" + 
                                    "filename: " + filename[filename.Length - 1]);
        }
        else
        {
            //ShowMsgPanel("Warning", "Exhibition setting is not saved.");
        }
        
    }

    public void LoadExhibitionInfo()
    {
        string path = "";
        var bp = new BrowserProperties();
        bp.title = "Load Exhibition Setting File";
        bp.initialDir = Application.streamingAssetsPath + "/ExhibitionSetting";
        bp.filter = "json files (*.json)|*.json";
        bp.filterIndex = 0;

        new FileBrowser().OpenFileBrowser(bp, filepath =>
        {
            //Do something with path(string)
            Debug.Log(filepath);
            path = filepath;
        });

        /*
        string defaultFolder = Application.streamingAssetsPath + "/ExhibitionSetting";
        var path = EditorUtility.OpenFilePanel("Load exhibitions information",
                                                defaultFolder,
                                               "json");
        */

        if (path.Length != 0)
        {
            string tmpJsonDataStr = File.ReadAllText(path);
            exhibitionsInScene exhibitionsInScene = new exhibitionsInScene();
            exhibitionsInScene = JsonMapper.ToObject<exhibitionsInScene>(tmpJsonDataStr);

            GameObject scene = GameObject.Find("/[EnvironmentsOfEachScene]/" + exhibitionsInScene.sceneName);
            foreach (exhibitionInfo exInfo in exhibitionsInScene.exhibitionsInfo)
            {
                GameObject ex = scene.transform.Find(exInfo.name).gameObject;
                Vector3 pos = new Vector3((float)exInfo.posX, (float)exInfo.posY, (float)exInfo.posZ);
                ex.transform.position = pos;
                ex.transform.rotation = Quaternion.Euler((float)exInfo.rotX, (float)exInfo.rotY, (float)exInfo.rotZ);
                Vector3 scl = new Vector3((float)exInfo.sclX, (float)exInfo.sclY, (float)exInfo.sclZ);
                ex.transform.localScale = scl;

                string key = exInfo.name.Replace(UIController.instance.currentScene + "_", "p");
                if (dynamicSystem.instance.currentSceneSettings.Exhibitions.ContainsKey(key))
                {
                    dynamicSystem.instance.currentSceneSettings.Exhibitions[key].capacity.max = exInfo.capacityMax;
                    dynamicSystem.instance.currentSceneSettings.Exhibitions[key].capacity.mean = exInfo.capacityMean;
                    dynamicSystem.instance.currentSceneSettings.Exhibitions[key].capacity.median = exInfo.capacityMedian;
                    dynamicSystem.instance.currentSceneSettings.Exhibitions[key].stayTime.max = exInfo.stayTimeMax;
                    dynamicSystem.instance.currentSceneSettings.Exhibitions[key].stayTime.min = exInfo.stayTimeMin;
                    dynamicSystem.instance.currentSceneSettings.Exhibitions[key].stayTime.mean = exInfo.stayTimeMean;
                    dynamicSystem.instance.currentSceneSettings.Exhibitions[key].stayTime.std = exInfo.stayTimeStd;
                    dynamicSystem.instance.currentSceneSettings.Exhibitions[key].chosenProbabilty = exInfo.chooseProbability;
                    dynamicSystem.instance.currentSceneSettings.Exhibitions[key].repeatChosenProbabilty = exInfo.repeatChooseProbability;
                }
            }
            string[] filename = path.Split('/');
            string msg = "Load exhibition setting to the scene option (" + exhibitionsInScene.sceneName + ")\n" + 
                         "filename: " + filename[filename.Length - 1];
            if(curOption != exhibitionsInScene.sceneName)
            {
                msg += "\nPlease choose the coresponding scene option.";
            }
            ShowMsgPanel("Success", msg);

            /*
            // have some error
            setScene(exhibitionsInScene.sceneName.Substring(0, 3));
            int idx = 0;
            if (exhibitionsInScene.sceneName.Contains("A")) idx = 1;
            if (exhibitionsInScene.sceneName.Contains("B")) idx = 2;
            changeOption(idx);
            */
        }
        else
        {
            ShowMsgPanel("Warning", "Exhibition setting is not saved.");
        }
    }

    public void DefaultExhibitionInfo()
    {
        string path = Application.streamingAssetsPath + "/ExhibitionSetting/original/";
        foreach (string file in System.IO.Directory.GetFiles(path))
        {
            if (file.Contains(".meta")) continue;
            string tmpJsonDataStr = File.ReadAllText(file);
            exhibitionsInScene exhibitionsInScene = new exhibitionsInScene();
            exhibitionsInScene = JsonMapper.ToObject<exhibitionsInScene>(tmpJsonDataStr);

            GameObject scene = GameObject.Find("/[EnvironmentsOfEachScene]/" + exhibitionsInScene.sceneName);
            foreach (exhibitionInfo exInfo in exhibitionsInScene.exhibitionsInfo)
            {
                GameObject ex = scene.transform.Find(exInfo.name).gameObject;
                Vector3 pos = new Vector3((float)exInfo.posX, (float)exInfo.posY, (float)exInfo.posZ);
                ex.transform.position = pos;
                ex.transform.rotation = Quaternion.Euler((float)exInfo.rotX, (float)exInfo.rotY, (float)exInfo.rotZ);
                Vector3 scl = new Vector3((float)exInfo.sclX, (float)exInfo.sclY, (float)exInfo.sclZ);
                ex.transform.localScale = scl;
            }
        }
        ShowMsgPanel("Success", "Exhibition layouts in all scene options are changed to default.");
    }
    #endregion

    public void ShowMsgPanel(string type, string content)
    {
        msgType.text = type;
        if (type == "Warning") msgType.color = Color.red;
        else if(type == "Notice") msgType.color = new Color(1.0f, 0.64f, 0.0f); //orange
        else if (type == "Success") msgType.color = Color.green;
        
        msgContent.text = content;
        msgPanel.SetActive(true);
    }

    public void CloseMsgPanel()
    {
        msgPanel.SetActive(false);
    }
}

public class UIOperationCount
{
    public GlobalUIOperationCount globalUIOperationCount = new GlobalUIOperationCount();
    public HumanFeatureUIOperationCount humanFeatureUIOperationCount = new HumanFeatureUIOperationCount();
    public ExhibitionsUIOperationCount exhibitionsUIOperationCount = new ExhibitionsUIOperationCount();
    public HumanWalkStageUIOperationCount humanWalkStageUIOperationCount = new HumanWalkStageUIOperationCount();
    public InfluenceMapWeightUIOperationCount influenceMapWeightUIOperationCount = new InfluenceMapWeightUIOperationCount();
}

public class GlobalUIOperationCount 
{
    public int agentCountOpCount, adultPercentOpCount, addAgentCountOpCount, startAddAgentOpCount;
    public int updateRateGatherSliderOpCount, updateRateStatusSliderOpCount, updateRateMapSliderOpCount;
}

public class HumanFeatureUIOperationCount 
{
    public int walkSpeedOpCount, freeTimeOpCount, gatherProbabilityMeanOpCount, gatherProbabilityStdSliderOpCount;
    public int joinProbabilitySliderOpCount, keepAloneProbabilitySliderOpCount, keepGatherSameGroupProbabilitySliderOpCount;
    public int keepGatherDifGroupProbabilitySliderOpCount, leaveProbabilitySliderOpCount;
}

public class ExhibitionsUIOperationCount 
{
    public int capacityTimesSliderOpCount, popularThresholdSliderOpCount, crowdedThresholdSliderOpCount, crowdedTimeLimitSliderOpCount;
}

public class HumanWalkStageUIOperationCount
{
    public int StageRadiusGoToSliderOpCount, StageRadiusCloseSliderOpCount, StageRadiusAtSliderOpCount;
    public int StageSpeedGoToSliderOpCount, StageSpeedCloseSliderOpCount, StageSpeedAtSliderOpCount;
}

public class InfluenceMapWeightUIOperationCount
{
    public int weightHumanSliderOpCount, weightExhibitSliderOpCount;
    public int humanFollowDesireInputOpCount, humanTakeTimeInputOpCount, humanGatherDesireInputOpCount, humanTypeAttractInputOpCount, humanBehaviorAttractInputOpCount;
    public int exhibitCapacityInputOpCount, exhibitTakeTimeInputOpCount, exhibitPopularLvInputOpCount, exhibitHumanPreferInputOpCount, exhibitCloseBestInputOpCount;
}

public partial class UIController : PersistentSingleton<UIController>  // seperate for combining lines
{
    /* simulation mode UI */
    public UISettings tmpSaveUISettings = new UISettings();
    public Dictionary<string, settingsClass> allSceneSettings = new Dictionary<string, settingsClass>();
    // public Dictionary<string, settingsClass> allSceneSettings_Custom = new Dictionary<string, settingsClass>();
    // public Dictionary<string, settingsClass> allSceneSettings_Json = new Dictionary<string, settingsClass>(); // backUp original settings
    //public Text errorDebuger, successLog;
    public float countDownTime;
    /* Global */
    public Text agentCountText, adultPercentText, addAgentCountText;
    public Slider agentCountSlider, adultPercentSlider, addAgentCountSlider;
    public InputField startAddAgentMinInput, startAddAgentMaxInput;
    public Text updateRateGatherText, updateRateStatusText, updateRateMapText;
    public Slider updateRateGatherSlider, updateRateStatusSlider, updateRateMapSlider, timeScaleSlider;
    public TextMeshProUGUI timeScaleText;
    /* Human */
    public InputField walkSpeedMinInput, walkSpeedMaxInput, freeTimeMinInput, freeTimeMaxInput;
    public Slider gatherProbabilityMeanSlider, gatherProbabilityStdSlider;
    public Text gatherProbabilityMeanText, gatherProbabilityStdText;
    public Text joinProbabilityText, keepAloneProbabilityText, keepGatherSameGroupProbabilityText, keepGatherDifGroupProbabilityText, leaveProbabilityText;
    public Slider joinProbabilitySlider, keepAloneProbabilitySlider, keepGatherSameGroupProbabilitySlider, keepGatherDifGroupProbabilitySlider, leaveProbabilitySlider;
    /* Exhibit */
    public Text capacityTimesText, popularThresholdText, crowdedThresholdText, crowdedTimeLimitText;
    public Slider capacityTimesSlider, popularThresholdSlider, crowdedThresholdSlider, crowdedTimeLimitSlider;
    /* Influence Map */
    public Text weightHumanText, weightExhibitText;
    public Slider weightHumanSlider, weightExhibitSlider;
    public InputField humanFollowDesireInput, humanTakeTimeInput, humanGatherDesireInput, humanTypeAttractInput, humanBehaviorAttractInput;
    public InputField exhibitCapacityInput, exhibitTakeTimeInput, exhibitPopularLvInput, exhibitHumanPreferInput, exhibitCloseBestInput;
    public double totalValueOfHumanInfluence = 0, totalValueOfExhibitInfluence = 0;
    /* Immediate change */
    public Text StageRadiusGoToText, StageRadiusCloseText, StageRadiusAtText;
    public Slider StageRadiusGoToSlider, StageRadiusCloseSlider, StageRadiusAtSlider;
    public Text StageSpeedGoToText, StageSpeedCloseText, StageSpeedAtText;
    public Slider StageSpeedGoToSlider, StageSpeedCloseSlider, StageSpeedAtSlider;
    /*Heatmap*/
    public TMP_InputField heatmapMaxValueInput;
    public HeatMap_Float heatmap;

    /* UI operation adjustment count*/
    public UIOperationCount uiOperationCount = new UIOperationCount();

    public class UIOrder 
    {
        public string orderName;
        public float orderTime;
    }
    public float loadingTime; // UI Loading Time
    public bool startModifyUI = false;

    public List<UIOrder> uiOperationOrder = new List<UIOrder>();
    public List<string> realUIOperationOrder = new List<string>();
    

    /* Update UI*/
    /* Global */
    #region Global UI
    public void changeChosenAgentCount()
    {
        int value = (int)agentCountSlider.value;
        agentCountText.text = (value).ToString();
        tmpSaveUISettings.UI_Global.agentCount = value;
        addAgentCountSlider.maxValue = value;
        //heatmap.maxLimit = (value * tmpSaveUISettings.UI_Human.freeTimeMax) / dynamicSystem.instance.trajectoryRecordTime;
        //heatmapMaxValueInput.text = heatmap.maxLimit.ToString();

        //change basic UI setting
        SimpleUISetting.instance.numberOfAgentSlider.value = value;
        SimpleUISetting.instance.numberOfAgentText.text = value.ToString();
        SimpleUISetting.instance.laterVisitorSlider.maxValue = value;

        changeAddAgentCount();
        uiOperationOrder.RemoveAt(uiOperationOrder.Count - 1); //remove add agent 

        AddUIOrderToList("Num of Agent");
    }

    public void changeAdultPercent()
    {
        float value = (float)(adultPercentSlider.value / 100);  // % -> 0.xx
        adultPercentText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Global.adultPercentage = value;

        //change basic UI setting
        SimpleUISetting.instance.adultRatioSlider.value = value * 100;
        SimpleUISetting.instance.adultRatioText.text = value.ToString();

        AddUIOrderToList("Adult Percentage");
    }

    public void changeAddAgentCount()
    {
        int value = (int)addAgentCountSlider.value;
        addAgentCountText.text = (value).ToString();
        tmpSaveUISettings.UI_Global.addAgentCount = value;
        AddUIOrderToList("Add Agent");
    }

    public void changeStartAddAgentMin()
    {
        int value = int.Parse(startAddAgentMinInput.text);
        tmpSaveUISettings.UI_Global.startAddAgentMin = value;
        AddUIOrderToList("Start Add Range");
    }

    public void changeStartAddAgentMax()
    {
        int value = int.Parse(startAddAgentMaxInput.text);
        tmpSaveUISettings.UI_Global.startAddAgentMax = value;
        AddUIOrderToList("Start Add Range");
    }

    public void changeUpdateRateGather()
    {
        int value = (int)updateRateGatherSlider.value;
        updateRateGatherText.text = (value).ToString() + "s";
        tmpSaveUISettings.UI_Global.UpdateRate["gathers"] = value;
        AddUIOrderToList("Update Rate Gather");
    }

    public void changeUpdateRateStatus()
    {
        int value = (int)updateRateStatusSlider.value;
        updateRateStatusText.text = (value).ToString() + "s";
        tmpSaveUISettings.UI_Global.UpdateRate["stopWalkStatus"] = value;
        AddUIOrderToList("Update Rate Status");
    }

    public void changeUpdateRateMap()
    {
        int value = (int)updateRateMapSlider.value;
        updateRateMapText.text = (value).ToString() + "s";
        tmpSaveUISettings.UI_Global.UpdateRate["influenceMap"] = value;
        AddUIOrderToList("Update Rate Map");
    }

    public void changeTimescale()
    {
        int value = (int)timeScaleSlider.value;
        timeScaleText.text = value.ToString();
        Time.timeScale = value;
        Time.fixedDeltaTime = 0.03333333f;
    }
    #endregion
    /* Human */
    #region Human UI
    public void changeWalkSpeedMin()
    {
        int value = int.Parse(walkSpeedMinInput.text);
        tmpSaveUISettings.UI_Human.walkSpeedMin = value;
        AddUIOrderToList("Walk Speed Range");
    }

    public void changeWalkSpeedMax()
    {
        int value = int.Parse(walkSpeedMaxInput.text);
        tmpSaveUISettings.UI_Human.walkSpeedMax = value;
        AddUIOrderToList("Walk Speed Range");
    }

    public void changeFreeTimeMin()
    {
        int value = int.Parse(freeTimeMinInput.text);
        tmpSaveUISettings.UI_Human.freeTimeMin = value;
        AddUIOrderToList("Free Time Range");
    }

    public void changeFreeTimeMax()
    {
        int value = int.Parse(freeTimeMaxInput.text);
        tmpSaveUISettings.UI_Human.freeTimeMax = value;
        AddUIOrderToList("Free Time Range");
        //heatmap.maxLimit = (value * tmpSaveUISettings.UI_Human.freeTimeMax) / dynamicSystem.instance.trajectoryRecordTime;
        //heatmapMaxValueInput.text = heatmap.maxLimit.ToString();
    }

    public void changeGatherMeanProbability()
    {
        float value = (float)(gatherProbabilityMeanSlider.value / 100);  // % -> 0.xx
        gatherProbabilityMeanText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Human.gatherProbability.mean = value;
        AddUIOrderToList("Gather Desire Mean");
    }

    public void changeGatherStdProbability()
    {
        float value = (float)(gatherProbabilityStdSlider.value / 100);  // % -> 0.xx
        gatherProbabilityStdText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Human.gatherProbability.std = value;
        AddUIOrderToList("Gather Desire Std");
    }

    public void changeJoinProbability()
    {
        float value = (float)(joinProbabilitySlider.value / 100);  // % -> 0.xx
        joinProbabilityText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Human.behaviorProbability["join"].mean = value;
        AddUIOrderToList("Behavior Join Mean");
    }

    public void changeKeepAloneProbability()
    {
        float value = (float)(keepAloneProbabilitySlider.value / 100);  // % -> 0.xx
        keepAloneProbabilityText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Human.behaviorProbability["keepAlone"].mean = value;
        AddUIOrderToList("Behavior Keep Alone Mean");
    }

    public void changeKeepGatherSameGroupProbability()
    {
        float value = (float)(keepGatherSameGroupProbabilitySlider.value / 100);  // % -> 0.xx
        keepGatherSameGroupProbabilityText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Human.behaviorProbability["keepGather_sameGroup"].mean = value;
        AddUIOrderToList("Behavior Keep Same Group Mean");
    }

    public void changeKeepGatherDifGroupProbability()
    {
        float value = (float)(keepGatherDifGroupProbabilitySlider.value / 100);  // % -> 0.xx
        keepGatherDifGroupProbabilityText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Human.behaviorProbability["keepGather_difGroup"].mean = value;
        AddUIOrderToList("Behavior Keep Diff Group Mean");
    }

    public void changeLeaveProbability()
    {
        float value = (float)(leaveProbabilitySlider.value / 100);  // % -> 0.xx
        leaveProbabilityText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Human.behaviorProbability["leave"].mean = value;
        AddUIOrderToList("Behavior Leave Mean");
    }
    #endregion
    /* Exhibit */
    #region Exhibit UI
    public void changeCapacityLimitTime()
    {
        float value = (float)capacityTimesSlider.value;
        capacityTimesText.text = (value).ToString();
        tmpSaveUISettings.UI_Exhibit.capacityLimitTimes = value;
        AddUIOrderToList("Capacity Limit");
    }

    public void changePopularThreshold()
    {
        int value = (int)popularThresholdSlider.value;
        popularThresholdText.text = (value).ToString() + "%";
        tmpSaveUISettings.UI_Exhibit.popularThreshold = value / 100f;
        AddUIOrderToList("Popular Threshold");
    }

    public void changeCrowdedThreshold()
    {
        int value = (int)crowdedThresholdSlider.value;
        crowdedThresholdText.text = (value).ToString() + "%";
        tmpSaveUISettings.UI_Exhibit.crowdedThreshold = value / 100f;
        AddUIOrderToList("Crowded Threshold");
    }

    public void changeCrowdedTimeLimit()
    {
        int value = (int)crowdedTimeLimitSlider.value;
        crowdedTimeLimitText.text = (value).ToString() + "s";
        tmpSaveUISettings.UI_Exhibit.crowdedTimeLimit = value;
        AddUIOrderToList("Crowded Time Limit");
    }
    #endregion
    /* Influence Map */
    #region Influence Map UI
    public void changeHumanInfluenceWeight()
    {
        float value = (float)weightHumanSlider.value;
        weightHumanText.text = (value).ToString("F1");
        tmpSaveUISettings.UI_InfluenceMap.weightHuman = value;
        AddUIOrderToList("Human Influence Weight");
    }

    public void changeExhibitInfluenceWeight()
    {
        float value = (float)weightExhibitSlider.value;
        weightExhibitText.text = (value).ToString("F1");
        tmpSaveUISettings.UI_InfluenceMap.weightExhibit = value;
        AddUIOrderToList("Exhibit Influence Weight");
    }

    public void changeHumanInfluence_followDesire()
    {
        int value = int.Parse(humanFollowDesireInput.text);
        tmpSaveUISettings.UI_InfluenceMap.humanInflence["followDesire"] = value / 100f;
        AddUIOrderToList("Human Follow Desire");
    }

    public void changeHumanInfluence_takeTime()
    {
        int value = int.Parse(humanTakeTimeInput.text);
        tmpSaveUISettings.UI_InfluenceMap.humanInflence["takeTime"] = value / 100f;
        AddUIOrderToList("Human Take Time");
    }

    public void changeHumanInfluence_gatherDesire()
    {
        int value = int.Parse(humanGatherDesireInput.text);
        tmpSaveUISettings.UI_InfluenceMap.humanInflence["gatherDesire"] = value / 100f;
        AddUIOrderToList("Human Gather Desire");
    }

    public void changeHumanInfluence_humanTypeAttraction()
    {
        int value = int.Parse(humanTypeAttractInput.text);
        tmpSaveUISettings.UI_InfluenceMap.humanInflence["humanTypeAttraction"] = value / 100f;
        AddUIOrderToList("Human Human Type Attraction");
    }

    public void changeHumanInfluence_behaviorAttraction()
    {
        int value = int.Parse(humanBehaviorAttractInput.text);
        tmpSaveUISettings.UI_InfluenceMap.humanInflence["behaviorAttraction"] = value / 100f;
        AddUIOrderToList("Human Behavior Attraction");
    }

    public void changeExhibitInfluence_capactiy()
    {
        int value = int.Parse(exhibitCapacityInput.text);
        tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["capactiy"] = value / 100f;
        AddUIOrderToList("Exhibit Capacity");
    }

    public void changeExhibitInfluence_takeTime()
    {
        int value = int.Parse(exhibitTakeTimeInput.text);
        tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["takeTime"] = value / 100f;
        AddUIOrderToList("Exhibit Take Time");
    }

    public void changeExhibitInfluence_popularLevel()
    {
        int value = int.Parse(exhibitPopularLvInput.text);
        tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["popularLevel"] = value / 100f;
        AddUIOrderToList("Exhibit Popular Level");
    }

    public void changeExhibitInfluence_humanPreference()
    {
        int value = int.Parse(exhibitHumanPreferInput.text);
        tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["humanPreference"] = value / 100f;
        AddUIOrderToList("Exhibit Human Preference");
    }

    public void changeExhibitInfluence_closeToBestViewDirection()
    {
        int value = int.Parse(exhibitCloseBestInput.text);
        tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["closeToBestViewDirection"] = value / 100f;
        AddUIOrderToList("Exhibit Close To Best View Direction");
    }

    public void NormalizeInfluenceValue()
    {
        totalValueOfExhibitInfluence = 
            tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["capactiy"] +
            tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["takeTime"] +
            tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["popularLevel"] +
            tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["humanPreference"] +
            tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["closeToBestViewDirection"];
        totalValueOfHumanInfluence =
            tmpSaveUISettings.UI_InfluenceMap.humanInflence["followDesire"] +
            tmpSaveUISettings.UI_InfluenceMap.humanInflence["takeTime"] +
            tmpSaveUISettings.UI_InfluenceMap.humanInflence["gatherDesire"] +
            tmpSaveUISettings.UI_InfluenceMap.humanInflence["humanTypeAttraction"] +
            tmpSaveUISettings.UI_InfluenceMap.humanInflence["behaviorAttraction"];
        if (totalValueOfExhibitInfluence != 1.0d)
        {
            tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["capactiy"] /= totalValueOfExhibitInfluence;
            tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["takeTime"] /= totalValueOfExhibitInfluence;
            tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["popularLevel"] /= totalValueOfExhibitInfluence;
            tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["humanPreference"] /= totalValueOfExhibitInfluence;
            tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["closeToBestViewDirection"] /= totalValueOfExhibitInfluence;
        }
        if(totalValueOfHumanInfluence != 1.0d)
        {
            tmpSaveUISettings.UI_InfluenceMap.humanInflence["followDesire"] /= totalValueOfHumanInfluence;
            tmpSaveUISettings.UI_InfluenceMap.humanInflence["takeTime"] /= totalValueOfHumanInfluence;
            tmpSaveUISettings.UI_InfluenceMap.humanInflence["gatherDesire"] /= totalValueOfHumanInfluence;
            tmpSaveUISettings.UI_InfluenceMap.humanInflence["humanTypeAttraction"] /= totalValueOfHumanInfluence;
            tmpSaveUISettings.UI_InfluenceMap.humanInflence["behaviorAttraction"] /= totalValueOfHumanInfluence;
        }
        //UI Modify Text To Correct Value
        humanFollowDesireInput.text = (tmpSaveUISettings.UI_InfluenceMap.humanInflence["followDesire"] * 100).ToString("f1");
        humanTakeTimeInput.text = (tmpSaveUISettings.UI_InfluenceMap.humanInflence["takeTime"] * 100).ToString("f1");
        humanGatherDesireInput.text = (tmpSaveUISettings.UI_InfluenceMap.humanInflence["gatherDesire"] * 100).ToString("f1");
        humanTypeAttractInput.text = (tmpSaveUISettings.UI_InfluenceMap.humanInflence["humanTypeAttraction"] * 100).ToString("f1");
        humanBehaviorAttractInput.text = (tmpSaveUISettings.UI_InfluenceMap.humanInflence["humanTypeAttraction"] * 100).ToString("f1");

        exhibitCapacityInput.text = (tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["capactiy"] * 100).ToString("f1"); 
        exhibitTakeTimeInput.text = (tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["takeTime"] * 100).ToString("f1");
        exhibitPopularLvInput.text = (tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["popularLevel"] * 100).ToString("f1");
        exhibitHumanPreferInput.text = (tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["humanPreference"] * 100).ToString("f1");
        exhibitCloseBestInput.text = (tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["closeToBestViewDirection"] * 100).ToString("f1");
    }
    #endregion
    /* Immediate change variables */
    #region Immediate change variables UI
    public void changeStageRadius_GoTo()
    {
        float value = (float)StageRadiusGoToSlider.value;
        StageRadiusGoToText.text = (value).ToString();
        tmpSaveUISettings.walkStage["GoTo"].radius = value;
        AddUIOrderToList("GoTo Radius");
    }
    public void changeStageSpeed_GoTo()
    {
        float value = (float)StageSpeedGoToSlider.value;
        StageSpeedGoToText.text = "x " + (value).ToString();
        tmpSaveUISettings.walkStage["GoTo"].speed = value;
        AddUIOrderToList("GoTo Speed");
    }
    public void changeStageRadius_Close()
    {
        float value = (float)StageRadiusCloseSlider.value;
        StageRadiusCloseText.text = (value).ToString();
        tmpSaveUISettings.walkStage["Close"].radius = value;
        AddUIOrderToList("Close Radius");
    }
    public void changeStageSpeed_Close()
    {
        float value = (float)StageSpeedCloseSlider.value;
        StageSpeedCloseText.text = "x " + (value).ToString();
        tmpSaveUISettings.walkStage["Close"].speed = value;
        AddUIOrderToList("Close Speed");
    }
    public void changeStageRadius_At()
    {
        float value = (float)StageRadiusAtSlider.value;
        StageRadiusAtText.text = (value).ToString();
        tmpSaveUISettings.walkStage["At"].radius = value;
        AddUIOrderToList("At Radius");
    }
    public void changeStageSpeed_At()
    {
        float value = (float)StageSpeedAtSlider.value;
        StageSpeedAtText.text = "x " + (value).ToString();
        tmpSaveUISettings.walkStage["At"].speed = value;
        AddUIOrderToList("At Speed");
    }

    public void LoadTmpSettingToCurrentSceneSettings(){
        allSceneSettings[currentScene].customUI = tmpSaveUISettings.copy();
        dynamicSystem.instance.currentSceneSettings = allSceneSettings[currentScene];
    }
    #endregion
    /* Heatmap*/
    #region Heatmap UI
    public void changeHeatmapMaxValue()
    {
        int value = int.Parse(heatmapMaxValueInput.text);
        //heatmap.maxLimit = value;
    }
    #endregion

    /*UI Operation Count*/
    public void InitialOperationCount()
    {
        InitialGlobalOperationCount();
        InitialHumanFeatureOperationCount();
        InitialExhibitionsOperationCount();
        InitialHumanWalkStageOperationCount();
        InitialInfluenceMapWeightOperationCount();
        loadingTime = Time.time;
        uiOperationOrder.Clear();
    }
    public void InitialGlobalOperationCount()
    {
        uiOperationCount.globalUIOperationCount.agentCountOpCount = 0;
        uiOperationCount.globalUIOperationCount.adultPercentOpCount = 0;
        uiOperationCount.globalUIOperationCount.addAgentCountOpCount = 0;
        uiOperationCount.globalUIOperationCount.startAddAgentOpCount = 0;
        uiOperationCount.globalUIOperationCount.updateRateGatherSliderOpCount = 0;
        uiOperationCount.globalUIOperationCount.updateRateMapSliderOpCount = 0;
        uiOperationCount.globalUIOperationCount.updateRateStatusSliderOpCount = 0;
    }
    public void InitialHumanFeatureOperationCount()
    {
        uiOperationCount.humanFeatureUIOperationCount.freeTimeOpCount = 0;
        uiOperationCount.humanFeatureUIOperationCount.walkSpeedOpCount = 0;
        uiOperationCount.humanFeatureUIOperationCount.gatherProbabilityMeanOpCount = 0;
        uiOperationCount.humanFeatureUIOperationCount.gatherProbabilityStdSliderOpCount = 0;
        uiOperationCount.humanFeatureUIOperationCount.joinProbabilitySliderOpCount = 0;
        uiOperationCount.humanFeatureUIOperationCount.keepAloneProbabilitySliderOpCount = 0;
        uiOperationCount.humanFeatureUIOperationCount.keepGatherDifGroupProbabilitySliderOpCount = 0;
        uiOperationCount.humanFeatureUIOperationCount.keepGatherSameGroupProbabilitySliderOpCount = 0;
        uiOperationCount.humanFeatureUIOperationCount.leaveProbabilitySliderOpCount = 0;
    }
    public void InitialExhibitionsOperationCount()
    {
        uiOperationCount.exhibitionsUIOperationCount.capacityTimesSliderOpCount = 0;
        uiOperationCount.exhibitionsUIOperationCount.popularThresholdSliderOpCount = 0;
        uiOperationCount.exhibitionsUIOperationCount.crowdedThresholdSliderOpCount = 0;
        uiOperationCount.exhibitionsUIOperationCount.crowdedTimeLimitSliderOpCount = 0;
    }
    public void InitialHumanWalkStageOperationCount()
    {
        uiOperationCount.humanWalkStageUIOperationCount.StageRadiusGoToSliderOpCount = 0;
        uiOperationCount.humanWalkStageUIOperationCount.StageRadiusCloseSliderOpCount = 0;
        uiOperationCount.humanWalkStageUIOperationCount.StageRadiusAtSliderOpCount = 0;
        uiOperationCount.humanWalkStageUIOperationCount.StageSpeedGoToSliderOpCount = 0;
        uiOperationCount.humanWalkStageUIOperationCount.StageSpeedCloseSliderOpCount = 0;
        uiOperationCount.humanWalkStageUIOperationCount.StageSpeedAtSliderOpCount = 0;
    }
    public void InitialInfluenceMapWeightOperationCount()
    {
        uiOperationCount.influenceMapWeightUIOperationCount.weightHumanSliderOpCount = 0;
        uiOperationCount.influenceMapWeightUIOperationCount.weightExhibitSliderOpCount = 0;
        uiOperationCount.influenceMapWeightUIOperationCount.humanFollowDesireInputOpCount = 0;
        uiOperationCount.influenceMapWeightUIOperationCount.humanTakeTimeInputOpCount = 0;
        uiOperationCount.influenceMapWeightUIOperationCount.humanGatherDesireInputOpCount = 0;
        uiOperationCount.influenceMapWeightUIOperationCount.humanTypeAttractInputOpCount = 0;
        uiOperationCount.influenceMapWeightUIOperationCount.humanBehaviorAttractInputOpCount = 0;
        uiOperationCount.influenceMapWeightUIOperationCount.exhibitCapacityInputOpCount = 0;
        uiOperationCount.influenceMapWeightUIOperationCount.exhibitTakeTimeInputOpCount = 0;
        uiOperationCount.influenceMapWeightUIOperationCount.exhibitPopularLvInputOpCount = 0;
        uiOperationCount.influenceMapWeightUIOperationCount.exhibitHumanPreferInputOpCount = 0;
        uiOperationCount.influenceMapWeightUIOperationCount.exhibitCloseBestInputOpCount = 0;
    }

    public void CalulateOperation()
    {
        string curOperation = "";
        for(int i = 0; i < uiOperationOrder.Count(); i++)
        {
            if (uiOperationOrder[i].orderName != curOperation)
            {
                realUIOperationOrder.Add(uiOperationOrder[i].orderName + ":" + GetOperationAreaName(uiOperationOrder[i].orderName) + ":"  + uiOperationOrder[i].orderTime.ToString("f2"));
                CalulateOperationCount(uiOperationOrder[i].orderName);
                curOperation = uiOperationOrder[i].orderName;
            }
        }
        WriteOpertationCountToFile();
        WriteOperationOrderToFile();
    }

    public void CalulateOperationCount(string operation){
        switch (operation) 
        {
            case "Num of Agent":
                uiOperationCount.globalUIOperationCount.agentCountOpCount++;
                break;
            case "Adult Percentage":
                uiOperationCount.globalUIOperationCount.adultPercentOpCount++;
                break;
            case "Add Agent":
                uiOperationCount.globalUIOperationCount.addAgentCountOpCount++;
                break;
            case "Start Add Range":
                uiOperationCount.globalUIOperationCount.startAddAgentOpCount++;
                break;
            case "Update Rate Gather":
                uiOperationCount.globalUIOperationCount.updateRateGatherSliderOpCount++;
                break;
            case "Update Rate Status":
                uiOperationCount.globalUIOperationCount.updateRateStatusSliderOpCount++;
                break;
            case "Update Rate Map":
                uiOperationCount.globalUIOperationCount.updateRateMapSliderOpCount++;
                break;
            case "Walk Speed Range":
                uiOperationCount.humanFeatureUIOperationCount.walkSpeedOpCount++;
                break;
            case "Free Time Range":
                uiOperationCount.humanFeatureUIOperationCount.freeTimeOpCount++;
                break;
            case "Gather Desire Mean":
                uiOperationCount.humanFeatureUIOperationCount.gatherProbabilityMeanOpCount++;
                break;
            case "Gather Desire Std":
                uiOperationCount.humanFeatureUIOperationCount.gatherProbabilityStdSliderOpCount++;
                break;
            case "Behavior Join Mean":
                uiOperationCount.humanFeatureUIOperationCount.joinProbabilitySliderOpCount++;
                break;
            case "Behavior Keep Alone Mean":
                uiOperationCount.humanFeatureUIOperationCount.keepAloneProbabilitySliderOpCount++;
                break;
            case "Behavior Keep Same Group Mean":
                uiOperationCount.humanFeatureUIOperationCount.keepGatherSameGroupProbabilitySliderOpCount++;
                break;
            case "Behavior Keep Diff Group Mean":
                uiOperationCount.humanFeatureUIOperationCount.keepGatherDifGroupProbabilitySliderOpCount++;
                break;
            case "Behavior Leave Mean":
                uiOperationCount.humanFeatureUIOperationCount.leaveProbabilitySliderOpCount++;
                break;
            case "Capacity Limit":
                uiOperationCount.exhibitionsUIOperationCount.capacityTimesSliderOpCount++;
                break;
            case "Popular Threshold":
                uiOperationCount.exhibitionsUIOperationCount.popularThresholdSliderOpCount++;
                break;
            case "Crowded Threshold":
                uiOperationCount.exhibitionsUIOperationCount.crowdedThresholdSliderOpCount++;
                break;
            case "Crowded Time Limit":
                uiOperationCount.exhibitionsUIOperationCount.crowdedTimeLimitSliderOpCount++;
                break;
            case "Human Influence Weight":
                uiOperationCount.influenceMapWeightUIOperationCount.weightHumanSliderOpCount++;
                break;
            case "Exhibit Influence Weight":
                uiOperationCount.influenceMapWeightUIOperationCount.weightExhibitSliderOpCount++;
                break;
            case "Human Follow Desire":
                uiOperationCount.influenceMapWeightUIOperationCount.humanFollowDesireInputOpCount++;
                break;
            case "Human Take Time":
                uiOperationCount.influenceMapWeightUIOperationCount.humanTakeTimeInputOpCount++;
                break;
            case "Human Gather Desire":
                uiOperationCount.influenceMapWeightUIOperationCount.humanGatherDesireInputOpCount++;
                break;
            case "Human Human Type Attraction":
                uiOperationCount.influenceMapWeightUIOperationCount.humanTypeAttractInputOpCount++;
                break;
            case "Human Behavior Attraction":
                uiOperationCount.influenceMapWeightUIOperationCount.humanBehaviorAttractInputOpCount++;
                break;
            case "Exhibit Capacity":
                uiOperationCount.influenceMapWeightUIOperationCount.exhibitCapacityInputOpCount++;
                break;
            case "Exhibit Take Time":
                uiOperationCount.influenceMapWeightUIOperationCount.exhibitTakeTimeInputOpCount++;
                break;
            case "Exhibit Popular Level":
                uiOperationCount.influenceMapWeightUIOperationCount.exhibitPopularLvInputOpCount++;
                break;
            case "Exhibit Human Preference":
                uiOperationCount.influenceMapWeightUIOperationCount.exhibitHumanPreferInputOpCount++;
                break;
            case "Exhibit Close To Best View Direction":
                uiOperationCount.influenceMapWeightUIOperationCount.exhibitCloseBestInputOpCount++;
                break;
            case "GoTo Radius":
                uiOperationCount.humanWalkStageUIOperationCount.StageRadiusGoToSliderOpCount++;
                break;
            case "GoTo Speed":
                uiOperationCount.humanWalkStageUIOperationCount.StageSpeedGoToSliderOpCount++;
                break;
            case "Close Radius":
                uiOperationCount.humanWalkStageUIOperationCount.StageRadiusCloseSliderOpCount++;
                break;
            case "Close Speed":
                uiOperationCount.humanWalkStageUIOperationCount.StageSpeedCloseSliderOpCount++;
                break;
            case "At Radius":
                uiOperationCount.humanWalkStageUIOperationCount.StageRadiusAtSliderOpCount++;
                break;
            case "At Speed":
                uiOperationCount.humanWalkStageUIOperationCount.StageSpeedAtSliderOpCount++;
                break;
        }
    }

    public string GetOperationAreaName(string operation)
    {
        string area = "";
        switch (operation)
        {
            case "Num of Agent":
            case "Adult Percentage":
            case "Add Agent":
            case "Start Add Range":
            case "Update Rate Gather":
            case "Update Rate Status":
            case "Update Rate Map":
                area = "Global";
                break;
            case "Walk Speed Range":
            case "Free Time Range":
            case "Gather Desire Mean":
            case "Gather Desire Std":
            case "Behavior Join Mean":
            case "Behavior Keep Alone Mean":
            case "Behavior Keep Same Group Mean":
            case "Behavior Keep Diff Group Mean":
            case "Behavior Leave Mean":
                area = "Human Feature";
                break;
            case "Capacity Limit":
            case "Popular Threshold":
            case "Crowded Threshold":
            case "Crowded Time Limit":
                area = "Exhibit";
                break;
            case "Human Influence Weight":
            case "Exhibit Influence Weight":
            case "Human Follow Desire":
            case "Human Take Time":
            case "Human Gather Desire":
            case "Human Human Type Attraction":
            case "Human Behavior Attraction":
            case "Exhibit Capacity":
            case "Exhibit Take Time":
            case "Exhibit Popular Level":
            case "Exhibit Human Preference":
            case "Exhibit Close To Best View Direction":
                area = "Influence Map";
                break;
            case "GoTo Radius":
            case "GoTo Speed":
            case "Close Radius":
            case "Close Speed":
            case "At Radius":
            case "At Speed":
                area = "Human Walk Stage";
                break;
        }
        return area;
    }
    
    public void AddUIOrderToList(string orderName)
    {
        UIOrder uiOrder = new UIOrder();
        uiOrder.orderName = orderName;
        uiOrder.orderTime = Time.time - loadingTime;
        uiOperationOrder.Add(uiOrder);
    }

    public void WriteOpertationCountToFile()
    {
        string path = dynamicSystem.instance.directory + "UIOperationCount.json";
        StringBuilder sb = new StringBuilder();
        JsonWriter writer = new JsonWriter(sb);
        writer.PrettyPrint = true;
        JsonMapper.ToJson(uiOperationCount, writer);
        string outputJsonStr = sb.ToString();
        System.IO.File.WriteAllText(path, outputJsonStr);
    }

    public void WriteOperationOrderToFile() 
    {
        string path = dynamicSystem.instance.directory + "UIOperationOrder.json";
        StringBuilder sb = new StringBuilder();
        JsonWriter writer = new JsonWriter(sb);
        writer.PrettyPrint = true;
        JsonMapper.ToJson(realUIOperationOrder, writer);
        string outputJsonStr = sb.ToString();
        System.IO.File.WriteAllText(path, outputJsonStr);
    }

}
