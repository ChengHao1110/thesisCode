using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using LitJson;

public class frameData
{
    // public int frameNum;
    public float x;
    public float y;
    public Vector2 direction;
}

public class realDataDrawTest : PersistentSingleton<realDataDrawTest>
{
    public GameObject peopleParent;
    public GameObject signPrefab;
    public Text timeText, videoNameText, prepareVideoText, fpsAndFrameText;
    public RawImage videoPlayer_rawImage;
    public Slider playTimeSlider;
    UnityEngine.Video.VideoPlayer videoPlayer;

    static string videoName = "119_1 01_50-02_15";
    string jsonDataDir = videoName + "_fishEyeCorrect_id"; //"119_12 03_00-03_50_water";
    List<string> jsonDataStr = new List<string>();
    Dictionary<string, Dictionary<int, frameData>> trajectoryDatas = new Dictionary<string, Dictionary<int, frameData>>();
    Dictionary<string, int> prefabId = new Dictionary<string, int>();
    int humanCount;
    float deltaTimeCounter = 0f;
    int fps = 1;
    int preFrame, nextFrame;
    int maxFrameNum = 0;
    // float scaleUnit = 90;
    public float halfW = 960, halfH = 540;

    // simulation used
    bool Run = false;
    Dictionary<string, GameObject> models = new Dictionary<string, GameObject>();
    Dictionary<string, GameObject> sign = new Dictionary<string, GameObject>();
    Dictionary<string, bool> finish = new Dictionary<string, bool>();
    List<string> keys;

    void Start()
    {
        videoPlayer = videoPlayer_rawImage.GetComponent<VideoPlayer>();
    }

    public void restart()
    {
        deltaTimeCounter = 0f;
        int curFrame = synchronizeFrame();
        frameData nullFrame = new frameData();
        nullFrame.x = -1;
        nullFrame.y = -1;
        keys  = new List<string>(models.Keys);
        // Debug.Log(keys.Count);
        foreach (string updateName in keys)
        {            
            if (trajectoryDatas[updateName].Keys.Contains(1)) // if exist in the first frame
            {                
                models[updateName].SetActive(true);
                // sign[updateName].SetActive(true);
                models[updateName].transform.position = scalePosTo3D(trajectoryDatas[updateName][1].x, trajectoryDatas[updateName][1].y);
                //sign[updateName].transform.position = scalePosTo3D(trajectoryDatas[updateName][1].direction.x, trajectoryDatas[updateName][1].direction.y);
            }
            else { models[updateName].SetActive(false); sign[updateName].SetActive(false); };
            finish[updateName] = false;
        }       
    }

    public void play()
    {
        if (Run == false)
        {
            Run = true;
        }       
    }

    public void pause()
    {
        if(Run == true)
        {
            Run = false;
        }        
    }

    public void jumpTo()
    {
        Run = false;
        deltaTimeCounter = playTimeSlider.value;
        int curFrame = synchronizeFrame();
        foreach (string updateName in keys)
        {
            if (trajectoryDatas[updateName].Keys.Contains(curFrame)) // if exist in the first frame
            {
                models[updateName].SetActive(true);
                // sign[updateName].SetActive(true);
                models[updateName].transform.position = scalePosTo3D(trajectoryDatas[updateName][curFrame].x, trajectoryDatas[updateName][curFrame].y);
                // sign[updateName].transform.position = scalePosTo3D(trajectoryDatas[updateName][curFrame].direction.x, trajectoryDatas[updateName][curFrame].direction.y);
            }
            else { models[updateName].SetActive(false); sign[updateName].SetActive(false); };
        }
    }

    int synchronizeFrame()
    {
        int curFrame = (int)(deltaTimeCounter * fps); // Mathf.RoundToInt(deltaTimeCounter * fps); // 
        timeText.text = "Time: " + deltaTimeCounter.ToString("0.0000");  //  + "→ pre: " + preFrame.ToString() + ", next: " + nextFrame.ToString();
        fpsAndFrameText.text = "fps: " + fps.ToString() + "\tFrame: " + (curFrame).ToString();
        playTimeSlider.value = deltaTimeCounter;
        videoPlayer.frame = curFrame + 1;

        return curFrame;
    }

    // Update is called once per frame
    void Update()
    {       
        if (Run == true)
        {            
            deltaTimeCounter += Time.deltaTime;
            int curFrame = synchronizeFrame();
            if (curFrame >= maxFrameNum)
            {
                Run = false;
                foreach (KeyValuePair<string, Dictionary<int, frameData>> trajectory in trajectoryDatas)
                {
                    models[trajectory.Key].GetComponent<Animator>().SetBool("walk", false);
                }
            }
            else if (curFrame >= 1)
            {
                updatePosition();
            }
            else
            {
                
            }
        }
        else
        {
            foreach(KeyValuePair<string, Dictionary<int, frameData>> trajectory in trajectoryDatas)
            {
                models[trajectory.Key].GetComponent<Animator>().SetBool("walk", false);
            }
        }
    }

