using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnotherFileBrowser.Windows;

public class ReplaySimulation{
    public GameObject model;
    public List<FrameData> fd = new List<FrameData>();
}

public class ReplayMode : MonoBehaviour
{
    SimulationReplayData simulationReplayData = new SimulationReplayData();
    bool Run = false, hasReplayFile = false;
    int visitorNumber = 0;
    int currentFrameIdx = 0;
    int totalFrameCount = 0;
    public GameObject peopleParent; // 放people
    public Slider frameSlider;
    public TextMeshProUGUI filename, frameInfoText;
    public Dictionary<string, ReplaySimulation> ReplaySimulationInfo = new Dictionary<string, ReplaySimulation>();

    // Start is called before the first frame update
    void OnEnable()
    {
        visitorNumber = 0;
        currentFrameIdx = 0;
        totalFrameCount = 0;
        ReplaySimulationInfo.Clear();
        frameInfoText.text = "Time : - / -";
        filename.text = "No File";
        hasReplayFile = false;
        Run = false;
        frameSlider.value = 0;
        frameSlider.maxValue = 0;
    }
    void OnDisable()
    {
        visitorNumber = 0;
        currentFrameIdx = 0;
        totalFrameCount = 0;
        ReplaySimulationInfo.Clear();
        frameInfoText.text = "Time : - / -";
        filename.text = "No File";
        hasReplayFile = false;
        Run = false;
        frameSlider.value = 0;
        frameSlider.maxValue = 0;
        //clean people
        foreach (Transform person in peopleParent.transform)
        {
            Destroy(person.gameObject);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Run && hasReplayFile)
        {
            if (currentFrameIdx > totalFrameCount)
            {
                Run = false;
                FinishPlay();
                return;
            }
            ShowFrameInfo();
            PlayEachFrame(currentFrameIdx);
            ChangeFrameSliderValue();
            currentFrameIdx++;
        }
    }

    public void LoadReplayData()
    {
        string path = "";
        var bp = new BrowserProperties();
        bp.title = "Load Replay File";
        bp.initialDir = Application.streamingAssetsPath + "/Simulation_Result";
        bp.filter = "json files (*.json)|*.json";
        bp.filterIndex = 0;

        new FileBrowser().OpenFileBrowser(bp, filepath =>
        {
            //Do something with path(string)
            Debug.Log(filepath);
            path = filepath;
        });

        /*
        string defaultFolder = Application.streamingAssetsPath + "/Simulation_Result";
        var path = EditorUtility.OpenFilePanel("Load Replay information",
                                                defaultFolder,
                                               "json");
        */
        if (path.Length != 0)
        {
            string tmpJsonDataStr = File.ReadAllText(path);
            hasReplayFile = true;
            //get file name
            string[] frac = path.Split('\\');
            filename.text = frac[frac.Length - 2];
            ReplaySimulationInfo.Clear();
            simulationReplayData = JsonMapper.ToObject<SimulationReplayData>(tmpJsonDataStr);

            //
            visitorNumber = simulationReplayData.visitorsReplayData.Count;
            totalFrameCount = simulationReplayData.visitorsReplayData[0].replayData.Count - 1; // start from 0
            
            // set scene
            ChangeScene(simulationReplayData.sceneOption);

            // load eexhibition
            LoadExhibition();

            // load character
            LoadCharacter();

            // initialize frame slider
            FrameSliderInitialize();
        }
    }
    #region Loading

    void ChangeScene(string sceneName)
    {
        if (sceneName.Contains("119")) UIController.instance.setScene("119");
        else if (sceneName.Contains("120")) UIController.instance.setScene("120");
        else if (sceneName.Contains("225")) UIController.instance.setScene("225");

        if (sceneName.Contains("A")) UIController.instance.changeOption(1);
        else if (sceneName.Contains("B")) UIController.instance.changeOption(2);
        else UIController.instance.changeOption(0);
    }

    void LoadCharacter()
    {
        for(int i = 0; i < visitorNumber; i++)
        {
            string modelName = simulationReplayData.visitorsReplayData[i].modelName;
            int age = simulationReplayData.visitorsReplayData[i].age;
            int gender = simulationReplayData.visitorsReplayData[i].gender;
            string type = GetType(gender, age);
            GameObject model = Instantiate(Resources.Load<GameObject>("CharactersPrefab/" + type + "/" + modelName), Vector3.zero, Quaternion.identity);
            model.transform.parent = peopleParent.transform;
            model.name = simulationReplayData.visitorsReplayData[i].name;
            //remove children camera gameobject -> avoid camera bug
            foreach(Transform child in model.transform)
            {
                if (child.name.Contains("Camera")) Destroy(child.gameObject);
            }

            //get start pos
            Vector3 startPos = new Vector3(
                (float)simulationReplayData.visitorsReplayData[i].replayData[0].posX,
                (float)simulationReplayData.visitorsReplayData[i].replayData[0].posY,
                (float)simulationReplayData.visitorsReplayData[i].replayData[0].posZ
                );
            Quaternion startRot = Quaternion.Euler(
                (float)simulationReplayData.visitorsReplayData[i].replayData[0].rotX,
                (float)simulationReplayData.visitorsReplayData[i].replayData[0].rotY,
                (float)simulationReplayData.visitorsReplayData[i].replayData[0].rotZ
                );
            model.transform.position = startPos;
            model.transform.rotation = startRot;
            model.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

            ReplaySimulation rs = new ReplaySimulation();
            rs.model = model;

            //Add model animation controller
            Animator animator = rs.model.GetComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("AnimationClips/ManPose");

            rs.fd = simulationReplayData.visitorsReplayData[i].replayData;

            ReplaySimulationInfo.Add(model.name, rs);
        }
    }

