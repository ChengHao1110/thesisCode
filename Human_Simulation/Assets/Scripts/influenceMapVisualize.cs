using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class influenceMapVisualize : PersistentSingleton<influenceMapVisualize>
{
    /* As a visualizer, mostly just read and show informations
     * don't change things in this script */

    public string mainHumanName = "", oldMainHumanName = "";
    public human_single mainHuman; // as a pointer
    public Dictionary<string, GameObject> markers = new Dictionary<string, GameObject>();

    /* GameObject root and prefabs */
    public Text nameText, informationText;
    public Material notActive, main, humanActive, exhibitActive;
    Color textColor_exhibit = new Color(77 / 255f, 19 / 255f, 96 / 255f);
    Color textColor_human = new Color(62 / 255f, 75 / 255f, 176 / 255f);
    Color textColor_main = new Color(134 / 255f, 34 / 255f, 43 / 255f);
    Color textColor_notActive = new Color(92 / 255f, 92 / 255f, 92 / 255f);
    /**/

    public GameObject showInfoPanel;
    public string mainExhibitName = "", oldMainExhibitName = "";

    /*show information position*/
    Color32 selectedColor = new Color32(120, 194, 196, 200);
    Color32 unSelectedColor = new Color32(255, 255, 255, 255);
    bool visLower = true, visRight = false, visUpper = false, exRight = true, exUpper = false;
    public Button visLowerBtn, visRightBtn, visUpperBtn, exRightBtn, exUpperBtn;
    public GameObject visInfoAtLowerPanel;
    public string mainHumanText = "", mainExhibitText = "";

    /*select object*/


    string baseInformationText = "";
    void Update()
    {
        if (!dynamicSystem.instance.Run) return;
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            if (dynamicSystem.instance.quickSimulationMode) return;
            var ray = Camera.main.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                //visitor
                if (hit.collider != null && hit.collider.transform.name.StartsWith("id"))
                {                    
                    // Debug.Log("focus on: " + hit.collider.transform.name);
                    changeMainHuman(hit.collider.transform.name);

                    //show visitor info on right top
                    if(mainHumanName != "") oldMainHumanName = mainHumanName;
                    mainHumanName = hit.collider.transform.name;
                    //mainExhibitName = "";
                    //oldMainExhibitName = "";
                    if (visRight) ShowVisitorInfoOnRightTopPanel();
                }
                //exhibit
                else if (hit.transform.gameObject.tag == "Exhibition" && !hit.transform.gameObject.name.Contains("x"))
                {
                    //show exhibit info
                    if (mainExhibitName != "") oldMainExhibitName = mainExhibitName;
                    mainExhibitName = hit.transform.gameObject.name;
                    //mainHumanName = "";
                    //oldMainHumanName = "";
                    if (exRight) ShowExhibitInfoOnRightTopPanel();
                }
            }
        }
        if (showInfoPanel.activeSelf)
        {
            TextMeshProUGUI infoText = showInfoPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            /*
            if (mainHumanName != "" && mainExhibitName != "") infoText.text = mainHumanText + "--------------------\n" + mainExhibitText;
            else if (mainHumanName != "" && mainExhibitName == "") infoText.text = mainHumanText;
            else if (mainHumanName == "" && mainExhibitName != "") infoText.text = mainExhibitText;
            */
            if (visRight && exRight) {
                if(mainExhibitText == "") infoText.text = mainHumanText;
                else infoText.text = mainHumanText + "--------------------\n" + mainExhibitText; 
            }
            else if (visRight && !exRight) infoText.text = mainHumanText;
            else if (!visRight && exRight) infoText.text = mainExhibitText;
        }
        if (visUpper)
        {
            showVisInfoOnTop(true);
        }
        if (exUpper)
        {
            showExInfoOnTop(true);
        }
    }

    public void influenceMapUpdate()
    {
        if (!visLower)
        {
            return;
        }
        if (mainHumanName != "")
        {
            foreach (KeyValuePair<string, GameObject> marker in markers)
            {
                TextMesh text = marker.Value.transform.Find("Text").GetComponent<TextMesh>();
                string nameWithoutMarker = marker.Key.Replace("marker_", "");
                if (nameWithoutMarker == mainHumanName)
                {
                    marker.Value.SetActive(true);
                    marker.Value.GetComponent<MeshRenderer>().material = main;
                    text.color = textColor_main;
                    if (mainHuman.informationBoard.activeSelf) mainHuman.informationBoard.GetComponent<MeshRenderer>().material = main;
                }
                else
                {
                    if (nameWithoutMarker.StartsWith("id")) // a person
                    {
                        if (dynamicSystem.instance.peopleGathers[mainHuman.gatherIndex].humans.Contains(nameWithoutMarker))
                        {
                            marker.Value.SetActive(true);
                            marker.Value.GetComponent<MeshRenderer>().material = humanActive;
                            text.color = textColor_human;
                        }
                        else
                        {
                            marker.Value.SetActive(false);
                            // marker.Value.GetComponent<MeshRenderer>().material = notActive;
                            // marker.Value.transform.Find("Text").GetComponent<TextMesh>().color = textColor_notActive;
                        }
                        if (dynamicSystem.instance.people[nameWithoutMarker].informationBoard.activeSelf) dynamicSystem.instance.people[nameWithoutMarker].informationBoard.GetComponent<MeshRenderer>().material = humanActive;
                    }
                    else // exhibit or exit
                    {
                        marker.Value.SetActive(true);
                        if (mainHuman.desireExhibitionList.Contains(nameWithoutMarker))
                        {
                            marker.Value.GetComponent<MeshRenderer>().material = exhibitActive;
                            text.color = textColor_exhibit;
                        }
                        else
                        {
                            marker.Value.GetComponent<MeshRenderer>().material = notActive;
                            text.color = textColor_notActive;
                        }
                    }

                    if (mainHuman.influenceMap.ContainsKey(nameWithoutMarker))
                    {
                        text.text = marker.Key + "\n" + mainHuman.influenceMap[marker.Key].ToString("F1");
                    }
                    else
                    {
                        text.text = marker.Key;
                    }
                }
            }

            string walkStatusText = "walk status: " + mainHuman.walkStopState + "\n";
            string desireListText = "desire List : " + mainHuman.desireExhibitionList.Count + "\n(" + string.Join(", ", mainHuman.desireExhibitionList) + ")\n";
            string freeTimeText = "free time: " + mainHuman.freeTime_totalLeft.ToString("F2") + " / " + mainHuman.freeTime_total.ToString("F2") + "\n";
            string nextTargetText = "next target: " + mainHuman.nextTarget_name;
            nextTargetText += " (stay: " + mainHuman.freeTime_stayInNextExhibit.ToString("F0") + "s)\n";
            if (mainHuman.nextTarget_name.StartsWith("p")) nextTargetText += " (index: " + mainHuman.nextTarget_direction + ", stay: "+ mainHuman.wanderStayTime.ToString("F2") + ")";
            float nextUpdateTime = dynamicSystem.instance.deltaTimeCounter - mainHuman.lastTimeStamp_recomputeMap;
            nextUpdateTime = dynamicSystem.instance.currentSceneSettings.customUI.UI_Global.UpdateRate["influenceMap"] - nextUpdateTime;
            if (nextUpdateTime > dynamicSystem.instance.updateVisBoard) nextUpdateTime = dynamicSystem.instance.updateVisBoard;
            string nextUpdateText = "Map next update in: " + nextUpdateTime.ToString("F2") + "s";
            string showInfoText = baseInformationText + walkStatusText + freeTimeText + nextTargetText + "\n" + desireListText/* + nextUpdateText*/;
            informationText.text = showInfoText;
        }
    }

    public void changeMainHuman(string inputName)
    {
        string preName = mainHumanName;
        if (inputName != "" && dynamicSystem.instance.people.ContainsKey(inputName))
        {
            mainHumanName = inputName;
            mainHuman = dynamicSystem.instance.people[mainHumanName];            
            string genderStr;
            if (mainHuman.gender == 0) genderStr = "female";
            else genderStr = "male";
            string age_genderText = "features: " + genderStr + " " + mainHuman.humanType + "\n";
            string speedText = "speed : " + mainHuman.walkSpeed.ToString("F2") + "\n";
            baseInformationText = age_genderText + speedText;
            influenceMapUpdate();
        }
        else
        {
            mainHumanName = "";
            mainHuman = null;
            baseInformationText = "";
            informationText.text = "";
            initializeVis();
        }
        nameText.text = "mainHuman:  " + mainHumanName;
    }

    public void ShowVisitorInfoOnRightTopPanel()
    {
        if (!dynamicSystem.instance.afterGenerate) return;
        // get visitor info 
        if (mainHumanName == "") return;
        showInfoPanel.SetActive(true);
        string showText = "";
        human_single visitor = dynamicSystem.instance.people[mainHumanName];
        // id
        showText += "name: " + visitor.name + "\n";
        string genderStr;
        // feature
        if (mainHuman.gender == 0) genderStr = "female";
        else genderStr = "male";
        showText += "feature: " + genderStr + " " + mainHuman.humanType + "\n";
        //speed
        //showText += "speed: " + mainHuman.walkSpeed.ToString("F2") + "\n";
        // walk status
        showText += "walk status: " + mainHuman.walkStopState + "\n";
        // free time
        showText += "free time: " + mainHuman.freeTime_totalLeft.ToString("F2") + " / " + mainHuman.freeTime_total.ToString("F2") + "\n";
        // next target
        string nextTargetText = "next target: " + mainHuman.nextTarget_name;
        nextTargetText += " (stay: " + mainHuman.freeTime_stayInNextExhibit.ToString("F0") + "s)\n";
        if (mainHuman.nextTarget_name.StartsWith("p")) nextTargetText += " (index: " + mainHuman.nextTarget_direction + ", stay: " + mainHuman.wanderStayTime.ToString("F2") + ")";
        showText += nextTargetText + "\n";
        // desire list 
        showText += "desire List : " + mainHuman.desireExhibitionList.Count + "\n(" + string.Join(", ", mainHuman.desireExhibitionList) + ")\n";
        // next update in : (sec)
        /*
        float nextUpdateTime = dynamicSystem.instance.deltaTimeCounter - mainHuman.lastTimeStamp_recomputeMap;
        nextUpdateTime = dynamicSystem.instance.currentSceneSettings.customUI.UI_Global.UpdateRate["influenceMap"] - nextUpdateTime;
        if (nextUpdateTime > dynamicSystem.instance.updateVisBoard) nextUpdateTime = dynamicSystem.instance.updateVisBoard;
        string nextUpdateText = "Map next update in: " + nextUpdateTime.ToString("F2") + "s";
        
        showText += nextUpdateText;
        */
        mainHumanText = showText;
        // Get TMP
        //TextMeshProUGUI infoText = showInfoPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        //infoText.text = showText;
    }
    public void CloseVisitorInfoOnRightTopPanel()
    {
        showInfoPanel.SetActive(false);
    }

    public void ShowExhibitInfoOnRightTopPanel()
    {
        if (!dynamicSystem.instance.afterGenerate) return;
        // get visitor info 
        if (mainExhibitName == "") return;
        showInfoPanel.SetActive(true);
        string showText = "";
        //check exit or exhibit
        string name = "p" + mainExhibitName.Replace(UIController.instance.currentScene + "_", "");
        exhibition_single exhibit = dynamicSystem.instance.exhibitions[name];
        string changeText = "capacity: \n" + exhibit.capacity_cur + " / " + exhibit.capacity_max + "\n";
        showText += exhibit.fixedText + changeText;

        mainExhibitText = showText;
        // Get TMP
        //TextMeshProUGUI infoText = showInfoPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        //infoText.text = showText;
    }

    public void initializeVis()
    {
        foreach (KeyValuePair<string, GameObject> marker in markers)
        {
            TextMesh text = marker.Value.transform.Find("Text").GetComponent<TextMesh>();
            text.text = marker.Key;
            marker.Value.SetActive(true);
            if (marker.Key.StartsWith("id")) // a person
            {
                marker.Value.GetComponent<MeshRenderer>().material = humanActive;
                text.color = textColor_human;
            }
            else
            {
                marker.Value.GetComponent<MeshRenderer>().material = exhibitActive;
                text.color = textColor_exhibit;
            }
        }
    }

    //info position button
    //Visitor
    public void VisLowerButtton()
    {
        visLower = true;
        visUpper = false;
        visRight = false;
        visInfoAtLowerPanel.SetActive(true);
        if (!exRight) showInfoPanel.SetActive(false);
        visLowerBtn.GetComponent<Image>().color = selectedColor;
        //change other button
        visRightBtn.GetComponent<Image>().color = unSelectedColor;
        visUpperBtn.GetComponent<Image>().color = unSelectedColor;
    }
    public void VisRightButtton()
    {
        visRight = true;
        visUpper = false;
        visLower = false;
        visInfoAtLowerPanel.SetActive(false);
        showInfoPanel.SetActive(true);
        visRightBtn.GetComponent<Image>().color = selectedColor;
        //change other button
        visLowerBtn.GetComponent<Image>().color = unSelectedColor;
        visUpperBtn.GetComponent<Image>().color = unSelectedColor;
    }
    public void VisUpperButtton()
    {
        visUpper = true;
        visLower = false;
        visRight = false;
        visInfoAtLowerPanel.SetActive(false);
        if(!exRight) showInfoPanel.SetActive(false);
        visUpperBtn.GetComponent<Image>().color = selectedColor;
        //change other button
        visRightBtn.GetComponent<Image>().color = unSelectedColor;
        visLowerBtn.GetComponent<Image>().color = unSelectedColor;
    }

    //Exibit
    public void ExRightButtton()
    {
        exRight = true;
        exUpper = false;
        exRightBtn.GetComponent<Image>().color = selectedColor;
        showInfoPanel.SetActive(true);
        //change other button
        exUpperBtn.GetComponent<Image>().color = unSelectedColor;
        if (mainExhibitName != "") showExInfoOnTop(false);
    }

    public void ExUpperButtton()
    {
        exUpper = true;
        exRight = false;

        if(!visRight) showInfoPanel.SetActive(false);
        exUpperBtn.GetComponent<Image>().color = selectedColor;
        //change other button
        exRightBtn.GetComponent<Image>().color = unSelectedColor;
    }

    public void showVisInfoOnTop(bool active)
    {
        if (mainHumanName == "") return;
        if(oldMainHumanName != "" && active) dynamicSystem.instance.people[mainHumanName].informationBoard.SetActive(false);
        dynamicSystem.instance.people[mainHumanName].informationBoard.SetActive(active);
    }

    public void showExInfoOnTop(bool active)
    {
        if (mainExhibitName == "") return;
        if (oldMainExhibitName != "" && active) 
        {
            string oldNname = "p" + oldMainExhibitName.Replace(UIController.instance.currentScene + "_", "");
            Debug.Log(oldMainExhibitName);
            dynamicSystem.instance.exhibitions[oldNname].informationBoard.SetActive(false); 
        }
        string name = "p" + mainExhibitName.Replace(UIController.instance.currentScene + "_", "");
        dynamicSystem.instance.exhibitions[name].informationBoard.SetActive(active);
    }
}
