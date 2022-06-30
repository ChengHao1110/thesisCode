using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ExhibitionInfo : PersistentSingleton<ExhibitionInfo>
{
    public TextMeshProUGUI Name;
    public TMP_InputField capacityMaxInput, capacityMeanInput, capacityMedianInput;
    public TMP_InputField stayTimeMaxInput, stayTimeMinInput, stayTimeMeanInput, stayTimeStdInput;
    public TMP_InputField chooseInput, reChooseInput;

    // Start is called before the first frame update
    void Start()
    {
        Name.text = "Name: Not an exhibition";
        capacityMaxInput.text = "0";
        capacityMeanInput.text = "0";
        capacityMedianInput.text = "0";
        stayTimeMaxInput.text = "0";
        stayTimeMinInput.text = "0";
        stayTimeMeanInput.text = "0";
        stayTimeStdInput.text = "0";
        chooseInput.text = "0";
        reChooseInput.text = "0";
        /*
        capacityMaxInput.onValueChanged.AddListener(delegate { ChangeCapacityMaxValue(); });
        capacityMeanInput.onValueChanged.AddListener(delegate { ChangeCapacityMeanValue(); });
        capacityMedianInput.onValueChanged.AddListener(delegate { ChangeCapacityMedianValue(); });
        stayTimeMaxInput.onValueChanged.AddListener(delegate { ChangeStayTimeMaxValue(); });
        stayTimeMinInput.onValueChanged.AddListener(delegate { ChangeStayTimeMinValue(); });
        stayTimeMeanInput.onValueChanged.AddListener(delegate { ChangeStayTimeMeanValue(); });
        stayTimeStdInput.onValueChanged.AddListener(delegate { ChangeStayTimeStdValue(); });
        chooseInput.onValueChanged.AddListener(delegate { ChangeChooseProbabilityValue(); });
        reChooseInput.onValueChanged.AddListener(delegate { ChangeRepeatChooseProbabilityValue(); });
        */
        capacityMaxInput.onEndEdit.AddListener(delegate { ChangeCapacityMaxValue(); });
        capacityMeanInput.onEndEdit.AddListener(delegate { ChangeCapacityMeanValue(); });
        capacityMedianInput.onEndEdit.AddListener(delegate { ChangeCapacityMedianValue(); });
        stayTimeMaxInput.onEndEdit.AddListener(delegate { ChangeStayTimeMaxValue(); });
        stayTimeMinInput.onEndEdit.AddListener(delegate { ChangeStayTimeMinValue(); });
        stayTimeMeanInput.onEndEdit.AddListener(delegate { ChangeStayTimeMeanValue(); });
        stayTimeStdInput.onEndEdit.AddListener(delegate { ChangeStayTimeStdValue(); });
        chooseInput.onEndEdit.AddListener(delegate { ChangeChooseProbabilityValue(); });
        reChooseInput.onEndEdit.AddListener(delegate { ChangeRepeatChooseProbabilityValue(); });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //capacity max
    public void ChangeCapacityMaxValue()
    {
        if (ExhibitionMouseContrller.instance.hasSelecetedExhibition)
        {
            double value = double.Parse(capacityMaxInput.text);
            GameObject ex = ExhibitionMouseContrller.instance.selectedExhibition;
            //show info
            string key = ex.name.Replace(UIController.instance.currentScene + "_", "p");
            if (dynamicSystem.instance.currentSceneSettings.Exhibitions.ContainsKey(key))
            {
                dynamicSystem.instance.currentSceneSettings.Exhibitions[key].capacity.max = value;
            }
            else
            {
                UIController.instance.ShowMsgPanel("Warning", "Invalid Modification");
            }
        }
        else
        {
            //error
            UIController.instance.ShowMsgPanel("Warning", "Please choose an exhibition");
            ExhibitionMouseContrller.instance.SetInfoToNULL("Not an exhibition");
        }
    }

    //capacity mean
    public void ChangeCapacityMeanValue()
    {
        if (ExhibitionMouseContrller.instance.hasSelecetedExhibition)
        {
            double value = double.Parse(capacityMeanInput.text);
            GameObject ex = ExhibitionMouseContrller.instance.selectedExhibition;
            //show info
            string key = ex.name.Replace(UIController.instance.currentScene + "_", "p");
            if (dynamicSystem.instance.currentSceneSettings.Exhibitions.ContainsKey(key))
            {
                dynamicSystem.instance.currentSceneSettings.Exhibitions[key].capacity.mean = value;
            }
            else
            {
                UIController.instance.ShowMsgPanel("Warning", "Invalid Modification");
            }
        }
        else
        {
            //error
            UIController.instance.ShowMsgPanel("Warning", "Please choose an exhibition");
            ExhibitionMouseContrller.instance.SetInfoToNULL("Not an exhibition");
        }
    }

    //capacity median
    public void ChangeCapacityMedianValue()
    {
        if (ExhibitionMouseContrller.instance.hasSelecetedExhibition)
        {
            double value = double.Parse(capacityMedianInput.text);
            GameObject ex = ExhibitionMouseContrller.instance.selectedExhibition;
            //show info
            string key = ex.name.Replace(UIController.instance.currentScene + "_", "p");
            if (dynamicSystem.instance.currentSceneSettings.Exhibitions.ContainsKey(key))
            {
                dynamicSystem.instance.currentSceneSettings.Exhibitions[key].capacity.median = value;
            }
            else
            {
                UIController.instance.ShowMsgPanel("Warning", "Invalid Modification");
            }
        }
        else
        {
            //error
            UIController.instance.ShowMsgPanel("Warning", "Please choose an exhibition");
            ExhibitionMouseContrller.instance.SetInfoToNULL("Not an exhibition");
        }
    }

    //staytime max
    public void ChangeStayTimeMaxValue()
    {
        if (ExhibitionMouseContrller.instance.hasSelecetedExhibition)
        {
            double value = double.Parse(stayTimeMaxInput.text);
            GameObject ex = ExhibitionMouseContrller.instance.selectedExhibition;
            //show info
            string key = ex.name.Replace(UIController.instance.currentScene + "_", "p");
            if (dynamicSystem.instance.currentSceneSettings.Exhibitions.ContainsKey(key))
            {
                dynamicSystem.instance.currentSceneSettings.Exhibitions[key].stayTime.max = value;
            }
            else
            {
                UIController.instance.ShowMsgPanel("Warning", "Invalid Modification");
            }
        }
        else
        {
            //error
            UIController.instance.ShowMsgPanel("Warning", "Please choose an exhibition");
            ExhibitionMouseContrller.instance.SetInfoToNULL("Not an exhibition");
        }
    }

    //staytime min
    public void ChangeStayTimeMinValue()
    {
        if (ExhibitionMouseContrller.instance.hasSelecetedExhibition)
        {
            double value = double.Parse(stayTimeMinInput.text);
            GameObject ex = ExhibitionMouseContrller.instance.selectedExhibition;
            //show info
            string key = ex.name.Replace(UIController.instance.currentScene + "_", "p");
            if (dynamicSystem.instance.currentSceneSettings.Exhibitions.ContainsKey(key))
            {
                dynamicSystem.instance.currentSceneSettings.Exhibitions[key].stayTime.min = value;
            }
            else
            {
                UIController.instance.ShowMsgPanel("Warning", "Invalid Modification");
            }
        }
        else
        {
            //error
            UIController.instance.ShowMsgPanel("Warning", "Please choose an exhibition");
            ExhibitionMouseContrller.instance.SetInfoToNULL("Not an exhibition");
        }
    }

    //staytime mean
    public void ChangeStayTimeMeanValue()
    {
        if (ExhibitionMouseContrller.instance.hasSelecetedExhibition)
        {
            double value = double.Parse(stayTimeMeanInput.text);
            GameObject ex = ExhibitionMouseContrller.instance.selectedExhibition;
            //show info
            string key = ex.name.Replace(UIController.instance.currentScene + "_", "p");
            if (dynamicSystem.instance.currentSceneSettings.Exhibitions.ContainsKey(key))
            {
                dynamicSystem.instance.currentSceneSettings.Exhibitions[key].stayTime.mean = value;
            }
            else
            {
                UIController.instance.ShowMsgPanel("Warning", "Invalid Modification");
            }
        }
        else
        {
            //error
            UIController.instance.ShowMsgPanel("Warning", "Please choose an exhibition");
            ExhibitionMouseContrller.instance.SetInfoToNULL("Not an exhibition");
        }
    }

    //staytime max
    public void ChangeStayTimeStdValue()
    {
        if (ExhibitionMouseContrller.instance.hasSelecetedExhibition)
        {
            double value = double.Parse(stayTimeStdInput.text);
            GameObject ex = ExhibitionMouseContrller.instance.selectedExhibition;
            //show info
            string key = ex.name.Replace(UIController.instance.currentScene + "_", "p");
            if (dynamicSystem.instance.currentSceneSettings.Exhibitions.ContainsKey(key))
            {
                dynamicSystem.instance.currentSceneSettings.Exhibitions[key].stayTime.std = value;
            }
            else
            {
                UIController.instance.ShowMsgPanel("Warning", "Invalid Modification");
            }
        }
        else
        {
            //error
            UIController.instance.ShowMsgPanel("Warning", "Please choose an exhibition");
            ExhibitionMouseContrller.instance.SetInfoToNULL("Not an exhibition");
        }
    }

    //choose probability
    public void ChangeChooseProbabilityValue()
    {
        if (ExhibitionMouseContrller.instance.hasSelecetedExhibition)
        {
            double value = double.Parse(chooseInput.text);
            GameObject ex = ExhibitionMouseContrller.instance.selectedExhibition;
            //show info
            string key = ex.name.Replace(UIController.instance.currentScene + "_", "p");
            if (dynamicSystem.instance.currentSceneSettings.Exhibitions.ContainsKey(key))
            {
                dynamicSystem.instance.currentSceneSettings.Exhibitions[key].chosenProbabilty = value;
            }
            else
            {
                UIController.instance.ShowMsgPanel("Warning", "Invalid Modification");
            }
        }
        else
        {
            //error
            UIController.instance.ShowMsgPanel("Warning", "Please choose an exhibition");
            ExhibitionMouseContrller.instance.SetInfoToNULL("Not an exhibition");
        }
    }

    //choose probability
    public void ChangeRepeatChooseProbabilityValue()
    {
        if (ExhibitionMouseContrller.instance.hasSelecetedExhibition)
        {
            double value = double.Parse(reChooseInput.text);
            GameObject ex = ExhibitionMouseContrller.instance.selectedExhibition;
            //show info
            string key = ex.name.Replace(UIController.instance.currentScene + "_", "p");
            if (dynamicSystem.instance.currentSceneSettings.Exhibitions.ContainsKey(key))
            {
                dynamicSystem.instance.currentSceneSettings.Exhibitions[key].repeatChosenProbabilty = value;
            }
            else
            {
                UIController.instance.ShowMsgPanel("Warning", "Invalid Modification");
            }
        }
        else
        {
            //error
            UIController.instance.ShowMsgPanel("Warning", "Please choose an exhibition");
            ExhibitionMouseContrller.instance.SetInfoToNULL("Not an exhibition");
        }
    }
}
