using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class influenceMapVisualize : PersistentSingleton<influenceMapVisualize>
{
    /* As a visualizer, mostly just read and show informations
     * don't change things in this script */

    public string mainHumanName = "";
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
    string baseInformationText = "";

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (dynamicSystem.instance.quickSimulationMode) return;
            var ray = Camera.main.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider != null && hit.collider.transform.name.StartsWith("id"))
                {                    
                    // Debug.Log("focus on: " + hit.collider.transform.name);
                    changeMainHuman(hit.collider.transform.name);
                }
            }
        }
    }

    public void influenceMapUpdate()
    {
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
            string showInfoText = baseInformationText + walkStatusText + freeTimeText + nextTargetText + "\n" + desireListText + nextUpdateText;
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
}
