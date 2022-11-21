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
                
                if(dynamicSystem.instance.people[humanName].nextTarget_name == exhibitName && dynamicSystem.instance.people[humanName].nextTarget_name.StartsWith("p")
                   && dynamicSystem.instance.people[humanName].lastLookExhibitionName != exhibitName)
                {
                    dynamicSystem.instance.people[humanName].justIn = true;
                    //Debug.Log(dynamicSystem.instance.people[humanName].name + " in " + exhibitName);
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
                //dynamicSystem.instance.people[humanName].justIn = false;
                if (!dynamicSystem.instance.people[humanName].justIn)
                {
                    //dynamicSystem.instance.people[humanName].nearExhibition("goTo");
                }
            }
        }
        
    }
}