    void updatePosition()
    {
        foreach (KeyValuePair<string, Dictionary<int, frameData>> trajectory in trajectoryDatas)
        {
            int curFrame = (int)(deltaTimeCounter * fps);
            if (trajectory.Value.Keys.First() <= curFrame && curFrame < trajectory.Value.Keys.Last() && trajectory.Value.Keys.Contains(curFrame))  //check if this human should appear 
            {
                models[trajectory.Key].SetActive(true);
                // sign[trajectory.Key].SetActive(true);
                models[trajectory.Key].GetComponent<Animator>().SetBool("walk", true);
                models[trajectory.Key].GetComponent<Animator>().SetFloat("speed", 1);

                preFrame = curFrame;
                while (!trajectory.Value.Keys.Contains(preFrame) && preFrame > 0)
                {
                    preFrame -= 1;
                }
                nextFrame = curFrame + 1;
                while (!trajectory.Value.Keys.Contains(nextFrame) && nextFrame < maxFrameNum)
                {
                    nextFrame += 1;
                }

                // print for debug
                string outputLog = "-------------  find intepolation before after\n";
                outputLog += "deltaTimeCounter: " + deltaTimeCounter + "\n";
                outputLog += "human: " + trajectory.Key + ", currentFrame: " + curFrame + "\n";
                outputLog += "preFrame: " + preFrame + ", nextFrame: " + nextFrame;
                // Debug.Log(outputLog);

                if (nextFrame - curFrame <= fps)  // less than 1 second
                {
                    frameData prePos, nextPos;
                    trajectory.Value.TryGetValue(preFrame, out prePos);
                    trajectory.Value.TryGetValue(nextFrame, out nextPos);
                    // Debug.Log("pre pos: " + prePos.x + ", " + prePos.y + " / next pos: " + nextPos.x + ", " + nextPos.y);                            
                    float by = (deltaTimeCounter * fps - preFrame) * (nextFrame - preFrame);  // step * scale
                    Vector2 curPos = Lerp(prePos, nextPos, by);
                    Vector3 cur = scalePosTo3D(curPos.x, curPos.y);
                    Vector3 target;
                    if (nextPos.direction.x != -1 && nextPos.direction.y != -1)
                        target = scalePosTo3D(nextPos.direction.x, nextPos.direction.y);
                    else
                        target = scalePosTo3D(nextPos.x, nextPos.y);
                    models[trajectory.Key].transform.position = cur;
                    models[trajectory.Key].transform.LookAt(target);
                    string debugLog = "[ori] cur: (" + curPos.x + "," + curPos.y + "), target: (" + prePos.direction.x + "," + prePos.direction.y + ")";
                    debugLog += "\n[transform] cur: " + cur + ", target: " + target;
                    // sign[trajectory.Key].transform.position = target;
                    // Debug.Log(debugLog);
                }
            }
            else
            {
                models[trajectory.Key].SetActive(false);
                // sign[trajectory.Key].SetActive(false);
                models[trajectory.Key].GetComponent<Animator>().SetBool("walk", false);
                models[trajectory.Key].GetComponent<Animator>().SetFloat("speed", 0);
            }
        }
    }

    Vector3 scalePosTo3D(float x, float y)
    {
        float scaleUnit_x = 100.0f, scaleUnit_y = 100.0f;
        Vector3 outputVec = new Vector3((halfW - Mathf.Round(x)) / scaleUnit_x, 0, (y - Mathf.Round(halfH)) / scaleUnit_y);
        outputVec.x += 50 * UIController.instance.allScene.IndexOf(UIController.instance.currentScene);
        return outputVec;
    }

    float Lerp(float firstFloat, float secondFloat, float by)
    {
        return firstFloat + (secondFloat - firstFloat) * by;  // firstFloat * (1 - by) + secondFloat * by;
    }

    Vector2 Lerp(frameData firstVector, frameData secondVector, float by)
    {        
        float retX = Lerp(firstVector.x, secondVector.x, by);
        // Debug.Log(firstVector.x + ", " +  secondVector.x);
        float retY = Lerp(firstVector.y, secondVector.y, by);
        return new Vector2(retX, retY);
    }

