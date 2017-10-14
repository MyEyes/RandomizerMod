using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using HutongGames.PlayMaker;

namespace RandomizerMod
{
    // Token: 0x02000922 RID: 2338
    public static class SceneHandler
    {
        public static void CheckForChanges(Scene from, Scene to)
        {
            CheckForChanges(to.name);
        }

        // Token: 0x0600312E RID: 12590 RVA: 0x00127658 File Offset: 0x00125858
        public static void CheckForChanges(string destScene)
        {
            if (!Randomizer.randomizer)
            {
                return;
            }

            try
            {
                if (GameManager.instance.IsGameplayScene())
                {
                    PlayMakerFSM spell = HeroController.instance.spellControl;

                    for (int i = 0; i < spell.FsmStates.Length; i++)
                    {
                        if (spell.FsmStates[i].Name == "Has Fireball?" || spell.FsmStates[i].Name == "Has Quake?" || spell.FsmStates[i].Name == "Has Scream?")
                        {
                            FieldInfo[] fieldInfo = typeof(HutongGames.PlayMaker.ActionData).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            for (int j = 0; j < fieldInfo.Length; j++)
                            {
                                if (fieldInfo[j].Name == "fsmStringParams")
                                {
                                    foreach (FsmString str in (List<FsmString>)fieldInfo[j].GetValue(spell.FsmStates[i].ActionData))
                                    {
                                        List<FsmString> val = new List<FsmString>();

                                        if (str.Value.Contains("fireball"))
                                        {
                                            val.Add("_fireballLevel");
                                        }
                                        else if (str.Value.Contains("quake"))
                                        {
                                            val.Add("_quakeLevel");
                                        }
                                        else if (str.Value.Contains("scream"))
                                        {
                                            val.Add("_screamLevel");
                                        }

                                        if (val.Count > 0)
                                        {
                                            fieldInfo[j].SetValue(spell.FsmStates[i].ActionData, val);
                                        }
                                    }

                                    spell.FsmStates[i].LoadActions();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Modding.ModHooks.ModLog(e.ToString());
            }

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

                    if (objectsFromScene[i].name.Contains("Quake Floor"))
                    {
                        GameObject.Destroy(objectsFromScene[i]);
                    }
                }
            }

            if ((destScene == "Ruins1_30" || destScene == "Ruins1_32") && Randomizer._quake1 == 0 && Randomizer._quake2 == 0 && PlayerData.instance.killedMageLord)
            {
                List<GameObject> objectsFromScene = SceneHandler.GetObjectsFromScene(destScene);
                for (int i = 0; i < objectsFromScene.Count; i++)
                {
                    if (objectsFromScene[i].name.Contains("Quake Floor"))
                    {
                        GameObject.Destroy(objectsFromScene[i]);
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

            if (destScene == "Crossroads_ShamanTemple")
            {
                foreach (GameObject obj in SceneHandler.GetObjectsFromScene("Crossroads_ShamanTemple"))
                {
                    if (obj.name == "Blocker")
                    {
                        PlayMakerFSM fsm = FSMUtility.LocateFSM(obj, "Blocker Control");

                        for (int i = 0; i < fsm.FsmStates.Length; i++)
                        {
                            if (fsm.FsmStates[i].Name == "Idle" || fsm.FsmStates[i].Name == "Shot Anim End")
                            {
                                List<FsmTransition> transList = new List<FsmTransition>();
                                foreach (FsmTransition trans in fsm.FsmStates[i].Transitions)
                                {
                                    if (trans.ToState != "Close")
                                    {
                                        transList.Add(trans);
                                    }
                                }
                                fsm.FsmStates[i].Transitions = transList.ToArray();
                            }
                        }

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
