﻿using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using LitJson;
using UnityEditor;
using UnityEngine.Animations.Rigging;
using AnotherFileBrowser.Windows;

public class desiredListStatus
{
    public Dictionary<string, listOrderPair> peopleTrajectory;
    public double endSimulationTime = 0;
    public int finishDesireListCount = 0;
    public int leaveEarlyCount = 0;
    public double leaveEarly_avg = 0;
    public int leaveLateCount = 0;
    public double leaveLate_avg = 0;
}

public class listOrderPair
{
    public string humanType;
    public double walkSpeed = 0;
    public double travelTime = 0;
    public List<string> desiredList;
    public List<string> trajectoryOrder;
    public List<List<double>> fullTrajectory;
    public List<gatheringEvent> gatherEvent;
    public bool allvisit;
    public bool earlyLeave;
}

public class wrapExhibitionRecord
{
    public double popularThreshold;
    public double crowdedThreshold;
    public Dictionary<string, exhibitionRecord> rec;
}

public class exhibitionRecord
{
    public Dictionary<string, int> bestViewCount; // direction: human count
    public List<exhibitionInfluence> influences;
    public List<double> stayingTime;
    public int humanCount; // count those who stay in the exhibit
}

public class exhibitionInfluence
{
    public string mainHuman;
    public double occupancy;
    public double influence;
}

public class gatheringEvent
{
    public double time;
    public string preState;
    public string curState;
}

// visitior initial setting
public class humanInfo 
{
    public string name;
    public int age;
    public int gender;
    public string humanType;
    public double freeTime_total;
    public string modelName;
    public List<string> desireExhibitionList;
    public double walkSpeed;
    public double gatherDesire;
    public double startSimulateTime;
    public int randomSeed;
}

// record simulation
public class SimulationReplayData 
{
    public string sceneOption;
    public List<VisitorReplayData> visitorsReplayData = new List<VisitorReplayData>();
    public exhibitionsInScene exhibitionsInScene = new exhibitionsInScene();
}

public class VisitorReplayData
{
    public string name;
    public int age;
    public int gender;
    public string modelName;
    public List<FrameData> replayData = new List<FrameData>();
}

public partial class dynamicSystem : PersistentSingleton<dynamicSystem>
{
    /*NavMeahSurface*/
    public NavMeshSurface navMeshSurface;

    /* unity UI */
    public GameObject peopleParent;
    public Text timeText, fpsText;
    public GameObject signPrefab, signPrefab2, signPrefab3;
    public GameObject informationBoardPrefab, informationBoardPrefab_exhibit;
    public GameObject markerPrefab_human, markerPrefab_exhibit, markersParent, ballMarker;
    public GameObject cylinderCollider;
    public Toggle humanBoardToggle, exhibitToggle, targetSignToggle, colliderShapeToggle, exhibitRangeToggle, heatmapToggle;
    public GameObject screenshotCamera;


    /* can set by others */
    public settingsClass currentSceneSettings; // will be used as a pointer to the settings, do not change value here
    // public string curScene;    
    // public string curOption;

    /* system use - */
    public bool Run = false;  // signal to start running result
    public bool afterGenerate = false;
    public Dictionary<string, human_single> people = new Dictionary<string, human_single>();
    public Dictionary<string, human_gather> peopleGathers = new Dictionary<string, human_gather>();
    public Dictionary<string, exhibition_single> exhibitions = new Dictionary<string, exhibition_single>();
    public Dictionary<string, exhibition_single> exits = new Dictionary<string, exhibition_single>();
    public Dictionary<string, exhibitionRecord> exhibitRec = new Dictionary<string, exhibitionRecord>();
    int adultCount;
    public float deltaTimeCounter = 0f;
    public float storeTimeCounter = 0f;
    NavMeshPath path;
    public int walkableMask;
    public bool showInfoBoard_human = true;
    int gatherCounter = 0;
    public float updateVisBoard = 0;
    

    /* fixed parameters */
    //public System.Random random = new System.Random();  // public for humanClass
    public float halfW = 960, halfH = 540;
    float difGroupUpperThreshold, leaveUpperThreshold, joinUpperThreshold;

    /* record time as performance test */    
    float fps_deltaTime = 0.0f;
    List<double> fpsList = new List<double>();
    float influenceMap_startTime, influenceMap_endTime;
    List<double> analysis_influenceMap = new List<double>();
    float updatePosition_startTime, updatePosition_endTime;
    List<double> analysis_updatePosition = new List<double>();

    /*social distance*/
    List<float> socialDistance = new List<float>();
    float updateSocialDistance = 0;

    /*realtime ex human count*/
    float updateExhibitionRealtimeHumanCount = 0;

    /*store trajectory time*/
    public float trajectoryRecordTime = 0.1f; //influence heatmap maxValue in UIController

    /*directory where data analysis store in*/
    public string directory;

    /*visitors initial setting */
    public bool saveVIS = false, loadVIS = false, randomVIS = false;
    public Button saveVISBtn, loadVISBtn, randomVISBtn;
    string saveVISFilename = "", loadVISFilename = "";

    /*realtime heatmap*/
    public float updateHeatmapTime = 0;
    public HeatMap_Float heatMap_Float;
    public float[,] matrix, rotMatrix;
    public float[,] staticMatrix;
    public int matrixSize = 500, gaussianFilterSize = 10, sceneSize = 22;
    public float moveMaxLimit = 5000, stayMaxLimit = 5000;
    public bool useGaussian = false;
    public bool firstGenerateHeatmap = true;
    public GameObject Heatmap;
    public int gaussian_rate = 4;
    float[] gaussianValue;
    public string heatmapMode = "static", heatmapFilename = "";

    //replay mode
    public Dictionary<string, string> modelName = new Dictionary<string, string>();

    public bool generateAnalyzeData = true;

    public bool quickSimulationMode = false;
    public GameObject mainCamera, miniCamera;

    //exhibition target point isUse
    public Dictionary<string, bool> isTargetPointUse = new Dictionary<string, bool>();
    public Dictionary<string, GameObject> targetPointBall = new Dictionary<string, GameObject>();

    /*dashboard & info pos panel*/
    public GameObject dashboard, infoPos;

    /* update information */
    void updatePeople()
    {
        if (allPeopleFinish()) // all people finish, stop the time counter.
        {
            Run = false;
            if (generateAnalyzeData)
            {
                if (quickSimulationMode)
                {
                    QuickSimulationModeQuit();
                }
                UIController.instance.ShowMsgPanel("Notice", "Wait for saving simulation analysis data.");
                writeLog_fps("viewMode_fps", fpsList);
                writeLog_fps("compute_influenceMap", analysis_influenceMap);
                writeLog_fps("compute_updatePosition", analysis_updatePosition);

                logTheLeavingAnalysis();
                wrapExhibitionRecord tmpRec = new wrapExhibitionRecord();
                tmpRec.rec = exhibitRec;
                tmpRec.popularThreshold = currentSceneSettings.customUI.UI_Exhibit.popularThreshold;
                tmpRec.crowdedThreshold = currentSceneSettings.customUI.UI_Exhibit.crowdedThreshold;
                writeLog_fps("simulation_exhibit", tmpRec);

                /*heatmap*/
                TrajectoryToHeatmapWithGaussian(matrixSize, sceneSize / 2, gaussian_rate, true, 0, "both");

                //exhibition transform
                ExhibitionsTransform();

                RecordExhibitionRealtimeHumanCount();
                RecordVisitingTimeInEachExhibition();
                RecordSocialDistance();
                RecordVisitorStatusTime();
                RecordSatisfiction();
                Debug.Log("analyze finish");

                SaveReplayDataToLocal();
                Debug.Log("replay save");
                UIController.instance.ShowMsgPanel("Success", "Finish saving simulation analysis data.");
            }
        }

        //NavMeshBake();
        // system simulating
        UpdateIsTargetPointUseStatus();
        //print(Time.fixedDeltaTime);
        if (influenceMapVisualize.instance.showInfoPanel.activeSelf)
        {
            influenceMapVisualize.instance.ShowVisitorInfoOnRightTopPanel();
            influenceMapVisualize.instance.ShowExhibitInfoOnRightTopPanel();
        }
        foreach (KeyValuePair<string, human_single> person in people)
        {
            if (person.Value.startSimulateTime <= deltaTimeCounter)
            {
                updatePosition_startTime = Time.realtimeSinceStartup;

                /* Check if visit all and get to the last target, if yes then stop all behavior */
                if (person.Value.checkIfFinishVisit())
                {
                    // Debug.Log(person.Key + " finish all visit");
                    person.Value.ifMoveNavMeshAgent(false);
                    person.Value.modelVisible(false);
                }
                /* Keep simulating */
                else
                {
                    person.Value.modelVisible(true);
                    
                    /*record the status time*/
                    if (person.Value.status == "close") person.Value.statusTime[1] += Time.fixedDeltaTime;
                    else if (person.Value.status == "at") person.Value.statusTime[2] += Time.fixedDeltaTime;
                    else person.Value.statusTime[0] += Time.fixedDeltaTime;

                    //check just leave
                    /*
                    if (person.Value.justIn && person.Value.justInTimer <= 2.0f)
                    {
                        person.Value.justInTimer += Time.fixedDeltaTime;
                        if(person.Value.justInTimer > 2.0f)
                        {
                            person.Value.justIn = false;
                            person.Value.justInTimer = 0.0f;
                        }
                    }
                    */
                    /* calculate stopStateContinuedTime*/
                    if (person.Value.walkStopState == "stop") person.Value.stopStateContinuedTime += Time.fixedDeltaTime;
                    else person.Value.stopStateContinuedTime = 0f;

                    /* gather state machine */
                    //calculate gather
                    int humanCount = 0;
                    Vector3 sumPosition = Vector3.zero;
                    foreach (KeyValuePair<string, human_gather> g in peopleGathers)
                    {
                        if (g.Key == person.Value.gatherIndex && g.Value.humans.Count() != 1)
                        {
                            //calculate group center
                            foreach (string otherPerson in g.Value.humans)
                            {
                                humanCount++;
                                sumPosition += people[otherPerson].currentPosition;
                            }
                            break;
                        }
                    }
                    Vector3 groupCenter = sumPosition / humanCount;
                    float distanceToGroup = Vector3.Distance(person.Value.currentPosition, groupCenter);
                    if (distanceToGroup > 2.0f)
                    {
                        Debug.Log(person.Value.name + " too far change group");
                        human_gather newGather = new human_gather();
                        newGather.humans.Add(person.Value.name);
                        string newKey = "group" + gatherCounter.ToString();

                        newGather.markColor = generateColorByIndex(gatherCounter + 1);
                        person.Value.groupColorIndex = gatherCounter;

                        gatherCounter++;
                        peopleGathers.Add(newKey, newGather);

                        peopleGathers[person.Value.gatherIndex].humans.Remove(person.Value.name);
                        if (peopleGathers[person.Value.gatherIndex].humans.Count == 0) peopleGathers.Remove(person.Value.gatherIndex);
                        person.Value.gatherIndex = newKey; // update gatherIndex
                    };
                    //update color
                    if (peopleGathers[person.Value.gatherIndex].humans.Count == 1) // alone
                    {
                        //person.gatherMarker.GetComponent<MeshRenderer>().material.color = Color.white;
                        person.Value.gatherMarker.GetComponent<MeshRenderer>().material.color = peopleGathers[person.Value.gatherIndex].markColor;
                        person.Value.gatherMarker.SetActive(false);
                    }
                    else
                    {
                        person.Value.gatherMarker.GetComponent<MeshRenderer>().material.color = peopleGathers[person.Value.gatherIndex].markColor;

                        person.Value.gatherMarker.SetActive(true);
                    }

                    if (deltaTimeCounter - person.Value.lastTimeStamp_recomputeGathers > currentSceneSettings.customUI.UI_Global.UpdateRate["gathers"])
                    {
                        changeGathersWithProbability(person.Value);
                        if (person.Key == influenceMapVisualize.instance.mainHumanName) influenceMapVisualize.instance.influenceMapUpdate();
                        person.Value.lastTimeStamp_recomputeGathers = deltaTimeCounter;
                    }
                    
                    /* compute new influence map, remember to update exhibition information first */
                    if (deltaTimeCounter - person.Value.lastTimeStamp_recomputeMap > currentSceneSettings.customUI.UI_Global.UpdateRate["influenceMap"])  // every x seconds
                    {
                        /* recompute exit, to get the closer one */
                        float minDis = calculateDistance(person.Value.currentPosition, exits[person.Value.exitName].centerPosition);
                        string newExitName = person.Value.exitName;
                        foreach (KeyValuePair<string, exhibition_single> exit in exits)
                        {
                            float dis = calculateDistance(person.Value.currentPosition, exit.Value.centerPosition);
                            if (dis < minDis)
                            {
                                minDis = dis;
                                newExitName = exit.Key;
                            }
                        }
                        if (newExitName != person.Value.exitName) // change desireList and exitName
                        {
                            person.Value.desireExhibitionList.Remove(person.Value.exitName);
                            person.Value.exitName = newExitName;
                            person.Value.desireExhibitionList.Add(newExitName);
                        }

                        /* save new */
                        // Debug.Log(string.Join(", ", person.Value.desireExhibitionList));
                        computeNewInfluenceMap(person.Value);
                        person.Value.updateInfluenceMap();
                        if (person.Key == influenceMapVisualize.instance.mainHumanName) influenceMapVisualize.instance.influenceMapUpdate();
                        person.Value.lastTimeStamp_recomputeMap = deltaTimeCounter;
                    }
                       
                    person.Value.freeTime_totalLeft -= Time.fixedDeltaTime;

                    float personDistanceWithExit = calculateDistance(person.Value.currentPosition, exits[person.Value.exitName].leavePosition);
                    float personNeededTimeToExit = personDistanceWithExit / person.Value.agent.speed + 2; // add 2 second for save

                    // Debug.Log("free: " + person.Value.freeTime_totalLeft + ", time to exit: " + personNeededTimeToExit);
                    /* stay in the target, watching the exhibition
                     * Not update target until time is not enough or finish watching*/


                    if ((person.Value.checkIfArriveTarget() || person.Value.wanderAroundExhibit) && person.Value.freeTime_totalLeft > personNeededTimeToExit)
                    {
                        //get view point list
                        if (person.Value.lookExhibitionStatus == "None" && person.Value.lastLookExhibitionName != person.Value.nextTarget_name && person.Value.nextTarget_name.StartsWith("p"))
                        {
                            person.Value.viewPoints.Clear();
                            //Debug.Log(person.Value.nextTarget_name);
                            Transform viewPointsInExhibition = exhibitions[person.Value.nextTarget_name].model.transform.parent.Find("ViewPoint");
                            foreach (Transform viewPoint in viewPointsInExhibition)
                            {
                                //calculate distance
                                float dist = Vector3.Distance(person.Value.head.transform.position, viewPoint.position);
                                ViewPointAttribute viewPointAttribte = new ViewPointAttribute();
                                viewPointAttribte.viewPoint = viewPoint.gameObject;
                                viewPointAttribte.distance = dist;
                                person.Value.viewPoints.Add(viewPointAttribte);
                                //Debug.Log("choose " + viewPoint.name);

                            }

                            //sort
                            person.Value.viewPoints.Sort(delegate (ViewPointAttribute v1, ViewPointAttribute v2) {
                                return v1.distance.CompareTo(v2.distance);                                                                    
                            });

                            //Debug
                            /*
                            for(int i = 0; i < person.Value.viewPoints.Count; i++)
                            {
                                Debug.Log(person.Value.viewPoints[i].viewPoint.name);
                            }
                            */
                            person.Value.viewPoint.transform.position = person.Value.viewPoints[0].viewPoint.transform.position;
                            person.Value.viewPointIdx = 0;
                            person.Value.lookExhibitionStatus = "Start";
                            //Debug.Log(name + " Status: None -> Start Looking");
                        }

                        //Debug.Log(person.Value.freeTime_stayInNextExhibit);
                        //calculate visiting time
                        if (person.Value.status == "at" && person.Value.nextTarget_name.StartsWith("p"))
                        {
                            int exId = person.Value.nextTarget_name[1] - '0' - 1;
                            person.Value.visitingTimeInEachEx[exId] += Time.fixedDeltaTime;
                            //isTargetPointUse[person.Value.targetPointName] = true;
                        }

                        person.Value.freeTime_stayInNextExhibit -= Time.fixedDeltaTime;
                        if (person.Value.freeTime_stayInNextExhibit <= 0.5f || person.Value.freeTime_totalLeft <= personNeededTimeToExit)
                        {
                            person.Value.justIn = false;

                            if (person.Value.lookExhibitionStatus == "Watching" || person.Value.lookExhibitionStatus == "Start")
                            {
                                //Debug.Log(person.Value.name + " Status: Watching/Start -> End");
                                person.Value.lookExhibitionStatus = "End";
                            }

                            person.Value.wanderAroundExhibit = false;
                            /* Use new influenceMap to check target 
                            * deal with suddenly appear target and normally arrive and go next */
                            //isTargetPointUse[person.Value.targetPointName] = false;
                            person.Value.targetPointName = "";

                            if (person.Value.nextTarget_name.StartsWith("p"))
                            {
                                exhibitRec[person.Value.nextTarget_name].stayingTime.Add(person.Value.freeTime_stayInNextExhibit_copy - person.Value.freeTime_stayInNextExhibit);
                            }

                            if (person.Value.updateTarget(personNeededTimeToExit, "arriveOrWander"))
                            {
                                /* if target change or move, update */
                                //person.Value.agent.SetDestination(person.Value.nextTarget_pos);
                                //person.Value.SetDestination(person.Value.nextTarget_pos);
                                // person.Value.generateNewPath(walkableMask);

                                if (Run)
                                {
                                    //Debug.Log(person.Value.name + " " + person.Value.rigWeight);
                                    //Debug.Log(person.Value.name + " animation not end!!!");
                                    //if(person.Value.lookExhibitionStatus != "End") person.Value.ifMoveNavMeshAgent(true);
                                }
                                person.Value.lastTimeStamp_rotate = deltaTimeCounter;
                            }
                            person.Value.nearExhibition("goTo");
                            /*
                            if (person.Value.status != "close")
                            {
                                person.Value.nearExhibition("close");
                                Debug.Log("no time");
                            }
                            */
                            //person.Value.ifMoveNavMeshAgent(true);
                        }
                        //continue visiting
                        else
                        {
                            // person.Value.ifMoveNavMeshAgent(false);
                            person.Value.wanderAroundExhibit = true;
                            // should move around the best view direction if having enough space (instead of stand still)
                            if (person.Value.nextTarget_name.StartsWith("p"))  // is an exhibition
                            {
                                if (person.Value.status != "at") person.Value.nearExhibition("at");
                                dealWithWanderAroundExhibit(person.Value);
                            }
                        }                       

                    }

                    /* walk to next target */
                    else
                    {
                        /* Use new influenceMap to check target 
                         * deal with suddenly appear target and normally arrive and go next */
                        if (person.Value.updateTarget(personNeededTimeToExit, "normalWalking"))
                        {
                            /* if target change or move, update */
                            //person.Value.agent.SetDestination(person.Value.nextTarget_pos);
                            //person.Value.SetDestination(person.Value.nextTarget_pos);
                            // person.Value.generateNewPath(walkableMask);

                            if (person.Value.nextTarget_name.Contains("exit"))
                            {
                                person.Value.nearExhibition("goTo");

                            }
                            if (Run)
                            {
                                person.Value.ifMoveNavMeshAgent(true);
                            }
                            person.Value.lastTimeStamp_rotate = deltaTimeCounter;
                        }
                        // deal with  walk and stop state machine      
                        if (deltaTimeCounter - person.Value.lastTimeStamp_stopWalk > currentSceneSettings.customUI.UI_Global.UpdateRate["stopWalkStatus"])  // every x seconds
                        {
                            changeStateWithProbability(person.Value, personNeededTimeToExit, personDistanceWithExit);
                            person.Value.lastTimeStamp_stopWalk = deltaTimeCounter;
                        }                                     
                    }

                    if (person.Value.agent.enabled)
                    {
                        if (person.Value.obstacleToAgent)
                        {
                            person.Value.agent.updatePosition = false;
                            //person.Value.obstacleToAgent = false;
                            person.Value.changeCounter += Time.fixedDeltaTime;
                            person.Value.model.GetComponent<Animator>().SetBool("walk", false);
                            if (person.Value.changeCounter >= 0.5f)
                            {
                                person.Value.obstacleToAgent = false;
                                person.Value.changeCounter = 0.0f;
                            }
                        }
                        else
                        {                            
                            person.Value.agent.updatePosition = true;
                            person.Value.model.GetComponent<Animator>().SetBool("walk", true);
                        }

                        if(deltaTimeCounter - person.Value.lastTimeStamp_rePath > 5.0f)
                        {
                            person.Value.agent.SetDestination(person.Value.nextTarget_pos);
                            person.Value.lastTimeStamp_rePath = deltaTimeCounter;
                        }
                        
                        
                        
                        //draw path
                        /*
                        Color c = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
                        path = person.Value.agent.path;
                        for (int i = 0; i < path.corners.Length - 1; i++)
                        {
                            Debug.DrawLine(path.corners[i], path.corners[i + 1], c, 10);
                        }
                        */
                    }

                    


                    //person.Value.UpdatePosition();
                    //handle collision 
                    foreach (KeyValuePair<string, human_single> other in people)
                    {
                        if (other.Value.name == person.Value.name) continue;
                        Vector3 otherToMe = other.Value.model.transform.position - person.Value.model.transform.position;
                        float dist = otherToMe.magnitude;
                        if (dist < 0.6f){
                            float overlay = (0.6f - dist) / 2.0f;
                            
                            if (person.Value.status == "at" || person.Value.walkStopState == "stop")
                            {
                                person.Value.model.transform.position -= 0.1f * overlay * otherToMe.normalized;
                                other.Value.model.transform.position += 1.9f * overlay * otherToMe.normalized;
                            }
                            else if (other.Value.status == "at" || other.Value.walkStopState == "stop")
                            {
                                person.Value.model.transform.position -= 1.9f * overlay * otherToMe.normalized;
                                other.Value.model.transform.position += 0.1f * overlay * otherToMe.normalized;
                            }
                            else
                            {
                                person.Value.model.transform.position -= overlay * otherToMe.normalized;
                                other.Value.model.transform.position += overlay * otherToMe.normalized;
                            }
                            
                        }
                    }

                    /* save human new position */
                    person.Value.currentPosition = person.Value.model.transform.position;

                    /* set animation Speed (idle ~ walk) */
                    float newAnimeSpeed = person.Value.agent.velocity.magnitude / 1.05f;
                    //float newAnimeSpeed = person.Value.agent.speed / 1.05f;
                    if (Math.Abs(newAnimeSpeed - 0.2) < 0.1 && Math.Abs(newAnimeSpeed - person.Value.animeSpeed) < 0.2) { }
                    else
                    {
                        if (newAnimeSpeed <= 0.2f)
                        {
                            person.Value.model.GetComponent<Animator>().SetFloat("speed", 0.2f, 0.01f, Time.fixedDeltaTime);
                            person.Value.model.GetComponent<Animator>().SetFloat("walkSpeed", 1);
                        }
                        else
                        {
                            person.Value.model.GetComponent<Animator>().SetFloat("speed", 1f, 0.01f, Time.fixedDeltaTime);
                            person.Value.model.GetComponent<Animator>().SetFloat("walkSpeed", newAnimeSpeed);
                        }
                    }
                    person.Value.oldAnimeSpeed = person.Value.animeSpeed;
                    person.Value.animeSpeed = newAnimeSpeed;

                    dealWithRotate(person.Value, person.Value.lookAt_pos);              

                    //Handle Animation
                    //Look animation
                    if (!quickSimulationMode) 
                    {
                        person.Value.updateInformationBoard();
                        person.Value.animationStatusMachine();
                    } 
                }

                /* Store trajectory */
                
                if (deltaTimeCounter - person.Value.lastTimeStamp_storeTrajectory > trajectoryRecordTime && person.Value.model.activeSelf == true)
                {
                    //Debug.Log("x: " + scalePosBackTo2D(person.Value.currentPosition)[0]);
                    //Debug.Log("z: " + scalePosBackTo2D(person.Value.currentPosition)[1]);
                    person.Value.fullTrajectory.Add(scalePosBackTo2D(person.Value.currentPosition));
                    person.Value.velocity_Trajectory.Add(person.Value.agent.velocity.magnitude);
                    person.Value.lastTimeStamp_storeTrajectory = deltaTimeCounter;
                }
                
                /*
                if (person.Value.model.activeSelf == true)
                {
                    //Debug.Log("x: " + scalePosBackTo2D(person.Value.currentPosition)[0]);
                    //Debug.Log("z: " + scalePosBackTo2D(person.Value.currentPosition)[1]);
                    person.Value.fullTrajectory.Add(scalePosBackTo2D(person.Value.currentPosition));
                }
                */

                /* Store computation time */
                updatePosition_endTime = Time.realtimeSinceStartup;
                float dif = (updatePosition_endTime - updatePosition_startTime) * 1000f;
                analysis_updatePosition.Add(dif);
            }
            //store replay data
            if (deltaTimeCounter - person.Value.lastStoreReplayInfoTime >= 0.03333333f)
            {
                person.Value.SaveReplayFrameData();
                person.Value.lastStoreReplayInfoTime = deltaTimeCounter;
            }
        }        
    }

