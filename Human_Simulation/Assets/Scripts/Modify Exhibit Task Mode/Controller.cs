using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class Controller : PersistentSingleton<Controller>
{
    //measure
    public SystemTask systemTask;
    public bool firstEdit = false;

    //select object
    public GameObject selectedExhibition;
    public GameObject boundingBox;
    public bool hasSelecetedExhibition;
    List<GameObject> allBoundingBox = new List<GameObject>();

    // Modify Controller UI
    public Toggle showAllBoundingBox;
    public Slider rotateSpeedSlider;
    public TextMeshProUGUI rotateValueText;

    // mode
    public bool mode1 = true, mode2 = false;
    public string sceneName = "119";

    //for ModifyExhibitForTask.cs
    public float rotateSpeedTime;

    private void Awake()
    {
        mode1 = true;
        mode2 = false;
    }
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
        if (showAllBoundingBox.isOn)
        {
            DrawAllExhibitionsBoundingBox();
            return;
        }

        if (hasSelecetedExhibition)
        {
            boundingBox.GetComponent<DrawBoundingBox>().DrawBox(Color.red);
        }

        if (Input.GetMouseButtonDown(0))
        {
            /*
            if (EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("click on UI");
                return;
            }
            */
            RaycastHit hitInfo = new RaycastHit();
            //bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo);
            bool hit = false;
            RaycastHit[] hits;
            hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition), 100.0F);
            for(int i = 0; i < hits.Length; i++)
            {
                if(hits[i].transform.gameObject.tag == "Exhibition")
                {
                    hitInfo = hits[i];
                    hit = true;
                    break;
                }
            }
            if (hit)
            {
                //for mode2
                if (hasSelecetedExhibition && selectedExhibition.GetComponent<ModifyExhibitForTask>().copyGO != null)
                {
                    Debug.Log("has copy object");
                    return;
                }
                //Debug.Log("Hit " + hitInfo.transform.gameObject.name);

                //select new exhibition
                if (hitInfo.transform.gameObject.tag == "Exhibition" && !hasSelecetedExhibition)
                {
                    if (!firstEdit) 
                    { 
                        firstEdit = true;
                        systemTask.modifyTimeCounter = Time.time;
                        Debug.Log("start time: " + systemTask.modifyTimeCounter);
                    }

                    hasSelecetedExhibition = true;
                    selectedExhibition = hitInfo.transform.gameObject;
                    selectedExhibition.GetComponent<ModifyExhibitForTask>().isSelected = true;
                    boundingBox = selectedExhibition.transform.Find("BoundingBoxCube").gameObject;
                }
                //change another exhibition from the exhibition has been selected
                else if (hitInfo.transform.gameObject.tag == "Exhibition" && hasSelecetedExhibition)
                {
                    //remove the previous bounding box
                    boundingBox.GetComponent<DrawBoundingBox>().DeleteBoundingBox();
                    selectedExhibition.GetComponent<ModifyExhibitForTask>().isSelected = false;
                    //add the new bounding box
                    selectedExhibition = hitInfo.transform.gameObject;
                    selectedExhibition.GetComponent<ModifyExhibitForTask>().isSelected = true;
                    boundingBox = selectedExhibition.transform.Find("BoundingBoxCube").gameObject;
                }
                else
                {
                    Debug.Log("not exhibition");
                    if (hasSelecetedExhibition)
                    {
                        //remove the previous bounding box
                        boundingBox.GetComponent<DrawBoundingBox>().DeleteBoundingBox();
                        selectedExhibition.GetComponent<ModifyExhibitForTask>().isSelected = false;
                        hasSelecetedExhibition = false;
                    }
                }
            }
        }
    }

    void DrawAllExhibitionsBoundingBox()
    {
        for (int i = 0; i < allBoundingBox.Count; i++)
        {
            allBoundingBox[i].GetComponent<DrawBoundingBox>().DrawBox(Color.green);
        }
    }

    void GetAllExhibitions()
    {
        GameObject scene = GameObject.Find("/[EnvironmentsOfEachScene]/" + sceneName);
        foreach (Transform child in scene.transform)
        {
            if (child.gameObject.tag == "Exhibition")
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
