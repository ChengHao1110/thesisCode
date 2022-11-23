using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CameraSetting 
{
    public Vector3 pos;
    public Quaternion rot;
    public float fov;
    public bool orthographic;
    public float size;
}


public class CameraController : MonoBehaviour
{
    public Dictionary<string, List<CameraSetting>> cameras;
    string number;
    int idx = 0;
    int cameraId;
    float cameraRotSpeed = 2.0f;
    float cameraTranSpeed = 0.01f;
    public List<CameraSetting> cameraList = new List<CameraSetting>();

    // Start is called before the first frame update
    void Awake()
    {
        cameras = new Dictionary<string, List<CameraSetting>>();
        cameraId = 0;
        cameras.Clear();
        cameraList.Clear();
        number = transform.name.Substring(11, 3);
        switch (number)
        {
            case "119":
                {
                    //original
                    cameraList.Add(AddCamera(transform.position, transform.rotation, transform.GetComponent<Camera>().fieldOfView, false, 0));
                    //add new camera
                    cameraList.Add(AddCamera(new Vector3(4.94f, 6.3f, 3.77f), 
                                             Quaternion.Euler(52.44f, 224.8f, 0f), 
                                             70, false, 0));
                    cameraList.Add(AddCamera(new Vector3(7.64f, 5.69f, -7.1f),
                                             Quaternion.Euler(30.25f, 316f, 0f),
                                             60, false, 0));
                    cameraList.Add(AddCamera(new Vector3(-10.24f, 5.2f, -5.44f),
                                             Quaternion.Euler(28.69f, 62.6f, 0f),
                                             60, false, 0));
                    cameraList.Add(AddCamera(new Vector3(-4.8f, 5.75f, 4.49f),
                                             Quaternion.Euler(39.7f, 139.85f, 0f),
                                             70, false, 0));
                    cameraList.Add(AddCamera(new Vector3(-0.4f, 7f, -0.2f),
                                             Quaternion.Euler(90f, 180f, -0.96f),
                                             75, true, 7.71f));
                    cameras.Add(number, cameraList);
                }
                break;
            case "120":
                {
                    //original
                    cameraList.Add(AddCamera(transform.position, transform.rotation, transform.GetComponent<Camera>().fieldOfView, false, 0));
                    //add new camera
                    cameraList.Add(AddCamera(new Vector3(60.4f, 5.21f, 3.84f),
                                             Quaternion.Euler(41.51f, 269.73f, 0),
                                             70, false, 0));
                    cameraList.Add(AddCamera(new Vector3(60.4f, 5.21f, -4.74f),
                                             Quaternion.Euler(41.51f, 269.73f, 0),
                                             70, false, 0));
                    cameraList.Add(AddCamera(new Vector3(49.14f, 5f, -10f),
                                             Quaternion.Euler(39.6f, 0f, 0),
                                             80, false, 0));
                    cameraList.Add(AddCamera(new Vector3(40.33f, 5f, -3.8f),
                                             Quaternion.Euler(37f, 90f, 0),
                                             70, false, 0));
                    cameraList.Add(AddCamera(new Vector3(40.6f, 5f, 2.5f),
                                             Quaternion.Euler(37f, 90f, 0),
                                             70, false, 0));
                    cameraList.Add(AddCamera(new Vector3(50.89f, 7f, -0.3f),
                                             Quaternion.Euler(90f, 180f, 0),
                                             105, true, 10.5f));
                    cameras.Add(number, cameraList);
                }
                break;
            case "225":
                {
                    //original
                    cameraList.Add(AddCamera(transform.position, transform.rotation, transform.GetComponent<Camera>().fieldOfView, false, 0));
                    //add new camera
                    cameraList.Add(AddCamera(new Vector3(103.16f, 6.3f, -2.8f),
                                             Quaternion.Euler(56.31f, -41.2f, 0),
                                             60, false, 0));
                    cameraList.Add(AddCamera(new Vector3(98.1f, 6.2f, -2.8f),
                                             Quaternion.Euler(55.36f, 29.2f, 0),
                                             60, false, 0));
                    cameraList.Add(AddCamera(new Vector3(101.7f, 8f, 3.56f),
                                             Quaternion.Euler(90f, 180f, 0),
                                             90, true, 8.5f));
                    cameras.Add(number, cameraList);
                }
                break;
        }
        if (UIController.instance.curOption.Contains("A")) idx = 1;
        else if (UIController.instance.curOption.Contains("B")) idx = 2;
        else idx = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (UIController.instance.isPanelUsing["modifyScene"]) return;

        int preIdx = idx;
        if (UIController.instance.curOption.Contains("A")) idx = 1;
        if (UIController.instance.curOption.Contains("B")) idx = 2;

        if (preIdx != idx) SetCamera(cameras[number][0], idx);

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            cameraId++;
            if (cameraId == cameras[number].Count) cameraId = 0;
            SetCamera(cameras[number][cameraId], idx);
        }
        if(Input.GetKeyDown(KeyCode.LeftArrow))
        {
            cameraId--;
            if (cameraId == -1) cameraId = cameras[number].Count - 1;
            SetCamera(cameras[number][cameraId], idx);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            cameraId = 0;
            SetCamera(cameras[number][cameraId], idx);
        }

        
        FreeControl();
    }
    CameraSetting AddCamera(Vector3 pos, Quaternion rot, float fov, bool orthographic, float size)
    {
        CameraSetting cs = new CameraSetting();
        cs.pos = pos;
        cs.rot = rot;
        cs.fov = fov;
        cs.orthographic = orthographic;
        cs.size = size;
        return cs;
    }

    public void SetCamera(CameraSetting cs, int idx)
    {

        Vector3 zOffset = new Vector3(0, 0, 50 * idx);
        transform.position = cs.pos + zOffset;
        transform.rotation = cs.rot;
        if (cs.orthographic)
        {
            transform.GetComponent<Camera>().orthographic = cs.orthographic;
            transform.GetComponent<Camera>().orthographicSize = cs.size;
        }
        else
        {
            transform.GetComponent<Camera>().orthographic = cs.orthographic;
            transform.GetComponent<Camera>().fieldOfView = cs.fov;
        }
    }

    public void SetCameraByBtn(CameraSetting cs, int idx, int camId)
    {
        cameraId = camId;
        Vector3 zOffset = new Vector3(0, 0, 50 * idx);
        transform.position = cs.pos + zOffset;
        transform.rotation = cs.rot;
        if (cs.orthographic)
        {
            transform.GetComponent<Camera>().orthographic = cs.orthographic;
            transform.GetComponent<Camera>().orthographicSize = cs.size;
        }
        else
        {
            transform.GetComponent<Camera>().orthographic = cs.orthographic;
            transform.GetComponent<Camera>().fieldOfView = cs.fov;
        }
    }
    void FreeControl()
    {
        if (Input.GetMouseButton(1))
        {
            //transform.LookAt(transform);
            transform.RotateAround(transform.position, Vector3.up, Input.GetAxis("Mouse X") * cameraRotSpeed);
            transform.RotateAround(transform.position, Vector3.right, Input.GetAxis("Mouse Y") * cameraRotSpeed);
            Quaternion cameraRot = transform.rotation;
            cameraRot = Quaternion.Euler(cameraRot.eulerAngles.x, cameraRot.eulerAngles.y, 0);
            transform.rotation = cameraRot;
        }
        if (Input.GetKey(KeyCode.W)) transform.position = new Vector3(transform.position.x, transform.position.y + cameraTranSpeed, transform.position.z);
        if (Input.GetKey(KeyCode.S)) transform.position = new Vector3(transform.position.x, transform.position.y - cameraTranSpeed, transform.position.z);

        if (Input.GetKey(KeyCode.A)) transform.position += transform.right * -cameraTranSpeed;
        if (Input.GetKey(KeyCode.D)) transform.position += transform.right * cameraTranSpeed;

        transform.position += transform.forward * Input.GetAxis("Mouse ScrollWheel");
    }

}