    public bool allPeopleFinish()
    {
        foreach (KeyValuePair<string, human_single> person in people)
        {
            if (!person.Value.checkIfFinishVisit()) return false;
        }
        return true;
    }

    void dealWithWanderAroundExhibit(human_single person)
    {
        //float remainDistance = person.agent.remainingDistance;
        /*
        float remainDistance = Vector3.Distance(person.nextPoint, person.model.transform.position);
        for (int i = 0; i < person.navPathCorners.Count; i++)
        {
            if(i == 0)
            {
                remainDistance += Vector3.Distance(person.navPathCorners.ElementAt(i), person.nextPoint);
            }
            else
            {
                remainDistance += Vector3.Distance(person.navPathCorners.ElementAt(i), person.navPathCorners.ElementAt(i - 1));
            }
        }
        */
        //if (person.agent.remainingDistance < 0.5f /*remainDistance < 0.5f*/) // get to
        //{
            if (person.wanderStayTime <= 0 && exhibitions[person.nextTarget_name].bestViewDirection_vector3.Count > 1)
            {
                //Debug.Log(person.name + " Wander Change View Point");
                Vector3 anotherBestPosSelected = selectAnotherBestViewPos(person.nextTarget_pos, exhibitions[person.nextTarget_name], person);
                float gotoDistance = calculateDistance(person.currentPosition, anotherBestPosSelected);
                float gotoTakeTime = gotoDistance / person.agent.speed;

                if (person.freeTime_stayInNextExhibit >= gotoTakeTime + 5f)  // if can't stay more than 5 seconds, don't move
                {
                    person.nextTarget_pos = anotherBestPosSelected;
                    int directionIndex = exhibitions[person.nextTarget_name].bestViewDirection_vector3.IndexOf(person.nextTarget_pos);
                    person.nextTarget_direction = exhibitions[person.nextTarget_name].bestViewDirection[directionIndex];
                    //person.agent.SetDestination(person.nextTarget_pos);
                    //person.SetDestination(person.nextTarget_pos);
                    person.wanderStayTime = person.generateWanderStayTime();
                    person.lookAt_pos = person.nextTarget_pos;
                    person.model.transform.LookAt(person.lookAt_pos);
                    person.ifMoveNavMeshAgent(true);
                }                
            }
            else
            {
                person.wanderStayTime -= Time.fixedDeltaTime;

                if(person.wanderStayTime <= 0)
                {
                    exhibitRec[person.nextTarget_name].bestViewCount[person.nextTarget_direction.ToString()]++;
                }
                
                // rotate to see the exhitbit
                if(person.lookAt_pos != exhibitions[person.nextTarget_name].centerPosition)
                {
                    person.lastTimeStamp_rotate = deltaTimeCounter;
                    person.lookAt_pos = exhibitions[person.nextTarget_name].centerPosition;
                }                
                dealWithRotate(person, exhibitions[person.nextTarget_name].centerPosition);
                person.ifMoveNavMeshAgent(false);
            }
        //}
    }

    void dealWithRotate(human_single person, Vector3 lookAtPos)
    {
        if (person.lastTimeStamp_rotate != -1)
        {
            //Debug.Log("rotate");
            Quaternion OriginalRot = person.model.transform.rotation;

            person.model.transform.LookAt(lookAtPos);
            Quaternion NewRot = person.model.transform.rotation;
            //animation
            person.model.GetComponent<Animator>().SetBool("walk", true);
            person.model.GetComponent<Animator>().SetFloat("speed", 0.75f);
            //Debug.Log("Rotation Animation");
            //Quaternion NewRot = Quaternion.LookRotation(lookAtPos - person.currentPosition);
            //var angle = Quaternion.Angle(OriginalRot, NewRot);
            //Debug.Log("angle: " + angle);
            //Debug.Log("time: " + (deltaTimeCounter - person.lastTimeStamp_rotate));
            person.model.transform.rotation = Quaternion.Slerp(OriginalRot, NewRot, (deltaTimeCounter - person.lastTimeStamp_rotate) / 30);
            
            if (deltaTimeCounter - person.lastTimeStamp_rotate > 1)
            {
                person.lastTimeStamp_rotate = -1;
            }
        }        
        else 
        {
            //Debug.Log("not rotate");
            if (person.agent.velocity.magnitude >= 0.5f)
            {                
                Vector3 fixedNorm = person.agent.velocity.normalized;
                fixedNorm.y = 0;
                Quaternion OriginalRot = person.model.transform.rotation;
                Quaternion NewRot = Quaternion.LookRotation(fixedNorm);
                person.model.transform.rotation = Quaternion.Slerp(OriginalRot, NewRot, (deltaTimeCounter - person.lastTimeStamp_rotate) / 30); ;
            }
        }
    }

    public float computeAnimeSpeed(float velocityLength, float curSpeed)
    {
        /*  velocityLength = person.agent.velocity.magnitude 
            curSpeed is a regular step
         */
        float returnSpeed = velocityLength / 1.05f;

        // if (returnSpeed <= 0.2f) returnSpeed = 0.2f;
        // else returnSpeed *= 2;
        // if (velocityLength <= 0.2f) returnSpeed = 0.2f;
        // else returnSpeed = velocityLength * 2;
        // Debug.Log("velocity: " + velocityLength + "/1.05 → AnimateSpeed: " + returnSpeed);

        return returnSpeed; //  Mathf.Clamp(returnSpeed, 0, 1);
    }

    Vector3 selectAnotherBestViewPos(Vector3 currentTargetPos, exhibition_single currentExhibit, human_single person)
    {
        //System.Random random = new System.Random(person.randomSeed);
        System.Random random = person.random; // new random change
        int currentDirectionIndex = currentExhibit.bestViewDirection_vector3.IndexOf(currentTargetPos);
        
        int randomIndex = random.Next(currentExhibit.bestViewDirection_vector3.Count);
        while (currentDirectionIndex == randomIndex)
        {
            randomIndex = random.Next(currentExhibit.bestViewDirection_vector3.Count);
        }
        isTargetPointUse[person.targetPointName] = false;
        person.nextTarget_direction = currentExhibit.bestViewDirection[randomIndex];
        person.targetPointName = person.nextTarget_name + " " + person.nextTarget_direction;

        return currentExhibit.bestViewDirection_vector3[randomIndex];
    }

    void changeStateWithProbability(human_single person, float personNeededTimeToExit, float personDistanceWithExit)
    {
        if(person.stopStateContinuedTime >= 3.0f)
        {
            person.walkStopState = "walk";
            person.stopStateContinuedTime = 0f;
            person.ifMoveNavMeshAgent(true);
            return;
        }

        //System.Random random = new System.Random(person.randomSeed);
        System.Random random = person.random; // new random change
        //System.Random random = new System.Random((int)DateTime.Now.Millisecond + person.randomSeed);
        //System.Random random = new System.Random();
        // the last 10 second should totally be walking
        float num = random.Next(0, 101);
        num /= 100f;
        if (person.walkStopState == "walk")
        {
            bool transitionRate = num < currentSceneSettings.humanTypes[person.humanType].walkToStopRate;
            bool enoughTime = person.freeTime_totalLeft > personNeededTimeToExit;
            bool reasonableArea = !checkInExit(person.name);
            if (transitionRate && enoughTime && reasonableArea)
            {
                // Debug.Log("walk → stop: " + num + " < " + currentSceneSettings.humanTypes[person.humanType].walkToStopRate);
                person.walkStopState = "stop";
                person.ifMoveNavMeshAgent(false);
            } // else keep walking
        }
        else  // person.Value.walkStopState == "stop"
        {
            if (num < currentSceneSettings.humanTypes[person.humanType].stopToWalkRate || person.freeTime_totalLeft <= personNeededTimeToExit || checkInExit(person.name))
            {
                // Debug.Log("stop → walk: " + num + " < " + currentSceneSettings.humanTypes[person.humanType].walkToStopRate);
                person.walkStopState = "walk";
                person.ifMoveNavMeshAgent(true);
                person.stopStateContinuedTime = 0f;
            } // else keep stopping
        }
    }

    bool checkInExit(string humanId)
    {
        foreach(exhibition_single exit in exits.Values)
        {
            if (exit.currentHumanInside.Contains(humanId)) return true;
        }

        return false;
    }
            
    public Vector3 findPosByObjName(string objName, human_single person)
    {
        if (objName.StartsWith("id_")) // is a human
        {
            Debug.Log(person.name + " choose person for destination");
            person.targetPointName = "";
            person.lookAt_pos = people[objName].currentPosition;
            return people[objName].currentPosition;
        }
        else if (objName.StartsWith("exit")) // is a exit
        {
            //Debug.Log(person.name + " choose exit for destination");
            person.targetPointName = "";
            person.lookAt_pos = exits[objName].leavePosition;
            return exits[objName].leavePosition;// centerPosition;
        }
        else // is a exhibition
        {
            //System.Random random = new System.Random(person.randomSeed);
            System.Random random = person.random; // new random change
            List<int> indexList = new List<int>();
            for (int i = 0; i < exhibitions[objName].bestViewDirection_vector3.Count; i++) indexList.Add(i);

            //choose
            bool canFindPoint = false;
            int indexChosen = 0;
            //int index = random.Next(exhibitions[objName].bestViewDirection_vector3.Count);
            for(int i = 0; i < exhibitions[objName].bestViewDirection_vector3.Count; i++)
            {
                //Debug.Log(person.name + " index count: " + indexList.Count + " " + person.nextTarget_name);
                int index = random.Next(indexList.Count);
                int indexForEx = indexList[index];
                // Debug.Log(person.name + ": " + exhibitions[objName].bestViewDirection_vector3.Count + ", and pick direction: " + exhibitions[objName].bestViewDirection[index]);
                person.nextTarget_direction = exhibitions[objName].bestViewDirection[indexForEx];
                string targetPointName = person.nextTarget_name + " " + person.nextTarget_direction;
                if (!isTargetPointUse[targetPointName])
                {
                    person.lookAt_pos = exhibitions[objName].bestViewDirection_vector3[indexForEx]; // exhibitions[objName].centerPosition;//
                    indexChosen = indexForEx;
                    canFindPoint = true;
                    isTargetPointUse[targetPointName] = true;
                    person.targetPointName = targetPointName;
                    break;
                    // GameObject sign_test = Instantiate(signPrefab, exhibitions[objName].bestViewDirection_vector3[index], Quaternion.identity);
                }
                else
                {
                    //var item = indexList.Single(r => r == index);
                    //indexList.Remove(item);
                    indexList.RemoveAt(index);
                }
            }
            if (!canFindPoint)
            {
                //Debug.Log(person.name + " choose " + person.nextTarget_name + " no space");
                int index = random.Next(exhibitions[objName].bestViewDirection_vector3.Count);
                person.lookAt_pos = exhibitions[objName].bestViewDirection_vector3[index];
                person.nextTarget_direction = exhibitions[objName].bestViewDirection[index];
                string targetPointName = person.nextTarget_name + " " + person.nextTarget_direction;
                person.targetPointName = targetPointName;
                indexChosen = index; 
            }
            //Debug.Log(person.name + " choose " + person.nextTarget_name + " " + indexChosen);
            return exhibitions[objName].bestViewDirection_vector3[indexChosen];
        }
    }

    public float calculateDistance(Vector3 posA, Vector3 posB)
    {
        float distance = 0;
        path.ClearCorners();
        if (NavMesh.CalculatePath(posA, posB, walkableMask, path) == false)
        {
            // Debug.Log("no path");
            distance = -1;
        }
        else
        {
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                distance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }
        }      
        
