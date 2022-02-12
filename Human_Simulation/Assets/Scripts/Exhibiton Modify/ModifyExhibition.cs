using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModifyExhibition : MonoBehaviour
{
    public bool isSelected;
    float rotateSpeed = 25f;
    public float rotateSpeedTime = 1f;
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
            //clockwise
            if (Input.GetKey(KeyCode.Q))
            {
                transform.RotateAround(transform.Find("BoundingBoxCube").transform.position, Vector3.up, ExhibitionMouseContrller.instance.rotateSpeedTime * rotateSpeed * Time.deltaTime);
            }
            //counterclockwise
            if (Input.GetKey(KeyCode.E))
            {
                transform.RotateAround(transform.Find("BoundingBoxCube").transform.position, Vector3.up, ExhibitionMouseContrller.instance.rotateSpeedTime * - rotateSpeed * Time.deltaTime);
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
        if (UIController.instance.modifyScene && isSelected)
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
        if (UIController.instance.modifyScene && isSelected)
        {
            float disX = Input.mousePosition.x - posX;
            float disY = Input.mousePosition.y - posY;
            float disZ = Input.mousePosition.z - posZ;
            Vector3 lastPos = Camera.main.ScreenToWorldPoint(new Vector3(disX, disY, disZ));
            transform.position = new Vector3(lastPos.x, startPos.y, lastPos.z);
        }
    }
}
