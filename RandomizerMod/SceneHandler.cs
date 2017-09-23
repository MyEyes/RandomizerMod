using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RandomizerMod
{
    // Token: 0x02000922 RID: 2338
    public static class SceneHandler
    {
        // Token: 0x0600312E RID: 12590 RVA: 0x00127658 File Offset: 0x00125858
        public static void CheckForChanges(string destScene)
        {
            if (destScene == "Ruins1_32" && !PlayerData.instance.hasWalljump)
            {
                List<GameObject> objectsFromScene = SceneHandler.GetObjectsFromScene("Ruins1_32");
                for (int i = 0; i < objectsFromScene.Count; i++)
                {
                    if (objectsFromScene[i].name == "ruind_int_plat_float_02 (3)")
                    {
                        GameObject gameObject = UnityEngine.Object.Instantiate(objectsFromScene[i], objectsFromScene[i].transform.position, objectsFromScene[i].transform.rotation) as GameObject;
                        gameObject.SetActiveRecursively(true);
                        gameObject.transform.position = new Vector3(40.5f, 72f, 0f);
                        gameObject.name = "CustomPlatform";
                        gameObject.tag = "Untagged";
                    }
                }
            }
            if (destScene == "Fungus2_21" && PlayerData.instance.hasCityKey)
            {
                foreach (GameObject gameObject2 in SceneHandler.GetObjectsFromScene("Fungus2_21"))
                {
                    if (gameObject2.name == "City Gate Control" || gameObject2.name == "Ruins_front_gate" || gameObject2.name.Contains("Ruins_gate"))
                    {
                        UnityEngine.Object.Destroy(gameObject2);
                    }
                }
            }
            if (destScene == "Abyss_18" && !PlayerData.instance.hasWalljump)
            {
                foreach (GameObject gameObject3 in SceneHandler.GetObjectsFromScene("Abyss_18"))
                {
                    if (gameObject3.name == "Toll Machine Bench")
                    {
                        UnityEngine.Object.Destroy(gameObject3);
                        break;
                    }
                }
            }
        }

        // Token: 0x0600312F RID: 12591 RVA: 0x0012783C File Offset: 0x00125A3C
        public static List<GameObject> GetObjectsFromScene(string sceneName)
        {
            List<GameObject> list = new List<GameObject>();
            GameObject[] rootGameObjects = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName).GetRootGameObjects();
            if (rootGameObjects != null && rootGameObjects.Length != 0 && rootGameObjects != null && rootGameObjects.Length != 0)
            {
                list.AddRange(rootGameObjects);
                for (int i = 0; i < rootGameObjects.Length; i++)
                {
                    List<Transform> list2 = new List<Transform>();
                    foreach (object obj in rootGameObjects[i].transform)
                    {
                        Transform transform = (Transform)obj;
                        list.Add(transform.gameObject);
                        list2.Add(transform);
                    }
                    for (int j = 0; j < list2.Count; j++)
                    {
                        if (list2[j].childCount > 0)
                        {
                            foreach (object obj2 in list2[j])
                            {
                                Transform transform2 = (Transform)obj2;
                                list.Add(transform2.gameObject);
                                list2.Add(transform2);
                            }
                        }
                    }
                }
            }
            return list;
        }
    }
}