        return distance;
    }

    public float calculateDistance_checkCenter(human_single human, exhibition_single ex)
    {
        float distance = calculateDistance(human.currentPosition, ex.centerPosition);
        int bestIndex = 0;
        while (distance == -1 && bestIndex < ex.bestViewDirection_vector3.Count)
        {
            distance = calculateDistance(human.currentPosition, ex.bestViewDirection_vector3[bestIndex]);
            bestIndex++;
        }

        // Debug.Log("conclusion: " + distance);
        return distance;
    }

    void initialComputeGathers()
    {
        Dictionary<string, human_gather> allGathers = new Dictionary<string, human_gather>();
        foreach(KeyValuePair<string, human_single> person in people)
        {
            if (allGathers.Count == 0 || person.Value.startSimulateTime != 0) 
            {
                human_gather newGather = new human_gather();
                newGather.humans.Add(person.Key);
                //newGather.markColor = generateRandomColor();
                string newKey = "group" + gatherCounter.ToString();

                person.Value.groupColorIndex = gatherCounter;
                newGather.markColor = generateColorByIndex(gatherCounter + 1);

                gatherCounter++;
                person.Value.gatherIndex = newKey;
                allGathers.Add(newKey, newGather);
            }
            else
            {
                string findGatherIndex = "";
                //for (int i = 0; i < allGathers.Count; i++) // while (i < allGathers.Count && findGatherIndex == -1)
                foreach (KeyValuePair<string, human_gather> g in allGathers)
                {
                    foreach (string otherPerson in g.Value.humans)
                    {
                        float distance = calculateDistance(person.Value.currentPosition, people[otherPerson].currentPosition);
                        if (distance < 1f)
                        {
                            findGatherIndex = g.Key;
                        }
                    }
                }

                if(findGatherIndex == "")
                {
                    human_gather newGather = new human_gather();
                    newGather.humans.Add(person.Key);
                    //newGather.markColor = generateRandomColor();
                    string newKey = "group" + gatherCounter.ToString();
                    
                    newGather.markColor = generateColorByIndex(gatherCounter + 1);
                    person.Value.groupColorIndex = gatherCounter;

                    gatherCounter++;
                    person.Value.gatherIndex = newKey;
                    allGathers.Add(newKey, newGather);
                }
                else
                {
                    allGathers[findGatherIndex].humans.Add(person.Key);
                    person.Value.gatherIndex = findGatherIndex;
                }
            }
        }

        // print for check
        /*foreach(human_gather g in allGathers)
        {
            Debug.Log(string.Join(", ", g.humans));
        }*/        

        peopleGathers.Clear();
        peopleGathers = allGathers;
        updateGathersColor();
    }

    void changeGathersWithProbability(human_single person)
    {
        //System.Random random = new System.Random(person.randomSeed);
        System.Random random = person.random; // new random change
        float num = random.Next(0, 101);
        num /= 100f;
        gatheringEvent tmpEvent = new gatheringEvent();
        tmpEvent.time = deltaTimeCounter;

        if (peopleGathers[person.gatherIndex].humans.Count != 1) // gather
        {
            tmpEvent.preState = "gather";
            if (num < difGroupUpperThreshold) /* switch group */
            {
                tmpEvent.curState = "gather";
                //Debug.Log("switch group");
                string findNearGatherIndex = findShortestDistanceGroup(person.currentPosition, person.gatherIndex);
                if (findNearGatherIndex != "")
                {
                    peopleGathers[findNearGatherIndex].humans.Add(person.name);
                    peopleGathers[person.gatherIndex].humans.Remove(person.name);
                    if (peopleGathers[person.gatherIndex].humans.Count == 0) peopleGathers.Remove(person.gatherIndex);
                    person.gatherIndex = findNearGatherIndex; // update gatherIndex   
                }
            }
            else if (num < leaveUpperThreshold) /* leave group -> alone */
            {
                tmpEvent.curState = "alone";
                //Debug.Log("leave group -> alone");
                human_gather newGather = new human_gather();
                newGather.humans.Add(person.name);
                //newGather.markColor = generateRandomColor();
                string newKey = "group" + gatherCounter.ToString();

                newGather.markColor = generateColorByIndex(gatherCounter + 1);
                person.groupColorIndex = gatherCounter;

                gatherCounter++;
                peopleGathers.Add(newKey, newGather);

                peopleGathers[person.gatherIndex].humans.Remove(person.name);
                if (peopleGathers[person.gatherIndex].humans.Count == 0) peopleGathers.Remove(person.gatherIndex);
                person.gatherIndex = newKey; // update gatherIndex
            }
            // else : stay at the same group, don't do any change
            else
                tmpEvent.curState = "gather";
        }
        else  // alone
        {
            tmpEvent.preState = "alone";
            if (num < joinUpperThreshold) /* alone -> group */
            {
                tmpEvent.curState = "gather";
                // Debug.Log("alone > group");
                // join new
                string findNearGatherIndex = findShortestDistanceGroup(person.currentPosition, person.gatherIndex);
                if (findNearGatherIndex != "")
                {
                    peopleGathers[findNearGatherIndex].humans.Add(person.name);

                    // delete old
                    peopleGathers.Remove(person.gatherIndex);
                    person.gatherIndex = findNearGatherIndex; // update gatherIndex
                }
                else
                {
                    tmpEvent.curState = "alone";
                }
            }
            // else : stay alone, don't do any change
            else
                tmpEvent.curState = "alone";
        }

        if (peopleGathers[person.gatherIndex].humans.Count == 1) // alone
        {
            //person.gatherMarker.GetComponent<MeshRenderer>().material.color = Color.white;
            person.gatherMarker.GetComponent<MeshRenderer>().material.color = peopleGathers[person.gatherIndex].markColor;
            person.gatherMarker.SetActive(false);
        }
        else
        {
            person.gatherMarker.GetComponent<MeshRenderer>().material.color = peopleGathers[person.gatherIndex].markColor;

            person.gatherMarker.SetActive(true);
        }

        person.gatherEvent.Add(tmpEvent);
    }

    void updateGathersColor() // update all colors
    {
        foreach(human_single person in people.Values)
        {
            if (peopleGathers[person.gatherIndex].humans.Count == 1) // alone
            {
                person.gatherMarker.GetComponent<MeshRenderer>().material.color = Color.white;
                person.gatherMarker.SetActive(false);
            }
            else
            {
                person.gatherMarker.GetComponent<MeshRenderer>().material.color = peopleGathers[person.gatherIndex].markColor;
                person.gatherMarker.SetActive(true);
            }
        }
    }

    string findShortestDistanceGroup(Vector3 currentPos, string currentGatherIndex)
    {
        /* Find another gather that is nearest and not equal to ourself */
        string finalUseIndex = "";
        float minDistance = 10000;
        foreach (KeyValuePair<string, human_gather> g in peopleGathers)
        {
            if(g.Key != currentGatherIndex /*&& g.Value.humans.Count() != 1 */)
            {
                //calculate group center
                int humanCount = 0;
                Vector3 sumPosition = Vector3.zero;
                foreach(string otherPerson in g.Value.humans)
                {
                    humanCount++;
                    sumPosition += people[otherPerson].currentPosition;
                }
                Vector3 groupCenter = sumPosition / humanCount;
                float distanceToGroup = Vector3.Distance(currentPos, groupCenter);
                if (distanceToGroup > 2.0f) continue;

                foreach (string otherPerson in g.Value.humans)
                {
                    if (people[otherPerson].model.activeSelf) // should be in the scene and not in exit
                    {
                        float distance = calculateDistance(currentPos, people[otherPerson].currentPosition);
                        if (distance < minDistance)
                        {
                            finalUseIndex = g.Key;
                            minDistance = distance;
                        }
                    }                    
                }
            }            
        }
        // Debug.Log("final use index: " + finalUseIndex);
        return finalUseIndex;
    }
                
    /* Generation and Initialization*/
    public void generateScene()
    {
        //load tmpUIsetting to allscene
        UIController.instance.LoadTmpSettingToCurrentSceneSettings();

        //check whether ui setting have error
        string error = UIController.instance.checkSettings();
        if (error != "") 
        {
            UIController.instance.ShowMsgPanel("Warning", error);
            return;
        }
        UIController.instance.NormalizeInfluenceValue();
        //store analysis data
        //create directory
        
        System.DateTime dt = System.DateTime.Now;
        string date = dt.Year + "-" + dt.Month + "-" + dt.Day + "T" + dt.Hour + "-" + dt.Minute + "-" + dt.Second;
        string directoryName = date + "_" +
                               UIController.instance.curOption + "_" +
                               currentSceneSettings.customUI.UI_Global.agentCount + "agents";

        directory = Application.streamingAssetsPath + "/Simulation_Result/" + directoryName + "/";
        if (!Directory.Exists(directory))
        {
            DirectoryInfo di = Directory.CreateDirectory(directory);
            Debug.Log("Create Directory!");
        }

        UIController.instance.CalulateOperation();
        
        // clean every thing
        cleanPeopleBeforeGenerate();

        //heatmap max value
        firstGenerateHeatmap = true;
        moveMaxLimit = 5000;
        stayMaxLimit = 5000;

        // set nav mesh and cameras
        walkableMask = (1 << NavMesh.GetAreaFromName(UIController.instance.curOption));

        //bake navmesh
        NavMeshBake();

        // settings
        maxAttraction_C = 100f - maxAttraction_A - maxAttraction_B;
        slopeB = ((maxAttraction_A + maxAttraction_B) - maxAttraction_A) / (float)(currentSceneSettings.customUI.UI_Exhibit.crowdedThreshold * 100 - currentSceneSettings.customUI.UI_Exhibit.popularThreshold * 100);
        slopeC = (0 - maxAttraction_C) / (100 - (float)currentSceneSettings.customUI.UI_Exhibit.crowdedThreshold * 100);

        /* gather state thresholds */
        difGroupUpperThreshold = (float)currentSceneSettings.customUI.UI_Human.behaviorProbability["keepGather_difGroup"].mean;
        leaveUpperThreshold = (float)currentSceneSettings.customUI.UI_Human.behaviorProbability["keepGather_difGroup"].mean + (float)currentSceneSettings.customUI.UI_Human.behaviorProbability["leave"].mean;
        joinUpperThreshold = (float)currentSceneSettings.customUI.UI_Human.behaviorProbability["join"].mean;

        deltaTimeCounter = 0;
        fps_deltaTime = 0.0f;
        updatePosition_startTime = 0.0f;
        fpsList.Clear();
        analysis_influenceMap.Clear();
        analysis_updatePosition.Clear();
        updateVisBoard = 0;
        timeText.text = "Time: " + deltaTimeCounter.ToString("0.00");

        /* Exhibitions */
        isTargetPointUse.Clear();
        targetPointBall.Clear();
        generateExhibitions();

        /*Take Screenshot For Layout*/
        TakeLayoutScreenShot();

        /* People */
        generateHumans();

        /* Initial update */
        /* Initial update information Board of each exhibit */
        foreach (KeyValuePair<string, exhibition_single> exhibit in exhibitions)
        {
            exhibit.Value.updateInformationBoard();            
        }

        /* Initialize influence map of each person */
        // choose some people to appear late
        System.Random random = new System.Random(Guid.NewGuid().GetHashCode());
        List<int> indexList = new List<int>(Enumerable.Range(1, currentSceneSettings.customUI.UI_Global.agentCount).OrderBy(x => random.Next()).Take(currentSceneSettings.customUI.UI_Global.addAgentCount));
        foreach (KeyValuePair<string, human_single> person in people)
        {
            /* for time stamp */
            person.Value.lastTimeStamp_stopWalk = 0;
            person.Value.lastTimeStamp_recomputeMap = random.Next(1, currentSceneSettings.customUI.UI_Global.UpdateRate["influenceMap"] * 2 + 1);
            person.Value.lastTimeStamp_rePath = -5f;
            person.Value.lastTimeStamp_recomputeGathers = random.Next(1, currentSceneSettings.customUI.UI_Global.UpdateRate["gathers"] * 2 + 1);

            /* for influence Map */
            Vector3 markerPos = person.Value.currentPosition;
            markerPos.y = 7.8f;
            person.Value.marker = Instantiate(markerPrefab_human, markerPos, Quaternion.identity);
            person.Value.marker.name = "marker_" + person.Key;
            person.Value.marker.transform.parent = person.Value.model.transform;            
            influenceMapVisualize.instance.markers.Add(person.Key, person.Value.marker);
            person.Value.marker.transform.Find("Text").GetComponent<TextMesh>().text = person.Key.Replace("_", "");

            /* for collider shape */
            markerPos.y = 1.5f;
            person.Value.colliderShape = Instantiate(cylinderCollider, markerPos, Quaternion.identity);
            person.Value.colliderShape.name = "colliderShape";
            person.Value.colliderShape.transform.parent = person.Value.model.transform;
            float radiusTimes2 = person.Value.agent.radius * 2f;
            person.Value.colliderShape.transform.localScale = new Vector3(radiusTimes2, 1, radiusTimes2);
            person.Value.colliderShape.transform.Find("Cylinder").GetComponent<MeshRenderer>().material.color = Color.green;

            if (loadVIS)
            {
                person.Value.preTarget_pos = exits[person.Value.exitName].enterPosition;
                person.Value.model.transform.position = person.Value.preTarget_pos;
                if (person.Value.startSimulateTime > 0)
                {
                    person.Value.modelVisible(false);
                }
                else
                {
                    person.Value.modelVisible(false);
                    person.Value.modelVisible(true);
                }
            }
            else
            {
                if (indexList.Contains(int.Parse(person.Key.Replace("id_", "")))) /* if appear last */
                {
                    Debug.Log(currentSceneSettings.customUI.UI_Global.startAddAgentMin + " " + currentSceneSettings.customUI.UI_Global.startAddAgentMax);
                    person.Value.startSimulateTime = random.Next(currentSceneSettings.customUI.UI_Global.startAddAgentMin,
                        currentSceneSettings.customUI.UI_Global.startAddAgentMax); // late appearence
                    person.Value.preTarget_pos = exits[person.Value.exitName].enterPosition;
                    person.Value.model.transform.position = person.Value.preTarget_pos;
                    person.Value.modelVisible(false);
                }
                else
                {
                    person.Value.startSimulateTime = 0;
                    //person.Value.preTarget_pos = generateNewVector3();
                    person.Value.preTarget_pos = exits[person.Value.exitName].enterPosition;
                    person.Value.model.transform.position = person.Value.preTarget_pos;
                    person.Value.modelVisible(false);
                    person.Value.modelVisible(true);
                }
            }



            person.Value.currentPosition = person.Value.preTarget_pos;            
            // person.Value.model.transform.position = person.Value.preTarget_pos;
        }

        /*save visitor initial setting*/
        if (saveVIS)
        {
            List<humanInfo> visitorsInfo = new List<humanInfo>();
            foreach (KeyValuePair<string, human_single> person in people)
            {
                humanInfo visitorInfo = new humanInfo();
                visitorInfo.name = person.Value.name;
                visitorInfo.age = person.Value.age;
                visitorInfo.gender = person.Value.gender;
                visitorInfo.humanType = person.Value.humanType;
                visitorInfo.modelName = person.Value.modelName;
                visitorInfo.desireExhibitionList = person.Value.desireExhibitionList;
                visitorInfo.freeTime_total = (double)person.Value.freeTime_total;
                visitorInfo.walkSpeed = (double)person.Value.walkSpeed;
                visitorInfo.gatherDesire = (double)person.Value.gatherDesire;
                visitorInfo.startSimulateTime = (double)person.Value.startSimulateTime;
                visitorInfo.randomSeed = person.Value.randomSeed;
                visitorsInfo.Add(visitorInfo);
            }
            StringBuilder sb = new StringBuilder();
            JsonWriter writer = new JsonWriter(sb);
            writer.PrettyPrint = true;
            JsonMapper.ToJson(visitorsInfo, writer);
            string outputJsonStr = sb.ToString();
            // Debug.Log(outputJsonStr);
            System.IO.File.WriteAllText(saveVISFilename, outputJsonStr);
        }

        initialComputeGathers();
        foreach (KeyValuePair<string, human_single> person in people)
        {
            computeNewInfluenceMap(person.Value);
            person.Value.updateInfluenceMap();
            person.Value.ifMoveNavMeshAgent(false);
        }
        influenceMapVisualize.instance.changeMainHuman(""); // initialize with no mainHuman
        updatePeople();  /* Initial update of target */

        /* update showing */
        setActiveAllInformationBoard_human(humanBoardToggle.isOn);
        setActiveAllInformationBoard_exhibit(exhibitToggle.isOn);
        setActiveAllTargetPosSign(targetSignToggle.isOn);
        setActiveColliderShape_human(colliderShapeToggle.isOn);
        setVisibleAllRange_exhibit(exhibitRangeToggle.isOn);

        pauseSimulation(); // stop all
        afterGenerate = true;
    }

    void generateHumans()
    {
        //need to update
        string dirPath = Application.streamingAssetsPath + "/SettingsJson/Default Settings";
        string jsonPath = dirPath + "/specificDesireList_" + UIController.instance.currentScene + ".json";
        string tmpJsonDataStr = File.ReadAllText(jsonPath);
        JsonData tmpJsonData = new JsonData();
        tmpJsonData = JsonMapper.ToObject(tmpJsonDataStr);
        List<List<string>> specificList_adult = JsonMapper.ToObject<List<List<string>>>(tmpJsonData["specificDesireList_adult"].ToJson());
        List<List<string>> specificList_child = JsonMapper.ToObject<List<List<string>>>(tmpJsonData["specificDesireList_child"].ToJson());

        /*Load Visitor Initial Setting*/
        List<humanInfo> humansInfo = new List<humanInfo>();
        if (loadVIS)
        {
            string visJsonDataStr = File.ReadAllText(loadVISFilename);
            humansInfo = JsonMapper.ToObject<List<humanInfo>>(visJsonDataStr);
        }
        


        /* Start generate new humans */
        System.Random random = new System.Random(Guid.NewGuid().GetHashCode());
        adultCount = (int)Math.Round(currentSceneSettings.customUI.UI_Global.agentCount * currentSceneSettings.customUI.UI_Global.adultPercentage) + 1;
        for (int i = 1; i < currentSceneSettings.customUI.UI_Global.agentCount + 1; i++) // id start from 1
        {
            /* initialize human features */
            human_single newPerson = new human_single();
            int idx = i - 1;
            if (loadVIS)
            {
                newPerson.randomSeed = humansInfo[idx].randomSeed;
                newPerson.random = new System.Random(newPerson.randomSeed);
                newPerson.name = humansInfo[idx].name;
                newPerson.gender = humansInfo[idx].gender;
            }
            else
            {
                newPerson.randomSeed = Guid.NewGuid().GetHashCode();
                newPerson.random = new System.Random(newPerson.randomSeed);
                newPerson.name = "id_" + i.ToString();
                newPerson.gender = random.Next(2);
            }

            /* Deal with input if specific desired list */
            if (loadVIS)
            {
                newPerson.age = humansInfo[idx].age;
                newPerson.humanType = humansInfo[idx].humanType;
                newPerson.desireExhibitionList = humansInfo[idx].desireExhibitionList;
            }
            else
            {
                if (i < adultCount)
                {
                    newPerson.age = random.Next(1, 5);
                    newPerson.humanType = "adult";

                    if (specificList_adult.Count <= 0 || randomVIS) { newPerson.desireExhibitionList = generateDesireExhibitionList(newPerson.humanType); }
                    else
                    {
                        newPerson.desireExhibitionList = specificList_adult[0];
                        specificList_adult.RemoveAt(0);
                    }
                }
                else
                {
                    newPerson.age = random.Next(1);
                    newPerson.humanType = "child";
                    if (specificList_child.Count <= 0 || randomVIS) { newPerson.desireExhibitionList = generateDesireExhibitionList(newPerson.humanType); }
                    else
                    {
                        newPerson.desireExhibitionList = specificList_child[0];
                        specificList_child.RemoveAt(0);
                    }
                }
            }

            newPerson.desireExhibitionList_copy = new List<string>(newPerson.desireExhibitionList);

            /* initialize human movement */
            newPerson.preTarget_name = "init";            
            newPerson.nextTarget_name = "";
            if (loadVIS)
            {
                string type = GetType(humansInfo[idx].gender, humansInfo[idx].age);
                newPerson.model = Instantiate(Resources.Load<GameObject>("CharactersPrefab/" + type + "/" + humansInfo[idx].modelName), Vector3.zero, Quaternion.identity);
            }
            else
            {
                newPerson.model = loadAllCharacterModels.instance.randomCreatePrefab(newPerson.gender, newPerson.age);
            }
            
            newPerson.model.tag = "Visitor";

            //save model name for replay mode
            newPerson.modelName = newPerson.model.name.Replace("(Clone)", "");
            newPerson.visitorFrameData = new List<FrameData>();
            
            newPerson.model.AddComponent<CapsuleCollider>();
            CapsuleCollider capCol = newPerson.model.GetComponent<CapsuleCollider>();
            capCol.radius = 0.2f;
            capCol.center = new Vector3(0, 0.8f, 0);
            capCol.height = 2;
            newPerson.model.name = newPerson.name;
            newPerson.model.transform.parent = peopleParent.transform;
            Animator animator = newPerson.model.GetComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("AnimationClips/ManPose");     
            
            newPerson.model.AddComponent<Rigidbody>();
            Rigidbody rigid = newPerson.model.GetComponent<Rigidbody>();
            rigid.useGravity = false;
            rigid.isKinematic = false;
            rigid.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePosition;
            rigid.interpolation = RigidbodyInterpolation.Interpolate;
            
            
            /*initialize animation rigging compoment*/
            newPerson.model.AddComponent<RigBuilder>();
            newPerson.viewPoint = new GameObject(newPerson.name + "_ViewPoint");

            GameObject rig = new GameObject("Rig");
            rig.AddComponent<Rig>();
            rig.transform.SetParent(newPerson.model.transform);
            GameObject headAim = new GameObject("Aim");
            headAim.AddComponent<MultiAimConstraint>();
            headAim.transform.SetParent(rig.transform);
            MultiAimConstraint mac = headAim.GetComponent<MultiAimConstraint>();
            Queue<Transform> allChildren = new Queue<Transform>();
            allChildren.Enqueue(newPerson.model.transform);
            while(allChildren.Count != 0)
            {
                Transform parent = allChildren.Dequeue();
                foreach(Transform child in parent)
                {
                    if (child.name.Contains("Head")) 
                    {
                        //Debug.Log(child.name);
                        newPerson.head = child.transform.gameObject;
                        //Debug.Log("Find");

                        while (allChildren.Count != 0) allChildren.Dequeue();
                        break;
                    }
                    else allChildren.Enqueue(child);
                }
            }
            
            mac.data = new MultiAimConstraintData
            {
                constrainedObject = newPerson.head.transform,
                aimAxis = MultiAimConstraintData.Axis.Y,
                constrainedXAxis = true,
                constrainedYAxis = true,
                constrainedZAxis = true,
                limits = new Vector2(-180.0f, 180.0f)
            };
            RigBuilder rb = newPerson.model.GetComponent<RigBuilder>();
            RigLayer rl = new RigLayer(rig.GetComponent<Rig>());
            rb.layers.Add(rl);
            rb.Build();

            /* set human feature */
            if (loadVIS)
            {
                newPerson.walkSpeed = (float)humansInfo[idx].walkSpeed;
                newPerson.gatherDesire = (float)humansInfo[idx].gatherDesire;
                newPerson.freeTime_total = (float)humansInfo[idx].freeTime_total;
                newPerson.startSimulateTime = (float)humansInfo[idx].startSimulateTime;
            }
            else
            {
                float walkSpeedMin = (currentSceneSettings.customUI.UI_Human.walkSpeedMin != -1) ? currentSceneSettings.customUI.UI_Human.walkSpeedMin : (float)currentSceneSettings.humanTypes[newPerson.humanType].walkSpeed.min;
                float walkSpeedMax = (currentSceneSettings.customUI.UI_Human.walkSpeedMax != -1) ? currentSceneSettings.customUI.UI_Human.walkSpeedMax : (float)currentSceneSettings.humanTypes[newPerson.humanType].walkSpeed.max;
                newPerson.walkSpeed = generateByNormalDistribution((float)currentSceneSettings.humanTypes[newPerson.humanType].walkSpeed.mean,
                    (float)currentSceneSettings.humanTypes[newPerson.humanType].walkSpeed.std,
                    walkSpeedMin,
                    walkSpeedMax);
                //Debug.Log(newPerson.walkSpeed);
                newPerson.gatherDesire = generateByNormalDistribution((float)currentSceneSettings.customUI.UI_Human.gatherProbability.mean,
                    (float)currentSceneSettings.customUI.UI_Human.gatherProbability.std,
                    (float)currentSceneSettings.oriJson.UI_Human.gatherProbability.min,
                    (float)currentSceneSettings.oriJson.UI_Human.gatherProbability.max);
                newPerson.freeTime_total = random.Next(currentSceneSettings.customUI.UI_Human.freeTimeMin,
                    currentSceneSettings.customUI.UI_Human.freeTimeMax);
                newPerson.startSimulateTime = 0;
            }
            
            newPerson.freeTime_totalLeft = newPerson.freeTime_total;            
            newPerson.exitName = newPerson.desireExhibitionList.Where(r => r.StartsWith("exit")).FirstOrDefault();
            newPerson.desireExhibitionList_copy.Remove(newPerson.exitName);

            newPerson.trajectoryOrder.Add(newPerson.exitName);

            //add by ChengHao
            newPerson.visitingTimeInEachEx = new float[currentSceneSettings.Exhibitions.Count - 1]; // -1 remove walkable
            for (int j = 0; j < currentSceneSettings.Exhibitions.Count - 1; j++) newPerson.visitingTimeInEachEx[j] = 0f;
            newPerson.statusTime = new float[3];
            for (int j = 0; j < 3; j++) newPerson.statusTime[j] = 0f;

            newPerson.model.AddComponent<NavMeshAgent>();
            NavMeshAgent navAgent = newPerson.model.GetComponent<NavMeshAgent>();
            //navAgent.updateRotation = true;
            navAgent.speed *= 0.5f * (newPerson.walkSpeed / 100f);
            navAgent.acceleration *= 0.25f * (newPerson.walkSpeed / 100f);
            navAgent.radius = (float)currentSceneSettings.customUI.walkStage["GoTo"].radius;
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            navAgent.areaMask = walkableMask;
            newPerson.navSpeed = navAgent.speed;
            //navAgent.autoRepath = true;
            //random
            /*
            int val = random.Next(0, 99);
            navAgent.avoidancePriority = val;
            newPerson.avoidPriority = val;
            */
            //id

            newPerson.id = i;
            navAgent.avoidancePriority = 50 + i;
            newPerson.avoidPriority = 50 + i;
            
            //default
            //navAgent.avoidancePriority = 50;
            newPerson.agent = navAgent;

            //NavMeshObstacle
            
            newPerson.model.AddComponent<NavMeshObstacle>();
            NavMeshObstacle navObstacle = newPerson.model.GetComponent<NavMeshObstacle>();
            navObstacle.shape = NavMeshObstacleShape.Capsule;
            navObstacle.radius = 0.1f;
            navObstacle.height = 2f;
            navObstacle.carving = true;
            navObstacle.carvingMoveThreshold = 0.1f;
            navObstacle.carvingTimeToStationary = 0.5f;
            navObstacle.carveOnlyStationary = false;
            newPerson.obstacle = navObstacle;
            newPerson.obstacle.enabled = false;
                    

            /* set information board */
            newPerson.informationBoard = (GameObject)Instantiate(informationBoardPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            newPerson.informationBoard.name = "informationBoard";
            newPerson.informationBoard.transform.SetParent(newPerson.model.transform);
            newPerson.informationText = newPerson.informationBoard.transform.Find("Text").GetComponent<TextMesh>();
            string genderStr = "";
            if (newPerson.gender == 0) genderStr = "female";
            else genderStr = "male";
            string fixedText = newPerson.name + "\n";
            fixedText += "features: " + genderStr + " " + newPerson.humanType + "\n";
            //fixedText += "walkSpeed: " + newPerson.walkSpeed + "\n";
            newPerson.fixedText = fixedText;
            newPerson.gatherIndex = "";

            /* gather marker initialize */
            newPerson.gatherMarker = Instantiate(ballMarker, new Vector3(0, 2.5f, 0), Quaternion.identity);
            newPerson.gatherMarker.name = "gatherColorMarker";
            newPerson.gatherMarker.transform.SetParent(newPerson.model.transform);

            /* off mesh script */
            newPerson.model.AddComponent<offMeshLinkNavigation>();

            people.Add(newPerson.name, newPerson);
        }               
    }
    
    void generateExhibitions()
    {
        string curScene = UIController.instance.currentScene;
        string curOption = UIController.instance.curOption;

        // GameObject curEnvironRoot = EnvironmentsRoot.transform.Find(curScene).GetComponent<GameObject>();
        GameObject environRoot = GameObject.Find("/[EnvironmentsOfEachScene]/" + curOption + "/");
        foreach (Transform child in environRoot.transform)
        {
            if (child.name.StartsWith(curScene + "_ExitDoor"))
            {
                string exitName = "exit" + child.name.Replace(curScene + "_ExitDoor", "");
                string objectPathRoot = "/[EnvironmentsOfEachScene]/" + curOption + "/";
                objectPathRoot += child.name;

                exhibition_single newExit = new exhibition_single();
                newExit.name = exitName;
                newExit.model = child.gameObject;
                newExit.centerPosition = GameObject.Find(objectPathRoot + "/center").transform.position; // newExit.model.transform.position;
                newExit.centerPosition.y = 0;
                newExit.enterPosition = GameObject.Find(objectPathRoot + "/enterPos").transform.position; // newExit.model.transform.position;
                newExit.enterPosition.y = 0;
                newExit.leavePosition = GameObject.Find(objectPathRoot + "/leavePos").transform.position; // newExit.model.transform.position;
                newExit.leavePosition.y = 0;
                newExit.range.Add(GameObject.Find(objectPathRoot + "/range"));
                GameObject range1 = GameObject.Find(objectPathRoot + "/range1");
                if (range1 != null) newExit.range.Add(range1);

                newExit.stayTimeSetting.min = 1;
                newExit.stayTimeSetting.max = 60; // 1 mins
                newExit.stayTimeSetting.mean = 1;
                newExit.stayTimeSetting.std = 0;
                newExit.capacity_max = 10000;
                newExit.chosenProbabilty = 0; // for not affecting the popular level
                newExit.repeatChosenProbabilty = 0;

                Vector3 boardPos = newExit.centerPosition;
                boardPos.y = 3;
                newExit.informationBoard = (GameObject)Instantiate(informationBoardPrefab_exhibit, boardPos, Quaternion.identity);
                newExit.informationBoard.name = "informationBoard";
                newExit.informationBoard.transform.SetParent(newExit.model.transform);
                newExit.informationText = newExit.informationBoard.transform.Find("Text").GetComponent<TextMesh>();
                string fixedText = exitName + "\n";
                fixedText += "chosenProb: " + newExit.chosenProbabilty + "\n";
                fixedText += "AvgStayTime: " + newExit.stayTimeSetting.mean + "\n";
                newExit.fixedText = fixedText;
                newExit.updateInformationBoard();
                newExit.informationBoard.SetActive(false);

                /* for influence Map */
                Vector3 markerPos = newExit.centerPosition;
                markerPos.y = 7.5f;
                GameObject marker = Instantiate(markerPrefab_exhibit, markerPos, Quaternion.identity);
                marker.layer = 8;
                marker.name = "marker_" + exitName;
                marker.transform.parent = markersParent.transform;
                marker.transform.Find("Text").GetComponent<TextMesh>().fontSize = 36;
                marker.transform.Find("Text").GetComponent<TextMesh>().text = exitName;
                influenceMapVisualize.instance.markers.Add(exitName, marker);

                exits.Add(exitName, newExit);
            }
        }

        foreach (KeyValuePair<string, settings_exhibition> exhibit in currentSceneSettings.Exhibitions)
        {
            if (exhibit.Key != "walkableArea")
            {
                exhibition_single newExhibit = new exhibition_single();
                exhibitionRecord exhibitRecord = new exhibitionRecord();
                exhibitRecord.bestViewCount = new Dictionary<string, int>();
                exhibitRecord.influences = new List<exhibitionInfluence>();
                exhibitRecord.stayingTime = new List<double>();
                exhibitRec.Add(exhibit.Key, exhibitRecord);

                string modelInScene_center = "/[EnvironmentsOfEachScene]/" + curOption + "/" + exhibit.Key.Replace("p", curScene + "_") + "/center";
                string modelInScene_range = modelInScene_center.Replace("center", "range");
                // Debug.Log(modelInScene);
                newExhibit.name = exhibit.Key;
                newExhibit.model = GameObject.Find(modelInScene_center);
                if (newExhibit.model != null)
                {
                    newExhibit.bestViewDirection = new List<int>(exhibit.Value.bestViewDirection);                    
                    newExhibit.frontSide = exhibit.Value.frontSide;

                    //for(int i = 0; i < newExhibit.frontSide.Count; i++) Debug.Log(newExhibit.name + " " + newExhibit.frontSide[i]);

                    newExhibit.centerPosition = newExhibit.model.transform.position;
                    newExhibit.centerPosition.y = 0;
                    newExhibit.range.Add(GameObject.Find(modelInScene_range));
                    GameObject range1 = GameObject.Find(modelInScene_range + "1");
                    if (range1 != null) newExhibit.range.Add(range1);


                    // generateBestViewPos(newExhibit, exhibit.Key);
                    generateBestViewByStatisticDistance(newExhibit, exhibit.Key); // just test

                    // Debug.Log(exhibit.Key + ": " + newExhibit.centerPosition);
                    // GameObject sign = Instantiate(signPrefab, newExhibit.centerPosition, Quaternion.identity);
                    // sign.name = exhibit.Key;
                    // sign.transform.Find("Text").GetComponent<TextMesh>().text = "center";
                    // sign.transform.parent = newExhibit.model.transform;

                    newExhibit.stayTimeSetting = exhibit.Value.stayTime.copy();
                    newExhibit.capacity_max = (int)exhibit.Value.capacity.max; // may times 2 to contain more people
                    newExhibit.chosenProbabilty = (float)exhibit.Value.chosenProbabilty;
                    newExhibit.repeatChosenProbabilty = (float)exhibit.Value.repeatChosenProbabilty;

                    Vector3 boardPos = newExhibit.centerPosition;
                    boardPos.y = 3;
                    newExhibit.informationBoard = (GameObject)Instantiate(informationBoardPrefab_exhibit, boardPos, Quaternion.identity);
                    newExhibit.informationBoard.name = "informationBoard";
                    newExhibit.informationBoard.transform.SetParent(newExhibit.model.transform);
                    newExhibit.informationText = newExhibit.informationBoard.transform.Find("Text").GetComponent<TextMesh>();
                    string fixedText = exhibit.Key + "\n";
                    fixedText += "chosenProb: " + newExhibit.chosenProbabilty + "\n";
                    fixedText += "AvgStayTime: " + newExhibit.stayTimeSetting.mean.ToString("F2") + "\n";
                    newExhibit.fixedText = fixedText;

                    newExhibit.updateInformationBoard();

                    /* for influence Map */
                    Vector3 markerPos = newExhibit.centerPosition;
                    markerPos.y = 7.5f;
                    GameObject marker = Instantiate(markerPrefab_exhibit, markerPos, Quaternion.identity);
                    marker.layer = 8;
                    marker.name = "marker_" + exhibit.Key;
                    marker.transform.parent = markersParent.transform;
                    marker.transform.Find("Text").GetComponent<TextMesh>().text = exhibit.Key;
                    influenceMapVisualize.instance.markers.Add(exhibit.Key, marker);
                    newExhibit.currentHumanInside.Clear();
                    newExhibit.capacity_cur = 0;  // clean capacity in range
                    //Debug.Log(exhibit.Key);
                    exhibitions.Add(exhibit.Key, newExhibit);
                }            
            }
            
        }

        
    }

    void generateBestViewByStatisticDistance(exhibition_single ex, string exName)
    {
        Mesh mesh = ex.range[0].GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        List<Vector3> useVertices = new List<Vector3>();
        for (var i = 0; i < vertices.Length; i++)
        {
            Vector3 pos = ex.range[0].transform.TransformPoint(vertices[i]);
            pos.y = 0;
            if (!useVertices.Contains(pos)) useVertices.Add(pos);
        }
        Vector3 center = ex.range[0].transform.TransformPoint(mesh.bounds.center);
        center.y = 0;
        float length1 = Vector3.Distance(useVertices[0], useVertices[1]);
        float length2 = Vector3.Distance(useVertices[0], useVertices[2]);
        float a, b;
        if (length1 > length2)
        {
            a = length1;
            b = length2;
        }
        else
        {
            a = length2;
            b = length1;
        }
        a = a * 0.8f / 2;
        b = b * 0.7f / 2; // 直徑: 80%, 半徑 / 2
        float perimeter = 2 * (float)Math.PI * b + 4 * (a - b);
        // float slopeNum_perimeter = (6 - 3) / (15 - 5); = 0.33
        int pickBestViewCount = (int)(3 + 0.33f * (perimeter - 5)); /* map perimeter 4 ~ 15 to 3 ~ 6 */
        // Debug.Log(exName + " " + a + "  " + b + ", perimeter: " + perimeter +　", pikCount: " + pickBestViewCount);
        if (ex.bestViewDirection.Count > pickBestViewCount)
            ex.bestViewDirection.RemoveRange(pickBestViewCount, ex.bestViewDirection.Count - pickBestViewCount);

        /* find different rotation and position with the original exhibitions */
        string modelInScene = "/[EnvironmentsOfEachScene]/" + UIController.instance.currentScene;
        modelInScene += "/" + ex.model.transform.parent.transform.name;
        GameObject originalExhibit = GameObject.Find(modelInScene);

        float rotateOfy = ex.model.transform.parent.transform.rotation.eulerAngles.y;
        float rotateOfy_original = originalExhibit.transform.rotation.eulerAngles.y;
        float rotateYdif = rotateOfy - rotateOfy_original;

        Vector3 position = ex.model.transform.parent.transform.position;
        position.z -= 50 * UIController.instance.curSceneOptions.IndexOf(UIController.instance.curOption);
        Vector3 position_original = originalExhibit.transform.position;
        Vector3 positionDif = position - position_original;        
        // Debug.Log(exName + " " + rotateYdif + " " + positionDif);

        /* draw direction */
        int index = 0;
        Vector3 planeCenter = new Vector3(0, 0, 0);
        planeCenter.x += 50 * UIController.instance.allScene.IndexOf(UIController.instance.currentScene);
        planeCenter.z += 50 * UIController.instance.curSceneOptions.IndexOf(UIController.instance.curOption);
        if (UIController.instance.currentScene == "225")
        {
            planeCenter.x += 2;
        }
        
        GameObject rangeCopy = ex.range[0];
        float rate = 0.65f;
        rangeCopy.transform.localScale *= rate;
        Mesh meshCopy = rangeCopy.GetComponent<MeshFilter>().mesh;
        Vector3[] verticesCopy = meshCopy.vertices;
        List<Vector3> useVerticesCopy = new List<Vector3>();
        for (var i = 0; i < verticesCopy.Length; i++)
        {
            Vector3 pos = rangeCopy.transform.TransformPoint(verticesCopy[i]);
            pos.y = 0;
            
            //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //sphere.transform.position = pos;
            //sphere.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            
            if (!useVerticesCopy.Contains(pos)) useVerticesCopy.Add(pos);
        }

        /*
        for(int i = 0; i < 4; i++)
        {
            Debug.Log(i + ": " + useVerticesCopy[i]);
        }
        */
        //Color c = UnityEngine.Random.ColorHSV();

        Vector3 lineStart = new Vector3(center.x, 0, center.z);
        Quaternion rot = ex.model.transform.parent.transform.rotation;
        Vector3 lineEnd = lineStart + rot * Vector3.forward * 5;
        //lineEnd = lineStart + rot * Vector3.left * 5;
        lineEnd = lineStart + Vector3.left * 3;
        //Debug.DrawLine(lineStart, lineEnd, Color.red, 200);


        foreach (int direction in ex.bestViewDirection)
        {
            if (currentSceneSettings.Exhibitions[exName].bestViewDistance[index][0] != -1 && currentSceneSettings.Exhibitions[exName].bestViewDistance[index][1] != -1)
            {
                exhibitRec[exName].bestViewCount.Add(direction.ToString(), 0);
                float _x = (float)(currentSceneSettings.Exhibitions[exName].bestViewDistance[index][0]);
                float _z = (float)(currentSceneSettings.Exhibitions[exName].bestViewDistance[index][1]);
                
                Vector3 _direct = scalePosTo3D(new Vector2(_x, _z)) + positionDif - ex.centerPosition;
                _direct = Quaternion.Euler(0, rotateYdif, 0) * _direct;
                _direct += ex.centerPosition;

                //check if touchable by the agents
                Vector3 _direct_onNavMesh = _direct;
                NavMeshPath path = new NavMeshPath();
                NavMesh.CalculatePath(planeCenter, _direct, walkableMask, path);
                if (path.status != NavMeshPathStatus.PathComplete)
                {
                    // Debug.Log(exName + " " + direction + " " + walkableMask);
                    // GameObject st_ = Instantiate(signPrefab, planeCenter, Quaternion.identity);

                    NavMeshHit myNavHit;
                    Vector3 _direct_touch = new Vector3();
                    if (NavMesh.SamplePosition(_direct, out myNavHit, 5.0f, walkableMask))
                    {
                        _direct_touch = myNavHit.position;                        
                        NavMesh.CalculatePath(planeCenter, _direct_touch, walkableMask, path);                        
                        if (path.status == NavMeshPathStatus.PathComplete)
                        {
                            _direct_onNavMesh = _direct_touch;
                            // GameObject st = Instantiate(signPrefab, _direct_touch, Quaternion.identity);
                            // st.transform.Find("Text").GetComponent<TextMesh>().text = direction.ToString();
                        }
                    }
                }
                //ex.bestViewDirection_vector3.Add(_direct_onNavMesh);
                               
                GameObject sign_test = Instantiate(signPrefab2, _direct, Quaternion.identity);
                sign_test.transform.Find("Text").GetComponent<TextMesh>().text = direction.ToString();
                sign_test.name = exName + " " + direction;
                sign_test.transform.parent = ex.model.transform;

                isTargetPointUse.Add(sign_test.name, false);
                /*
                GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.transform.position = new Vector3(sign_test.transform.position.x, 4, sign_test.transform.position.z);
                ball.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                targetPointBall.Add(sign_test.name, ball);
                */
                //ex.bestViewSign.Add(sign_test);
                /*
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = sign_test.transform.position;
                sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                var sphereRenderer = sphere.GetComponent<Renderer>();
                sphereRenderer.material.SetColor("_Color", c);
                */
                
                float minDistance = float.PositiveInfinity;
                Vector3 finalPoint = Vector3.zero;
                Vector3 signPos = sign_test.transform.position;
                for (int i = 0; i < 4; i++)
                {
                    // Get 2 Line for cross point
                    float a1, a2, b1, b2, c1, c2;
                    // first line : 2 points -> center to (_x, 0, _z)
                    //according to the origin point
                    /*
                    a1 = center.z - signPos.z;
                    b1 = signPos.x - center.x;
                    c1 = center.x * signPos.z - signPos.x * center.z;
                    */
                    float angle = Vector3.Angle(Vector3.left, _direct - center);
                    //Debug.DrawLine(center, _direct, Color.yellow, 200);
                    //Debug.Log(angle);
                    //according to the degree
                    Vector3 afterRot = Quaternion.AngleAxis(-15 - direction * 30, Vector3.up) * Quaternion.Euler(0, rotateYdif, 0) * Vector3.left;
                    //Debug.Log(exName + " " + direction + " " + afterRot);
                    //Debug.DrawLine(center, center + afterRot * 2, Color.green, 200);
                    //Vector3 afterRot = Quaternion.AngleAxis(-15 - direction * 30, Vector3.up) * Vector3.right;
                    
                    a1 = -afterRot.z;
                    b1 = afterRot.x;
                    c1 = center.x * (center.z + afterRot.z) - (center.x + afterRot.x) * center.z;
                    
                    // second line : 2 points -> useVerticesCopy[i] to useVerticesCopy[i + 1] 
                    int next = 0;
                    switch (i) {
                        case 0:
                            next = 1;
                            break;
                        case 1:
                            next = 3;
                            break;
                        case 2:
                            next = 0;
                            break;
                        case 3:
                            next = 2;
                            break;
                    }


                    a2 = useVerticesCopy[i].z - useVerticesCopy[next].z;
                    b2 = useVerticesCopy[next].x - useVerticesCopy[i].x;
                    c2 = useVerticesCopy[i].x * useVerticesCopy[next].z - useVerticesCopy[next].x * useVerticesCopy[i].z;

                    // Get the cross point
                    float d = a1 * b2 - a2 * b1;
                    if (d == 0) continue;
                    float crossX, crossY;
                    crossX = (b1 * c2 - b2 * c1) / d;
                    crossY = (a2 * c1 - a1 * c2) / d;



                    //check cross point is reasonable
                    //in line 
                    Vector2 v1, v2;
                    if ( (crossX - useVerticesCopy[i].x) * (useVerticesCopy[next].z - useVerticesCopy[i].z) - (useVerticesCopy[next].x - useVerticesCopy[i].x) * (crossY - useVerticesCopy[i].z) < 0.01f &&
                         Math.Min(useVerticesCopy[i].x, useVerticesCopy[next].x) <= crossX && crossX <= Math.Max(useVerticesCopy[i].x, useVerticesCopy[next].x) &&
                         Math.Min(useVerticesCopy[i].z, useVerticesCopy[next].z) <= crossY && crossY <= Math.Max(useVerticesCopy[i].z, useVerticesCopy[next].z))
                    {
                        v1 = new Vector2(crossX - center.x, crossY - center.z);
                        //Vector2 v2 = new Vector2(afterRot.x, afterRot.z);
                        v2 = new Vector2(_direct.x - center.x, _direct.z - center.z);
                        if ( Math.Abs(Vector2.Dot(v1, v2) / (v1.magnitude * v2.magnitude) - 1) < 0.0001f)
                        {
                            finalPoint.x = crossX;
                            finalPoint.z = crossY;
                        }
   
                    }
                    else
                    {
                        continue;
                    }
                    
                    
                    Vector3 dist = new Vector3(crossX - signPos.x, 0, crossY - signPos.z);
                    v1 = new Vector2(crossX - center.x, crossY - center.z);
                    v2 = new Vector2(signPos.x - center.x, signPos.z - center.z);
                    //Debug.Log(Vector2.Dot(v1, v2) / (v1.magnitude * v2.magnitude));
                    if ( Vector2.Dot(v1, v2) / (v1.magnitude * v2.magnitude) - 1 < 0.0001f && dist.magnitude <= minDistance)
                    {
                        minDistance = dist.magnitude;
                        finalPoint.x = crossX;
                        finalPoint.z = crossY;
                    }
                    
                }

                //check if touchable by the agents
                if(finalPoint == Vector3.zero)
                {
                    finalPoint = signPos;
                }
                _direct_onNavMesh = finalPoint;
                path = new NavMeshPath();
                NavMesh.CalculatePath(exits["exit1"].model.transform.position, finalPoint, walkableMask, path);
                //Debug.Log(sign_test.name + " path status: " + path.status);
                //print(_direct_onNavMesh);
                if (path.status != NavMeshPathStatus.PathComplete)
                {
                    //Debug.Log(exName + " " + direction + " " + walkableMask);
                    // GameObject st_ = Instantiate(signPrefab, planeCenter, Quaternion.identity);

                    NavMeshHit myNavHit;
                    Vector3 _direct_touch = new Vector3();
                    if (NavMesh.SamplePosition(finalPoint, out myNavHit, 5.0f, walkableMask))
                    {
                        _direct_touch = myNavHit.position;
                        NavMesh.CalculatePath(exits["exit1"].model.transform.position, _direct_touch, walkableMask, path);
                        print(_direct_touch);
                        Debug.Log(sign_test.name + " path status: " + path.status);
                        if (path.status == NavMeshPathStatus.PathComplete)
                        {
                            _direct_onNavMesh = _direct_touch;
                            // GameObject st = Instantiate(signPrefab, _direct_touch, Quaternion.identity);
                            // st.transform.Find("Text").GetComponent<TextMesh>().text = direction.ToString();
                        }
                    }
                }
                //sign_test.transform.position = finalPoint;
                sign_test.transform.position = _direct_onNavMesh;
                ex.bestViewSign.Add(sign_test);
                //ex.bestViewDirection_vector3.Add(sign_test.transform.position);
                ex.bestViewDirection_vector3.Add(_direct_onNavMesh);
                
            }

            index++;
        }
        rangeCopy.transform.localScale /= rate;

    }
    
    Vector3 generateNewVector3()
    {
        System.Random random = new System.Random(Guid.NewGuid().GetHashCode());
        Vector2 newPos = new Vector2();
        Vector3 outputPos = new Vector3();
        newPos.x = random.Next(0, currentSceneSettings.screenSize_w);
        newPos.y = random.Next(0, currentSceneSettings.screenSize_h);
        outputPos = scalePosTo3D(newPos);

        /* Have to generate in reasonable area */
        while (checkIfInReasonableArea(outputPos) == false)
        {
            newPos.x = random.Next(0, currentSceneSettings.screenSize_w);
            newPos.y = random.Next(0, currentSceneSettings.screenSize_h);
            outputPos = scalePosTo3D(newPos);
        }

        // Debug.Log(newPos + " > " + outputPos);
        return outputPos;
    }
    
    List<string> generateDesireExhibitionList(string humanType)
    {
        /* use chosen probability of each exhibition for different humanType */
        /* may change to state machine later */
        System.Random random = new System.Random(Guid.NewGuid().GetHashCode());
        List<string> newExhibitSet = new List<string>();
        foreach (KeyValuePair<string, double> interest in currentSceneSettings.humanTypes[humanType].interestForEachExhibition)
        {
            if (interest.Key != "walkableArea")
            {
                // Debug.Log(interest.Key + ": " + exhibitions[interest.Key].bestViewDirection_vector3.Count);
                float randomNum = random.Next(101);
                randomNum /= 100f;
                if (randomNum <= interest.Value * 3 && !newExhibitSet.Contains(interest.Key)) // can cheat to times value
                {
                    newExhibitSet.Add(interest.Key);
                    // relation exhibit using state map
                    foreach(KeyValuePair<string, double> prob in currentSceneSettings.exhibitionStateMap[interest.Key])
                    {
                        randomNum = random.Next(101);
                        randomNum /= 100f;
                        if (randomNum <= prob.Value && !newExhibitSet.Contains(prob.Key) && prob.Key != "walkableArea") 
                        {
                            newExhibitSet.Add(prob.Key);
                        }
                    }
                }
            }            
        }

        /* give if no desire*/ // this is a cheat
        /*int n0 = newExhibitSet.Count;
        if (n0 < 2)  
        {
            if (!newExhibitSet.Contains("p2")) newExhibitSet.Add("p2");
            if (!newExhibitSet.Contains("p4")) newExhibitSet.Add("p4");
        }*/

        /* Shuffle */
        int n = newExhibitSet.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            string value = newExhibitSet[k];
            newExhibitSet[k] = newExhibitSet[n];
            newExhibitSet[n] = value;
        }

        newExhibitSet.Add("exit" + random.Next(1, 3));
        // Debug.Log(string.Join(", ", newExhibitSet));

        return newExhibitSet;
    }

    Color generateRandomColor()
    {
        
        int keep = new System.Random().Next(0, 2);
        float red = UnityEngine.Random.Range(0f, 1f);
        float green = UnityEngine.Random.Range(0f, 1f);
        float blue = UnityEngine.Random.Range(0f, 1f);
        
        Color c = new Color(red, green, blue);
        float fixedComp = c[keep] + 0.5f;
        c[keep] = fixedComp - Mathf.Floor(fixedComp);
        return c;
    }

    Color generateColorByIndex(int idx)
    {
        //use distinct color -> obvious
        Color c = new Color();
        if (idx <= 20)
        {
            switch (idx)
            {
                case 1: //Red
                    c = new Color(230 / 255f, 25 / 255f, 75 / 255f, 1f);
                    break;
                case 2: //Green
                    c = new Color(60 / 255f, 180 / 255f, 75 / 255f, 1);
                    break;
                case 3: //Yellow
                    c = new Color(1f, 1f, 25 / 255f, 1);
                    break;
                case 4: //Blue
                    c = new Color(0, 130 / 255f, 200 / 255f, 1);
                    break;
                case 5: //Orange
                    c = new Color(245 / 255f, 130 / 255f, 48 / 255f, 1);
                    break;
                case 6: //Purple
                    c = new Color(145 / 255f, 30 / 255f, 180 / 255f, 1);
                    break;
                case 7: //Cyan
                    c = new Color(70 / 255f, 240 / 255f, 240 / 255f, 1);
                    break;
                case 8: //Magenta
                    c = new Color(240 / 255f, 50 / 255f, 230 / 255f, 1);
                    break;
                case 9: //Lime
                    c = new Color(210 / 255f, 245 / 255f, 60 / 255f, 1);
                    break;
                case 10: //Pink
                    c = new Color(250 / 255f, 190 / 255f, 190 / 255f, 1);
                    break;
                case 11: //Teal
                    c = new Color(0, 128 / 255f, 128 / 255f, 1);
                    break;
                case 12: //Lavender
                    c = new Color(230 / 255f, 190 / 255f, 255 / 255f, 1);
                    break;
                case 13: //Brown
                    c = new Color(170 / 255f, 110 / 255f, 40 / 255f, 1);
                    break;
                case 14: //Beige
                    c = new Color(255 / 255f, 250 / 255f, 200 / 255f, 1);
                    break;
                case 15: //Maroon
                    c = new Color(128 / 255f, 0, 0, 1);
                    break;
                case 16: //Mint
                    c = new Color(170 / 255f, 255 / 255f, 195 / 255f, 1);
                    break;
                case 17: //Olive
                    c = new Color(128 / 255f, 128 / 255f, 0, 1);
                    break;
                case 18: //Coral
                    c = new Color(1, 215 / 255f, 180 / 255f, 1);
                    break;
                case 19: //Navy
                    c = new Color(0, 0, 128 / 255f, 1);
                    break;
                case 20: //Grey
                    c = new Color(128 / 255f, 128 / 255f, 128 / 255f, 1);
                    break;
            }
        }
        else
        {
            c = generateRandomColor();
        }
        return c;
    }

    public void cleanPeopleBeforeGenerate() // also clean exhibitions
    {
        foreach (KeyValuePair<string, human_single> person in people)
        {
            Destroy(person.Value.model);
            Destroy(person.Value.viewPoint);
            Destroy(person.Value.informationBoard);
            Destroy(person.Value.marker);
            Destroy(person.Value.gatherMarker);
            Destroy(person.Value.colliderShape);
            person.Value.desireExhibitionList.Clear();
            person.Value.desireExhibitionList_copy.Clear();
            person.Value.trajectoryOrder.Clear();
            person.Value.influenceMap.Clear();
            person.Value.newInfluenceMap.Clear();
            Destroy(influenceMapVisualize.instance.markers[person.Key]);
            influenceMapVisualize.instance.markers.Remove(person.Key);
        }
        people.Clear();
        loadAllCharacterModels.instance.cleanUsedRecord();
        foreach (KeyValuePair<string, exhibition_single> ex in exhibitions)
        {
            Destroy(ex.Value.informationBoard);
            foreach(GameObject obj in ex.Value.bestViewSign)
            {
                Destroy(obj);
            }
            ex.Value.bestViewSign.Clear();
            ex.Value.bestViewDirection.Clear();
            ex.Value.bestViewDirection_vector3.Clear();
            ex.Value.frontSide.Clear();
            ex.Value.currentHumanInside.Clear();
            ex.Value.range.Clear();
            Destroy(influenceMapVisualize.instance.markers[ex.Key]);
            influenceMapVisualize.instance.markers.Remove(ex.Key);

            exhibitRec[ex.Key].influences.Clear();
            exhibitRec[ex.Key].bestViewCount.Clear();
            exhibitRec[ex.Key].stayingTime.Clear();
        }
        exhibitions.Clear();
        exhibitRec.Clear();
        foreach (KeyValuePair<string, exhibition_single> ex in exits)
        {
            Destroy(ex.Value.informationBoard);
            foreach (GameObject obj in ex.Value.bestViewSign)
            {
                Destroy(obj);
            }
            ex.Value.bestViewSign.Clear();
            ex.Value.bestViewDirection.Clear();
            ex.Value.bestViewDirection_vector3.Clear();
            ex.Value.frontSide.Clear();
            ex.Value.currentHumanInside.Clear();
            Destroy(influenceMapVisualize.instance.markers[ex.Key]);
            influenceMapVisualize.instance.markers.Remove(ex.Key);
        }
        exits.Clear();
    }
}

