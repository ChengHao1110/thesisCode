using LitJson;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ReplaySimulation{
    public GameObject model;
    public List<FrameData> fd = new List<FrameData>();
}

public class ReplayMode : MonoBehaviour
{
    SimulationReplayData simulationReplayData = new SimulationReplayData();
    bool Run = false;
    int visitorNumber = 0;
    int currentFrameIdx = 0;
    public GameObject peopleParent; // 放people
    public Button loadReplayDataButton, playButton;
    public Dictionary<string, ReplaySimulation> ReplaySimulationInfo = new Dictionary<string, ReplaySimulation>();

    // Start is called before the first frame update
    void Start()
    {
        loadReplayDataButton.onClick.AddListener(delegate { LoadReplayData(); });
        playButton.onClick.AddListener(delegate { Play(); });
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Run)
        {
            foreach(KeyValuePair<string, ReplaySimulation> rs in ReplaySimulationInfo)
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
                }
                else
                {
                    rs.Value.model.SetActive(false);
                }
            }
            currentFrameIdx++;
        }
    }

    public void LoadReplayData()
    {
        string defaultFolder = Application.streamingAssetsPath + "/Simulation_Result";
        var path = EditorUtility.OpenFilePanel("Load exhibitions information",
                                                defaultFolder,
                                               "json");
        if (path.Length != 0)
        {
            string tmpJsonDataStr = File.ReadAllText(path);
            simulationReplayData = JsonMapper.ToObject<SimulationReplayData>(tmpJsonDataStr);

            //
            visitorNumber = simulationReplayData.visitorsReplayData.Count;

            // set scene
            ChangeScene(simulationReplayData.sceneOption);

            // load character
            LoadCharacter();


        }
    }

    void Play()
    {
        Run = true;
    }

    void HandleRotation()
    {

    }

    void HandleAnimation()
    {

    }

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

            ReplaySimulation rs = new ReplaySimulation();
            rs.model = model;
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
}
