using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class settingsClass {
    public int screenSize_w, screenSize_h;
    public UISettings oriJson = new UISettings();
    public UISettings customUI = new UISettings();
    public Dictionary<string, Dictionary<string, double>> exhibitionStateMap = new Dictionary<string, Dictionary<string, double>>();
    public Dictionary<string, settings_exhibition> Exhibitions = new Dictionary<string, settings_exhibition>();
    public Dictionary<string, settings_humanType> humanTypes = new Dictionary<string, settings_humanType>();

    public settingsClass copy()
    {
        settingsClass set = new settingsClass();
        oriJson = this.oriJson.copy();
        customUI = this.customUI.copy();
        
        foreach (KeyValuePair<string, settings_exhibition> dict in this.Exhibitions)
        {
            set.Exhibitions.Add(dict.Key, dict.Value.copy());
        }
        foreach (KeyValuePair<string, settings_humanType> dict in this.humanTypes)
        {
            set.humanTypes.Add(dict.Key, dict.Value.copy());
        }

        return set;
    }
}

public class UISettings
{
    public UISetting_Global UI_Global = new UISetting_Global();
    public UISetting_Human UI_Human = new UISetting_Human();
    public UISetting_Exhibit UI_Exhibit = new UISetting_Exhibit();
    public UISetting_InfluenceMap UI_InfluenceMap = new UISetting_InfluenceMap();
    public Dictionary<string, navAgentParameters> walkStage = new Dictionary<string, navAgentParameters>()
    {
        ["GoTo"] = new navAgentParameters { radius = 0.4, speed =  1 },
        ["Close"] = new navAgentParameters { radius = 0.35, speed = 0.7 },
        ["At"] = new navAgentParameters { radius = 0.3, speed = 0.5 },
    };
    public UISettings copy()
    {
        UISettings set = new UISettings();
        set.UI_Global = this.UI_Global.copy();
        set.UI_Human = this.UI_Human.copy();
        set.UI_Exhibit = this.UI_Exhibit.copy();
        set.UI_InfluenceMap = this.UI_InfluenceMap.copy();
        foreach (KeyValuePair<string, navAgentParameters> dict in this.walkStage)
        {
            if (this.walkStage.ContainsKey(dict.Key))
                set.walkStage[dict.Key] = dict.Value;
            else
                set.walkStage.Add(dict.Key, dict.Value.copy());
        }
        return set;
    }
}

public class UISetting_Global
{
    public int agentCount = 15;
    public double adultPercentage = 0.6;
    public int addAgentCount = 5;
    public int startAddAgentMin = 10, startAddAgentMax = 60;
    public Dictionary<string, int> UpdateRate = new Dictionary<string, int>() {
        ["gathers"] = 5,
        ["stopWalkStatus"] = 2,
        ["influenceMap"] = 5
    };
    public UISetting_Global copy()
    {
        UISetting_Global set = new UISetting_Global();
        set.agentCount = this.agentCount;
        set.adultPercentage = this.adultPercentage;
        set.addAgentCount = this.addAgentCount;
        foreach (KeyValuePair<string, int> dict in this.UpdateRate)
        {
            if (this.UpdateRate.ContainsKey(dict.Key))
                set.UpdateRate[dict.Key] = dict.Value;
            else
                set.UpdateRate.Add(dict.Key, dict.Value);
        }

        return set;
    }
}

public class UISetting_Human
{
    public int walkSpeedMin = 60, walkSpeedMax = -1;
    public int freeTimeMin = 50, freeTimeMax = 300;
    public statisticParameters gatherProbability = new statisticParameters();
    public Dictionary<string, statisticParameters> behaviorProbability = new Dictionary<string, statisticParameters>() {
        ["join"] = new statisticParameters(),
        ["keepAlone"] = new statisticParameters(),
        ["keepGather_sameGroup"] = new statisticParameters(),
        ["keepGather_difGroup"] = new statisticParameters(),
        ["leave"] = new statisticParameters(),
    };
    public UISetting_Human copy()
    {
        UISetting_Human set = new UISetting_Human();
        set.walkSpeedMin = this.walkSpeedMin;
        set.walkSpeedMax = this.walkSpeedMax;
        set.freeTimeMin = this.freeTimeMin;
        set.freeTimeMax = this.freeTimeMax;
        set.gatherProbability = this.gatherProbability.copy();
        foreach (KeyValuePair<string, statisticParameters> dict in this.behaviorProbability)
        {
            if (this.behaviorProbability.ContainsKey(dict.Key))
                set.behaviorProbability[dict.Key] = dict.Value;
            else
                set.behaviorProbability.Add(dict.Key, dict.Value.copy());
        }

        return set;
    }
}