/* Analysis record information */
public partial class dynamicSystem : PersistentSingleton<dynamicSystem>
{
    void logTheLeavingAnalysis()
    {
        desiredListStatus analysis_desiredList = new desiredListStatus();
        int finishDesireListCount = 0;
        int leaveEarlyCount = 0;
        float leaveEarly_avg = 0;
        int leaveLateCount = 0;
        float leaveLate_avg = 0;

        analysis_desiredList.peopleTrajectory = new Dictionary<string, listOrderPair>();
        foreach (human_single person in people.Values)
        {
            listOrderPair storeData = new listOrderPair();
            storeData.desiredList = new List<string>(person.desireExhibitionList_copy);
            storeData.trajectoryOrder = new List<string>(person.trajectoryOrder);
            storeData.fullTrajectory = new List<List<double>>(person.fullTrajectory);
            storeData.gatherEvent = new List<gatheringEvent>(person.gatherEvent);
            storeData.humanType = person.humanType;
            storeData.walkSpeed = person.walkSpeed;

            bool allvisit = true;
            List<string> notVisit = new List<string>();
            foreach (string target in person.desireExhibitionList_copy)
            {
                if (!person.trajectoryOrder.Contains(target))
                {
                    notVisit.Add(target);
                    allvisit = false;
                }
                else // did go
                {
                    exhibitRec[target].humanCount++;
                }
            }
            string outputCheckList = person.name + " : \nOrder: " + string.Join(", ", person.trajectoryOrder);
            outputCheckList += "\nDesire:" + string.Join(", ", person.desireExhibitionList_copy);
            outputCheckList += "\nNot visit:" + string.Join(", ", notVisit) + "→" + allvisit;
            // Debug.Log(outputCheckList);

            storeData.allvisit = allvisit;
            if (allvisit)
            {
                finishDesireListCount += 1;
            }

            if (person.freeTime_totalLeft >= 0)
            {
                leaveEarlyCount += 1;
                leaveEarly_avg += person.freeTime_totalLeft;
                storeData.earlyLeave = true;
            }
            else
            {
                leaveLateCount += 1;
                leaveLate_avg += person.freeTime_totalLeft;
                storeData.earlyLeave = false;
            }

            storeData.travelTime = person.freeTime_total - person.freeTime_totalLeft;

            analysis_desiredList.peopleTrajectory.Add(person.name, storeData);
        }

        leaveEarly_avg /= leaveEarlyCount;
        leaveLate_avg /= leaveLateCount;
        float finishDesire = (float)finishDesireListCount / people.Count;
        float peopleLate = (float)leaveLateCount / people.Count;

        analysis_desiredList.finishDesireListCount = finishDesireListCount;
        analysis_desiredList.leaveEarlyCount = leaveEarlyCount;
        analysis_desiredList.leaveEarly_avg = leaveEarly_avg;
        analysis_desiredList.leaveLateCount = leaveLateCount;
        if(leaveLateCount == 0)
            analysis_desiredList.leaveLate_avg = 0.0;
        else
            analysis_desiredList.leaveLate_avg = leaveLate_avg;
        analysis_desiredList.endSimulationTime = deltaTimeCounter;
        writeLog_fps("simulation_agent", analysis_desiredList);
    }

