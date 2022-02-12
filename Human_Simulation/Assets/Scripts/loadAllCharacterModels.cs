using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class prefab
{
    public GameObject model;
    public bool used;
}

public class loadAllCharacterModels : PersistentSingleton<loadAllCharacterModels>
{
    public Dictionary<string, List<prefab>> humanPrefab = new Dictionary<string, List<prefab>>();
    public string[] humanPrefabType = new string[] { "Male", "Male_old", "Male_young", "Female", "Female_old", "Female_young", "Boy", "Girl" };
    public Dictionary<string, int> countPrefabs = new Dictionary<string, int>();
    public Dictionary<string, bool> prefabFullUsed = new Dictionary<string, bool>();

    System.Random random = new System.Random();

    void Awake()
    {
        foreach(string humanType in humanPrefabType)
        {
            List<prefab> prefabsForTheType = new List<prefab>(); 
            string charactersPath = "CharactersPrefab/" + humanType + "/";
            // string dirPath = Application.streamingAssetsPath + "/" + charactersPath;
            string dirPath = Application.dataPath + "/Resources/" + charactersPath;
            List<string> prefabNames = Directory.EnumerateFiles(dirPath, "*.*").Where(s => s.EndsWith(".prefab") || s.EndsWith(".FBX")).ToList();// Directory.GetFiles(dirPath, "*.prefab");

            foreach (string prefabName in prefabNames)
            {
                prefab tmp = new prefab();
                tmp.model = Resources.Load<GameObject>(charactersPath + Path.GetFileName(prefabName).Split('.')[0]);
                tmp.used = false;
                prefabsForTheType.Add(tmp);
            }

            // Debug.Log(humanType + ": " + prefabsForTheType.Count);
            humanPrefab.Add(humanType, prefabsForTheType);
            countPrefabs.Add(humanType, prefabsForTheType.Count);
            prefabFullUsed.Add(humanType, false);
        }
    }

    public GameObject randomCreatePrefab(int genderType, int ageIndex)
    {
        /*
            ageIndex：人物年齡標籤(-1 : 無法判定，0：12歲以下，1：12～20歲，2：20～45歲，3：45～65歲，4：65歲以上)
            genderType：人物性別標籤(-1：無法辨別，0：女性，1：男性)
         */
        int gender = genderType;
        int age = ageIndex;
        if (gender == -1){ gender = random.Next(2); }
        if (age == -1){ age = random.Next(5); }

        GameObject newModel;
        if (gender == 0) // female
        {
            if(age == 0) // Girl
            {
                newModel = (GameObject)Instantiate(getNotRepeatModel("Girl"), new Vector3(0, 0, 0), Quaternion.identity);
            }
            else if (age == 1) // young female
            {
                newModel = (GameObject)Instantiate(getNotRepeatModel("Female_young"), new Vector3(0, 0, 0), Quaternion.identity);
            }
            else if (age == 4) // granny
            {
                newModel = (GameObject)Instantiate(getNotRepeatModel("Female_old"), new Vector3(0, 0, 0), Quaternion.identity);
            }
            else  // 2 and 3
            {
                newModel = (GameObject)Instantiate(getNotRepeatModel("Female"), new Vector3(0, 0, 0), Quaternion.identity);
            }
        }
        else // male
        {
            if (age == 0) // Boy
            {
                newModel = (GameObject)Instantiate(getNotRepeatModel("Boy"), new Vector3(0, 0, 0), Quaternion.identity);
            }
            else if (age == 1) // young male
            {
                newModel = (GameObject)Instantiate(getNotRepeatModel("Male_young"), new Vector3(0, 0, 0), Quaternion.identity);
            }
            else if (age == 4) // grandpa
            {
                newModel = (GameObject)Instantiate(getNotRepeatModel("Male_old"), new Vector3(0, 0, 0), Quaternion.identity);
            }
            else  // 2 and 3
            {
                newModel = (GameObject)Instantiate(getNotRepeatModel("Male"), new Vector3(0, 0, 0), Quaternion.identity);
            }
        }

        newModel.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
        for (int i = newModel.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = newModel.transform.GetChild(i).gameObject;
            if (child.name.StartsWith("Camera"))
            {
                Destroy(child);
            }                
        }
        return newModel;
    }

    public void cleanUsedRecord()
    {
        foreach (string humanType in humanPrefabType)
        {
            foreach (prefab p in humanPrefab[humanType])
            {
                p.used = false;
            }
            prefabFullUsed[humanType] = false;
        }
    }

    GameObject getNotRepeatModel(string type)
    {        
        int objIndex;
        objIndex = random.Next(countPrefabs[type]);
        if (prefabFullUsed[type]) { return humanPrefab[type][objIndex].model; } // all used, have to repeat
        else
        {

            while (humanPrefab[type][objIndex].used)  // find a model that is not used
            {
                objIndex = random.Next(countPrefabs[type]);
            }

            humanPrefab[type][objIndex].used = true;

            // check if all model used
            checkFullUsed(type);

            return humanPrefab[type][objIndex].model;
        }
    }

    void checkFullUsed(string type)
    {
        bool check = true;
        foreach(prefab p in humanPrefab[type])
        {
            if (p.used == false) check = false;
        }

        prefabFullUsed[type] = check;
    }
}
