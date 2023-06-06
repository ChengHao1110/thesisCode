using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModifyExhibition : MonoBehaviour
{
    public bool isSelected;
    float rotateSpeed = 25f;
    public float rotateSpeedTime = 1f;
    //public bool mode1 = false, mode2 = true;
    public GameObject copyGO;

    //double click
    float firstClickTime;
    float timeBetweenClick = 0.5f;
    int clickTimes = 0;
    bool timeAllowed = true;


    // Start is called before the first frame update
    void Start()
    {
        isSelected = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isSelected && !UIController.instance.isOriginalScene)
        {
            if (ExhibitionMouseContrller.instance.mode1) 
            {
                //clockwise
                if (Input.GetKey(KeyCode.E))
                {
                    transform.RotateAround(transform.Find("BoundingBoxCube").transform.position, Vector3.up, ExhibitionMouseContrller.instance.rotateSpeedTime * rotateSpeed * Time.deltaTime);
                }
                //counterclockwise
                if (Input.GetKey(KeyCode.Q))
                {
                    transform.RotateAround(transform.Find("BoundingBoxCube").transform.position, Vector3.up, ExhibitionMouseContrller.instance.rotateSpeedTime * -rotateSpeed * Time.deltaTime);
                }
            }
            

            // mode2
            if (ExhibitionMouseContrller.instance.mode2)
            {
                //check double ckick
                if(Input.GetMouseButtonDown(0))
                {
                    clickTimes++;
                }
                if (clickTimes == 1 && timeAllowed)
                {
                    firstClickTime = Time.time;
                    StartCoroutine(CheckDoubleClick());
                }


                //teleport exhibit
                if (Input.GetMouseButtonDown(1))
                {
                    Debug.Log("click right key");
                    if (UIController.instance.isPanelUsing["modifyScene"] && !UIController.instance.isOriginalScene)
                    {
                        float disX = Input.mousePosition.x - posX;
                        float disY = Input.mousePosition.y - posY;
                        float disZ = Input.mousePosition.z - posZ;
                        Vector3 lastPos = Camera.main.ScreenToWorldPoint(new Vector3(disX, disY, disZ));
                        if (copyGO == null)
                        {
                            copyGO = Instantiate(gameObject);
                            copyGO.transform.rotation = transform.rotation;
                        }
                        //adjust transparent
                        /*
                        foreach (Transform child in copyGO.transform)
                        {
                            if (child.name == "center" || child.name == "range" || child.name == "BoundingBoxCube" || child.name == "ViewPoint")
                            {
                                continue;
                            }
                            Color oriColor = child.GetComponent<MeshRenderer>().material.color;
                            child.GetComponent<MeshRenderer>().material.color = new Color(oriColor.r, oriColor.g, oriColor.b, 0.25f);
                        }
                        */
                        Debug.Log("click pos: " + lastPos);
                        copyGO.transform.position = new Vector3(lastPos.x, startPos.y, lastPos.z);
                    }
                }
                // set the final position
                else if (Input.GetKeyDown(KeyCode.Return))
                {
                    transform.position = copyGO.transform.position;
                    transform.rotation = copyGO.transform.rotation;
                    Destroy(copyGO);
                }

                if (copyGO != null)
                {
                    float var = 0;
                    if (Input.GetAxisRaw("Mouse ScrollWheel") > 0) var = 10;
                    else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0) var = -10;
                    copyGO.transform.RotateAround(copyGO.transform.Find("BoundingBoxCube").position,
                            Vector3.up,
                            var * ExhibitionMouseContrller.instance.rotateSpeedTime * -rotateSpeed * Time.deltaTime);
                    GameObject boundingBox = copyGO.transform.Find("BoundingBoxCube").gameObject;
                    boundingBox.GetComponent<DrawBoundingBox>().DrawBox(Color.cyan);
                }
                
                

            }
        }
    }

    Vector3 dist;
    Vector3 startPos;
    float posX;
    float posZ;
    float posY;
    void OnMouseDown()
    {
        if (ExhibitionMouseContrller.instance.mode1)
        {
            if (UIController.instance.isPanelUsing["modifyScene"] && isSelected && !UIController.instance.isOriginalScene)
            {
                startPos = transform.position;
                dist = Camera.main.WorldToScreenPoint(transform.position);
                posX = Input.mousePosition.x - dist.x;
                posY = Input.mousePosition.y - dist.y;
                posZ = Input.mousePosition.z - dist.z;
            }
        }
    }

    void OnMouseDrag()
    {
        if (ExhibitionMouseContrller.instance.mode1) {
            if (UIController.instance.isPanelUsing["modifyScene"] && isSelected && !UIController.instance.isOriginalScene)
            {
                float disX = Input.mousePosition.x - posX;
                float disY = Input.mousePosition.y - posY;
                float disZ = Input.mousePosition.z - posZ;
                Vector3 lastPos = Camera.main.ScreenToWorldPoint(new Vector3(disX, disY, disZ));
                transform.position = new Vector3(lastPos.x, startPos.y, lastPos.z);
            }
        }
        
    }

    private IEnumerator CheckDoubleClick()
    {
        timeAllowed = false;
        while (Time.time < firstClickTime + timeBetweenClick)
        {
            if(clickTimes == 2)
            {
                Debug.Log("double click");
                transform.position = copyGO.transform.position;
                transform.rotation = copyGO.transform.rotation;
                Destroy(copyGO);
                break;
            }
            yield return new WaitForEndOfFrame();
        }
        clickTimes = 0;
        timeAllowed = true;
    }
}
