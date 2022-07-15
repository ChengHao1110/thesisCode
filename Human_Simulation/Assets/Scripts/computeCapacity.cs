using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class computeCapacity : MonoBehaviour
{
    string exhibitName = "";
    bool exit = false;

    void Start()
    {
        string transformName = this.gameObject.transform.parent.name;
        if (transformName.Contains("Exit"))
        {
            exhibitName = transformName.Split('_')[1].Replace("ExitDoor", "exit");
            exit = true;
        }
        else
        {
            exhibitName = "p" + transformName.Split('_')[1];
            exit = false;
        }
    }
    
    void OnTriggerEnter(Collider col)
    {
        adjustHumanList(col.gameObject.name, true);
    }

    void OnTriggerStay(Collider col)
    {
        adjustHumanList(col.gameObject.name, true);
    }

    void OnTriggerExit(Collider col)
    {
        adjustHumanList(col.gameObject.name, false);
    }

    void adjustHumanList(string humanName, bool add)
    {
        if (!humanName.StartsWith("id")) return;

        exhibition_single thisExhibit;
        if (exit) thisExhibit = dynamicSystem.instance.exits[exhibitName];
        else thisExhibit = dynamicSystem.instance.exhibitions[exhibitName];
        
        if (add)
        {
            if (!thisExhibit.currentHumanInside.Contains(humanName))
            {
                thisExhibit.currentHumanInside.Add(humanName);
                thisExhibit.capacity_cur = thisExhibit.currentHumanInside.Count;
                thisExhibit.updateInformationBoard();
                
                if(dynamicSystem.instance.people[humanName].nextTarget_name == exhibitName)
                {
                    Debug.Log(exhibitName);
                    Debug.Log("enter");
                    dynamicSystem.instance.people[humanName].justIn = true;
                    dynamicSystem.instance.people[humanName].nearExhibition("close");
                }
                
            }
        }
        else
        {
            if (thisExhibit.currentHumanInside.Contains(humanName))
            {
                thisExhibit.currentHumanInside.Remove(humanName);
                thisExhibit.capacity_cur = thisExhibit.currentHumanInside.Count;
                thisExhibit.updateInformationBoard();
                if (!dynamicSystem.instance.people[humanName].justIn)
                {
                    dynamicSystem.instance.people[humanName].nearExhibition("goTo");
                    Debug.Log(exhibitName);
                    Debug.Log("exit");
                }
            }
        }
        
    }
}