    string GetType(int gender, int age)
    {
        string type = "";
        if (gender == 0) // female
        {
            if (age == 0) // Girl
            {
                type = "Girl";
            }
            else if (age == 1) // young female
            {
                type = "Female_young";
            }
            else if (age == 4) // granny
            {
                type = "Female_old";
            }
            else  // 2 and 3
            {
                type = "Female";
            }
        }
        else // male
        {
            if (age == 0) // Boy
            {
                type = "Boy";
            }
            else if (age == 1) // young male
            {
                type = "Male_young";
            }
            else if (age == 4) // grandpa
            {
                type = "Male_old";
            }
            else  // 2 and 3
            {
                type = "Male";
            }
        }
        return type;
    }

    void LoadExhibition()
    {
        GameObject scene = GameObject.Find("/[EnvironmentsOfEachScene]/" + simulationReplayData.exhibitionsInScene.sceneName);
        foreach (exhibitionInfo exInfo in simulationReplayData.exhibitionsInScene.exhibitionsInfo)
        {
            GameObject ex = scene.transform.Find(exInfo.name).gameObject;
            ex.transform.position = new Vector3((float)exInfo.posX, (float)exInfo.posY, (float)exInfo.posZ);
            ex.transform.rotation = Quaternion.Euler((float)exInfo.rotX, (float)exInfo.rotY, (float)exInfo.rotZ);
            ex.transform.localScale = new Vector3((float)exInfo.sclX, (float)exInfo.sclY, (float)exInfo.sclZ);

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
    }
    #endregion

    #region Play
    void ShowFrameInfo()
    {
        frameInfoText.text = "Time : " + (currentFrameIdx / 50.0f).ToString("f2") + " / " + (totalFrameCount / 50.0f).ToString("f2");
    }
    void PlayEachFrame(int currentFrameIdx)
    {
        foreach (KeyValuePair<string, ReplaySimulation> rs in ReplaySimulationInfo)
        {
            if (rs.Value.fd[currentFrameIdx].isVisible)
            {
                rs.Value.model.SetActive(true);
                Vector3 pos = new Vector3((float)rs.Value.fd[currentFrameIdx].posX,
                                          (float)rs.Value.fd[currentFrameIdx].posY,
                                          (float)rs.Value.fd[currentFrameIdx].posZ
                                         );
                Quaternion rot = Quaternion.Euler((float)rs.Value.fd[currentFrameIdx].rotX,
                                                  (float)rs.Value.fd[currentFrameIdx].rotY,
                                                  (float)rs.Value.fd[currentFrameIdx].rotZ
                                                 );
                rs.Value.model.transform.position = pos;
                rs.Value.model.transform.rotation = rot;

                //handle animation
                rs.Value.model.GetComponent<Animator>().SetBool("walk", rs.Value.fd[currentFrameIdx].animationWalk);

                //handle animation speed
                /* set animation Speed (idle ~ walk) */
                float newAnimeSpeed = (float)rs.Value.fd[currentFrameIdx].navAgentVelocity / 1.05f;
                if (Math.Abs(newAnimeSpeed - 0.2) < 0.1 && Math.Abs(newAnimeSpeed - rs.Value.fd[currentFrameIdx].animationSpeed) < 0.2) { }
                else
                {
                    if (newAnimeSpeed <= 0.2f)
                    {
                        rs.Value.model.GetComponent<Animator>().SetFloat("speed", 0.2f, 0.01f, Time.deltaTime);
                        rs.Value.model.GetComponent<Animator>().SetFloat("walkSpeed", 1);
                    }
                    else
                    {
                        rs.Value.model.GetComponent<Animator>().SetFloat("speed", 1f, 0.01f, Time.deltaTime);
                        rs.Value.model.GetComponent<Animator>().SetFloat("walkSpeed", newAnimeSpeed);
                    }
                }

            } // is visible
            else
            {
                rs.Value.model.SetActive(false);
            }
        }
    }

    void HandleRotation()
    {

    }

    void HandleAnimation()
    {

    }
    #endregion

    #region Buttons & Slider
    public void Play()
    {
        if(hasReplayFile) Run = true;
    }

    public void Pause()
    {
        if (hasReplayFile)
        {
            Run = false;
            foreach (KeyValuePair<string, ReplaySimulation> rs in ReplaySimulationInfo)
            {
                rs.Value.model.GetComponent<Animator>().SetBool("walk", false);
            }
        }
    }


    public void End()
    {
        if (hasReplayFile)
        {
            Run = false;
            currentFrameIdx = totalFrameCount;
            FinishPlay();
            ChangeFrameSliderValue();
            ShowFrameInfo();
        }
    }

    public void Replay()
    {
        if (hasReplayFile)
        {
            Run = true;
            currentFrameIdx = 0;
        }
    }

    void FinishPlay()
    {
        foreach(Transform person in peopleParent.transform)
        {
            person.gameObject.SetActive(false);
        }
    }
    
    public void FrameSliderOnValuedChanged()
    {
        int value = (int)frameSlider.value;
        currentFrameIdx = value;
        ShowFrameInfo();
        PlayEachFrame(value);
        if (!Run) Pause();
    }

    void FrameSliderInitialize()
    {
        frameSlider.maxValue = totalFrameCount;
        frameSlider.value = 0;
        ShowFrameInfo();
    }

    void ChangeFrameSliderValue()
    {
        frameSlider.value = currentFrameIdx;
    }
    #endregion
}
