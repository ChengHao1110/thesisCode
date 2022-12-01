using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class HeatmapSetting : PersistentSingleton<HeatmapSetting>
{
    public Toggle useGaussianFilterToggle;
    public TMP_InputField filterRateInput, moveMaxValueInput, stayMaxValueInput, matrixSizeInput;
    public TextMeshProUGUI originalMoveHeatmapValue, originalStayHeatmapValue;
    public GameObject heatmap;

    
    // Start is called before the first frame update
    void Start()
    {
        //initial value
        filterRateInput.text = dynamicSystem.instance.gaussianFilterSize.ToString();
        originalMoveHeatmapValue.text = dynamicSystem.instance.moveMaxLimit.ToString();
        moveMaxValueInput.text = dynamicSystem.instance.moveMaxLimit.ToString();
        originalStayHeatmapValue.text = dynamicSystem.instance.stayMaxLimit.ToString();
        stayMaxValueInput.text = dynamicSystem.instance.stayMaxLimit.ToString();
        matrixSizeInput.text = dynamicSystem.instance.matrixSize.ToString();
        useGaussianFilterToggle.isOn = dynamicSystem.instance.useGaussian;

        /*
        useGaussianFilterToggle.onValueChanged.AddListener(ifselect => {
            if (ifselect) UseGaussianFilterToggle();
        });
        */
        useGaussianFilterToggle.onValueChanged.AddListener(UseGaussianFilterToggle);

        filterRateInput.onValueChanged.AddListener(delegate { ChangeHeatmapFilterRateValue(); });
        moveMaxValueInput.onValueChanged.AddListener(delegate { ChangeMoveHeatmapMaxValue(); });
        stayMaxValueInput.onValueChanged.AddListener(delegate { ChangeStayHeatmapMaxValue(); });
        matrixSizeInput.onValueChanged.AddListener(delegate { ChangeHeatmapSizeValue(); });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UseGaussianFilterToggle(bool on)
    {
        dynamicSystem.instance.useGaussian = (on) ? true : false;
        Debug.Log(dynamicSystem.instance.useGaussian);

        //dynamicSystem.instance.useGaussian = true;
    }
    public void ChangeMoveHeatmapMaxValue()
    {
        float value = float.Parse(moveMaxValueInput.text);
        dynamicSystem.instance.moveMaxLimit = value;
    }
    public void ChangeStayHeatmapMaxValue()
    {
        float value = float.Parse(stayMaxValueInput.text);
        dynamicSystem.instance.stayMaxLimit = value;
    }

    public void ChangeHeatmapFilterRateValue()
    {
        int value = int.Parse(filterRateInput.text);
        dynamicSystem.instance.gaussianFilterSize = value;
    }

    public void ChangeHeatmapSizeValue()
    {
        int value = int.Parse(matrixSizeInput.text);
        dynamicSystem.instance.matrixSize = value;
    }

    public void GenerateMoveHeatmap()
    {
        if (dynamicSystem.instance.allPeopleFinish())
        {
            //heatmap.SetActive(true);
            //get another filename
            int min = -1;
            var info = new DirectoryInfo(dynamicSystem.instance.directory);
            var fileInfo = info.GetFiles("*.png");
            foreach (var file in fileInfo)
            {
                if (file.Name.Contains("moveHeatMap"))
                {
                    string filename = file.Name;
                    filename = filename.Replace(".png", "");
                    string[] frac = filename.Split('_');
                    int newIndex = int.Parse(frac[1]);
                    if (newIndex > min) min = newIndex;
                }
            }
            dynamicSystem.instance.TrajectoryToHeatmapWithGaussian(dynamicSystem.instance.matrixSize, dynamicSystem.instance.sceneSize / 2,
                                                                   dynamicSystem.instance.gaussian_rate, false, min + 1, "move");
        }
    }

    public void GenerateStayHeatmap()
    {
        if (dynamicSystem.instance.allPeopleFinish())
        {
            //heatmap.SetActive(true);
            //get another filename
            int min = -1;
            var info = new DirectoryInfo(dynamicSystem.instance.directory);
            var fileInfo = info.GetFiles("*.png");
            foreach (var file in fileInfo)
            {
                if (file.Name.Contains("stayHeatMap"))
                {
                    string filename = file.Name;
                    filename = filename.Replace(".png", "");
                    string[] frac = filename.Split('_');
                    int newIndex = int.Parse(frac[1]);
                    if (newIndex > min) min = newIndex;
                }
            }
            dynamicSystem.instance.TrajectoryToHeatmapWithGaussian(dynamicSystem.instance.matrixSize, dynamicSystem.instance.sceneSize / 2,
                                                                   dynamicSystem.instance.gaussian_rate, false, min + 1, "stay");
        }
    }
}