    public void writeLog_fps<T>(string outputName, T list)
    {
        string path = directory;
        string outputFileName = UIController.instance.curOption + "_" + currentSceneSettings.customUI.UI_Global.agentCount +"agent_" + outputName + ".json";
        StringBuilder sb = new StringBuilder();
        JsonWriter writer = new JsonWriter(sb);
        writer.PrettyPrint = true;
        JsonMapper.ToJson(list, writer);
        string outputJsonStr = sb.ToString();
        // Debug.Log(outputJsonStr);
        System.IO.File.WriteAllText(path + outputFileName, outputJsonStr);
    }    
}

/* Influence Map */
public partial class dynamicSystem : PersistentSingleton<dynamicSystem>
{
    public void computeNewInfluenceMap(human_single mainHuman)
    {
        influenceMap_startTime = Time.realtimeSinceStartup;
        mainHuman.newInfluenceMap.Clear();

        /* Should have different attraction level: 
         * each influence have maximum 100 scores of influence (0 ~ 100)
         * and each influence have their own weight */

        string exitName = mainHuman.exitName; // mainHuman.desireExhibitionList.Where(r => r.StartsWith("exit")).FirstOrDefault();
        float speed = mainHuman.agent.speed;
        /* exhibition attractors */
        // Debug.Log("desire: " + string.Join(", ", mainHuman.desireExhibitionList));

        float personDistanceWithExit = calculateDistance(mainHuman.currentPosition, exits[mainHuman.exitName].leavePosition);
        float personNeededTimeToExit = personDistanceWithExit / mainHuman.agent.speed + 2;
        // Debug.Log(mainHuman.name + " totalLeft: " + mainHuman.freeTime_totalLeft + ", needExit: " + personNeededTimeToExit);
        bool noTime = false;
        if (mainHuman.freeTime_totalLeft <= personNeededTimeToExit) noTime = true;

        foreach (string ex in mainHuman.desireExhibitionList)
        {
            exhibition_single pickExhibition;
            float exhibitAttraction = 0;
            float humanPreference = 0;
            if (ex.StartsWith("exit"))
            {
                pickExhibition = exits[ex];
                if (noTime)
                {
                    exhibitAttraction = 100;
                    // Debug.Log("notTime, rush to exit");
                }
                else
                {
                    if (ex == mainHuman.nextTarget_name) // after decide a target should not decide another too soon
                    {
                        humanPreference = 100;
                    }
                    float takeTimeAttraction = computeTakeTimeAttraction_exhibit(mainHuman, pickExhibition, personDistanceWithExit, exitName, speed);
                    exhibitAttraction = takeTimeAttraction * (float)currentSceneSettings.customUI.UI_InfluenceMap.exhibitInflence["takeTime"];
                    exhibitAttraction += humanPreference *= (float)currentSceneSettings.customUI.UI_InfluenceMap.exhibitInflence["humanPreference"];
                }
            }
            else
            {
                pickExhibition = exhibitions[ex];

                /* Human: Preference for the exhibit*/
                if (ex == mainHuman.nextTarget_name) // after decide a target should not decide another too soon
                {
                    humanPreference = 100;
                }
                else
                {
                    humanPreference = (float)currentSceneSettings.humanTypes[mainHuman.humanType].interestForEachExhibition[ex];
                    humanPreference *= 100f;
                }

                float distance = calculateDistance_checkCenter(mainHuman, pickExhibition);

                /* low take time score means the time is not enough to go to. Weight more and should become a threshold! */
                float takeTimeAttraction = computeTakeTimeAttraction_exhibit(mainHuman, pickExhibition, distance, exitName, speed);

                bool capacityThreshold = (pickExhibition.capacity_cur < pickExhibition.capacity_max * currentSceneSettings.customUI.UI_Exhibit.capacityLimitTimes);
                // Debug.Log(pickExhibition.crowdedTime);
                //capacityThreshold = capacityThreshold || (pickExhibition.crowdedTime > currentSceneSettings.customUI.UI_Exhibit.crowdedTimeLimit);
                capacityThreshold = capacityThreshold && (pickExhibition.crowdedTime < currentSceneSettings.customUI.UI_Exhibit.crowdedTimeLimit);

                if (capacityThreshold && takeTimeAttraction > 0) // threshold
                {
                    /* Exhibit: capacity, take time (Distance), popular level */
                    float capacityAttraction = computeCapacityAttraction(pickExhibition.capacity_cur, pickExhibition.capacity_max);

                    float popularLevelAttraction = pickExhibition.chosenProbabilty * 100f;

                    /* best View direction */
                    float dx = mainHuman.currentPosition.x - pickExhibition.centerPosition.x;
                    float dy = mainHuman.currentPosition.z - pickExhibition.centerPosition.z;
                    float angle = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
                    if (angle < 0) angle += 360;
                    int angle_int = (int)(angle / 30);
                    float closeToBestViewDirection = computeCloseToBestDirectionAttraction(angle_int, pickExhibition.bestViewDirection, distance);

                    /* maximum of score: 300 ( 5 element to evaluate ) */
                    /* Base unity: 100%
                        * capacity[20%], take time (Distance)[30%], popular level[15%] 
                        * Human Preference [20%], best View direction [15%]
                        */
                    capacityAttraction *= (float)currentSceneSettings.customUI.UI_InfluenceMap.exhibitInflence["capactiy"];
                    takeTimeAttraction *= (float)currentSceneSettings.customUI.UI_InfluenceMap.exhibitInflence["takeTime"];
                    popularLevelAttraction *= (float)currentSceneSettings.customUI.UI_InfluenceMap.exhibitInflence["popularLevel"];
                    humanPreference *= (float)currentSceneSettings.customUI.UI_InfluenceMap.exhibitInflence["humanPreference"];
                    closeToBestViewDirection *= (float)currentSceneSettings.customUI.UI_InfluenceMap.exhibitInflence["closeToBestViewDirection"];

                    exhibitAttraction = capacityAttraction + takeTimeAttraction + popularLevelAttraction;
                    exhibitAttraction += humanPreference + closeToBestViewDirection;

                    string debugStr = "main: " + mainHuman.name + ", compute: " + ex + "\n";
                    debugStr += "total influence of exhibit: " + exhibitAttraction + " / 100\n";
                    debugStr += "<Capacity> " + capacityAttraction + "\n";
                    debugStr += "<Take Time> " + takeTimeAttraction + "\n";
                    debugStr += "<Popular level> " + popularLevelAttraction + "\n";
                    debugStr += "<Human Preference> " + humanPreference + "\n";
                    debugStr += "<Best Direction> " + closeToBestViewDirection + "\n";
                    // Debug.Log(debugStr);
                    // if (mainHuman.name == influenceMapVisualize.instance.mainHumanName) Debug.Log(debugStr);

                    exhibitAttraction *= (float)currentSceneSettings.customUI.UI_InfluenceMap.weightExhibit;
                }

                /* Store the exhibit influences*/
                exhibitionInfluence tmpInfluence = new exhibitionInfluence();
                tmpInfluence.mainHuman = mainHuman.name;
                tmpInfluence.occupancy = pickExhibition.capacity_cur / (pickExhibition.capacity_max * currentSceneSettings.customUI.UI_Exhibit.capacityLimitTimes);
                tmpInfluence.influence = exhibitAttraction;
                exhibitRec[ex].influences.Add(tmpInfluence);
            }
            mainHuman.newInfluenceMap.Add(ex, exhibitAttraction);
        }

        // if in range of exit, don't care others ( to avoid stopping in the scene).
        if (checkInExit(mainHuman.name)) return;

        /* human attractors: only matters when human are close enough */
        float GatherDesire = mainHuman.gatherDesire * 100f;
        GatherDesire *= (float)currentSceneSettings.customUI.UI_InfluenceMap.humanInflence["gatherDesire"];

        // Debug.Log(string.Join(",", peopleGathers[mainHuman.gatherIndex].humans));
        foreach (string personName in peopleGathers[mainHuman.gatherIndex].humans)
        {
            if (personName != mainHuman.name && !people[personName].checkIfFinishVisit()) // not dealing ourself and those finished
            {
                float humanAttraction = 0;
                float takeTime = computeTakeTimeAttraction_human(mainHuman, people[personName], exitName, speed);
                if (takeTime > 0)
                {
                    float followDesire = computeFollowDesire(mainHuman, people[personName]);

                    float humanTypeAttraction = (people[personName].humanType == "child" && mainHuman.humanType == "adult") ? 100f : 0f;

                    float behaviorAttraction = 0; // not yet

                    followDesire *= (float)currentSceneSettings.customUI.UI_InfluenceMap.humanInflence["followDesire"];
                    humanTypeAttraction *= (float)currentSceneSettings.customUI.UI_InfluenceMap.humanInflence["humanTypeAttraction"];
                    takeTime *= (float)currentSceneSettings.customUI.UI_InfluenceMap.humanInflence["takeTime"];
                    behaviorAttraction *= (float)currentSceneSettings.customUI.UI_InfluenceMap.humanInflence["behaviorAttraction"];
                    humanAttraction = followDesire + GatherDesire + humanTypeAttraction + takeTime + behaviorAttraction;

                    string debugStr = "main: " + mainHuman.name + ", compute: " + personName + "\n";
                    debugStr += "total influence: " + humanAttraction + " / 100\n";
                    debugStr += "gather desire: " + GatherDesire + "\n";
                    debugStr += "follow desire: " + followDesire + "\n";
                    debugStr += "humanType Attraction: " + humanTypeAttraction + "\n";
                    debugStr += "takeTime: " + takeTime + "\n";
                    debugStr += "behaviorAttraction: " + behaviorAttraction + "\n";
                    // debugStr += "<Capacity> " + capacityAttraction + "\n";
                    // Debug.Log(debugStr);

                    humanAttraction *= (float)currentSceneSettings.customUI.UI_InfluenceMap.weightHuman;
                }

                mainHuman.newInfluenceMap.Add(personName, humanAttraction);
            }
        }

        // Debug.Log(mainHuman.name + " newiMap: " + string.Join(", ", mainHuman.newInfluenceMap.Keys));
        // Debug.Log("====================================");
        influenceMap_endTime = Time.realtimeSinceStartup;
        float dif = (influenceMap_endTime - influenceMap_startTime) * 1000f;
        analysis_influenceMap.Add(dif);
    }
    /* influence Map update component */
    // float threshold_popular = 50f, threshold_crowded = 90f;  // %       
    float maxAttraction_A = 30f, maxAttraction_B = 50f;
    float maxAttraction_C = 0f;
    float slopeB = 0f, slopeC = 0f;
    /*
    float maxAttraction_C; //= 100f - maxAttraction_A - maxAttraction_B;
    float slopeB; // = ((maxAttractionA + maxAttraction_B) - maxAttraction_A) / (threshold_crowded - threshold_popular);
    float slopeC; // = (0 - maxAttraction_C) / (100 - threshold_crowded);    
    */

