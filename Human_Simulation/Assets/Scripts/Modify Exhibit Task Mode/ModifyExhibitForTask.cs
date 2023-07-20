using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModifyExhibitForTask : MonoBehaviour
{
    public bool isSelected;
    float rotateSpeed = 25f;
    public float rotateSpeedTime = 1f;
    public GameObject copyGO;

    // double click
    float firstClickTime;
    float timeBetweenClick = 0.5f;
    int clickTimes = 0;
    bool timeAllowed = true;

    // mouse
    Vector3 dist;
    Vector3 startPos;
    float posX;
    float posZ;
    float posY;

    // Start is called before the first frame update
    void Start()
    {
        isSelected = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isSelected)
        {
            if (Controller.instance.mode1)
            {
                //clockwise
                if (Input.GetKey(KeyCode.E))
                {
                    transform.RotateAround(transform.Find("BoundingBoxCube").transform.position, Vector3.up, 2 * Controller.instance.rotateSpeedTime * rotateSpeed * Time.deltaTime);
                    if( Mathf.Abs(transform.rotation.eulerAngles.y - 180) < 0.1f)
                    {
                        Debug.Log(Time.time);
                    }
                }
                //counterclockwise
                if (Input.GetKey(KeyCode.Q))
                {
                    transform.RotateAround(transform.Find("BoundingBoxCube").transform.position, Vector3.up,  2 * Controller.instance.rotateSpeedTime * -rotateSpeed * Time.deltaTime);
                }
            }


            // mode2
            if (Controller.instance.mode2)
            {
                //check double ckick
                if (Input.GetMouseButtonDown(0))
                {
                    clickTimes++;
                }
                if (clickTimes == 1 && timeAllowed)
                {
                    firstClickTime = Time.time;
                    StartCoroutine(CheckDoubleClick());
                }

                //move
                if (Input.GetMouseButton(1))
                {
                    AdjustExhibition();
                }

                //teleport exhibit
                if (Input.GetMouseButtonDown(1))
                {
                    Debug.Log("click right key");

                    float disX = Input.mousePosition.x - posX;
                    float disY = Input.mousePosition.y - posY;
                    float disZ = Input.mousePosition.z - posZ;
                    Vector3 lastPos = Camera.main.ScreenToWorldPoint(new Vector3(disX, disY, disZ));
                    if (copyGO == null)
                    {
                        copyGO = Instantiate(gameObject);
                        copyGO.transform.rotation = transform.rotation;
                    }
                    copyGO.transform.position = new Vector3(lastPos.x, startPos.y, lastPos.z);
                    
                }
                // set the final position
                else if (Input.GetKeyDown(KeyCode.Return))
                {
                    transform.position = copyGO.transform.position;
                    transform.rotation = copyGO.transform.rotation;
                    Destroy(copyGO);
                }

                if (Input.GetAxisRaw("Mouse ScrollWheel") > 0) 
                {
                    float var = 12;
                    //Debug.Log(Time.frameCount);
                    if (copyGO == null)
                    {
                        copyGO = Instantiate(gameObject);
                        copyGO.transform.rotation = transform.rotation;
                        copyGO.transform.position = transform.position;
                    }
                    copyGO.transform.RotateAround(copyGO.transform.Find("BoundingBoxCube").position,
                            Vector3.up,
                            var * Controller.instance.rotateSpeedTime * -rotateSpeed * Time.deltaTime);
                }
                else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
                {
                    float var = -12;
                    //Debug.Log(Time.frameCount);
                    if (copyGO == null)
                    {
                        copyGO = Instantiate(gameObject);
                        copyGO.transform.rotation = transform.rotation;
                        copyGO.transform.position = transform.position;
                    }
                    copyGO.transform.RotateAround(copyGO.transform.Find("BoundingBoxCube").position,
                            Vector3.up,
                            var * Controller.instance.rotateSpeedTime * -rotateSpeed * Time.deltaTime);
                    if (Mathf.Abs(transform.rotation.eulerAngles.y - 180) < 0.1f)
                    {
                        Debug.Log(Time.time);
                    }
                }
                if (copyGO != null)
                {
                    GameObject boundingBox = copyGO.transform.Find("BoundingBoxCube").gameObject;
                    boundingBox.GetComponent<DrawBoundingBox>().DrawBox(Color.cyan);
                }

            }
        }
    }

    void OnMouseDown()
    {
        if (Controller.instance.mode1 && isSelected)
        {
            startPos = transform.position;
            dist = Camera.main.WorldToScreenPoint(transform.position);
            posX = Input.mousePosition.x - dist.x;
            posY = Input.mousePosition.y - dist.y;
            posZ = Input.mousePosition.z - dist.z;
        }
    }

    void OnMouseDrag()
    {
        if (Controller.instance.mode1 && isSelected)
        {
            float disX = Input.mousePosition.x - posX;
            float disY = Input.mousePosition.y - posY;
            float disZ = Input.mousePosition.z - posZ;
            Vector3 lastPos = Camera.main.ScreenToWorldPoint(new Vector3(disX, disY, disZ));
            transform.position = new Vector3(lastPos.x, startPos.y, lastPos.z);
        }
        
    }

    void AdjustExhibition()
    {
        if (Controller.instance.mode2 && isSelected && copyGO != null)
        {
            float disX = Input.mousePosition.x - posX;
            float disY = Input.mousePosition.y - posY;
            float disZ = Input.mousePosition.z - posZ;
            Vector3 lastPos = Camera.main.ScreenToWorldPoint(new Vector3(disX, disY, disZ));
            copyGO.transform.position = new Vector3(lastPos.x, startPos.y, lastPos.z);
        }
    }

    private IEnumerator CheckDoubleClick()
    {
        timeAllowed = false;
        while (Time.time < firstClickTime + timeBetweenClick)
        {
            if (clickTimes == 2)
            {
                if (copyGO != null)
                {
                    Debug.Log("double click");
                    transform.position = copyGO.transform.position;
                    transform.rotation = copyGO.transform.rotation;
                    Destroy(copyGO);
                }
                break;
            }
            yield return new WaitForEndOfFrame();
        }
        clickTimes = 0;
        timeAllowed = true;
    }
}
