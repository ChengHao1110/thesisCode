using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExhibitInfo
{
    //name
    public string exName;
    //position
    public double posX, posY, posZ;
    //rotation
    public double rotX, rotY, rotZ;
}

public class TaskContent
{
    public string taskImg;
    public int taskNo, mode;
    public List<ExhibitInfo> exInfoList = new List<ExhibitInfo>();    
}