    float computeCapacityAttraction(float capacity_cur, float capacity_max)
    {
        /* if a exhibition gather more the % of the capacity,
         * may means lot of people want to vist.
         * if gather more than 90%, capacity will descend */

        float capacityPercent = ((float)capacity_cur / (float)capacity_max) * 100f; // map to 0 ~ 100
        float popularThreshold = (float)currentSceneSettings.customUI.UI_Exhibit.popularThreshold * 100f;
        float crowdedThreshold = (float)currentSceneSettings.customUI.UI_Exhibit.crowdedThreshold * 100f;
        float outputAttraction;

        maxAttraction_C = 100f - maxAttraction_A - maxAttraction_B;
        slopeB = ((maxAttraction_A + maxAttraction_B) - maxAttraction_A) / (crowdedThreshold - popularThreshold);
        slopeC = (0 - maxAttraction_C) / (100 - crowdedThreshold);

        /* A: attraction = 0 ~ maxAttraction_A if capacityPercent < threshold_popular.
         * B: attraction = maxAttraction_C ~ 0 if capacityPercent > threshold_crowded, -> crowd is not able to go in 
         * C: attraction = maxAttraction_A ~ maxAttraction_A + maxAttraction_B else*/
        if (capacityPercent <= popularThreshold)
        {
            /* map 0 ~ threshold_popular to 0 ~ maxAttraction_A */
            outputAttraction = capacityPercent * (maxAttraction_A / popularThreshold);
        }
        else if (capacityPercent <= crowdedThreshold)
        {
            /* map threshold_popular ~ threshold_crowded to maxAttraction_A ~ maxAttraction_B */
            outputAttraction = maxAttraction_A + slopeB * (capacityPercent - popularThreshold);
        }
        else
        {
            /* map threshold_crowded ~ 100 to maxAttraction_C ~ 0 */
            outputAttraction = maxAttraction_C + slopeC * (capacityPercent - crowdedThreshold);
        }

        //Debug.Log("<Capacity> " + (float)capacity_cur + " / " + (float)capacity_max + " = "+ capacityPercent + "% > " + outputAttraction.ToString("F2")); // debug.check
        //Debug.Log("outputAttraction:" + outputAttraction);
        return outputAttraction;
    }

    float computeTakeTimeAttraction_exhibit(human_single person, exhibition_single exhibit, float distance, string exitName, float speed)
    {
        /* consider the left time if can finish arrival and stay time */
        float outputAttraction;

        float arrivalTakingTime = distance / speed;
        float distance_toExit = dynamicSystem.instance.calculateDistance(exhibit.centerPosition, exits[exitName].leavePosition);
        float arrivalExitTakeTime = distance_toExit / speed;
        float predictUsingTime_relax = arrivalTakingTime + (float)exhibit.stayTimeSetting.max + arrivalExitTakeTime;
        float predictUsingTime_just = arrivalTakingTime + (float)exhibit.stayTimeSetting.mean + arrivalExitTakeTime;
        float predictUsingTime_min = arrivalTakingTime + (float)exhibit.stayTimeSetting.min + arrivalExitTakeTime;

        /* Can go to the exhibit and still have time */
        if (person.freeTime_totalLeft > predictUsingTime_relax)
        {
            outputAttraction = 100;
        }

        /* Predict to have enough time to finish */
        // predictUsingTime_just <= person.freeTime_totalLeft <= predictUsingTime_max
        else if (person.freeTime_totalLeft > predictUsingTime_just)
        {
            /* 100 score for the using time, use distance to modify down (normalize) */
            /* map 0 ~ longest distance in scene to 100 ~ 60*/
            // float slope = (60 - 100) / (22 - 0); = -1.82
            outputAttraction = 100f + (-1.82f * distance);
        }

        /* no time to go to*/
        else if (person.freeTime_totalLeft < predictUsingTime_min)
        {
            outputAttraction = 0;
        }

        /* rush, but may work */
        else // predictUsingTime_min <= person.freeTime_totalLeft < predictUsingTime_just
        {
            /* the closer to predictUsingTime gets the higher score*/
            /* map predictUsingTime_min ~ predictUsingTime_just to 0 ~ 60 */
            float slope = (60 - 0) / (predictUsingTime_just - predictUsingTime_min);
            outputAttraction = slope * person.freeTime_totalLeft;
        }

        string debugStr = "<Take Time>" + person.name + " " + exhibit.name + " distance: " + distance + ", speed: " + speed + "\n";
        debugStr += "timeLeft: " + person.freeTime_totalLeft + " > outputAttraction: " + outputAttraction + "\n";
        debugStr += "relax: " + predictUsingTime_relax + "\n";
        debugStr += "just: " + predictUsingTime_just + "\n";
        debugStr += "min: " + predictUsingTime_min + "\n";
        // Debug.Log(debugStr); // debug.check
        return outputAttraction;
    }

    float computeTakeTimeAttraction_human(human_single mainPerson, human_single otherPerson, string exitName, float speed)
    {
        float distance_toOtherPerson = dynamicSystem.instance.calculateDistance(mainPerson.currentPosition, otherPerson.currentPosition);
        float arrivalOtherPersonTakingTime = distance_toOtherPerson / speed;
        float distance_toExit = dynamicSystem.instance.calculateDistance(otherPerson.currentPosition, exits[exitName].centerPosition);
        float arrivalExitTakingTime = distance_toExit / speed;
        float predictUsingTime = arrivalOtherPersonTakingTime + 5 + arrivalExitTakingTime;

        if (mainPerson.freeTime_totalLeft >= predictUsingTime)
        {
            return 100f;
        }
        else
        {
            return 0f;
        }
    }

    float computeCloseToBestDirectionAttraction(int current, List<int> bestDirections, float distance)
    {
        /* the same direction: 50, +-1: 30, else: 0; chose the best answer from bestDirections */

        float outputAttraction = 0;

        if (distance < 2f)
        {
            if (bestDirections.Contains(current)) outputAttraction = 100;
            else
            {
                foreach (int direction in bestDirections)
                {
                    int abs = Math.Abs(current - direction);
                    if (abs == 1)
                    {
                        outputAttraction = 60;
                    }
                }
            }
        } // else: too far, no use           

        // Debug.Log("<Best Direction> current: " + current + " [" + string.Join(", ", bestDirections) + "] > " + outputAttraction); // debug.check
        return outputAttraction;
    }

    float computeFollowDesire(human_single mainPerson, human_single otherPerson)
    {
        float outputAttraction = 0;
        Vector3 mainPerson_vec = Vector3.Normalize(mainPerson.nextTarget_pos - mainPerson.currentPosition);
        Vector3 otherPerson_vec = Vector3.Normalize(otherPerson.nextTarget_pos - otherPerson.currentPosition);
        float cosineSimilarity = Vector3.Dot(mainPerson_vec, otherPerson_vec); // -1 ~ 1
        outputAttraction = 50f + (50 * cosineSimilarity);

        return outputAttraction;
    }

}

// else
public partial class dynamicSystem : PersistentSingleton<dynamicSystem>
{
    void Start()
    {
        //Time.fixedDeltaTime = 0.03333333f;
        path = new NavMeshPath();
        matrixSize = 500;
        sceneSize = 22;
        matrix = new float[matrixSize, matrixSize];
        for (int i = 0; i < matrixSize; i++) for (int j = 0; j < matrixSize; j++) matrix[i, j] = 0;
        gaussianValue = new float[gaussian_rate];
        for (int i = 0; i < gaussian_rate; i++)
        {
            gaussianValue[i] = (float)Math.Exp(-(i * i) / 2 * (gaussian_rate * gaussian_rate));
        }
    }

    void FixedUpdate()
    {
        if (Run == true)
        {
            deltaTimeCounter += Time.fixedDeltaTime;
            timeText.text = "Time: " + deltaTimeCounter.ToString("0.00");

            if (deltaTimeCounter - updateVisBoard > 1)
            {
                influenceMapVisualize.instance.influenceMapUpdate();
                updateVisBoard = deltaTimeCounter;
            }

            foreach (exhibition_single exhibit in exhibitions.Values)
            {
                //calculate exhibition realtime human count
                exhibit.CalculateRealtimeHumanCount();

                //calculate exhibition crowded time
                if (exhibit.capacity_cur >= exhibit.capacity_max)
                {
                    exhibit.crowdedTime += Time.fixedDeltaTime;
                }
                else
                {
                    exhibit.crowdedTime = 0f;
                }
            }

            updatePeople(); // and computeCapacity.cs will update capacity of each exhibit

            //calculate social distance
            if (deltaTimeCounter - updateSocialDistance > 1)
            {
                CalculateSocialDistance();
                updateSocialDistance = deltaTimeCounter;
            }

            // show fps
            fps_deltaTime += (Time.unscaledDeltaTime - fps_deltaTime) * 0.1f;
            double fps = 1.0f / fps_deltaTime;
            float msec = fps_deltaTime * 1000.0f;
            fpsText.text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
            fpsList.Add(fps);
            
        }
    }

    public void startSimulate()
    {
        Run = true;
        /* activate animation */
        foreach (KeyValuePair<string, human_single> person in people)
        {
            person.Value.ifMoveNavMeshAgent(true);
        }
    }

    public void pauseSimulation()
    {
        Run = false;
        foreach (KeyValuePair<string, human_single> person in people)
        {
            person.Value.ifMoveNavMeshAgent(false);
        }
        writeLog_fps("viewMode_fps", fpsList);
        writeLog_fps("compute_influenceMap", analysis_influenceMap);
        writeLog_fps("compute_updatePosition", analysis_updatePosition);

        // logTheLeavingAnalysis();
        // writeLog_fps("simulation_exhibit", exhibitRec);
    }

    
    float Lerp(float firstFloat, float secondFloat, float by)
    {
        return firstFloat + (secondFloat - firstFloat) * by;  // firstFloat * (1 - by) + secondFloat * by;
    }

    Vector3 Lerp(Vector3 firstVector, Vector3 secondVector, float by)
    {
        float retX = Lerp(firstVector.x, secondVector.x, by);
        float retZ = Lerp(firstVector.z, secondVector.z, by);
        return new Vector3(retX, 0, retZ);
    }
    
    public float generateByNormalDistribution(float mean, float std, float min, float max)
    {
        if (min > max)
        {
            // Debug.Log("wrong range");
            return mean;
        }
        else if (min == max)
        {
            // Debug.Log("min = max");
            return min;
        }
        /* y = mean* x + std; */
        float seedX = 0, returnValue = -1;
        int chance = 0;
        while (!(min < returnValue && returnValue < max) && chance < 100) // not in a reasonable range
        {
            //System.Random random = new System.Random(Guid.NewGuid().GetHashCode());
            System.Random random = new System.Random(Guid.NewGuid().GetHashCode());
            //seedX = dynamicSystem.instance.random.Next(-100, 100);
            seedX = random.Next(-100, 100);
            seedX /= 100f; // -1 ~ 1
            returnValue = mean * seedX + std;
            chance++;
        }

        if (!(min < returnValue && returnValue < max)) return mean;

        return returnValue;
    }

    public float generateByNormalDistributionForStayTime(float mean, float std, float min, float max, human_single person)
    {
        if (min > max)
        {
            // Debug.Log("wrong range");
            return mean;
        }
        else if (min == max)
        {
            // Debug.Log("min = max");
            return min;
        }
        /* y = mean* x + std; */
        float seedX = 0, returnValue = -1;
        int chance = 0;
        while (!(min < returnValue && returnValue < max) && chance < 100) // not in a reasonable range
        {
            //System.Random random = new System.Random(Guid.NewGuid().GetHashCode());
            //System.Random random = new System.Random(person.randomSeed);
            System.Random random = person.random; // new random change
            //seedX = dynamicSystem.instance.random.Next(-100, 100);
            seedX = random.Next(-100, 100);
            seedX /= 100f; // -1 ~ 1
            returnValue = mean * seedX + std;
            chance++;
        }

        if (!(min < returnValue && returnValue < max)) return mean;

        return returnValue;
    }

    /* Smooth move and scale pos to 3D */
    Vector3 scalePosTo3D(Vector2 input)
    {
        float scaleUnit_x = 100.0f, scaleUnit_y = 100.0f;
        Vector3 outputVec = new Vector3((halfW - Mathf.Round(input.x)) / scaleUnit_x, 0, (input.y - Mathf.Round(halfH)) / scaleUnit_y);
        outputVec.x += 50 * UIController.instance.allScene.IndexOf(UIController.instance.currentScene);
        outputVec.z += 50 * UIController.instance.curSceneOptions.IndexOf(UIController.instance.curOption);

        return outputVec;
    }

    List<double> scalePosBackTo2D(Vector3 input)
    {
        // back to the position 119 is
        List<double> outputVec = new List<double>();
        outputVec.Add(input.x);
        outputVec.Add(input.z);

        outputVec[0] -= 50 * UIController.instance.allScene.IndexOf(UIController.instance.currentScene);
        outputVec[1] -= 50 * UIController.instance.curSceneOptions.IndexOf(UIController.instance.curOption);

        return outputVec;
    }

    void UpdateIsTargetPointUseStatus()
    {
        for (int i = 0; i < isTargetPointUse.Count; i++) 
        { 
            isTargetPointUse[isTargetPointUse.ElementAt(i).Key] = false; 
        }
        foreach (KeyValuePair<string, human_single> person in people)
        {
            if (person.Value.targetPointName.StartsWith("p")) isTargetPointUse[person.Value.targetPointName] = true;
        }
        /* In generateExhibitions 
        foreach (KeyValuePair<string, bool> tp in isTargetPointUse)
        {
            //Debug.Log("tp.Key: " + tp.Key);
            GameObject ball = targetPointBall[tp.Key];
            var renderer = ball.GetComponent<Renderer>();
            if (tp.Value)
            {
                renderer.material.SetColor("_Color", Color.red);
            }
            else
            {
                renderer.material.SetColor("_Color", Color.green);
            }
        }
        */
    }

    /* Checker */
    bool checkIfInReasonableArea(Vector3 pos_3D)
    {
        NavMeshHit hit;
        /* Should be in walkable area and not too close to boundary */
        if (NavMesh.SamplePosition(pos_3D, out hit, 0.1f, walkableMask))
        {
            NavMesh.FindClosestEdge(pos_3D, out hit, walkableMask);
            if (hit.distance > 0.15f)
            {
                return true;
            }
        }

        // else
        return false;
    }

    /* for debug and comfirm */
    public void setActiveAllInformationBoard_human(bool ifActive)
    {
        foreach (KeyValuePair<string, human_single> person in people)
        {
            person.Value.informationBoard.SetActive(ifActive);
        }
        showInfoBoard_human = ifActive;
    }

    public void setActiveColliderShape_human(bool ifActive)
    {
        foreach (KeyValuePair<string, human_single> person in people)
        {
            person.Value.colliderShape.SetActive(ifActive);
        }
    }

    public void setActiveAllInformationBoard_exhibit(bool ifActive)
    {
        foreach (KeyValuePair<string, exhibition_single> exhibit in exhibitions)
        {
            exhibit.Value.setActiveInformationBoard(ifActive);
        }
    }

    public void setActiveAllTargetPosSign(bool ifActive)
    {
        foreach (KeyValuePair<string, exhibition_single> exhibit in exhibitions)
        {
            foreach (GameObject sign in exhibit.Value.bestViewSign)
            {
                sign.SetActive(ifActive);
            }
        }
    }

    public void setVisibleAllRange_exhibit(bool ifVisible)
    {
        foreach (KeyValuePair<string, exhibition_single> exhibit in exhibitions)
        {
            foreach (GameObject range in exhibit.Value.range)
            {
                range.GetComponent<MeshRenderer>().enabled = ifVisible;
            }
        }
    }

    public void QuickSimulationMode()
    {
        
        quickSimulationMode = true;
        Time.timeScale = 7;

        mainCamera = UIController.instance.cameras[UIController.instance.currentScene].mainCamera;
        miniCamera = UIController.instance.cameras[UIController.instance.currentScene].minimapCamera;
        mainCamera.SetActive(false);
        miniCamera.SetActive(false);
        //Time.fixedDeltaTime = Time.timeScale * 0.01666667f;
        Time.fixedDeltaTime = 0.0333333f;
        
        foreach (Transform p in peopleParent.transform) ShowVisitor(p, false);
        // Get Environment
        GameObject environment = GameObject.Find("/[EnvironmentsOfEachScene]/" + UIController.instance.curOption);
        foreach (Transform exhibit in environment.transform) ShowExhibit(exhibit, false);
        
        startSimulate();
    }

    void QuickSimulationModeQuit()
    {
        mainCamera = UIController.instance.cameras[UIController.instance.currentScene].mainCamera;
        miniCamera = UIController.instance.cameras[UIController.instance.currentScene].minimapCamera;
        mainCamera.SetActive(true);
        miniCamera.SetActive(true);

        quickSimulationMode = false;
        Time.timeScale = 1;
        Time.fixedDeltaTime = 0.03333333f;


        //foreach (Transform p in peopleParent.transform) ShowVisitor(p, true);
        // Get Environment
        GameObject environment = GameObject.Find("/[EnvironmentsOfEachScene]/" + UIController.instance.curOption);
        foreach (Transform exhibit in environment.transform) ShowExhibit(exhibit, true);
        
    }

