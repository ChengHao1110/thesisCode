using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using UnityEngine.Animations.Rigging;

public class FrameData
{
    public bool isVisible;
    public double posX, posY, posZ;
    public double rotX, rotY, rotZ;
    public bool animationWalk;
    public double animationSpeed;
    public double navAgentVelocity;
}
public class ViewPointAttribute
{
    public GameObject viewPoint;
    public float distance;
}
public class human_single// List<human_single> humanCrowd;
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
    public float navSpeed = 0.0f;
    public bool animeWalk = false;
    public float animeSpeed = 1;
    public float oldAnimeSpeed = 1;
    public float lastStoreReplayInfoTime = 0.0f;
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
    public string targetPointName = "";
    public Vector3 nextTarget_pos;
    public Vector3 lookAt_pos; // for exhibit: since where to stand and where to look at are different to a human
    public float freeTime_stayInNextExhibit;
    public float freeTime_stayInNextExhibit_copy;
    public float freeTime_totalLeft;
    public string walkStopState = "walk"; // for simulating the walk and stop behavior
    public float stopStateContinuedTime = 0f;
    public string status = "";
    public bool goToExit = false;
    public System.Random random;
    // save new influence map and update after all computation
    public Dictionary<string, float> influenceMap = new Dictionary<string, float>();
    public Dictionary<string, float> newInfluenceMap = new Dictionary<string, float>();
    public string gatherIndex;
    // wander at exhibit
    public bool wanderAroundExhibit = false;
    public float wanderStayTime = 5f;

    public bool justIn = false;
    public float justInTimer = 0.0f;

    /* time stamp for each person */
    public float lastTimeStamp_stopWalk = 0f;
    public float lastTimeStamp_recomputeMap = 0f;
    public float lastTimeStamp_rePath = -5f;
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

    //replay data
    public string modelName;
    public List<FrameData> visitorFrameData;

    //animation rigging component
    public GameObject head;
    public string lookExhibitionStatus = "None";
    public string lastLookExhibitionName = "None";
    public float rigWeight = 0.0f;
    public float weightChangeSpeed = 0.01f;
    public GameObject viewPoint;
    public List<ViewPointAttribute> viewPoints = new List<ViewPointAttribute>();
    public int viewPointIdx;

    //group color index
    public int groupColorIndex = -1;

    //handle stuck (not used)
    public Vector3 tempDestination;
    public List<string> nearByStuckedVisitors = new List<string>();

    //another method
    public bool isStuck = false;
    public Vector3 lastPos = new Vector3(0, -10, 0);
    public float stuckTimeCounter = 0.0f;
    public int avoidPriority = 50;
    public int id;
    public float lastMoveTime = 0.0f;    

    //wayPoints method
    public NavMeshObstacle obstacle;
    public Queue<Vector3> navPathCorners = new Queue<Vector3>();
    public bool detectCollision = false, needToBePolite = false;
    public List<string> agentWithCollision = new List<string>();
    public Vector3 nextPoint = Vector3.zero;
    public Vector3 currentVelocity = Vector3.zero;
    public Vector3 curMoveDirection = Vector3.zero;
    public bool hasTempDestination = false;
    public bool obstacleToAgent = false;
    public float changeCounter = 0.0f;

    public void CheckWhetherStuck()
    {
        if (status == "at" || walkStopState == "stop") return;
        if (Vector3.Distance(lastPos, model.transform.position) > 0.1f)
        {
            lastMoveTime = dynamicSystem.instance.deltaTimeCounter;
            lastPos = model.transform.position;
            isStuck = false;
        }
        if(lastMoveTime + 0.5f < dynamicSystem.instance.deltaTimeCounter)
        {
            isStuck = true;
            Debug.Log(name + " stuck");
        }
    }

    public void SetDestination(Vector3 targetPoint)
    {
        // enable agent & disable obstacle to pathfinding
        //agent.enabled = true;
        //obstacle.enabled = false;
        

        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(targetPoint, path);
        agent.updatePosition = false;

        navPathCorners.Clear();
        foreach (Vector3 corner in path.corners) navPathCorners.Enqueue(corner);
        GetNextPoint();
        //draw path
        Color c = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Debug.DrawLine(path.corners[i], path.corners[i + 1], c, 10);
        }
        
        // disable agent & enable obstacle to move
        //agent.enabled = false;
        //obstacle.enabled = true;
    }

    public void UpdatePosition()
    {
        if (checkIfArriveTarget()) return;

        if (ArriveAtPoint(nextPoint))
        {
            GetNextPoint();
        }
        // update position to the next point
        // detect collision -> decide whether to be polite
        curMoveDirection = (nextPoint - model.transform.position).normalized;
        model.transform.position += curMoveDirection * agent.speed * Time.fixedDeltaTime;

    }

    public void GetNextPoint()
    {
        if (navPathCorners.Count == 0) return;
        nextPoint = navPathCorners.Dequeue();
    }

    public bool ArriveAtPoint(Vector3 point)
    {
        if (Vector3.Distance(model.transform.position, point) <= 0.5f) return true;
        else return false;
    }

    
    public bool updateTarget(float personNeededTimeToExit, string fromWhichFunction = "")
    {
        string fromFunction = "[ " + fromWhichFunction + " ] ";
        if (goToExit) return false;
        if (isStuck) return false;

        if (freeTime_totalLeft <= personNeededTimeToExit && this.nextTarget_name != this.exitName)
        {
            Debug.Log(fromFunction + "force " + name + " to Exit");
            goToExit = true;
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
                //Debug.Log("change desireList");
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
        //System.Random random = new System.Random(this.randomSeed);
        System.Random random = this.random; // new random change
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
            //System.Random random = new System.Random(this.randomSeed);
            System.Random random = this.random; // new random change
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
            float newStayTime = dynamicSystem.instance.exhibitions[targetName].generateStayTime(maxTimeToStay, this);
            // Debug.Log("maxTimeToStay in " + targetName + ": " + maxTimeToStay + ", get: " + newStayTime);
            return newStayTime;
        }
    }

    void updateDesireList(string chosenTarget)
    {
        /* Update desire list*/
        //System.Random random = new System.Random(this.randomSeed);
        //System.Random random = new System.Random();
        System.Random random = this.random; // new random change
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
                Debug.Log("Add Repeat List");
                /* add to last if it will repeat chosen later */
                this.desireExhibitionList.Add(chosenTarget);
                this.influenceMap.Add(chosenTarget, 1);
                this.newInfluenceMap.Add(chosenTarget, 1);
            }
        }
    }

    string getMostAttractive  ()
    {
        //Debug.Log(name + " influenceMap.Count: " + this.influenceMap.Count);
        if (this.influenceMap.Count > 0)
        {
            int i = 0;
            bool notFound = true;
            while (i < this.influenceMap.Count)
            {
                string selectTarget = this.influenceMap.ElementAt(i).Key;
                //Debug.Log(name + " selectTatrget " + selectTarget);
                if (!selectTarget.StartsWith("p"))
                {
                    return selectTarget; // person or exit
                }

                if (targetPointName.Contains(selectTarget)) return selectTarget;

                int total = dynamicSystem.instance.exhibitions[selectTarget].bestViewDirection_vector3.Count, count = 0;
                foreach (KeyValuePair<string, bool> tp in dynamicSystem.instance.isTargetPointUse)
                {
                    if (tp.Key.Contains(selectTarget) && tp.Value)
                    {
                        //Debug.Log("this one has people");
                        count++;
                    }
                }
                if (count == total)
                {
                    //Debug.Log(name + " choose " + selectTarget + " is full");
                    i++;
                    continue;
                }
                else
                {
                    notFound = false;
                    return selectTarget;
                }
            }
            // all full condition
            if (notFound)
            {
                Debug.Log(name + " no found");
                string selectTarget = this.influenceMap.ElementAt(0).Key;
                return selectTarget;
            }
        }
        return "";  // nothing can return
        
        /*
        foreach(KeyValuePair<string, float> imp in this.influenceMap)
        {
            string selectTarget = imp.Key;
            foreach (KeyValuePair<string, bool> vp in dynamicSystem.instance.isTargetPointUse)
            {
                if (vp.Key.Contains(selectTarget))
                {
                    if (!dynamicSystem.instance.isTargetPointUse[vp.Key])
                    {
                        return selectTarget;
                    }
                }
            }
        }
        return "";
        */
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
        if (Vector3.Distance(this.currentPosition, this.nextTarget_pos) < 0.35f) // close enough means arrive
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
        if (desireExhibitionList.Count == 0)
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
            agent.avoidancePriority = avoidPriority;
            //obstacle.enabled = false;
            //agent.enabled = true;
            ifMoveNavMeshAgent(true);
        }
        else if (status == "close")
        {
            agent.radius = (float)dynamicSystem.instance.currentSceneSettings.customUI.walkStage["Close"].radius;
            agent.speed = speedBase * (float)dynamicSystem.instance.currentSceneSettings.customUI.walkStage["Close"].speed;
            agent.acceleration = accelerateBase * (float)dynamicSystem.instance.currentSceneSettings.customUI.walkStage["Close"].speed;
            colliderShape.transform.Find("Cylinder").GetComponent<MeshRenderer>().material.color = Color.yellow;
            agent.avoidancePriority = 30 + id;
        }
        else // status == at
        {
            agent.radius = (float)dynamicSystem.instance.currentSceneSettings.customUI.walkStage["At"].radius;
            agent.speed = speedBase * (float)dynamicSystem.instance.currentSceneSettings.customUI.walkStage["At"].speed;
            agent.acceleration = accelerateBase * (float)dynamicSystem.instance.currentSceneSettings.customUI.walkStage["At"].speed;
            colliderShape.transform.Find("Cylinder").GetComponent<MeshRenderer>().material.color = Color.red;
            agent.avoidancePriority = 0;
            //obstacle.enabled = true;
            //agent.enabled = false;
            ifMoveNavMeshAgent(false);
        }
        navSpeed = agent.speed;
        /* set collider Range */
        float radiusTimes2 = agent.radius * 2f;
        colliderShape.transform.localScale = new Vector3(radiusTimes2, 1, radiusTimes2);
    }

    public void ifMoveNavMeshAgent(bool isMove)
    {
        if (this.model.activeSelf)
        {
            
            if(obstacle.enabled && isMove)
            {
                obstacle.enabled = false;
                agent.enabled = isMove;
                obstacleToAgent = true;
            }
            else
            {
                agent.enabled = isMove;
            }

            if (agent.enabled && !obstacleToAgent)
            {
                agent.updatePosition = isMove;
                agent.isStopped = !isMove;
            }
            obstacle.enabled = !isMove;
            
            /*
            if (obstacle.enabled)
            {
                obstacle.carving = true;
            }
            else
            {
                obstacle.carving = false;
            }
            */
            // agent.updateRotation = isMove;
            // agent.velocity = Vector3.zero;

            //if (!isMove) agent.avoidancePriority = 45; // those stop have a higher priority
            //else agent.avoidancePriority = 50;
        }
        animeWalk = isMove;
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
        changeText += "next target: " + this.nextTarget_name + "(" + this.nextTarget_direction + ")\n";
        changeText += "stay: " + this.freeTime_stayInNextExhibit.ToString("F0");

        this.informationText.text = this.fixedText + changeText;

        /* make the board move to current position and face to camera*/
        this.informationBoard.transform.position = new Vector3(this.currentPosition.x, 3.3f, this.currentPosition.z);
        if (dynamicSystem.instance.quickSimulationMode) return;
        this.informationBoard.transform.LookAt(Camera.main.transform.position);
    }

    public void SaveReplayFrameData()
    {
        FrameData fd = new FrameData();
        fd.isVisible = model.activeSelf;
        if (fd.isVisible)
        {
            fd.posX = (double)model.transform.position.x;
            fd.posY = (double)model.transform.position.y;
            fd.posZ = (double)model.transform.position.z;
            fd.rotX = (double)model.transform.rotation.eulerAngles.x;
            fd.rotY = (double)model.transform.rotation.eulerAngles.y;
            fd.rotZ = (double)model.transform.rotation.eulerAngles.z;
        }
        else
        {
            fd.posX = 0;
            fd.posY = 0;
            fd.posZ = 0;
            fd.rotX = 0;
            fd.rotY = 0;
            fd.rotZ = 0;
        }
        fd.animationWalk = animeWalk;
        fd.animationSpeed = oldAnimeSpeed;
        fd.navAgentVelocity = (double)agent.velocity.magnitude;
        visitorFrameData.Add(fd);
    }

    public void StartLookingAnimation()
    {
        //Debug.Log("StartLookingAnimation");
        rigWeight += weightChangeSpeed;
        if (rigWeight >= 1.0f)
        {
            rigWeight = 1.0f;
            lookExhibitionStatus = "Watching";
            //Debug.Log(name + " Status: Start -> Watching");
        }
        MultiAimConstraint mac = model.GetComponentInChildren<MultiAimConstraint>();
        var data = mac.data.sourceObjects;
        data.Clear();
        data.Add(new WeightedTransform(viewPoint.transform, rigWeight));
        mac.data.sourceObjects = data;
        Rig rig = model.GetComponentInChildren<Rig>();
        rig.weight = rigWeight;

        RigBuilder rb = model.GetComponent<RigBuilder>();
        rb.Build(); 
    }

    public void ViewPointMoving()
    {
        //Debug.Log("ViewPointMoving");
        int nextViewPointIdx = viewPointIdx + 1;
        if (nextViewPointIdx == viewPoints.Count()) nextViewPointIdx = 0;

        Vector3 currentViewPos = viewPoint.transform.position;
        Vector3 targetViewPos = viewPoints[nextViewPointIdx].viewPoint.transform.position;

        viewPoint.transform.position = Vector3.MoveTowards(currentViewPos, targetViewPos, 0.005f);
        if(Vector3.Distance(viewPoint.transform.position, targetViewPos) < 0.001f)
        {
            viewPointIdx++;
            if (viewPointIdx == viewPoints.Count()) viewPointIdx = 0;
        }
    }

    public void FinishLookingAnimation()
    {
        //Debug.Log("FinishLookingAnimation");
        if (rigWeight > 0)
        {
            //Debug.Log(name + " " + rigWeight);
            rigWeight -= weightChangeSpeed * 10;
            if (rigWeight <= 0)
            {
                rigWeight = 0.0f;
                MultiAimConstraint mac = model.GetComponentInChildren<MultiAimConstraint>();
                var data = mac.data.sourceObjects;
                data.Clear();
                mac.data.sourceObjects = data;
                lookExhibitionStatus = "None";
                lastLookExhibitionName = nextTarget_name;
                nearExhibition("goTo");
                ifMoveNavMeshAgent(true);
                //Debug.Log(name + " Status: End -> None");
            }
            else
            {
                MultiAimConstraint mac = model.GetComponentInChildren<MultiAimConstraint>();
                var data = mac.data.sourceObjects;
                data.Clear();
                data.Add(new WeightedTransform(viewPoint.transform, 1));
                mac.data.sourceObjects = data;
                Rig rig = model.GetComponentInChildren<Rig>();
                rig.weight = rigWeight;
                ifMoveNavMeshAgent(false);
            }
            RigBuilder rb = model.GetComponent<RigBuilder>();
            //rb.layers.Clear();
            rb.Build();
        }
    }

    public void animationStatusMachine()
    {
        switch (lookExhibitionStatus) 
        {
            case "Start":
                StartLookingAnimation();
                break;
            case "Watching":
                ViewPointMoving();
                break;
            case "End":
                FinishLookingAnimation();
                break;
            case "None":
                break;
        }

    }
    /*
    public Vector3 CalculateStrangeForce(human_single otherAgent)
    {
        Vector3 strangeForce = Vector3.zero;
        Vector3 otherAgentPos = otherAgent.model.transform.position;
        Vector3 otherAgentToThisAgent = otherAgentPos - this.model.transform.position;
        if (otherAgentToThisAgent.magnitude <= 3.0f)
        {
            Vector3 otherAgentMoveDirection = (otherAgent.agent.steeringTarget - otherAgentPos).normalized;
            Vector3 thisAgentMoveDirection = (this.agent.steeringTarget - this.model.transform.position).normalized;
            Vector3 otherAgentVelocity = otherAgent.agent.velocity;
            Vector3 grad_ij = -1 * Gradient_v_ab(otherAgentToThisAgent, otherAgentMoveDirection, otherAgentVelocity.magnitude);
            //compute weight
            float c = 0.5f;
            float w = Vector3.Dot(thisAgentMoveDirection, -1 * grad_ij);
            float angle = 100;
            if (w >= (grad_ij.magnitude) * Mathf.Cos(angle * 2.0f * Mathf.PI / 360.0f))
            {
                c = 1.0f;
            }
            //add strange force to p[i]
            strangeForce = c * grad_ij;
        }
        return strangeForce;
    }

    public Vector3 Gradient_v_ab(Vector3 r_ab, Vector3 b_dir, float speed_b)
    {
        float delta = 0.001f;
        float v, dxdv, dzdv;
        Vector3 dx, dz, grad;
        v = Value_v_ab(r_ab, b_dir, speed_b);
        dx = new Vector3(delta, 0, 0);
        dz = new Vector3(0, 0, delta);
        dxdv = (Value_v_ab(r_ab + dx, b_dir, speed_b) - v) / delta;
        dzdv = (Value_v_ab(r_ab + dz, b_dir, speed_b) - v) / delta;
        grad = new Vector3(dxdv, 0, dzdv);
        return grad;
    }

    public float Value_v_ab(Vector3 r_ab, Vector3 b_dir, float speed_b)
    {
        float b, in_sqrt, v_ab;
        //compute b
        float delta_time = 2.0f;
        Vector3 diff = r_ab - speed_b * delta_time * b_dir;
        in_sqrt = Mathf.Pow(r_ab.magnitude + diff.magnitude, 2) - Mathf.Pow(speed_b * delta_time, 2);

        if (in_sqrt < 0) in_sqrt = 0;

        b = Mathf.Sqrt(in_sqrt) / 2.0f;
        //compute v_ab
        float v0 = 2.1f;
        float a = 0.3f;
        v_ab = v0 * Mathf.Exp((-1 * b) / a);  //v0 * e ^ (-b/a)
        return v_ab;
    }
    */
}

public class human_gather
{
    public List<string> humans = new List<string>();
    public int leaderIndex; // or string?
    public Color markColor;
}
