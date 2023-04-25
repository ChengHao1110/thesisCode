using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ExhibitionMouseContrller : PersistentSingleton<ExhibitionMouseContrller>
{
    //select object
    public GameObject selectedExhibition;
    GameObject boundingBox;
    public bool hasSelecetedExhibition;
    List<GameObject> allBoundingBox = new List<GameObject>();

    Color32 selectedColor = new Color32(120, 194, 196, 200);
    Color32 unSelectedColor = new Color32(255, 255, 255, 255);
    public bool mode1 = true, mode2 = false;
    public Button mode1Btn, mode2Btn;

    // Modify Controller UI
    public Toggle showAllBoundingBox;
    public Slider rotateSpeedSlider;
    public TextMeshProUGUI rotateValueText;

    //for ModifyExhibition.cs
    public float rotateSpeedTime;

    // Start is called before the first frame update
    void Start()
    {
        hasSelecetedExhibition = false;
        showAllBoundingBox.isOn = false;
        showAllBoundingBox.onValueChanged.AddListener(ifselect => { 
            if (ifselect) OnToggleValueChangeTrue();
            else OnToggleValueChangeFalse();
        });
        InitialRotateSlider();
        rotateSpeedSlider.onValueChanged.AddListener((float value) => OnSliderValueChange(value));
    }

    // Update is called once per frame
    void Update()
    {
        if(!UIController.instance.isPanelUsing["modifyScene"]) showAllBoundingBox.isOn = false;

        if (showAllBoundingBox.isOn)
        {
            DrawAllExhibitionsBoundingBox();
            return;
        }

        if (hasSelecetedExhibition)
        {
            boundingBox.GetComponent<DrawBoundingBox>().DrawBox(Color.red);
        }

        if (UIController.instance.isPanelUsing["modifyScene"])
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    Debug.Log("click on UI");
                    return;
                }

                RaycastHit hitInfo = new RaycastHit();
                bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo);
                if (hit)
                {
                    //for mode2
                    if (hasSelecetedExhibition && selectedExhibition.GetComponent<ModifyExhibition>().copyGO != null)
                    {
                        Debug.Log("has copy object");
                        return; 
                    }
                    Debug.Log("Hit " + hitInfo.transform.gameObject.name);
                    //select new exhibition
                    if (hitInfo.transform.gameObject.tag == "Exhibition" && !hasSelecetedExhibition)
                    {
                        hasSelecetedExhibition = true;
                        selectedExhibition = hitInfo.transform.gameObject;
                        selectedExhibition.GetComponent<ModifyExhibition>().isSelected = true;
                        boundingBox = selectedExhibition.transform.Find("BoundingBoxCube").gameObject;
                        SetInfo();
                    }
                    //change another exhibition from the exhibition has been selected
                    else if (hitInfo.transform.gameObject.tag == "Exhibition" && hasSelecetedExhibition)
                    {
                        //remove the previous bounding box
                        boundingBox.GetComponent<DrawBoundingBox>().DeleteBoundingBox();
                        selectedExhibition.GetComponent<ModifyExhibition>().isSelected = false;
                        //add the new bounding box
                        selectedExhibition = hitInfo.transform.gameObject;
                        selectedExhibition.GetComponent<ModifyExhibition>().isSelected = true;
                        boundingBox = selectedExhibition.transform.Find("BoundingBoxCube").gameObject;
                        SetInfo();
                    }
                    else
                    {
                        Debug.Log("not exhibition");
                        if (hasSelecetedExhibition)
                        {
                            //remove the previous bounding box
                            boundingBox.GetComponent<DrawBoundingBox>().DeleteBoundingBox();
                            selectedExhibition.GetComponent<ModifyExhibition>().isSelected = false;
                            hasSelecetedExhibition = false;
                            SetInfoToNULL("Not an exhibition");
                        }
                    }
                }
            }
        }
        else
        {
            if (hasSelecetedExhibition)
            {
                boundingBox.GetComponent<DrawBoundingBox>().DeleteBoundingBox();
                selectedExhibition.GetComponent<ModifyExhibition>().isSelected = false;
                hasSelecetedExhibition = false;
                selectedExhibition = null;
                SetInfoToNULL("Not an exhibition");
            }
        }
        
    }

    void DrawAllExhibitionsBoundingBox()
    {
        for(int i = 0; i < allBoundingBox.Count; i++)
        {
            allBoundingBox[i].GetComponent<DrawBoundingBox>().DrawBox(Color.green);
        }
    }

    void GetAllExhibitions()
    {
        GameObject scene = GameObject.Find("/[EnvironmentsOfEachScene]/" + UIController.instance.curOption);
        foreach(Transform child in scene.transform)
        {
            if(child.gameObject.tag == "Exhibition")
            {
                allBoundingBox.Add(child.Find("BoundingBoxCube").gameObject);
            }
        }
    }

    void OnToggleValueChangeTrue()
    {
        GetAllExhibitions();
    }

    void OnToggleValueChangeFalse()
    {
        for (int i = 0; i < allBoundingBox.Count; i++)
        {
            allBoundingBox[i].GetComponent<DrawBoundingBox>().DeleteBoundingBox();
        }
        allBoundingBox.Clear();
    }

    void InitialRotateSlider()
    {
        rotateSpeedSlider.value = 1;
        rotateSpeedTime = 1f;
        float realValue = (rotateSpeedSlider.value + 1) / 2;
        rotateValueText.text = realValue.ToString();
        rotateSpeedSlider.minValue = 0f;
        rotateSpeedSlider.maxValue = 5f;
    }

    void OnSliderValueChange(float value)
    {
        rotateSpeedSlider.value = Mathf.RoundToInt(rotateSpeedSlider.value);
        float realValue = (rotateSpeedSlider.value + 1) / 2;
        rotateValueText.text = realValue.ToString();
        rotateSpeedTime = realValue;
    }

    public void Mode1BtnON()
    {
        if (hasSelecetedExhibition) return;
        mode1 = true;
        mode2 = false;
        mode1Btn.GetComponent<Image>().color = selectedColor;
        mode2Btn.GetComponent<Image>().color = unSelectedColor;
    }

    public void Mode2BtnON()
    {
        if (hasSelecetedExhibition) return;
        mode1 = false;
        mode2 = true;
        mode1Btn.GetComponent<Image>().color = unSelectedColor;
        mode2Btn.GetComponent<Image>().color = selectedColor;
    }

    public void SetInfoToNULL(string name)
    {
        ExhibitionInfo.instance.Name.text = "Name: " + name;
        ExhibitionInfo.instance.capacityMaxInput.text = "0";
        ExhibitionInfo.instance.capacityMeanInput.text = "0";
        ExhibitionInfo.instance.capacityMedianInput.text = "0";
        ExhibitionInfo.instance.stayTimeMaxInput.text = "0";
        ExhibitionInfo.instance.stayTimeMinInput.text = "0";
        ExhibitionInfo.instance.stayTimeMeanInput.text = "0";
        ExhibitionInfo.instance.stayTimeStdInput.text = "0";
        ExhibitionInfo.instance.chooseInput.text = "0";
        ExhibitionInfo.instance.reChooseInput.text = "0";
    }



    void SetInfo()
    {
        string key = selectedExhibition.name.Replace(UIController.instance.currentScene + "_", "p");
        if (dynamicSystem.instance.currentSceneSettings.Exhibitions.ContainsKey(key))
        {
            settings_exhibition info = dynamicSystem.instance.currentSceneSettings.Exhibitions[key];
            ExhibitionInfo.instance.Name.text = "Name: " + key;
            ExhibitionInfo.instance.capacityMaxInput.text = info.capacity.max.ToString();
            ExhibitionInfo.instance.capacityMeanInput.text = info.capacity.mean.ToString();
            ExhibitionInfo.instance.capacityMedianInput.text = info.capacity.median.ToString();
            ExhibitionInfo.instance.stayTimeMaxInput.text = info.stayTime.max.ToString("f2");
            ExhibitionInfo.instance.stayTimeMinInput.text = info.stayTime.min.ToString("f2");
            ExhibitionInfo.instance.stayTimeMeanInput.text = info.stayTime.mean.ToString("f2");
            ExhibitionInfo.instance.stayTimeStdInput.text = info.stayTime.std.ToString("f2");
            ExhibitionInfo.instance.chooseInput.text = info.chosenProbabilty.ToString("f2");
            ExhibitionInfo.instance.reChooseInput.text = info.repeatChosenProbabilty.ToString("f2");
        }
        else
        {
            SetInfoToNULL("Can't Modify");
        }
    }
}