    void readJson()
    {
        cleanBeforeRead();

        string dirPath = Application.streamingAssetsPath + "/Jsons/" + jsonDataDir;        

        string[] jsonDataFileNames = Directory.GetFiles(dirPath, "*.json");
        humanCount = jsonDataFileNames.Length;        

        foreach (string file in jsonDataFileNames)
        {
            // Debug.Log(file);
            string tmpJsonDataStr = File.ReadAllText(file);
            // Debug.Log(tmpJsonDataStr);
            jsonDataStr.Add(tmpJsonDataStr);

            string key = System.IO.Path.GetFileName(file).Split('.')[0];
            // Debug.Log(key);

            JsonData tmpJsonData = new JsonData();
            tmpJsonData = JsonMapper.ToObject(tmpJsonDataStr);
            Dictionary<int, frameData> tmpFrameDatas = new Dictionary<int, frameData>();
            tmpFrameDatas.Clear();
            foreach (JsonData k in tmpJsonData["tracjectory"])
            {
                int tmpFrameNum = int.Parse(k["frameInt"].ToJson());
                frameData tmpFrameData = new frameData();
                tmpFrameData.x = int.Parse(k["pos"][0].ToJson());
                tmpFrameData.y = int.Parse(k["pos"][1].ToJson());
                tmpFrameData.direction = new Vector2(int.Parse(k["direction"][0].ToJson()), int.Parse(k["direction"][1].ToJson()));
                // Debug.Log("frame: " + tmpFrameNum + ", x: " + tmpFrameData.x + ", y: " + tmpFrameData.y);
                tmpFrameDatas.Add(tmpFrameNum, tmpFrameData);

                if (tmpFrameNum > maxFrameNum)
                {
                    maxFrameNum = tmpFrameNum;
                }
            }
            trajectoryDatas.Add(key, tmpFrameDatas);

            // init model
            //Debug.Log(key);
            GameObject newObject = loadAllCharacterModels.instance.randomCreatePrefab(int.Parse(tmpJsonData["feature"]["genderType"].ToJson()),
                                                                                         int.Parse(tmpJsonData["feature"]["ageIndex"].ToJson()));
            newObject.name = key;
            newObject.transform.parent = peopleParent.transform;
            newObject.SetActive(false);
            Animator animator = newObject.GetComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("AnimationClips/ManPose");
            models.Add(key, newObject);

            // debug target sign
            GameObject newSign = (GameObject)Instantiate(signPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            newSign.SetActive(false);
            sign.Add(key, newSign);

            bool fin = false;
            finish.Add(key, fin);
        }
    }
    
    IEnumerator readVideo()
    {
        string videoPath = Application.streamingAssetsPath + "/Videos/" + videoName; //"_realData.m4v";
        // videoPath += "_trackWithStateOutput.m4v";
        videoPath += "_realData.m4v";
        // Debug.Log(videoPath);        
        videoPlayer.url = videoPath;       

        prepareVideoText.color = Color.red;
        prepareVideoText.text = "Preparing Video...";
        videoPlayer.time = 0;
        videoPlayer.Play();  
        yield return new WaitUntil(() => videoPlayer.isPrepared);
        videoPlayer_rawImage.texture = videoPlayer.texture;
        videoPlayer.frame = 1;
        videoPlayer.Pause();

        videoNameText.text = "Video: " + videoName;
        playTimeSlider.maxValue = (int)videoPlayer.frameCount / fps;
        prepareVideoText.color = Color.green;
        prepareVideoText.text = "Video Ready";

        string outputLog = "========> Data Ready : \n";
        outputLog += "json: " + jsonDataDir + "\n";
        outputLog += "human count: " + humanCount.ToString() + "\n";
        outputLog += "jsonMaxCount: " + maxFrameNum.ToString() + ", videoFrameCount: " + videoPlayer.frameCount.ToString() + "\n";
        Debug.Log(outputLog);
    }
    
    public void cleanBeforeRead()
    {
        // clean gameobject before clear dictionary
        foreach (KeyValuePair<string, GameObject> model in models)
        {
            Destroy(model.Value);
            Destroy(sign[model.Key]);            
        }

        jsonDataStr.Clear();
        trajectoryDatas.Clear();
        humanCount = 0;
        prefabId.Clear();
        models.Clear();
        sign.Clear();
        loadAllCharacterModels.instance.cleanUsedRecord();
        finish.Clear();
        maxFrameNum = 0;
    }

    public void changeData(string dirName)  // select by UI
    {
        // stop all action
        stopAllActions();

        // update data
        jsonDataDir = dirName;
                
        string[] strSplit = dirName.Split('_');
        videoName = strSplit[0];
        for (int i = 1; i < 4; i++)
        {
            videoName += "_" + strSplit[i];
        }
        // Debug.Log(videoName);

        readJson(); // reload json
        StartCoroutine(readVideo()); // readVideo();  // 
        restart();        
    }

    void stopAllActions()
    {
        Run = false;
        videoPlayer.Stop();
        videoPlayer.targetTexture.Release();
        deltaTimeCounter = 0f;
        timeText.text = "Time: " + deltaTimeCounter.ToString("1.0000");
        fpsAndFrameText.text = "fps: " + fps.ToString() + "\tFrame: 1";
    }
}
