using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class switchToggle : MonoBehaviour
{
    // Start is called before the first frame update

    public Image img;
    public Sprite switchOn, switchOff;
    public Toggle toggle;
    void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(SwitchOn);
        if (toggle.isOn) SwitchOn(toggle.isOn);
    }

    public void SwitchOn(bool on)
    {
        if (on)
        {
            UIController.instance.DashBoard.SetActive(true);
            img.sprite = switchOn;
        }
        else
        {
            UIController.instance.DashBoard.SetActive(false);
            img.sprite = switchOff;
        }
    }
}
