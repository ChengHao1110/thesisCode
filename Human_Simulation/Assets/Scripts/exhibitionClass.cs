using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class exhibition_single  // List<exhibition_single> exhibition_many
{
    /* fixed */
    public string name;
    public GameObject model;
    public List<GameObject> bestViewSign = new List<GameObject>();
    public List<int> bestViewDirection = new List<int>();
    public List<Vector3> bestViewDirection_vector3 = new List<Vector3>();
    public List<int> frontSide = new List<int>();
    public Vector3 centerPosition, enterPosition, leavePosition;
    // public int range; // 半徑，影響範圍
    public statisticParameters stayTimeSetting = new statisticParameters();
    public int capacity_max;
    public float chosenProbabilty;
    public float repeatChosenProbabilty;
    public GameObject informationBoard;
    public TextMesh informationText;
    public string fixedText;
    public List<GameObject> range = new List<GameObject>(); // use collision to check capacity

    /* update */
    public int attractiveLevel;    
    public int capacity_cur = 0;
    public float crowdedTime = 0f;
    public List<string> currentHumanInside = new List<string>();

    /*real-time human count*/
    public List<int> realtimeHumanCount = new List<int>();
    public float timeCounter = 0f;

    /* random with statistic */
    public float generateStayTime(float max)
    {
        float stayTime = dynamicSystem.instance.generateByNormalDistribution((float)stayTimeSetting.mean, (float)stayTimeSetting.std, (float)stayTimeSetting.min, max);
        return stayTime;
    }

    /* for debug and check */
    public void updateInformationBoard()
    {
        /* update text on model */
        string changeText = "\n";
        changeText += "capacity: \n" + this.capacity_cur + " / " + this.capacity_max + "\n";

        if (this.informationBoard != null)
        {
            this.informationText.text = this.fixedText + changeText;

            /* make the board face to camera*/
            this.informationBoard.transform.LookAt(Camera.main.transform.position);
        }
    }

    public void setActiveInformationBoard(bool ifActive)
    {
        this.informationBoard.SetActive(ifActive);
    }

    //calculate real-time human count
    //add by ChengHao
    public void CalculateRealtimeHumanCount()
    {
        if(this.timeCounter >= 1.0f)
        {
            realtimeHumanCount.Add(this.capacity_cur);
        }
        else
        {
            this.timeCounter += Time.fixedDeltaTime;
        }
    }
}
