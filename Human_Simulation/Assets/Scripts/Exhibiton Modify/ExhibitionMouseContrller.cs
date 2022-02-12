using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExhibitionMouseContrller : PersistentSingleton<ExhibitionMouseContrller>
{
    //select object
    GameObject selectedExhibition;
    GameObject boundingBox;
    bool hasSelecetedExhibition;
    List<GameObject> allBoundingBox = new List<GameObject>();

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
        rotateSpeedSlider.onValueChanged.AddListener( (float value) => OnSliderValueChange(value)); ;
    }

    // Update is called once per frame
    void Update()
    {
        if(!UIController.instance.modifyScene) showAllBoundingBox.isOn = false;

        if (showAllBoundingBox.isOn)
        {
            DrawAllExhibitionsBoundingBox();
            return;
        }

        if (hasSelecetedExhibition)
        {
            boundingBox.GetComponent<DrawBoundingBox>().DrawBox(Color.red);
        }

        if (UIController.instance.modifyScene)
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hitInfo = new RaycastHit();
                bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo);
                if (hit)
                {
                    Debug.Log("Hit " + hitInfo.transform.gameObject.name);
                    //select new exhibition
                    if (hitInfo.transform.gameObject.tag == "Exhibition" && !hasSelecetedExhibition)
                    {
                        hasSelecetedExhibition = true;
                        selectedExhibition = hitInfo.transform.gameObject;
                        selectedExhibition.GetComponent<ModifyExhibition>().isSelected = true;
                        boundingBox = selectedExhibition.transform.Find("BoundingBoxCube").gameObject;
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
}