    public void QuickModeTest()
    {
        quickSimulationMode = true;
        //disable something don't care
        //foreach (Transform p in peopleParent.transform) ShowVisitor(p, false);
        GameObject environment = GameObject.Find("/[EnvironmentsOfEachScene]");
        foreach(Transform subScene in environment.transform)
        {
            if (subScene.name != UIController.instance.curOption) subScene.gameObject.SetActive(false);
        }
        environment = GameObject.Find("/[EnvironmentsOfEachScene]/" + UIController.instance.curOption);
        //foreach (Transform exhibit in environment.transform) ShowExhibit(exhibit, false);

        for(int i = 1; i <= 3; i++)
        {
            GameObject light = GameObject.Find("Area Light (" + i.ToString() + ")");
            light.SetActive(false);
        }
        
        //GameObject UI = GameObject.Find("[UI]");
        //UI.SetActive(false);
        GameObject Visual = GameObject.Find("[Visualization]");
        Visual.SetActive(false);

        mainCamera = UIController.instance.cameras[UIController.instance.currentScene].mainCamera;
        miniCamera = UIController.instance.cameras[UIController.instance.currentScene].minimapCamera;
        mainCamera.SetActive(false);
        miniCamera.SetActive(false);
        //Time.fixedDeltaTime = Time.timeScale * 0.01666667f;
        Time.fixedDeltaTime = 0.03333333f;
        Time.timeScale = 15;

        //startSimulate();
    }

    void ShowVisitor(Transform visitor, bool show)
    {
        Queue<Transform> components = new Queue<Transform>();
        components.Enqueue(visitor);
        while (components.Count > 0)
        {
            Transform component = components.Dequeue();
            SkinnedMeshRenderer smr = component.GetComponent<SkinnedMeshRenderer>();
            if (smr != null) smr.enabled = show;
            MeshRenderer mr = component.gameObject.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = show;
            if (component.childCount > 0) foreach (Transform child in component) components.Enqueue(child);
        }
        /*
        foreach (Transform child in visitor)
        {
            SkinnedMeshRenderer smr = child.GetComponent<SkinnedMeshRenderer>();
            if (smr != null) smr.enabled = show;
            MeshRenderer mr = child.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = show;
        }
        */
    }

    void ShowExhibit(Transform exhibit, bool show)
    {
        Queue<Transform> components = new Queue<Transform>();
        components.Enqueue(exhibit);
        while(components.Count > 0)
        {
            Transform component = components.Dequeue();
            if (component.name.Contains("Boundary")) continue;
            if (show && (component.name == "BoundingBoxCube" || component.name == "ViewPoint")) continue;
            
            MeshRenderer mr = component.gameObject.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = show;

            if (component.childCount > 0) foreach (Transform child in component) components.Enqueue(child);
        }
    }

    public void setHeatmap()
    {
        Debug.Log(heatmapToggle.isOn);
        if (heatmapToggle.isOn)
        {
            heatmapMode = "realtime";
            Heatmap.SetActive(heatmapToggle.isOn);
        }
        else 
        {
            Heatmap.SetActive(heatmapToggle.isOn);
        }
    }


    void NavMeshBake()
    {
        NavMesh.RemoveAllNavMeshData();
        NavMesh.avoidancePredictionTime = 5.0f;
        navMeshSurface.BuildNavMesh();
    }

