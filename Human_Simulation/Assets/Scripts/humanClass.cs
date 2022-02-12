using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class human_single  // List<human_single> humanCrowd;
{
    /* initialize */
    public string name;
    public GameObject model;
    public NavMeshAgent agent;
    public GameObject informationBoard, marker, gatherMarker, colliderShape;
    public TextMesh informationText;
    public string fixedText;
    public int age;  // be custered into few category
    public int gender;  // female: 0, male: 1
    public string humanType;
    public float freeTime_total;
    public List<string> desireExhibitionList;  // key of every desire exhibition
    public List<string> desireExhibitionList_copy;
    // public List<string> desireExhibitionList_left;
    public List<string> trajectoryOrder = new List<string>();
    public List<List<double>> fullTrajectory = new List<List<double>>();

    //record the velocity
    public List<float> velocity_Trajectory = new List<float>();

    public List<gatheringEvent> gatherEvent = new List<gatheringEvent>();
    public string exitName;
    public float walkSpeed;
    public float animeSpeed = 1;
    public float gatherDesire;
    public float startSimulateTime = 0;
    // personality

    /* update */
    public Vector3 currentPosition;
    public NavMeshPath currentPath = new NavMeshPath();
    public int cornerId;
    public bool finishAllExhibit = false;
    // target can be other person or exhibition
    public string preTarget_name;
    public Vector3 preTarget_pos;
    public string nextTarget_name;
    public int nextTarget_direction;
    public Vector3 nextTarget_pos;
    public Vector3 lookAt_pos; // for exhibit: since where to stand and where to look at are different to a human
    public float freeTime_stayInNextExhibit;
    public float freeTime_stayInNextExhibit_copy;
    public float freeTime_totalLeft;
    public string walkStopState = "walk"; // for simulating the walk and stop behavior
    public string status = "";
    // save new influence map and update after all computation
    public Dictionary<string, float> influenceMap = new Dictionary<string, float>();
    public Dictionary<string, float> newInfluenceMap = new Dictionary<string, float>();
    public string gatherIndex;
    // wander at exhibit
    public bool wanderAroundExhibit = false;
    public float wanderStayTime = 5f;

    /* time stamp for each person */
    public float lastTimeStamp_stopWalk = 0f;
    public float lastTimeStamp_recomputeMap = 0f;
    public float lastTimeStamp_rePath = 0f;
    public float lastTimeStamp_recomputeGathers = 0f;
    public float lastTimeStamp_rotate = -1f;
    public float lastTimeStamp_storeTrajectory = -1f;

    /*record for exhibition*/
    public float[] visitingTimeInEachEx;

    /*record status time*/
    //0: goto, 1: wander, 2:at
    public float[] statusTime;

    /*random seed, ensure*/
    public int randomSeed;

    public bool updateTarget(float personNeededTimeToExit, string fromWhichFunction="")
    {
        string fromFunction = "[ " + fromWhichFunction + " ] ";
        if (freeTime_totalLeft <= personNeededTimeToExit && this.nextTarget_name != this.exitName)
        {            
            // Debug.Log(fromFunction + "force " + name + " to Exit");
            dynamicSystem.instance.computeNewInfluenceMap(this);
            updateInfluenceMap();
            setTargetAndStayTime(this.exitName);
            return true;
        }
        else if (freeTime_totalLeft <= personNeededTimeToExit && this.nextTarget_name == this.exitName)
        {
            return false; // just keep going to exit
        }

        /* Get the most attractive object from influence map */
        string chosenTarget = this.getMostAttractive();
        bool changeTarget = false;
        /* Deal with diffent cases after influence map change */
        if (this.preTarget_name == "init") /* only assign nextTarget */
        {
            setTargetAndStayTime(chosenTarget);
            this.preTarget_name = "init_"; // change a little bit to avoid come here again
            changeTarget = true; // target change
        }

        /* not yet arrive target but attract by others */
        else if (!checkIfArriveTarget() && (this.nextTarget_name != chosenTarget)) 
        {
            // Debug.Log("suddenly change target to " + chosenTarget);
            /* 
             * when influence map update, something more attractive may appear 
             * -> save original nextTarget back to desireList if it is a exhibition
             * -> update nextTarget to handle the situation
             */
            setTargetAndStayTime(chosenTarget);
            // string debugstr = string.Join(", ", influenceMap);
            // debugstr += "\n" + " totalLeft: " + freeTime_totalLeft + ", needExit: " + personNeededTimeToExit;
            // Debug.Log(fromFunction + "suddenly " + this.name + " : " + this.preTarget_name + "->" + this.nextTarget_name + "\n" + debugstr);
            changeTarget = true; 
        }

        /* arrive target and stay long enough: simply find next desire exhibition */
        else if (checkIfArriveTarget() && this.freeTime_stayInNextExhibit <= 0)  
        {
            /* Arrive, update DesireList */
            if (this.nextTarget_name.StartsWith("p"))
            {
                updateDesireList(this.nextTarget_name); 
            }            

            /* use influence map to get new target*/
            string chosenTarget2 = getMostAttractive();
            if (chosenTarget2 == "")  // end
            {
                changeTarget = false;
            }
            else
            {
                setTargetAndStayTime(chosenTarget2);
                // Debug.Log(this.name + " : " + this.preTarget_name + "->" + this.nextTarget_name);

                changeTarget = true; // target change
            }            
        }

        /* just not arrive yet*/
        else
        {
            changeTarget = false;
        }

        /* if is a human, have to update target pos when they move */
        if (this.nextTarget_name.StartsWith("id"))
        {
            changeTarget = true;
        }

        // adjustPriority(personNeededTimeToExit);

        return changeTarget;
    }

    public float generateWanderStayTime()
    {
        System.Random random = new System.Random(this.randomSeed);
        float outputTime = 0;
        if (this.freeTime_stayInNextExhibit < 5) outputTime = 5;
        else outputTime = random.Next(5, (int)this.freeTime_stayInNextExhibit);
        return outputTime;
    }

    void setTargetAndStayTime(string chosenTarget)
    {
        this.preTarget_name = this.nextTarget_name;
        this.preTarget_pos = this.currentPosition;  /* start from current position */
        this.nextTarget_name = chosenTarget;
        this.nextTarget_pos = dynamicSystem.instance.findPosByObjName(this.nextTarget_name, this);
        this.freeTime_stayInNextExhibit = this.generateStayTime(this.nextTarget_name);
        this.freeTime_stayInNextExhibit_copy = this.freeTime_stayInNextExhibit;

        /* if is a exhibit, update wander time */
        if (this.nextTarget_name.StartsWith("p"))
        {
            this.wanderStayTime = generateWanderStayTime();
        }        
    }

    void adjustPriority(float personNeededTimeToExit)
    {       
        agent.avoidancePriority = 50;
        if (dynamicSystem.instance.exhibitions.Keys.Contains(preTarget_name))
        {
            if (dynamicSystem.instance.exhibitions[preTarget_name].currentHumanInside.Contains(name))
            {
                agent.avoidancePriority = 30;
            }
        }    
        
        if (status == "at") agent.avoidancePriority = 30;

        if (walkStopState == "stop") agent.avoidancePriority = 40;        

        if (nextTarget_name == exitName)
        {
            if (freeTime_totalLeft <= personNeededTimeToExit) agent.avoidancePriority = 0;
        }
    }
    
    float generateStayTime(string targetName)
    {
        // Debug.Log("generate target stay time: " + targetName);
        if (targetName.StartsWith("id")) // a person
        {
            System.Random random = new System.Random(this.randomSeed);
            return random.Next(1, 6);
            //return dynamicSystem.instance.random.Next(1, 6); // stay 1~6 second, 亂給的
        }
        else if (targetName.StartsWith("exit"))
        {
            return 1;
        }
        else // an exhibit
        {        
            float speed = agent.speed;

            float distance = dynamicSystem.instance.calculateDistance(this.currentPosition, this.nextTarget_pos);
            float arrivalTakingTime = distance / speed;

            float distance_toExit = dynamicSystem.instance.calculateDistance(nextTarget_pos, dynamicSystem.instance.exits[this.exitName].centerPosition);
            float arrivalExitTakeTime = distance_toExit / speed;

            // should use distribution to generate
            float maxTimeToStay = freeTime_totalLeft - arrivalTakingTime - arrivalExitTakeTime;
            maxTimeToStay = Mathf.Clamp(maxTimeToStay, (float)dynamicSystem.instance.exhibitions[targetName].stayTimeSetting.min + 10, (float)dynamicSystem.instance.exhibitions[targetName].stayTimeSetting.max);
            float newStayTime = dynamicSystem.instance.exhibitions[targetName].generateStayTime(maxTimeToStay);
            // Debug.Log("maxTimeToStay in " + targetName + ": " + maxTimeToStay + ", get: " + newStayTime);
            return newStayTime;
        }        
    }

    void updateDesireList(string chosenTarget)
    {
        /* Update desire list*/
        System.Random random = new System.Random(this.randomSeed);
        float randomNum = (float)random.Next(101);
        //float randomNum = dynamicSystem.instance.random.Next(101);
        randomNum /= 100f;
        this.desireExhibitionList.Remove(chosenTarget);        
        this.influenceMap.Remove(chosenTarget); /* update influenceMap too, so getMostAttractive will not get repeat exhibit*/
        this.newInfluenceMap.Remove(chosenTarget);
        // Debug.Log("update desire: " + chosenTarget);
        if (chosenTarget.StartsWith("p"))
        {
            if (randomNum < dynamicSystem.instance.currentSceneSettings.Exhibitions[chosenTarget].repeatChosenProbabilty)
            {
                /* add to last if it will repeat chosen later */
                this.desireExhibitionList.Add(chosenTarget);
                this.influenceMap.Add(chosenTarget, 1);
                this.newInfluenceMap.Add(chosenTarget, 1);
            }
        }        
    }

    string getMostAttractive()
    {
        if (this.influenceMap.Count > 0)
        {
            string selectTarget = this.influenceMap.First().Key;
            return selectTarget;
        }
        return "";  // nothing can return
    }

    public void updateInfluenceMap()
    {
        if (this.influenceMap.Count > 0)
        {
            this.influenceMap.Clear();
        }
        this.influenceMap = this.newInfluenceMap.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    public bool checkIfArriveTarget()
    {
        if (Vector3.Distance(this.currentPosition, this.nextTarget_pos) < 0.5f) // close enough means arrive
        {
            addTrajectory();
            return true;
        }
        else return false;
    }

    void addTrajectory()
    {
        if (this.trajectoryOrder.Count == 0)
        {
            this.trajectoryOrder.Add(nextTarget_name);
        }
        else if (this.trajectoryOrder[trajectoryOrder.Count - 1] != nextTarget_name)
        {
            this.trajectoryOrder.Add(nextTarget_name);
        }
    } // for output result on thesis
    
    bool checkIfVisitAllInDesireList()
    {
        if(desireExhibitionList.Count == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool checkIfFinishVisit()
    {
        if (this.checkIfVisitAllInDesireList() && this.checkIfArriveTarget())
        {
            // desireExhibitionList_left = new List<string>();
            finishAllExhibit = true;
        }
        else if (this.nextTarget_name.StartsWith("exit") && this.checkIfArriveTarget())
        {
            // desireExhibitionList_left = new List<string>(desireExhibitionList);
            // desireExhibitionList_left.Remove(exitName);
            // Debug.Log(this.currentPosition + ", " + this.nextTarget_pos);
            desireExhibitionList.Clear();
            finishAllExhibit = true;
        }
        else
        {
            finishAllExhibit = false;
        }
        return this.finishAllExhibit;
    }
    
    public void nearExhibition(string status)
    {
        this.status = status;
        /* Status: 
         * goTo: on the way to a target, 
         * close: close to the target, 
         * at: at the best View Direction*/
        float speedBase = 3.5f * 0.5f * (walkSpeed / 100f);
        float accelerateBase = 8f * 0.5f * (walkSpeed / 100f);
        if (status == "goTo")
        {
            agent.radius = (float)dynamicSystem.instance.currentSceneSettings.customUI.walkStage["GoTo"].radius;
            agent.speed = speedBase * (float)dynamicSystem.instance.currentSceneSettings.customUI.walkStage["GoTo"].speed;
            agent.acceleration = accelerateBase * (float)dynamicSystem.instance.currentSceneSettings.customUI.walkStage["GoTo"].speed;
            colliderShape.transform.Find("Cylinder").GetComponent<MeshRenderer>().material.color = Color.green;
        }
        else if (status == "close")
        {
            agent.radius = (float)dynamicSystem.instance.currentSceneSettings.customUI.walkStage["Close"].radius;
            agent.speed = speedBase * (float)dynamicSystem.instance.currentSceneSettings.customUI.walkStage["Close"].speed;
            agent.acceleration = accelerateBase * (float)dynamicSystem.instance.currentSceneSettings.customUI.walkStage["Close"].speed;
            colliderShape.transform.Find("Cylinder").GetComponent<MeshRenderer>().material.color = Color.yellow;
        }
        else // status == at
        {
            agent.radius = (float)dynamicSystem.instance.currentSceneSettings.customUI.walkStage["At"].radius;
            agent.speed = speedBase * (float)dynamicSystem.instance.currentSceneSettings.customUI.walkStage["At"].speed;
            agent.acceleration = accelerateBase * (float)dynamicSystem.instance.currentSceneSettings.customUI.walkStage["At"].speed;
            colliderShape.transform.Find("Cylinder").GetComponent<MeshRenderer>().material.color = Color.red;
        }

        /* set collider Range */
        float radiusTimes2 = agent.radius * 2;
        colliderShape.transform.localScale = new Vector3(radiusTimes2, 1, radiusTimes2);       
    }       

    public void ifMoveNavMeshAgent(bool isMove)
    {        
        if (this.model.activeSelf)
        {
            agent.updatePosition = isMove;
            // agent.updateRotation = isMove;
            // agent.velocity = Vector3.zero;
            agent.isStopped = !isMove;
            if (!isMove) agent.avoidancePriority = 45; // those stop have a higher priority
            else agent.avoidancePriority = 50;
        }
        this.model.GetComponent<Animator>().SetBool("walk", isMove);
    }

    public void modelVisible(bool isVis)
    {
        this.model.SetActive(isVis);
        this.informationBoard.SetActive(isVis && dynamicSystem.instance.showInfoBoard_human);

        // if is in mainHuman gather or is the mainHuman, can open marker
        bool inMainHumanGather = true; // when mainHumanName == ""
        if (influenceMapVisualize.instance.mainHumanName != "")
        {
            inMainHumanGather = dynamicSystem.instance.peopleGathers[influenceMapVisualize.instance.mainHuman.gatherIndex].humans.Contains(this.name);
            inMainHumanGather = inMainHumanGather || influenceMapVisualize.instance.mainHumanName == this.name;
        }            
        this.marker.SetActive(isVis && inMainHumanGather);
    }

    /* for debug and check */
    public void updateInformationBoard()  
    {
        /* update text on model */
        string changeText = "";
        changeText += "free time: \n" + this.freeTime_totalLeft.ToString("F2") + " / " + this.freeTime_total.ToString("F2") + "\n";
        changeText += "desireList: " + (this.desireExhibitionList.Count - 1) + "\n"; // minus the exit
        changeText += "next target: " + this.nextTarget_name + "(" + this.nextTarget_direction +")\n";
        changeText += "stay: " + this.freeTime_stayInNextExhibit.ToString("F0");

        this.informationText.text = this.fixedText + changeText;

        /* make the board move to current position and face to camera*/
        this.informationBoard.transform.position = new Vector3(this.currentPosition.x, 3.3f, this.currentPosition.z);
        this.informationBoard.transform.LookAt(Camera.main.transform.position);
    }
}

public class human_gather
{
    public List<string> humans = new List<string>();
    public int leaderIndex; // or string?
    public Color markColor;
}
