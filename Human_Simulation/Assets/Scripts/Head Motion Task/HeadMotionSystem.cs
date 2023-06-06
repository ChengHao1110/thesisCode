using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class HeadMotionSystem : MonoBehaviour
{
    public List<GameObject> visitorList = new List<GameObject>();
    public bool isHeadMotionOn = true;
    public int sceneCount = 1, startSceneIdx = 1, cameraIdx = 1;
    public List<List<GameObject>> sceneCameraList = new List<List<GameObject>>();
    public float timeCounter = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        
        System.Random _rand = new System.Random();
        for (int i = 1; i < sceneCount + 1; i++)
        {
            GameObject scene = GameObject.Find("scene" + i.ToString());
            if (scene != null)
            {
                //get camera
                Transform cameraParent = scene.transform.Find("Camera");
                List<GameObject> cameraList = new List<GameObject>();
                foreach(Transform child in cameraParent)
                {
                    cameraList.Add(child.gameObject);
                }
                sceneCameraList.Add(cameraList);

                GameObject exhibit = scene.transform.Find("Exhibit").GetChild(0).gameObject;
                Transform visitors = scene.transform.Find("Visitors");
                //add visitor to visitor list
                foreach (Transform child in visitors)
                {
                    GameObject vis = child.gameObject;
                    //add animation clip 
                    /*
                    Animator animator = vis.GetComponent<Animator>();
                    animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("AnimationClips/ManPose");
                    */

                    Animator animator = vis.GetComponent<Animator>();
                    animator.speed = 0.3f;

                    if (!isHeadMotionOn) continue;
                    visitorHeadMotionClass vHMC =  vis.AddComponent<visitorHeadMotionClass>();
                    AddRig(vis);

                    // setting visitorHeadMotionClass
                    vHMC.viewPointIdx = 0;
                    // get all view points
                    vHMC.viewPoints.Clear();
                    Transform viewPointsInExhibition = exhibit.transform.Find("ViewPoint");
                    foreach (Transform viewPoint in viewPointsInExhibition)
                    {
                        //calculate distance
                        float dist = Vector3.Distance(vHMC.head.transform.position, viewPoint.position);
                        ViewPointAttribute viewPointAttribte = new ViewPointAttribute();
                        viewPointAttribte.viewPoint = viewPoint.gameObject;
                        viewPointAttribte.distance = dist;
                        vHMC.viewPoints.Add(viewPointAttribte);
                    }
                    // sort
                    /*
                    vHMC.viewPoints.Sort(delegate (ViewPointAttribute v1, ViewPointAttribute v2) {
                        return v1.distance.CompareTo(v2.distance);
                    });
                    */
                    //random
                    for (int j = vHMC.viewPoints.Count - 1; j > 0; j--)
                    {
                        var k = _rand.Next(j + 1);
                        var value = vHMC.viewPoints[k];
                        vHMC.viewPoints[k] = vHMC.viewPoints[j];
                        vHMC.viewPoints[j] = value;
                    }

                    // set first point
                    vHMC.viewPoint = new GameObject("viewPoint");
                    vHMC.viewPoint.transform.SetParent(vis.transform);
                    vHMC.viewPoint.transform.position = vHMC.viewPoints[0].viewPoint.transform.position;

                    // set rig
                    MultiAimConstraint mac = vis.GetComponentInChildren<MultiAimConstraint>();
                    var data = mac.data.sourceObjects;
                    data.Clear();
                    data.Add(new WeightedTransform(vHMC.viewPoint.transform, 1));
                    mac.data.sourceObjects = data;
                    Rig rig = vis.GetComponentInChildren<Rig>();
                    rig.weight = 1;
                    RigBuilder rb = vis.GetComponent<RigBuilder>();
                    rb.Build();

                    visitorList.Add(vis);
                }
            }
        }

        //set camera
        for(int i = 0; i < sceneCameraList.Count; i++)
        {
            for(int j = 0; j < sceneCameraList[i].Count; j++)
            {
                sceneCameraList[i][j].SetActive(false);
            }
        }
        sceneCameraList[startSceneIdx - 1][0].SetActive(true);
        cameraIdx = 1;
    }

    // Update is called once per frame
    void Update()
    {
        timeCounter += Time.deltaTime;
        if(timeCounter >= 10.0f)
        {
            //change Camera
            int oldCameraIdx = cameraIdx;
            cameraIdx++;
            if (cameraIdx > sceneCameraList[startSceneIdx - 1].Count) cameraIdx = 1;
            sceneCameraList[startSceneIdx - 1][oldCameraIdx - 1].SetActive(false);
            sceneCameraList[startSceneIdx - 1][cameraIdx - 1].SetActive(true);
            timeCounter = 0.0f;
        }
        if (isHeadMotionOn) {
            foreach (GameObject vis in visitorList)
            {
                visitorHeadMotionClass vHMC = vis.GetComponent<visitorHeadMotionClass>();
                vHMC.ViewPointMoving();
            }
        }

        if (Input.GetKeyDown("right"))
        {
            int oldSceneIdx = startSceneIdx;
            startSceneIdx++;
            if(startSceneIdx > sceneCount)  startSceneIdx = 1;
            // close old camera
            for (int j = 0; j < sceneCameraList[oldSceneIdx-1].Count; j++)
            {
                sceneCameraList[oldSceneIdx - 1][j].SetActive(false);
            }
            // open new camera
            sceneCameraList[startSceneIdx - 1][0].SetActive(true);
            cameraIdx = 1;
        }
        if (Input.GetKeyDown("left"))
        {
            int oldSceneIdx = startSceneIdx;
            startSceneIdx--;
            if (startSceneIdx < 1) startSceneIdx = sceneCount;
            // close old camera
            for (int j = 0; j < sceneCameraList[oldSceneIdx-1].Count; j++)
            {
                sceneCameraList[oldSceneIdx - 1][j].SetActive(false);
            }
            // open new camera
            sceneCameraList[startSceneIdx - 1][0].SetActive(true);
            cameraIdx = 1;
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            int oldCameraIdx = cameraIdx;
            cameraIdx++;
            if (cameraIdx > sceneCameraList[startSceneIdx - 1].Count) cameraIdx = 1;
            sceneCameraList[startSceneIdx - 1][oldCameraIdx-1].SetActive(false);
            sceneCameraList[startSceneIdx - 1][cameraIdx - 1].SetActive(true);
        }
    }

    void AddRig(GameObject visitor)
    {
        //add viewPoint to visitor
        GameObject viewPoints = new GameObject("ViewPoints");
        viewPoints.transform.SetParent(visitor.transform);

        //rig
        visitor.AddComponent<RigBuilder>();
        GameObject rig = new GameObject("Rig");
        rig.AddComponent<Rig>();
        rig.transform.SetParent(visitor.transform);
        GameObject headAim = new GameObject("Aim");
        headAim.AddComponent<MultiAimConstraint>();
        headAim.transform.SetParent(rig.transform);
        MultiAimConstraint mac = headAim.GetComponent<MultiAimConstraint>();
        Queue<Transform> allChildren = new Queue<Transform>();
        allChildren.Enqueue(visitor.transform);
        visitorHeadMotionClass vHMC = visitor.GetComponent<visitorHeadMotionClass>();
        while (allChildren.Count != 0)
        {
            Transform parent = allChildren.Dequeue();
            foreach (Transform child in parent)
            {
                if (child.name.Contains("Head"))
                {
                    vHMC.head = child.transform.gameObject;
                    while (allChildren.Count != 0) allChildren.Dequeue();
                    break;
                }
                else allChildren.Enqueue(child);
            }
        }

        mac.data = new MultiAimConstraintData
        {
            constrainedObject = vHMC.head.transform,
            aimAxis = MultiAimConstraintData.Axis.Y,
            constrainedXAxis = true,
            constrainedYAxis = true,
            constrainedZAxis = true,
            limits = new Vector2(-180.0f, 180.0f)
        };

        RigBuilder rb = visitor.GetComponent<RigBuilder>();
        RigLayer rl = new RigLayer(rig.GetComponent<Rig>());
        rb.layers.Add(rl);
        rb.Build();

    } 
}