    public void SaveVISBtn()
    {
        saveVIS = !saveVIS;
        string msg = "";
        if (saveVIS)
        {
            string defaultFolder = "", defaultFileName = "";
            defaultFolder = Application.streamingAssetsPath + "/VisitorSetting";
            System.DateTime dt = System.DateTime.Now;
            string date = dt.Year + "-" + dt.Month + "-" + dt.Day + "_" + dt.Hour + "-" + dt.Minute + "-" + dt.Second;
            defaultFileName = "VisitorSetting_" + UIController.instance.currentScene + "_" + date;
            /*
            var path = EditorUtility.SaveFilePanel("Save UI setting as JSON",
                                        defaultFolder,
                                        defaultFileName + ".json",
                                        "json");
            */
            string path = "";
            var bp = new BrowserProperties();
            bp.title = "Save Visitor Initial Setting";
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
                saveVISFilename = path;
                saveVISBtn.GetComponent<Image>().color = Color.gray;

                string[] filename = path.Split('/');
                msg += "Save visitor initial setting!\n" +
                       "Click \'Generate\' button to save data to the file\n" +
                       "filename: " + filename[filename.Length - 1] + "\n";

                if (loadVIS)
                {
                    loadVIS = false;
                    loadVISBtn.GetComponent<Image>().color = Color.white;
                    loadVISFilename = "";
                    msg += "Loading visitor initial setting is canceled.\n";
                }
                
                UIController.instance.ShowMsgPanel("Success", msg);
            }
            else
            {
                saveVIS = false;
                //UIController.instance.ShowMsgPanel("Warning", "Visitor initial setting is not saved.");
            }
        }
        else
        {
            saveVISBtn.GetComponent<Image>().color = Color.white;
            if (saveVISFilename != "")  saveVISFilename = "";
            UIController.instance.ShowMsgPanel("Warning", "Saving visitor initial setting is canceled.\n");
        }
    }

    public void LoadVISBtn()
    {
        loadVIS = !loadVIS;
        string msg = "";
        if (loadVIS)
        {
            string path = "";
            var bp = new BrowserProperties();
            bp.title = "Load Visitor Initial Setting File";
            bp.initialDir = Application.streamingAssetsPath + "/VisitorSetting";
            bp.filter = "json files (*.json)|*.json";
            bp.filterIndex = 0;

            new FileBrowser().OpenFileBrowser(bp, filepath =>
            {
                //Do something with path(string)
                Debug.Log(filepath);
                path = filepath;
            });
            /*
            string defaultFolder = Application.streamingAssetsPath + "/VisitorSetting";
            var path = EditorUtility.OpenFilePanel("Load Visitor Initial Setting",
                                                    defaultFolder,
                                                   "json");
            */
            if (path.Length != 0)
            {
                if (!CheckVIS(path)) 
                {
                    loadVIS = false;
                    return;
                }
                
                loadVISFilename = path;
                loadVISBtn.GetComponent<Image>().color = Color.gray;

                string[] filename = path.Split('/');
                msg += "Load visitor initial setting!\n" +
                       "filename: " + filename[filename.Length - 1] + "\n";

                if (saveVIS)
                {
                    saveVIS = false;
                    saveVISBtn.GetComponent<Image>().color = Color.white;
                    saveVISFilename = "";
                    msg += "Saving visitor initial setting is canceled.\n";
                }
                if (randomVIS) 
                {
                    randomVIS = false;
                    randomVISBtn.GetComponent<Image>().color = Color.white;
                    msg += "Deactivate random desired list.\n";
                }

                UIController.instance.ShowMsgPanel("Success", msg);
            }
            else
            {
                loadVIS = false;
                //UIController.instance.ShowMsgPanel("Warning", "Visitor initial setting is not loaded.");
            }
        }
        else
        {
            loadVISBtn.GetComponent<Image>().color = Color.white;
            if (loadVISFilename != "") loadVISFilename = "";
            UIController.instance.ShowMsgPanel("Warning", "Loading visitor initial setting is canceled.\n");
        }
    }

    public void RandomVISBtn()
    {
        randomVIS = !randomVIS;
        string msg = "";
        if (randomVIS)
        {
            randomVISBtn.GetComponent<Image>().color = Color.gray;
            msg += "Activate random desired list!\n";
            if (loadVIS)
            {
                loadVIS = false;
                loadVISBtn.GetComponent<Image>().color = Color.white;
                loadVISFilename = "";
                msg += "Loading visitor initial setting is canceled.\n";
            }

            UIController.instance.ShowMsgPanel("Success", msg);
        }
        else
        {
            randomVISBtn.GetComponent<Image>().color = Color.white;
            UIController.instance.ShowMsgPanel("Warning", "Deactivate random desired list!\n\n");
        }
    }

    bool CheckVIS(string file)
    {
        List<humanInfo> humansInfo = new List<humanInfo>();
        string visJsonDataStr = File.ReadAllText(file);
        humansInfo = JsonMapper.ToObject<List<humanInfo>>(visJsonDataStr);

        string errorMsg = "";
        int visitorsCount = humansInfo.Count;
        
        if (UIController.instance.tmpSaveUISettings.UI_Global.agentCount != visitorsCount) 
        {
            errorMsg += "agentCount in UI is wrong.\n";
            UIController.instance.ShowMsgPanel("Warning", errorMsg);
            return false;
        }

        int addVisitorsCount = 0, adultCount = 0;
        List<string> walkspeedErrorId = new List<string>();
        List<string> freetimeTotalErrorId = new List<string>();
        List<string> startSimulationTimeErrorId = new List<string>();
        for (int i = 0; i < humansInfo.Count; i++)
        {
            //check addVisitorCount
            if (humansInfo[i].startSimulateTime > 0) 
            {
                addVisitorsCount++;
                //check startSimulationTime
                if (humansInfo[i].startSimulateTime > UIController.instance.tmpSaveUISettings.UI_Global.startAddAgentMax ||
                   humansInfo[i].startSimulateTime < UIController.instance.tmpSaveUISettings.UI_Global.startAddAgentMin)
                {
                    startSimulationTimeErrorId.Add(humansInfo[i].name);
                }
            } 
            //check freetimeTotal
            if(humansInfo[i].freeTime_total > UIController.instance.tmpSaveUISettings.UI_Human.freeTimeMax ||
               humansInfo[i].freeTime_total < UIController.instance.tmpSaveUISettings.UI_Human.freeTimeMin)
            {
                freetimeTotalErrorId.Add(humansInfo[i].name);
            }
            //check walkSpeed
            float maxSpeed = UIController.instance.tmpSaveUISettings.UI_Human.walkSpeedMax;
            if(UIController.instance.tmpSaveUISettings.UI_Human.walkSpeedMax == -1) maxSpeed = (float)currentSceneSettings.humanTypes[humansInfo[i].humanType].walkSpeed.max;
            if (humansInfo[i].walkSpeed > maxSpeed || humansInfo[i].walkSpeed < UIController.instance.tmpSaveUISettings.UI_Human.walkSpeedMin)
            {
                walkspeedErrorId.Add(humansInfo[i].name);
            }
            //check adultCount
            if (humansInfo[i].humanType == "adult") adultCount++;
        }

        if (adultCount != (int)Math.Round(UIController.instance.tmpSaveUISettings.UI_Global.agentCount * UIController.instance.tmpSaveUISettings.UI_Global.adultPercentage))
        {
            print("adultCount: " + adultCount);
            print(UIController.instance.tmpSaveUISettings.UI_Global.agentCount);
            print(UIController.instance.tmpSaveUISettings.UI_Global.adultPercentage);
            print((int)Math.Round(UIController.instance.tmpSaveUISettings.UI_Global.agentCount * UIController.instance.tmpSaveUISettings.UI_Global.adultPercentage));
            errorMsg += "adultPercentage in UI is wrong.\n";
        }

        if (UIController.instance.tmpSaveUISettings.UI_Global.addAgentCount != addVisitorsCount)
        {
            errorMsg += "addAgentCount in UI is wrong.\n";
        }

        if (startSimulationTimeErrorId.Count > 0)
        {
            errorMsg += "startSimulateTime error: ";
            for (int i = 0; i < startSimulationTimeErrorId.Count; i++)
            {
                if (i < startSimulationTimeErrorId.Count - 1) errorMsg += startSimulationTimeErrorId[i] + ", ";
                else errorMsg += startSimulationTimeErrorId[i] + "\n";
            }
        }

        if (freetimeTotalErrorId.Count > 0)
        {
            errorMsg += "freeTimeTotal error: ";
            for(int i = 0; i < freetimeTotalErrorId.Count; i++)
            {
                if (i < freetimeTotalErrorId.Count - 1) errorMsg += freetimeTotalErrorId[i] + ", ";
                else errorMsg += freetimeTotalErrorId[i] + "\n";
            }
        }

        if (walkspeedErrorId.Count > 0)
        {
            errorMsg += "walkSpeed error: ";
            for (int i = 0; i < walkspeedErrorId.Count; i++)
            {
                if (i < walkspeedErrorId.Count - 1) errorMsg += walkspeedErrorId[i] + ", ";
                else errorMsg += walkspeedErrorId[i] + "\n";
            }
        }

        if(errorMsg == "")
        {
            return true;
        }
        else
        {
            UIController.instance.ShowMsgPanel("Warning", errorMsg);
            return false;
        }

    }

    void TakeLayoutScreenShot()
    {
        int resWidth = 1200, resHeight = 1200;
        SetScreenShotCamera();
        Camera camera = screenshotCamera.GetComponent<Camera>();
        SetCameraCullingMask(camera, UIController.instance.currentScene);
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        camera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        camera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        camera.targetTexture = null;
        RenderTexture.active = null; // JC: added to avoid errors
        screenshotCamera.SetActive(false);
        Destroy(rt);
        byte[] bytes = screenShot.EncodeToPNG();
        //string filename = ScreenShotName(resWidth, resHeight);
        string filename = directory + "layout_screenshot.png";
        System.IO.File.WriteAllBytes(filename, bytes);
    }

    void SetScreenShotCamera()
    {
        screenshotCamera.SetActive(true);
        Vector3 offset = Vector3.zero;
        Vector3 heatmapCameraOriginPos = new Vector3(-0.5f, 15, 0);
        switch (UIController.instance.currentScene)
        {
            case "119":
                offset += new Vector3(0, 0, 0);
                screenshotCamera.GetComponent<Camera>().orthographicSize = 10.48f;
                break;
            case "120":
                offset += new Vector3(50, 0, 0);
                screenshotCamera.GetComponent<Camera>().orthographicSize = 10.9f;
                break;
            case "225":
                offset += new Vector3(100, 0, 4);
                screenshotCamera.GetComponent<Camera>().orthographicSize = 8.58f;
                break;
        }
        if (UIController.instance.curOption.Contains("A")) offset += new Vector3(0, 0, 50);
        else if (UIController.instance.curOption.Contains("B")) offset += new Vector3(0, 0, 100);
        else offset += new Vector3(0, 0, 0);

        screenshotCamera.transform.position = heatmapCameraOriginPos + offset;
    }

    void SetCameraCullingMask(Camera cam, string sceneHeadName)
    {
        int defaultLayer = LayerMask.NameToLayer("Default");
        int miniLayer = LayerMask.NameToLayer("miniMap");
        int marker = LayerMask.NameToLayer("marker");
        int scene = LayerMask.NameToLayer(sceneHeadName);
        cam.cullingMask = (1 << defaultLayer) | (1 << miniLayer) | (1 << marker) | (1 << scene);
    }
    /*****data analysis method*****/
    //trajectory to heatmap 
    //have little problem: wrong index 
    void TrajectoryToHeatmap(int size, int radius)
    {
        //size means square 2d array
        //radius means how many small grid should be divided into in half length(half of size)
        int[,] space_usage = new int[size, size];
        float[,] time_usage = new float[size, size];
        int mag = size / (2 * radius);

        //initial array
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                space_usage[i, j] = 0;
                time_usage[i, j] = 0;
            }
        }

        //calculate
        foreach (KeyValuePair<string, human_single> person in people)
        {
            //record the initial pos
            int sx = (int)Math.Floor(mag * (person.Value.fullTrajectory[0][0] + radius)); //col
            int sy = (int)Math.Floor(mag * (radius - person.Value.fullTrajectory[0][1])); //row
            sx = CheckValidValue(sx, size);
            sy = CheckValidValue(sy, size);
            space_usage[sy, sx]++;

            for (int i = 0; i < person.Value.fullTrajectory.Count() - 1; i++)
            {
                //Debug.Log(person.Value.fullTrajectory[i][0] + ", " + person.Value.fullTrajectory[i][1]);
                //get 2 points
                sx = (int)Math.Floor(mag * (person.Value.fullTrajectory[i][0] + radius)); //col
                sy = (int)Math.Floor(mag * (radius - person.Value.fullTrajectory[i][1])); //row
                int dx = (int)Math.Floor(mag * (person.Value.fullTrajectory[i + 1][0] + radius)); //col
                int dy = (int)Math.Floor(mag * (radius - person.Value.fullTrajectory[i + 1][1])); //row

                //check
                sx = CheckValidValue(sx, size);
                sy = CheckValidValue(sy, size);
                dx = CheckValidValue(dx, size);
                dy = CheckValidValue(dy, size);

                Vector2 source = new Vector2((float)sy, (float)sx);
                Vector2 destination = new Vector2((float)dy, (float)dx);

                //Debug.Log(Vector2.Distance(source, destination));
                //Debug.Log(person.Value.velocity_Trajectory[i]);
                //walk
                if (Vector2.Distance(source, destination) > 0.0f)
                {
                    float length = Mathf.Sqrt((dx - sx) * (dx - sx) + (dy - sy) * (dy - sy));
                    //no scope, verticle
                    if (destination.y - source.y == 0)
                    {
                        //go up
                        if (sy > dy)
                        {
                            for (int y = sy - 1; y >= dy; y--)
                            {
                                y = CheckValidValue(y, size);
                                space_usage[y, sx]++;
                                time_usage[y, sx] += 1 / length;
                            }
                        }
                        //go down
                        else
                        {
                            for (int y = sy + 1; y <= dy; y++)
                            {
                                y = CheckValidValue(y, size);
                                space_usage[y, sx]++;
                                time_usage[y, sx] += 1 / length;
                            }
                        }
                        continue;
                    }

                    float scope = (destination.x - source.x) / (destination.y - source.y);
                    int min_x, max_x;
                    float before_y;
                    if (sx > dx)
                    {
                        min_x = dx;
                        max_x = sx;
                        before_y = (float)dy;
                    }
                    else
                    {
                        min_x = sx;
                        max_x = dx;
                        before_y = (float)sy;
                    }
                    if (scope > 0) // go right-down or left-up
                    {
                        for (int x = min_x + 1; x <= max_x; x++)
                        {
                            //next_y is a integer or not
                            float next_y = before_y + scope;
                            int Int_next_y = (int)Math.Floor(next_y);
                            Int_next_y = CheckValidValue(Int_next_y, size);
                            space_usage[Int_next_y, x]++;
                            time_usage[Int_next_y, x] += 1 / length;
                            bool is_int = false;
                            if (next_y == (int)next_y) is_int = true;
                            if (scope > 1)
                            {
                                for (int y = (int)Math.Floor(before_y + 1); y < Int_next_y; y++)
                                {
                                    space_usage[y, x - 1]++;
                                    time_usage[y, x - 1] += 1 / length;
                                }
                                if (!is_int)
                                {
                                    space_usage[Int_next_y, x - 1]++;
                                    time_usage[Int_next_y, x - 1] += 1 / length;
                                }
                            }
                            if (scope < 1)
                            {
                                if (!is_int && Math.Floor(next_y) > Math.Floor(before_y))
                                {
                                    space_usage[Int_next_y, x - 1]++;
                                    time_usage[Int_next_y, x - 1] += 1 / length;
                                }
                            }
                            before_y = next_y;
                        }
                    }
                    else // go right-up or left-down
                    {
                        for (int x = min_x + 1; x <= max_x; x++)
                        {
                            //next_y is a integer or not
                            float next_y = before_y + scope;
                            int Int_next_y = (int)Math.Floor(next_y);
                            Int_next_y = CheckValidValue(Int_next_y, size);

                            space_usage[Int_next_y, x]++;
                            time_usage[Int_next_y, x] += 1 / length;
                            bool is_int = false;
                            if (next_y == (int)next_y) is_int = true;
                            if (scope < -1)
                            {
                                for (int y = (int)Math.Floor(before_y - 1); y > Int_next_y; y--)
                                {
                                    space_usage[y, x - 1]++;
                                    time_usage[y, x - 1] += 1 / length;
                                }
                                if (!is_int)
                                {
                                    space_usage[Int_next_y, x - 1]++;
                                    time_usage[Int_next_y, x - 1] += 1 / length;
                                }
                            }
                            if (scope > -1)
                            {
                                if (!is_int && Math.Floor(next_y) < Math.Floor(before_y))
                                {
                                    space_usage[Int_next_y, x - 1]++;
                                    time_usage[Int_next_y, x - 1] += 1 / length;
                                }
                            }
                            before_y = next_y;
                        }
                    }
                }// if walk
            }// each person trajectory
        }//foreach every person trajectory

        //write file
        //space usage file
        FileStream fs = new FileStream(directory + "space_usage.txt", FileMode.OpenOrCreate);
        StreamWriter sw = new StreamWriter(fs);
        for(int i = 0; i < size; i++)
        {
            for(int j = 0; j < size; j++)
            {
                sw.Write(space_usage[i, j] + " ");
            }
            sw.Write("\n");
        }
        sw.Flush();
        sw.Close();
        //time usage file
        fs = new FileStream(directory + "time_usage.txt", FileMode.OpenOrCreate);
        sw = new StreamWriter(fs);
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if(time_usage[i, j] == 0)
                {
                    sw.Write(0 + " ");
                }
                else
                {
                    float val = time_usage[i, j] / space_usage[i, j];
                    sw.Write(val + " ");
                }

            }
            sw.Write("\n");
        }
        sw.Flush();
        sw.Close();


        

        //record exhibition position in usage analysis
        fs = new FileStream(directory + "exhibition_record_usage.txt", FileMode.OpenOrCreate);
        sw = new StreamWriter(fs);
        GameObject scene = GameObject.Find("/[EnvironmentsOfEachScene]/" + UIController.instance.curOption);
        string sceneNum = UIController.instance.curOption.Substring(0, 3);
        foreach (Transform child in scene.transform)
        {
            if (child.name.Contains(sceneNum) && !child.name.Contains("ExitDoor") && !child.name.Contains("x"))
            {
                int sx = (int)Math.Floor(mag * (scalePosBackTo2D(child.position)[0] + radius)); //col
                int sy = (int)Math.Floor(mag * (radius - scalePosBackTo2D(child.position)[1])); //row

                sw.Write(sy + " " + sx + "\n");
            }
        }
        sw.Flush();
        sw.Close();
        Debug.Log("data analysis: heatmap complete");

    }

    //transform position in TrajectoryToHeatmap
    int CheckValidValue(int val, int size)
    {
        int modifyValue = val;
        if (val >= 0 && val < size) return modifyValue;
        else
        {
            if (val < 0)
            {
                modifyValue = 0;
            }
            if (val >= size)
            {
                modifyValue = size - 1;
            }
            return modifyValue;
        }
    }

    public void TrajectoryToHeatmapWithGaussian(int size, int scene_half_size, int gaussian_rate, bool saveTXT, int fileIndex, string mode)
    {
        float[,] space_usage = new float[size, size];
        float[,] time_usage = new float[size, size];
        int[,] count = new int[size, size];
        int radius = size / 2;
        int gaussian_distance = (radius / scene_half_size) / 2;
        int gaussian_total_distance = gaussian_distance * gaussian_rate;
        //int gaussian_total_distance = gaussian_distance;

        //initial gaussian value
        int sigma = gaussian_rate;

        float[] gaussianValue = new float[gaussian_rate];

        for(int i = 0; i < gaussian_rate; i++)
        {
            gaussianValue[i] = (float)Math.Exp( - (i * i) / 2 * (sigma * sigma));
        }


        //initial array
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                space_usage[i, j] = 0;
                time_usage[i, j] = 0;
                count[i, j] = 0;
            }
        }

        float offsetX = 0, offsetZ = 0;
        if(UIController.instance.currentScene == "225")
        {
            offsetX = 1.2f;
            offsetZ = 3.75f;
        }


        //calculate
        foreach (KeyValuePair<string, human_single> person in people)
        {
            /*
            //record the initial pos
            int sx = (int)Math.Floor( (1 + (person.Value.fullTrajectory[0][0] / scene_half_size)) * radius); //col
            int sy = (int)Math.Floor( (1 - (person.Value.fullTrajectory[0][1] / scene_half_size)) * radius); //row
            sx = CheckValidValue(sx, size);
            sy = CheckValidValue(sy, size);

            //gaussian
            int rowBegin = sy - gaussian_distance, rowEnd = sy + gaussian_distance;
            int colBegin = sx - gaussian_distance, colEnd = sx + gaussian_distance;

            rowBegin = CheckValidValue(rowBegin, size);
            rowEnd = CheckValidValue(rowEnd, size);
            colBegin = CheckValidValue(colBegin, size);
            colEnd = CheckValidValue(colEnd, size);

            for (int j = rowBegin; j <= rowEnd; j++)
            {
                for (int k = colBegin; k <= colEnd; k++)
                {
                    Vector2 diff = new Vector2(j - sy, k - sx);
                    if (diff.magnitude <= gaussian_distance)
                    {
                        space_usage[j, k]++;
                    }
                }
            }
            */

            for (int i = 0; i < person.Value.fullTrajectory.Count() - 1; i++)
            {
                float trueDistance = Vector3.Distance(new Vector3((float)person.Value.fullTrajectory[i][0], 0, (float)person.Value.fullTrajectory[i][1]),
                                                      new Vector3((float)person.Value.fullTrajectory[i + 1][0], 0, (float)person.Value.fullTrajectory[i + 1][1]));
                int sx = (int)Math.Floor( (1 + ( (person.Value.fullTrajectory[i][0] - offsetX) / scene_half_size)) * radius); //col
                int sy = (int)Math.Floor( (1 - ( (person.Value.fullTrajectory[i][1] - offsetZ) / scene_half_size)) * radius); //row
                int dx = (int)Math.Floor( (1 + ( (person.Value.fullTrajectory[i + 1][0] - offsetX) / scene_half_size)) * radius); //col
                int dy = (int)Math.Floor( (1 - ( (person.Value.fullTrajectory[i + 1][1] - offsetZ) / scene_half_size)) * radius); //row

                sx = CheckValidValue(sx, size);
                sy = CheckValidValue(sy, size);
                dx = CheckValidValue(dx, size);
                dy = CheckValidValue(dy, size);

                Vector2 source = new Vector2((float)sy, (float)sx);
                Vector2 destination = new Vector2((float)dy, (float)dx);
                float distance = Vector2.Distance(source, destination);
                if (distance > 0.0f)
                {
                    /*
                    //gaussian
                    int rowBegin = dy - gaussian_total_distance;
                    int rowEnd = dy + gaussian_total_distance;
                    int colBegin = dx - gaussian_total_distance;
                    int colEnd = dx + gaussian_total_distance;

                    rowBegin = CheckValidValue(rowBegin, size);
                    rowEnd = CheckValidValue(rowEnd, size);
                    colBegin = CheckValidValue(colBegin, size);
                    colEnd = CheckValidValue(colEnd, size);

                    for (int j = rowBegin; j <= rowEnd; j++)
                    {
                        for (int k = colBegin; k <= colEnd; k++)
                        {
                            Vector2 diff = new Vector2(j - dy, k - dx);

                            int level = (int) Math.Floor(diff.magnitude / gaussian_distance);

                            if ( level < gaussian_rate)
                            {
                                //space_usage[j, k]++;
                                space_usage[j, k] += gaussianValue[level];
                                time_usage[j, k] += (trajectoryRecordTime / distance) * gaussianValue[level];
                                count[j, k]++;
                                //time_usage[j, k] += distance / 0.5f;
                            }
                        }
                    }
                    */
                    int interpolateTimes = 1;
                    if (Time.fixedDeltaTime > trajectoryRecordTime) interpolateTimes = Mathf.FloorToInt(Time.fixedDeltaTime / trajectoryRecordTime);

                    print(interpolateTimes);
                    for(int t = 1; t < interpolateTimes + 1; t++)
                    {
                        int midYPoint = Mathf.FloorToInt(sy + t * ( (dy - sy) / interpolateTimes));
                        int midXPoint = Mathf.FloorToInt(sx + t * ((dx - sx) / interpolateTimes));

                        //gaussian
                        int rowBegin = midYPoint - gaussian_total_distance;
                        int rowEnd = midYPoint + gaussian_total_distance;
                        int colBegin = midXPoint - gaussian_total_distance;
                        int colEnd = midXPoint + gaussian_total_distance;

                        rowBegin = CheckValidValue(rowBegin, size);
                        rowEnd = CheckValidValue(rowEnd, size);
                        colBegin = CheckValidValue(colBegin, size);
                        colEnd = CheckValidValue(colEnd, size);

                        for (int j = rowBegin; j <= rowEnd; j++)
                        {
                            for (int k = colBegin; k <= colEnd; k++)
                            {
                                Vector2 diff = new Vector2(j - dy, k - dx);

                                int level = (int)Math.Floor(diff.magnitude / gaussian_distance);

                                if (level < gaussian_rate)
                                {
                                    //space_usage[j, k]++;
                                    space_usage[j, k] += gaussianValue[level] * trajectoryRecordTime;
                                    //time_usage[j, k] += ( (trueDistance / interpolateTimes) / trajectoryRecordTime) * gaussianValue[level]; 
                                    //time_usage[j, k] += person.Value.velocity_Trajectory[i];
                                    //Debug.LogError(person.Value.name + " td: " + trueDistance);
                                    time_usage[j, k] += (trueDistance / trajectoryRecordTime) * gaussianValue[level]; //speed


                                    count[j, k]++;
                                    //time_usage[j, k] += distance / 0.5f;
                                }
                            }
                        }
                    }
                }
            }
            
            for (int j = 0; j < size; j++)
            {
                for (int k = 0; k < size; k++)
                {
                    //if(count[j, k] != 0) time_usage[j, k] = (time_usage[j, k] / count[j, k]) / (1.0f / 0.0166667f) ;
                    if(count[j, k] != 0) time_usage[j, k] = (time_usage[j, k] / count[j, k]) * 2.25f;
                    //if (count[j, k] != 0) time_usage[j, k] = (time_usage[j, k] / space_usage[j, k]);
                }
            }
            
        }

        //make people disappear
        foreach (KeyValuePair<string, human_single> person in people)
        {
            person.Value.model.SetActive(false);
        }

        //set to Heatmap script
        if (mode == "both" || mode == "move")
        {
            heatmapMode = "static";
            staticMatrix = space_usage;
            heatmapFilename = directory + "moveHeatMap_" + fileIndex.ToString() + ".png";
            Heatmap.SetActive(true);
        }

        if (mode == "both" || mode == "stay")
        {
            staticMatrix = time_usage;
            heatmapFilename = directory + "stayHeatMap_" + fileIndex.ToString() + ".png";
            Heatmap.SetActive(true);
        }
        //write file
        //space usage file
        if (saveTXT)
        {
            FileStream fs = new FileStream(directory + "moveHeatMap.txt", FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    sw.Write(space_usage[i, j] + " ");
                }
                sw.Write("\n");
            }
            sw.Flush();
            sw.Close();
            
            //time usage file
            fs = new FileStream(directory + "stayHeatMap.txt", FileMode.OpenOrCreate);
            sw = new StreamWriter(fs);
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    sw.Write(time_usage[i, j] + " ");
                }
                sw.Write("\n");
            }
            sw.Flush();
            sw.Close();
            
        }

    }



    //exhibitions transform
    void ExhibitionsTransform()
    {
        bool calculateExit = true;

        if (!calculateExit)
        {
            int exhibitionCount = currentSceneSettings.Exhibitions.Count - 1; // -1 remove walkable area
            int[,] exMatrix = new int[exhibitionCount, exhibitionCount];
            for (int i = 0; i < exhibitionCount; i++) for (int j = 0; j < exhibitionCount; j++) exMatrix[i, j] = 0;

            foreach (KeyValuePair<string, human_single> person in people)
            {
                for (int i = 0; i < person.Value.trajectoryOrder.Count - 1; i++)
                {
                    string firstExhibition = person.Value.trajectoryOrder[i];
                    string secondExhibition = person.Value.trajectoryOrder[i + 1];

                    //get exhibitions id
                    if(firstExhibition.StartsWith("p") && secondExhibition.StartsWith("p"))
                    {
                        // -1: id begin from 1 -> matrix begin from 0
                        int firstId = firstExhibition[1] - '0' - 1;
                        int secondId = secondExhibition[1] - '0' - 1;
                        exMatrix[firstId, secondId]++;
                    }
                }
            }

            //write file
            FileStream fs = new FileStream(directory + "ex_trans.txt", FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);
            for (int i = 0; i < exhibitionCount; i++)
            {
                for (int j = 0; j < exhibitionCount; j++)
                {
                    sw.Write(exMatrix[i, j] + " ");
                }
                sw.Write("\n");
            }
            sw.Flush();
            sw.Close();
        }
        else
        {
            int exhibitionCount = currentSceneSettings.Exhibitions.Count - 1; // -1 remove walkable area 
            int[,] exMatrix = new int[exhibitionCount + 2, exhibitionCount + 2]; // +2 exits count
            for (int i = 0; i < exhibitionCount; i++) for (int j = 0; j < exhibitionCount; j++) exMatrix[i, j] = 0;

            foreach (KeyValuePair<string, human_single> person in people)
            {
                string curEx = "", nextEx = "";
                int index = 1;
                curEx = person.Value.trajectoryOrder[0];
                while(index < person.Value.trajectoryOrder.Count)
                {
                    nextEx = person.Value.trajectoryOrder[index];
                    if (nextEx.StartsWith("id"))
                    {
                        index++;
                        continue;
                    }

                    int firstId, secondId;
                    //get exhibitions id
                    if (curEx.StartsWith("p")) firstId = curEx[1] - '0' - 1;
                    else firstId = exhibitionCount + (curEx[4] - '0' - 1);

                    if (nextEx.StartsWith("p")) secondId = nextEx[1] - '0' - 1;
                    else secondId = exhibitionCount + (nextEx[4] - '0' - 1);

                    exMatrix[firstId, secondId]++;
                    curEx = nextEx;
                    index++;
                }
                /*
                for (int i = 0; i < person.Value.trajectoryOrder.Count - 1; i++)
                {
                    string firstExhibition = person.Value.trajectoryOrder[i];
                    string secondExhibition = person.Value.trajectoryOrder[i + 1];
                    
                    print(firstExhibition + " " + secondExhibition);
                    int firstId, secondId;
                    //get exhibitions id
                    if (firstExhibition.StartsWith("p"))
                    {
                        firstId = firstExhibition[1] - '0' - 1;
                    }
                    else
                    {
                        firstId = exhibitionCount + (firstExhibition[4] - '0' - 1);
                    }

                    if (secondExhibition.StartsWith("p"))
                    {
                        secondId = secondExhibition[1] - '0' - 1;
                    }
                    else
                    {
                        secondId = exhibitionCount + (secondExhibition[4] - '0' - 1);
                    }
                    print(firstId + " " + secondId);
                    exMatrix[firstId, secondId]++;
                }
                */
            }

            //write file
            FileStream fs = new FileStream(directory + "ex_trans.txt", FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);
            for (int i = 0; i < exhibitionCount + 2; i++)
            {
                for (int j = 0; j < exhibitionCount + 2; j++)
                {
                    sw.Write(exMatrix[i, j] + " ");
                }
                sw.Write("\n");
            }
            sw.Flush();
            sw.Close();
        }
        Debug.Log("Exhibition Transform complete");
    }

    //exhibition real-time human count
    void RecordExhibitionRealtimeHumanCount()
    {
        //write file
        FileStream fs = new FileStream(directory + "ex_realtime_human_count.txt", FileMode.OpenOrCreate);
        StreamWriter sw = new StreamWriter(fs);
        foreach (exhibition_single exhibit in exhibitions.Values)
        {
            for(int i = 0; i < exhibit.realtimeHumanCount.Count; i++)
            {
                sw.Write(exhibit.realtimeHumanCount[i] + " ");
            }
            sw.Write("\n");
        }
        sw.Flush();
        sw.Close();

        Debug.Log("Exhibition Realtime Human Count complete");
    }
    //social distance
    void CalculateSocialDistance()
    {

        foreach (KeyValuePair<string, human_single> p1 in people)
        {
            if (!p1.Value.model.activeSelf) continue;
            foreach (KeyValuePair<string, human_single> p2 in people)
            {
                if (!p2.Value.model.activeSelf) continue;
                int firstId = p1.Value.name[3] - '0';
                int secondId = p2.Value.name[3] - '0';
                if (firstId >= secondId) continue;

                //Add social distance
                List<double> p1_pos = new List<double>();
                List<double> p2_pos = new List<double>();
                p1_pos = scalePosBackTo2D(p1.Value.currentPosition);
                p2_pos = scalePosBackTo2D(p2.Value.currentPosition);
                float sd = Vector2.Distance(new Vector2((float) p1_pos[0], (float)p1_pos[1]), new Vector2((float)p2_pos[0], (float)p2_pos[1]));
                socialDistance.Add(sd);
            }

        }

    }

    void RecordSocialDistance() 
    {
        //write file
        FileStream fs = new FileStream(directory + "social_distance.txt", FileMode.OpenOrCreate);
        StreamWriter sw = new StreamWriter(fs);
        foreach (float sd in socialDistance)
        {
            sw.Write(sd + "\n");
        }
        sw.Flush();
        sw.Close();
        Debug.Log("Social Distance complete");
    }


    //visiting time
    void RecordVisitingTimeInEachExhibition()
    {
        //write file
        FileStream fs = new FileStream(directory + "visiting_time.txt", FileMode.OpenOrCreate);
        StreamWriter sw = new StreamWriter(fs);
        foreach (KeyValuePair<string, human_single> person in people)
        {
            for(int i = 0; i < person.Value.visitingTimeInEachEx.Length; i++)
            {
                sw.Write(person.Value.visitingTimeInEachEx[i] + " ");
            }
            sw.Write(person.Value.name + " " + person.Value.humanType + "\n");
        }
        sw.Flush();
        sw.Close();
        Debug.Log("Visiting Time complete");
    }

    //status time
    void RecordVisitorStatusTime()
    {
        //write file
        FileStream fs = new FileStream(directory + "status_time.txt", FileMode.OpenOrCreate);
        StreamWriter sw = new StreamWriter(fs);

        foreach (KeyValuePair<string, human_single> person in people)
        {
            sw.Write("go " + person.Value.statusTime[0] + " " + person.Value.name + " " + person.Value.humanType + '\n');
            sw.Write("close " + person.Value.statusTime[1] + " " + person.Value.name + " " + person.Value.humanType + '\n');
            sw.Write("at " + person.Value.statusTime[2] + " " + person.Value.name + " " + person.Value.humanType + '\n');
        }
        sw.Flush();
        sw.Close();
        Debug.Log("Status Time complete");
    }

    // satisfiction figure
    void RecordSatisfiction()
    {
        int[] level = new int[5];
        for (int i = 0; i < level.Length; i++) level[i] = 0;
        foreach (KeyValuePair<string, human_single> person in people)
        {
            int allEx = 0, visEx = 0, satisfictionRate = 0;
            for(int i = 0; i < person.Value.desireExhibitionList_copy.Count; i++)
            {
                string exName = person.Value.desireExhibitionList_copy[i];
                for(int j = 0; j < person.Value.trajectoryOrder.Count; j++)
                {
                    if (exName == person.Value.trajectoryOrder[j])
                    {
                        visEx++;
                        break;
                    }
                }
                allEx++;
            }
            /*
            satisfictionRate = Mathf.FloorToInt(((float)visEx / allEx ) / 0.25f) * 25; //以25為一級距
            if(satisfictionRate < 100) sw.Write(satisfictionRate.ToString() + "% ~ " + (satisfictionRate + 25).ToString() + "%" + '\n');
            else sw.Write("100%" + '\n');
            */
            satisfictionRate = Mathf.FloorToInt(((float)visEx / allEx) / 0.25f);
            print("satisfictionRate: " + satisfictionRate);
            level[satisfictionRate]++;
        }
        //write file
        FileStream fs = new FileStream(directory + "satisfiction.txt", FileMode.OpenOrCreate);
        StreamWriter sw = new StreamWriter(fs);
        for (int i = level.Length - 1; i >= 0; i--)
        {
            sw.Write(level[i] + "\n");
        }
        sw.Flush();
        sw.Close();
        Debug.Log("Satisfiction complete");
    }

    #region Replay Mode Function
    void SaveReplayDataToLocal()
    {
        SimulationReplayData simulationReplayData = new SimulationReplayData();
        simulationReplayData.sceneOption = UIController.instance.curOption;

        //store the information we need
        foreach (KeyValuePair<string, human_single> person in people)
        {
            VisitorReplayData visitorReplayData = new VisitorReplayData();
            visitorReplayData.name = person.Value.name;
            visitorReplayData.age = person.Value.age;
            visitorReplayData.gender = person.Value.gender;
            visitorReplayData.modelName = person.Value.modelName;
            visitorReplayData.replayData = new List<FrameData>();
            visitorReplayData.replayData = person.Value.visitorFrameData;
            simulationReplayData.visitorsReplayData.Add(visitorReplayData);
        }

        //save exhibition info
        exhibitionsInScene exhibitionsInScene = new exhibitionsInScene();
        GameObject scene = GameObject.Find("/[EnvironmentsOfEachScene]/" + UIController.instance.curOption);
        string sceneNum = UIController.instance.curOption.Substring(0, 3);
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
        exhibitionsInScene.sceneName = UIController.instance.curOption;
        exhibitionsInScene.exhibitionsInfo = exhibitionsInfo;
        simulationReplayData.exhibitionsInScene = exhibitionsInScene;

        StringBuilder sb = new StringBuilder();
        JsonWriter writer = new JsonWriter(sb);
        writer.PrettyPrint = true;
        JsonMapper.ToJson(simulationReplayData, writer);
        string outputJsonStr = sb.ToString();
        System.IO.File.WriteAllText(directory + "replay.json", outputJsonStr);

    }
    #endregion

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

    #region dashboard & info pos switch
    public void ChangeToInfoPosPanel()
    {
        dashboard.SetActive(false);
        infoPos.SetActive(true);
    }

    public void ChangeToDashBoardPanel()
    {
        dashboard.SetActive(true);
        infoPos.SetActive(false);
    }
    #endregion
}