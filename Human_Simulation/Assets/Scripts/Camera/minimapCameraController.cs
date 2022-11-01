using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class minimapCameraController : MonoBehaviour
{
    public float limitX, limitZ;
    Vector3 center = Vector3.zero;
    Vector3 startPos;
    float startSize;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (UIController.instance.isPanelUsing["modifyScene"])
        {
            //wheel
            transform.GetComponent<Camera>().orthographicSize -= Input.GetAxis("Mouse ScrollWheel");
            
            if (Input.GetMouseButton(1))
            {
                Vector3 newPosition = new Vector3();
                newPosition = transform.position;
                newPosition.x += Input.GetAxis("Mouse X");
                newPosition.z += Input.GetAxis("Mouse Y");
                transform.position = newPosition;
            }

            Check();

            if (Input.GetKeyDown(KeyCode.R))
            {
                transform.position = startPos;
                transform.GetComponent<Camera>().orthographicSize = startSize;
            }
        }
    }

    public void Initial()
    {
        if (UIController.instance.curOption.Contains("119")) center.x = 0;
        if (UIController.instance.curOption.Contains("120")) center.x = 50;
        if (UIController.instance.curOption.Contains("225")) center.x = 100;

        if (UIController.instance.curOption.Contains("A")) center.z = 50;
        else if (UIController.instance.curOption.Contains("B")) center.z = 100;
        else center.z = 0;

        startPos = transform.position;
        startSize = transform.GetComponent<Camera>().orthographicSize;
    }

    void Check()
    {
        if (transform.GetComponent<Camera>().orthographicSize > startSize) transform.GetComponent<Camera>().orthographicSize = startSize;
        if (transform.position.x > center.x + limitX) transform.position = new Vector3(center.x + limitX, transform.position.y, transform.position.z);
        if (transform.position.x < center.x - limitX) transform.position = new Vector3(center.x - limitX, transform.position.y, transform.position.z);
        if (transform.position.z > center.z + limitZ) transform.position = new Vector3(transform.position.x, transform.position.y, center.z + limitZ);
        if (transform.position.z < center.z  - limitZ) transform.position = new Vector3(transform.position.x, transform.position.y, center.z - limitZ);
    }
}
