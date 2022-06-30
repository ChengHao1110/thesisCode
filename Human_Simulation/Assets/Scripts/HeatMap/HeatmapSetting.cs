using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeatmapSetting : PersistentSingleton<HeatmapSetting>
{
    public Toggle useGaussianFilterToggle;
    public TMP_InputField filterRateInput, maxValueInput, matrixSizeInput;
    public GameObject heatmap;

    // Start is called before the first frame update
    void Start()
    {
        //initial value
        filterRateInput.text = dynamicSystem.instance.gaussianFilterSize.ToString();
        maxValueInput.text = dynamicSystem.instance.maxLimit.ToString();
        matrixSizeInput.text = dynamicSystem.instance.matrixSize.ToString();
        useGaussianFilterToggle.isOn = dynamicSystem.instance.useGaussian;
        
        
        useGaussianFilterToggle.onValueChanged.AddListener(ifselect => {
            if (ifselect) UseGaussianFilterToggle();
        });
        filterRateInput.onValueChanged.AddListener(delegate { ChangeHeatmapFilterRateValue(); });
        maxValueInput.onValueChanged.AddListener(delegate { ChangeHeatmapMaxValue(); });
        matrixSizeInput.onValueChanged.AddListener(delegate { ChangeHeatmapSizeValue(); });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UseGaussianFilterToggle()
    {
        dynamicSystem.instance.useGaussian = true;
    }

    public void ChangeHeatmapMaxValue()
    {
        int value = int.Parse(maxValueInput.text);
        dynamicSystem.instance.maxLimit = value;
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

    public void GenerateAnotherHeatmap()
    {
        if (dynamicSystem.instance.allPeopleFinish())
        {
            //heatmap.SetActive(true);
            dynamicSystem.instance.TrajectoryToHeatmapWithGaussian(dynamicSystem.instance.matrixSize, dynamicSystem.instance.sceneSize / 2,
                                                                   dynamicSystem.instance.gaussian_rate, false);
        }
    }
}
