using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleUISetting : PersistentSingleton<SimpleUISetting>
{
    #region General Setting
    public Slider numberOfAgentSlider, adultRatioSlider, laterVisitorSlider;
    public TextMeshProUGUI numberOfAgentText, adultRatioText, laterVisitorText;

    public void GetValueFromUIController()
    {
        numberOfAgentSlider.value = UIController.instance.tmpSaveUISettings.UI_Global.agentCount;
        numberOfAgentText.text = numberOfAgentSlider.value.ToString();
        adultRatioSlider.value = (float)UIController.instance.tmpSaveUISettings.UI_Global.adultPercentage * 100;
        adultRatioText.text = adultRatioSlider.value.ToString("f0");
        laterVisitorSlider.value = UIController.instance.tmpSaveUISettings.UI_Global.addAgentCount;
        laterVisitorText.text = laterVisitorSlider.value.ToString();
    }
    
    public void ChangeNumberOfAgentValue()
    {
        int value = (int)numberOfAgentSlider.value;
        numberOfAgentText.text = value.ToString();
        AgentNumberInfluence(value);
        laterVisitorSlider.maxValue = value;
        //change laterVisitor value

        //change UIController UI Setting
        UIController.instance.tmpSaveUISettings.UI_Global.agentCount = value;
        UIController.instance.agentCountSlider.value = value;
        UIController.instance.agentCountText.text = value.ToString();
        UIController.instance.addAgentCountSlider.maxValue = value;

        value = (int)UIController.instance.addAgentCountSlider.value;
        UIController.instance.addAgentCountText.text = (value).ToString();
        UIController.instance.tmpSaveUISettings.UI_Global.addAgentCount = value;

        
    }

    public void ChangeAdultRatioValue()
    {
        int value = (int)adultRatioSlider.value;
        adultRatioText.text = value.ToString();

        //change UIController UI Setting
        UIController.instance.adultPercentSlider.value = value;
        float uiValue = (float)value / 100;  // % -> 0.xx
        UIController.instance.adultPercentText.text = uiValue.ToString("F2");
        UIController.instance.tmpSaveUISettings.UI_Global.adultPercentage = uiValue;
    }

    public void ChangeLaterVisitorValue()
    {
        int value = (int)laterVisitorSlider.value;
        laterVisitorText.text = (value).ToString();

        //change UIController UI Setting
        UIController.instance.addAgentCountSlider.value = value;
        UIController.instance.addAgentCountText.text = value.ToString();
        UIController.instance.tmpSaveUISettings.UI_Global.addAgentCount = value;
    }

    public void AgentNumberInfluence(int value)
    {
        // radius
        // capacity
        if (value < 8)
        {
            UIController.instance.StageRadiusGoToText.text = (0.5f).ToString();
            UIController.instance.tmpSaveUISettings.walkStage["GoTo"].radius = 0.5f;
            UIController.instance.StageRadiusCloseText.text = (0.4f).ToString();
            UIController.instance.tmpSaveUISettings.walkStage["Close"].radius = 0.4f;
            UIController.instance.StageRadiusAtText.text = (0.3f).ToString();
            UIController.instance.tmpSaveUISettings.walkStage["At"].radius = 0.3f;

        }
        else
        {
            UIController.instance.StageRadiusGoToText.text = (0.3f).ToString();
            UIController.instance.tmpSaveUISettings.walkStage["GoTo"].radius = 0.3f;
            UIController.instance.StageRadiusCloseText.text = (0.2f).ToString();
            UIController.instance.tmpSaveUISettings.walkStage["Close"].radius = 0.2f;
            UIController.instance.StageRadiusAtText.text = (0.1f).ToString();
            UIController.instance.tmpSaveUISettings.walkStage["At"].radius = 0.1f;
        }

        if (value <= 15)
        {
            UIController.instance.StageSpeedGoToText.text = "x " + (1.0f).ToString();
            UIController.instance.tmpSaveUISettings.walkStage["GoTo"].speed = 1.0f;
            UIController.instance.StageSpeedCloseText.text = "x " + (0.7f).ToString();
            UIController.instance.tmpSaveUISettings.walkStage["Close"].speed = 0.7f;
            UIController.instance.StageSpeedAtText.text = "x " + (0.5f).ToString();
            UIController.instance.tmpSaveUISettings.walkStage["At"].speed = 0.5f;
        }
        else
        {
            UIController.instance.StageSpeedGoToText.text = "x " + (0.75f).ToString();
            UIController.instance.tmpSaveUISettings.walkStage["GoTo"].speed = 0.75f;
            UIController.instance.StageSpeedCloseText.text = "x " + (0.45f).ToString();
            UIController.instance.tmpSaveUISettings.walkStage["Close"].speed = 0.45f;
            UIController.instance.StageSpeedAtText.text = "x " + (0.25f).ToString();
            UIController.instance.tmpSaveUISettings.walkStage["At"].speed = 0.25f;
        }

    }
    #endregion

    #region Visitor Setting
    public Slider aloneSlider, relaxSlider;
    public TextMeshProUGUI aloneText, relaxText;

    public void ChangeAloneValue()
    {
        float value = aloneSlider.value;
        aloneText.text = value.ToString("f2");
        AloneInfluence(value);
        UIController.instance.NormalizeInfluenceValue();
    }

    public void ChangeRelaxValue()
    {
        float value = relaxSlider.value;
        relaxText.text = value.ToString("f2");
        RelaxInfluence(value);
        UIController.instance.NormalizeInfluenceValue();
    }

    public void AloneInfluence(float value)
    {
        float realValue = 1.0f - value; 
        UIController.instance.tmpSaveUISettings.UI_InfluenceMap.humanInflence["followDesire"] = realValue;
        UIController.instance.tmpSaveUISettings.UI_InfluenceMap.humanInflence["gatherDesire"] = realValue;
        UIController.instance.humanFollowDesireInput.text = ((int)(realValue * 100)).ToString();
        UIController.instance.humanGatherDesireInput.text = ((int)(realValue * 100)).ToString();

    }

    public void RelaxInfluence(float value)
    {
        //exhibit take time
        float realValue = value * 30 / 0.4f;
        UIController.instance.tmpSaveUISettings.UI_InfluenceMap.exhibitInflence["takeTime"] = realValue / 100;
        UIController.instance.exhibitTakeTimeInput.text = ((int)(realValue)).ToString();
    }
    #endregion


}
