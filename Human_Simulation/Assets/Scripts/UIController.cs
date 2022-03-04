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

    /*right panel in simulation mode UI*/
    public GameObject settingUIBoard;
    public GameObject modifyController;
    public GameObject saveLoadPanel;
    bool openSaveLoadPanel;
    public bool modifyScene;

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

    void Start()
    {
        //ui setting
        openSaveLoadPanel = false;
        modifyScene = false;

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
    public void ModifySceneButton()
    {
        if (!curOption.Contains("A") && !curOption.Contains("B"))
        {
            ShowMsgPanel("Warning", "Please choose A or B option of the scene.");
            return;
        }
        modifyScene = !modifyScene;
        if (modifyScene)
        {
            int childCount = simulationModeUI.transform.childCount;
            modifyController.transform.SetSiblingIndex(childCount - 1);
        }
        else
        {
            int childCount = simulationModeUI.transform.childCount;
            settingUIBoard.transform.SetSiblingIndex(childCount - 1);
        }

        string sceneHeadName = "119"; //default 119
        int idx = 0;
        for(int i = 0; i < allScene.Count; i++)
        {
            if (curOption.Contains(allScene[i]))
            {
                sceneHeadName = allScene[i];
            }
            if (curOption.Contains("A")) idx = 1;
            if (curOption.Contains("B")) idx = 2;
        }

        switch (sceneHeadName) {
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

    void Swap(string sceneHeadName, int idx, Vector3 minimapCameraPos, float miniCameraSize, float mainCameraFOV)
    {
        Camera mainCamera;
        Camera minimapCamera;
        RenderTexture minimapRT = (RenderTexture)AssetDatabase.LoadAssetAtPath("Assets/Resources/Materials/miniMapRenderTexture.renderTexture", typeof(RenderTexture));
        Vector3 zOffset = new Vector3(0, 0, 50 * idx);
        mainCamera = cameras[sceneHeadName].mainCamera.GetComponent<Camera>();
        minimapCamera = cameras[sceneHeadName].minimapCamera.GetComponent<Camera>();

        if (modifyScene)
        {
            DashBoard.SetActive(false);
            cameras[sceneHeadName].mainCamera.tag = "Untagged";
            cameras[sceneHeadName].minimapCamera.tag = "MainCamera";
            mainCamera.targetTexture = minimapRT;
            minimapCamera.targetTexture = null;
            //change minimap camera
            cameras[sceneHeadName].minimapCamera.transform.position = minimapCameraPos + zOffset;
            minimapCamera.orthographicSize = miniCameraSize;
            cameras[sceneHeadName].minimapCamera.GetComponent<minimapCameraController>().Initial();
            //change main camera
            mainCamera.fieldOfView = mainCameraFOV;       
        }
        else
        {
            DashBoard.SetActive(true);
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
    #endregion

    //Save/Load Button Functions
    public void SaveLoadButton()
    {
        openSaveLoadPanel = !openSaveLoadPanel;
        if(modifyScene) ModifySceneButton();
        if (openSaveLoadPanel)
        {
            int childCount = simulationModeUI.transform.childCount;
            saveLoadPanel.transform.SetSiblingIndex(childCount - 1);
        }
        else
        {
            int childCount = simulationModeUI.transform.childCount;
            settingUIBoard.transform.SetSiblingIndex(childCount - 1);
        }
    }

    /* update when mode or scene change*/
    public void setScene(string sceneName)
    {
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

        changeOption(0);
    }

    public void changeOption(int index)
    {
        if (!modifyScene)
        {
            // original : 0, A: 1, B: 2
            curOption = curSceneOptions[index];
            cameras[currentScene].mainCamera.transform.position = cameraPos[index];
            cameras[currentScene].minimapCamera.transform.position = miniCameraPos[index];

            if (currentMode == "RealDataVisualization")
            {
                realDataDrawTest.instance.cleanBeforeRead();
            }
            else  // DynamicSimulation
            {
                dynamicSystem.instance.cleanPeopleBeforeGenerate();
            }
        }
    }

    public void changeSettings()
    {
        tmpSaveUISettings = allSceneSettings[currentScene].customUI.copy();
        loadSettingToUI(tmpSaveUISettings);
    }
    
    public void switchMode(string mode)
    {
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
        tmpSaveUISettings = allSceneSettings[currentScene].oriJson.copy();
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
    }

    void loadSettingsFromJson(string scene)  // only do at start, load local file
    {
        settingsClass newSceneSetting = new settingsClass();
        string dirPath = Application.streamingAssetsPath + "/SettingsJson/";
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
        string[] allFiles = Directory.GetFiles(dirPath, "unitySettings_" + scene + "*.json"); 
        if (allFiles.Length != 0)
        {
            string newestJsonDataFileNames = allFiles.OrderByDescending(f => f.Substring(f.Substring(0, f.LastIndexOf("_")).LastIndexOf('_') + 1)).ToList()[0];
            Debug.Log("Load json: " + newestJsonDataFileNames);
            newSceneSetting.customUI = JsonMapper.ToObject<UISettings>(File.ReadAllText(newestJsonDataFileNames));
        }
        else
            newSceneSetting.customUI = newSceneSetting.oriJson.copy();

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
            if (tmpSaveUISettings.UI_Human.freeTimeMin >= tmpSaveUISettings.UI_Human.freeTimeMax)
            {
                errorMessage += "- freeTime Min should be <= to Max\n";
            }
        }

        /** Exhibition **/
        if (tmpSaveUISettings.UI_Exhibit.popularThreshold >= tmpSaveUISettings.UI_Exhibit.crowdedThreshold)
            errorMessage += "- popular Threshold should be <= to crowded Threshold\n";

        /** influence Map **/
        /* check exhibit influence total */
        float total = 0f;
        foreach (float value in tmpSaveUISettings.UI_InfluenceMap.exhibitInflence.Values)
        {
            total += value;
        }
        if (total != 1) errorMessage += "- exhibit Influence total is "+ total + " not 100\n";
        /* check human influence total */
        total = 0f;
        foreach (float value in tmpSaveUISettings.UI_InfluenceMap.exhibitInflence.Values)
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
            var path = EditorUtility.SaveFilePanel("Save UI setting as JSON",
                                                    defaultFolder,
                                                    defaultFileName + ".json",
                                                    "json");

            if(path.Length != 0)
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
        string defaultFolder = Application.streamingAssetsPath + "/UISetting";
        var path = EditorUtility.OpenFilePanel("Load UI Setting",
                                                defaultFolder,
                                               "json");
        
        if(path.Length != 0)
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
        var path = EditorUtility.SaveFilePanel("Save exhibitions information as JSON",
                                                defaultFolder,
                                                defaultFileName + ".json",
                                                "json");
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
        string defaultFolder = Application.streamingAssetsPath + "/ExhibitionSetting";
        var path = EditorUtility.OpenFilePanel("Load exhibitions information",
                                                defaultFolder,
                                               "json");
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
        else if (type == "Success") msgType.color = Color.green;
        
        msgContent.text = content;
        msgPanel.SetActive(true);
    }

    public void CloseMsgPanel()
    {
        msgPanel.SetActive(false);
    }
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
    /* Immediate change */
    public Text StageRadiusGoToText, StageRadiusCloseText, StageRadiusAtText;
    public Slider StageRadiusGoToSlider, StageRadiusCloseSlider, StageRadiusAtSlider;
    public Text StageSpeedGoToText, StageSpeedCloseText, StageSpeedAtText;
    public Slider StageSpeedGoToSlider, StageSpeedCloseSlider, StageSpeedAtSlider;
    /* Update UI*/
    /* Global */
    public void changeChosenAgentCount()
    {
        int value = (int)agentCountSlider.value;
        agentCountText.text = (value).ToString();
        tmpSaveUISettings.UI_Global.agentCount = value;
        addAgentCountSlider.maxValue = value;
        changeAddAgentCount();
    }

    public void changeAdultPercent()
    {
        float value = (float)(adultPercentSlider.value / 100);  // % -> 0.xx
        adultPercentText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Global.adultPercentage = value;
    }

    public void changeAddAgentCount()
    {

        int value = (int)addAgentCountSlider.value;
        addAgentCountText.text = (value).ToString();
        tmpSaveUISettings.UI_Global.addAgentCount = value;
    }

    public void changeStartAddAgentMin()
    {
        int value = int.Parse(startAddAgentMinInput.text);
        tmpSaveUISettings.UI_Global.startAddAgentMin = value;
    }

    public void changeStartAddAgentMax()
    {
        int value = int.Parse(startAddAgentMaxInput.text);
        tmpSaveUISettings.UI_Global.startAddAgentMax = value;
    }

    public void changeUpdateRateGather()
    {
        int value = (int)updateRateGatherSlider.value;
        updateRateGatherText.text = (value).ToString() + "s";
        tmpSaveUISettings.UI_Global.UpdateRate["gathers"] = value;
    }

    public void changeUpdateRateStatus()
    {
        int value = (int)updateRateStatusSlider.value;
        updateRateStatusText.text = (value).ToString() + "s";
        tmpSaveUISettings.UI_Global.UpdateRate["stopWalkStatus"] = value;
    }

    public void changeUpdateRateMap()
    {
        int value = (int)updateRateMapSlider.value;
        updateRateMapText.text = (value).ToString() + "s";
        tmpSaveUISettings.UI_Global.UpdateRate["influenceMap"] = value;
    }

    public void changeTimescale()
    {
        int value = (int)timeScaleSlider.value;
        timeScaleText.text = value.ToString();
        Time.timeScale = value;
    }

    /* Human */
    public void changeWalkSpeedMin()
    {
        int value = int.Parse(walkSpeedMinInput.text);
        tmpSaveUISettings.UI_Human.walkSpeedMin = value;
    }

    public void changeWalkSpeedMax()
    {
        int value = int.Parse(walkSpeedMaxInput.text);
        tmpSaveUISettings.UI_Human.walkSpeedMax = value;
    }

    public void changeFreeTimeMin()
    {
        int value = int.Parse(freeTimeMinInput.text);
        tmpSaveUISettings.UI_Human.freeTimeMin = value;
    }

    public void changeFreeTimeMax()
    {
        int value = int.Parse(freeTimeMaxInput.text);
        tmpSaveUISettings.UI_Human.freeTimeMax = value;
    }

    public void changeGatherMeanProbability()
    {
        float value = (float)(gatherProbabilityMeanSlider.value / 100);  // % -> 0.xx
        gatherProbabilityMeanText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Human.gatherProbability.mean = value;
    }

    public void changeGatherStdProbability()
    {
        float value = (float)(gatherProbabilityStdSlider.value / 100);  // % -> 0.xx
        gatherProbabilityStdText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Human.gatherProbability.std = value;
    }

    public void changeJoinProbability()
    {
        float value = (float)(joinProbabilitySlider.value / 100);  // % -> 0.xx
        joinProbabilityText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Human.behaviorProbability["join"].mean = value;
    }

    public void changeKeepAloneProbability()
    {
        float value = (float)(keepAloneProbabilitySlider.value / 100);  // % -> 0.xx
        keepAloneProbabilityText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Human.behaviorProbability["keepAlone"].mean = value;
    }

    public void changeKeepGatherSameGroupProbability()
    {
        float value = (float)(keepGatherSameGroupProbabilitySlider.value / 100);  // % -> 0.xx
        keepGatherSameGroupProbabilityText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Human.behaviorProbability["keepGather_sameGroup"].mean = value;
    }

    public void changeKeepGatherDifGroupProbability()
    {
        float value = (float)(keepGatherDifGroupProbabilitySlider.value / 100);  // % -> 0.xx
        keepGatherDifGroupProbabilityText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Human.behaviorProbability["keepGather_difGroup"].mean = value;
    }

    public void changeLeaveProbability()
    {
        float value = (float)(leaveProbabilitySlider.value / 100);  // % -> 0.xx
        leaveProbabilityText.text = (value).ToString("F2");
        tmpSaveUISettings.UI_Human.behaviorProbability["leave"].mean = value;
    }

    /* Exhibit */
    public void changeCapacityLimitTime()
    {
        float value = (float)capacityTimesSlider.value;
        capacityTimesText.text = (value).ToString();
        tmpSaveUISettings.UI_Exhibit.capacityLimitTimes = value;
    }

    public void changePopularThreshold()
    {
        int value = (int)popularThresholdSlider.value;
        popularThresholdText.text = (value).ToString() + "%";
        tmpSaveUISettings.UI_Exhibit.popularThreshold = value / 100f;
    }

    public void changeCrowdedThreshold()
    {
        int value = (int)crowdedThresholdSlider.value;
        crowdedThresholdText.text = (value).ToString() + "%";
        tmpSaveUISettings.UI_Exhibit.crowdedThreshold = value / 100f;
    }

    public void changeCrowdedTimeLimit()
    {
        int value = (int)crowdedTimeLimitSlider.value;
        crowdedTimeLimitText.text = (value).ToString() + "s";
        tmpSaveUISettings.UI_Exhibit.crowdedTimeLimit = value;
    }

    /* Influence Map */
    public void changeHumanInfluenceWeight()
    {
        float value = (float)weightHumanSlider.value;
        weightHumanText.text = (value).ToString("F1");
        tmpSaveUISettings.UI_InfluenceMap.weightHuman = value;
    }

    public void changeExhibitInfluenceWeight()
    {
        float value = (float)weightExhibitSlider.value;
        weightExhibitText.text = (value).ToString("F1");
        tmpSaveUISettings.UI_InfluenceMap.weightExhibit = value;
    }

    public void changeHumanInfluence_followDesire()
    {
        int value = int.Parse(humanFollowDesireInput.text);
        tmpSaveUISettings.UI_InfluenceMap.humanInflence["followDesire"] = value / 100f;
    }

    public void changeHumanInfluence_takeTime()
    {
        int value = int.Parse(humanTakeTimeInput.text);
        tmpSaveUISettings.UI_InfluenceMap.humanInflence["takeTime"] = value / 100f;
    }

    public void changeHumanInfluence_gatherDesire()
    {
        int value = int.Parse(humanGatherDesireInput.text);
        tmpSaveUISettings.UI_InfluenceMap.humanInflence["gatherDesire"] = value / 100f;
    }

    public void changeHumanInfluence_humanTypeAttraction()
    {
        int value = int.Parse(humanTypeAttractInput.text);
        tmpSaveUISettings.UI_InfluenceMap.humanInflence["humanTypeAttraction"] = value / 100f;
    }

    public void changeHumanInfluence_behaviorAttraction()
    {
        int value = int.Parse(humanBehaviorAttractInput.text);
        tmpSaveUISettings.UI_InfluenceMap.humanInflence["behaviorAttraction"] = value / 100f;
    }

    public void changeExhibitInfluence_capactiy()
    {
        int value = int.Parse(exhibitCapacityInput.text);
        tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["capactiy"] = value / 100f;
    }

    public void changeExhibitInfluence_takeTime()
    {
        int value = int.Parse(exhibitTakeTimeInput.text);
        tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["takeTime"] = value / 100f;
    }

    public void changeExhibitInfluence_popularLevel()
    {
        int value = int.Parse(exhibitPopularLvInput.text);
        tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["popularLevel"] = value / 100f;
    }

    public void changeExhibitInfluence_humanPreference()
    {
        int value = int.Parse(exhibitHumanPreferInput.text);
        tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["humanPreference"] = value / 100f;
    }

    public void changeExhibitInfluence_closeToBestViewDirection()
    {
        int value = int.Parse(exhibitCloseBestInput.text);
        tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["closeToBestViewDirection"] = value / 100f;
    }

    /* Immediate change variables */
    public void changeStageRadius_GoTo()
    {
        float value = (float)StageRadiusGoToSlider.value;
        StageRadiusGoToText.text = (value).ToString();
        tmpSaveUISettings.walkStage["GoTo"].radius = value;
    }
    public void changeStageSpeed_GoTo()
    {
        float value = (float)StageSpeedGoToSlider.value;
        StageSpeedGoToText.text = "x " + (value).ToString();
        tmpSaveUISettings.walkStage["GoTo"].speed = value;
    }
    public void changeStageRadius_Close()
    {
        float value = (float)StageRadiusCloseSlider.value;
        StageRadiusCloseText.text = (value).ToString();
        tmpSaveUISettings.walkStage["Close"].radius = value;
    }
    public void changeStageSpeed_Close()
    {
        float value = (float)StageSpeedCloseSlider.value;
        StageSpeedCloseText.text = "x " + (value).ToString();
        tmpSaveUISettings.walkStage["Close"].speed = value;
    }
    public void changeStageRadius_At()
    {
        float value = (float)StageRadiusAtSlider.value;
        StageRadiusAtText.text = (value).ToString();
        tmpSaveUISettings.walkStage["At"].radius = value;
    }
    public void changeStageSpeed_At()
    {
        float value = (float)StageSpeedAtSlider.value;
        StageSpeedAtText.text = "x " + (value).ToString();
        tmpSaveUISettings.walkStage["At"].speed = value;
    }

    public void LoadTmpSettingToCurrentSceneSettings(){
        allSceneSettings[currentScene].customUI = tmpSaveUISettings.copy();
        dynamicSystem.instance.currentSceneSettings = allSceneSettings[currentScene];
    }
}