public class UISetting_Exhibit
{
    public double capacityLimitTimes = 1.2, popularThreshold = 0.3, crowdedThreshold = 0.5;
    public int crowdedTimeLimit = 5;
    public UISetting_Exhibit copy()
    {
        return new UISetting_Exhibit
        {
            capacityLimitTimes = this.capacityLimitTimes,
            popularThreshold = this.popularThreshold,
            crowdedThreshold = this.crowdedThreshold,
            crowdedTimeLimit = this.crowdedTimeLimit
        };
    }
}

public class UISetting_InfluenceMap
{
    public double weightHuman = 0.2, weightExhibit = 1;
    public Dictionary<string, double> humanInflence = new Dictionary<string, double>() {
        ["followDesire"] = 0.3,
        ["takeTime"] = 0.3,
        ["gatherDesire"] = 0.2,
        ["humanTypeAttraction"] = 0.2,
        ["behaviorAttraction"] = 0.0
    };
    public Dictionary<string, double> exhibitInflence = new Dictionary<string, double>() {
        ["capactiy"] = 0.20,
        ["takeTime"] = 0.30,
        ["popularLevel"] = 0.15,
        ["humanPreference"] = 0.20,
        ["closeToBestViewDirection"] = 0.15
    };
    public UISetting_InfluenceMap copy()
    {
        UISetting_InfluenceMap set = new UISetting_InfluenceMap();
        set.weightHuman = this.weightHuman;
        set.weightExhibit = this.weightExhibit;
        foreach (KeyValuePair<string, double> dict in this.humanInflence)
        {
            if (this.humanInflence.ContainsKey(dict.Key))
                set.humanInflence[dict.Key] = dict.Value;
            else
                set.humanInflence.Add(dict.Key, dict.Value);
        }
        foreach (KeyValuePair<string, double> dict in this.exhibitInflence)
        {
            if (this.exhibitInflence.ContainsKey(dict.Key))
                set.exhibitInflence[dict.Key] = dict.Value;
            else
                set.exhibitInflence.Add(dict.Key, dict.Value);
        }
        return set;
    }
}

public class statisticParameters
{
    public double mean;
    public double std;
    public double min;
    public double max;
    public double median;
    public statisticParameters copy()
    {
        return new statisticParameters
        {
            mean = this.mean,
            std = this.std,
            min = this.min,
            max = this.max,
            median = this.median
        };
    }
}

public class navAgentParameters
{
    public double radius;
    public double speed;
    public navAgentParameters copy()
    {
        return new navAgentParameters
        {
            radius = this.radius,
            speed = this.speed
        };
    }
}

public class settings_exhibition
{
    public List<int> color = new List<int>(); // (R, G, B) on color map
    public Vector2 centerPos;
    public statisticParameters capacity = new statisticParameters();
    public statisticParameters stayTime = new statisticParameters();
    public List<int> bestViewDirection = new List<int>();
    public List<List<double>> bestViewDistance = new List<List<double>>();
    public List<int> frontSide = new List<int>();
    public double chosenProbabilty;
    public double repeatChosenProbabilty;

    public settings_exhibition copy()
    {
        settings_exhibition set = new settings_exhibition();
        foreach (int c in this.color)
        {
            set.color.Add(c);
        }
        // set.centerPos = this.centerPos;
        set.capacity = this.capacity.copy();
        set.stayTime = this.stayTime.copy();
        foreach (int d in this.bestViewDirection)
        {
            set.bestViewDirection.Add(d);
        }
        foreach (int s in this.frontSide)
        {
            set.frontSide.Add(s);
        }
        set.chosenProbabilty = this.chosenProbabilty;
        set.repeatChosenProbabilty = this.repeatChosenProbabilty;

        return set;
    }
}

public class settings_humanType
{
    public statisticParameters walkSpeed = new statisticParameters();
    public double walkToStopRate;
    public double stopToWalkRate;
    public Dictionary<string, double> interestForEachExhibition = new Dictionary<string, double>();

    public settings_humanType copy()
    {
        settings_humanType set = new settings_humanType();
        set.walkSpeed = this.walkSpeed.copy();
        set.walkToStopRate = this.walkToStopRate;
        set.stopToWalkRate = this.stopToWalkRate;
        foreach (KeyValuePair<string, double> dict in this.interestForEachExhibition)
        {
            set.interestForEachExhibition.Add(dict.Key, dict.Value);
        }
        return set;
    }
}
