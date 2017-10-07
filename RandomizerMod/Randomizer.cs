using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Linq;
using UnityEngine;
using HutongGames.PlayMaker;

namespace RandomizerMod
{
    public static class Randomizer
    {
        public static Dictionary<string, RandomizerEntry> entries = new Dictionary<string, RandomizerEntry>();
        public static Dictionary<string, string> reverseLookup = new Dictionary<string, string>();
        public static Dictionary<string, string> permutation = new Dictionary<string, string>();

        public static bool swappedCloak;
        public static bool swappedGate;
        public static bool swappedAwoken;
        public static bool randomizer;
        public static bool hardMode;
        public static int seed = -1;

        public static bool loadedSave = false;

        public static StreamWriter debugWriter = null;
        public static bool debug = false;

        //GetInt placeholder, currently unused
        public static int GetPlayerDataInt(string name)
        {
            return PlayerData.instance.GetIntInternal(name);
        }

        //Used only for relics currently
        public static void SetPlayerDataInt(string name, int value)
        {
            if (!Randomizer.randomizer)
            {
                PlayerData.instance.SetIntInternal(name, value);
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (name == "trinket1" || name == "trinket2" || name == "trinket3" || name == "trinket4")
            {
                //It would be cleaner to override this in SetPlayerDataBool, but this works just as well
                PlayerData.instance.SetBoolInternal("foundTrinket1", true);
                PlayerData.instance.SetBoolInternal("foundTrinket2", true);
                PlayerData.instance.SetBoolInternal("foundTrinket3", true);
                PlayerData.instance.SetBoolInternal("foundTrinket4", true);
                PlayerData.instance.SetBoolInternal("noTrinket1", false);
                PlayerData.instance.SetBoolInternal("noTrinket2", false);
                PlayerData.instance.SetBoolInternal("noTrinket3", false);
                PlayerData.instance.SetBoolInternal("noTrinket4", false);

                //Make sure the change is +1 so we don't randomize selling trinkets to Lemm
                int change = value - PlayerData.instance.GetIntInternal(name);

                if (change != 1)
                {
                    PlayerData.instance.SetIntInternal(name, value);
                    return;
                }

                int trinketNum = Randomizer.GetTrinketForScene();

                PlayerData.instance.SetIntInternal("trinket" + trinketNum, PlayerData.instance.GetIntInternal("trinket" + trinketNum) + 1);
                return;
            }

            PlayerData.instance.SetIntInternal(name, value);
        }

        //Randomize trinkets based on scene name and seed
        public static int GetTrinketForScene()
        {
            //Adding all chars from scene name to seed works well enough because there's only two places with multiple trinkets in a scene
            char[] sceneCharArray = GameManager.instance.GetSceneNameString().ToCharArray();
            int[] sceneNumbers = sceneCharArray.Select(c => Convert.ToInt32(c)).ToArray();

            int modifiedSeed = Randomizer.seed;

            for (int i = 0; i < sceneNumbers.Length; i++)
            {
                modifiedSeed += sceneNumbers[i];
            }

            //Total trinket count: 14 / 16 / 7 / 4, using those values to get mostly accurate randomization, rather than truely moving them around
            int trinketNum = new System.Random(modifiedSeed).Next(1, 42);

            if (trinketNum <= 14)
            {
                return 1;
            }
            else if (trinketNum <= (14 + 16))
            {
                return 2;
            }
            else if (trinketNum <= (14 + 16 + 7))
            {
                return 3;
            }
            else
            {
                return 4;
            }
        }

        //Override for PlayerData.GetBool
        public static bool GetPlayerDataBool(string name)
        {
            PlayerData pd = PlayerData.instance;

            //Don't run randomizer code in non-randomizer saves
	        if (!Randomizer.randomizer)
	        {
	            return pd.GetBoolInternal(name);
	        }

            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            //Check stack trace to see if player is in a menu
            string stack = new StackTrace().ToString();

            //Split into multiple ifs because this looks horrible otherwise
            //TODO: Cleaner way of checking than stack trace
            if (!stack.Contains("at ShopMenuStock.BuildItemList()"))
            {
                if (stack.Contains("at HutongGames.PlayMaker.Fsm.Start()"))
                {
                    return pd.GetBoolInternal(name);
                }

                if (name.Contains("gotCharm_") && stack.Contains("at HutongGames.PlayMaker.Fsm.DoTransition(HutongGames.PlayMaker.FsmTransition transition, Boolean isGlobal)"))
                {
                    return pd.GetBoolInternal(name);
                }
            }

            string key;
            string key2;

            //Don't run randomizer if bool is not in the loaded data
            if (!Randomizer.reverseLookup.TryGetValue(name, out key) || !Randomizer.permutation.TryGetValue(key, out key2))
            {
                return pd.GetBoolInternal(name);
            }
            else
            {
                int index = Randomizer.entries[key].GetIndex(name);
                RandomizerEntry randomizerEntry = Randomizer.entries[key2];

                //Return the matching bool or the first one if there is no matching index
                if (randomizerEntry.entries.Length > index)
                {
                    return pd.GetBoolInternal(randomizerEntry.entries[index]);
                }
                else
                {
                    return pd.GetBoolInternal(randomizerEntry.entries[0]);
                }
            }
        }

        //Override for PlayerData.SetBool
        public static void SetPlayerDataBool(string name, bool val)
        {
            PlayerData pd = PlayerData.instance;

            //Don't run randomizer code in non-randomizer saves
	        if (!Randomizer.randomizer)
	        {
	            pd.SetBoolInternal(name, val);
                return;
	        }

            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            
            string key;
            string text;

            //Check if bool is in data before running randomizer code
            if (Randomizer.reverseLookup.TryGetValue(name, out key) && Randomizer.permutation.TryGetValue(key, out text))
            {
                //Randomizer breaks progression, so we need to ensure the player never gets shade cloak before mothwing cloak
                if (text == "Shade Cloak" && !pd.hasDash && !pd.canDash)
                {
                    Randomizer.Swap("Shade Cloak", "Mothwing Cloak");
                    text = "Mothwing Cloak";
                    Randomizer.swappedCloak = true;
                }

                //Similar checks for dream nail
                if (text == "Dream Gate" && !pd.hasDreamNail)
                {
                    Randomizer.Swap("Dream Nail", "Dream Gate");
                    text = "Dream Nail";
                    Randomizer.swappedGate = true;
                }

                if (text == "Awoken Dream Nail" && !pd.hasDreamNail)
                {
                    Randomizer.Swap("Dream Nail", "Awoken Dream Nail");
                    text = "Dream Nail";
                    Randomizer.swappedAwoken = true;
                }

                //Set all bools relating to the given entry
                for (int i = 0; i < Randomizer.entries[text].entries.Length; i++)
                {
                    pd.SetBoolInternal(Randomizer.entries[text].entries[i], val);

                    //Need to make the charms page accessible if the player gets their first charm from a non-charm pickup
                    if (Randomizer.entries[text].type == RandomizerType.CHARM && Randomizer.entries[key].type != RandomizerType.CHARM) pd.hasCharm = true;
                }
                return;
            }

            pd.SetBoolInternal(name, val);
        }

        //Adds data to the randomizer dictionaries
        public static void AddEntry(XmlNode node, bool permadeath)
        {
            if (!permadeath || Convert.ToBoolean(node.SelectSingleNode("permadeath").InnerText))
            {
                RandomizerEntry entry = new RandomizerEntry(node);

                if (!Randomizer.entries.ContainsKey(entry.name))
                {
                    Randomizer.entries.Add(entry.name, entry);

                    //Build reverse lookup list for quickly finding pickup name from attributes
                    for (int i = 0; i < entry.entries.Length; i++)
                    {
                        Randomizer.reverseLookup.Add(entry.entries[i], entry.name);
                    }

                    for (int i = 0; i < entry.localeNames.Length; i++)
                    {
                        //TODO: Parse duplicate entries properly
                        if (i != 1 && i != 2)
                        {
                            Randomizer.reverseLookup.Add(entry.localeNames[i], entry.name);
                        }
                    }
                }
            }
        }

        //Randomization algorithm
        public static void Randomize(System.Random random)
        {
            bool flag = false;

            //Loop until a permutation with all items reachable is found
            while (!flag)
            {
                Randomizer.permutation.Clear();

                List<string> list = new List<string>();
                List<string> list2 = new List<string>();
                List<string> list3 = new List<string>();

                list.AddRange(Randomizer.entries.Keys);
                list2.AddRange(Randomizer.entries.Keys);

                //Loop until all items have been added to randomizer
                while (list.Count > 0)
                {
                    flag = false;

                    //Check for reachable pickups and assign them a random item
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (Randomizer.IsReachable(list3, Randomizer.entries[list[i]]))
                        {
                            flag = true;

                            bool itemPicked = false;
                            int index = -1;

                            //Terrible way to make abilities show up less frequently
                            while (!itemPicked)
                            {
                                itemPicked = true;
                                index = random.Next(list2.Count);

                                if (Randomizer.entries[list2[index]].type == RandomizerType.ABILITY && random.Next(1, 4) != 1)
                                {
                                    itemPicked = false;
                                }
                            }

                            Randomizer.permutation.Add(list[i], list2[index]);
                            list3.Add(list2[index]);
                            list.RemoveAt(i);
                            i--;
                            list2.RemoveAt(index);
                        }
                    }

                    //Break loop and try again if no items are reachable
                    if (!flag)
                    {
                        break;
                    }
                }
            }
        }

        //Checks requirements to see if an entry is reachable
        public static bool IsReachable(List<string> reachable, RandomizerEntry entry)
        {
            //Loop through requirement sets
            for (int i = 0; i < entry.requires.Length; i++)
            {
                bool flag = true;

                //Loop through requirements in sets
                for (int j = 0; j < entry.requires[i].Length; j++)
                {
                    if (!reachable.Contains(entry.requires[i][j]))
                    {
                        flag = false;
                        break;
                    }
                }

                //Return true if all requirements in set are met
                if (flag)
                {
                    return true;
                }
            }

            //Check for pickups with no requirements
            return entry.requires.Length == 0;
        }

        //Swap two given entries
        public static void Swap(string entry1, string entry2)
        {
            try
            {
                string key = Randomizer.permutation.FirstOrDefault((KeyValuePair<string, string> x) => x.Value == entry1).Key;
                string key2 = Randomizer.permutation.FirstOrDefault((KeyValuePair<string, string> x) => x.Value == entry2).Key;
                Randomizer.permutation[key] = entry2;
                Randomizer.permutation[key2] = entry1;
            }
            catch (Exception)
            {
                Randomizer.DebugLog("Could not swap entries " + entry1 + " and " + entry2);
            }
        }

        //Write randomizer save to file if applicable
        //TODO: Hook GameManager.SaveGame to write save to the same file as everything else
        public static void SaveGame(int profileId)
        {
            if (Randomizer.randomizer)
            {
                using (StreamWriter streamWriter = new StreamWriter(Application.persistentDataPath + @"\user" + profileId + ".rnd"))
                {
                    streamWriter.WriteLine(Randomizer.seed);
                    streamWriter.WriteLine(Randomizer.swappedCloak);
                    streamWriter.WriteLine(Randomizer.hardMode);
                    streamWriter.WriteLine(Randomizer.swappedGate);
                    streamWriter.WriteLine(Randomizer.swappedAwoken);
                }
            }
        }

        //Load randomizer save from file if applicable
        public static void LoadGame(int profileId)
        {
            Randomizer.randomizer = false;
            Randomizer.hardMode = false;
            Randomizer.loadedSave = true;

            if (File.Exists(Application.persistentDataPath + @"\user" + profileId + ".rnd"))
            {
                using (StreamReader streamReader = new StreamReader(Application.persistentDataPath + @"\user" + profileId + ".rnd"))
                {
                    Randomizer.seed = Convert.ToInt32(streamReader.ReadLine());
                    Randomizer.swappedCloak = Convert.ToBoolean(streamReader.ReadLine());
                    Randomizer.hardMode = Convert.ToBoolean(streamReader.ReadLine());
                    Randomizer.swappedGate = Convert.ToBoolean(streamReader.ReadLine());
                    Randomizer.swappedAwoken = Convert.ToBoolean(streamReader.ReadLine());
                }

                Randomizer.SetHardMode(Randomizer.hardMode);
                Randomizer.Randomize(new System.Random(Randomizer.seed));

                //Swap cloaks if player picked up shade cloak first
                if (Randomizer.swappedCloak)
                {
                    Randomizer.Swap("Mothwing Cloak", "Shade Cloak");
                }

                //Similar checks for dream nail
                if (Randomizer.swappedGate)
                {
                    Randomizer.Swap("Dream Nail", "Dream Gate");
                }

                if (Randomizer.swappedAwoken)
                {
                    Randomizer.Swap("Dream Nail", "Awoken Dream Nail");
                }

                Randomizer.randomizer = true;
            }
        }

        //Set up randomization if applicable
        public static void NewGame()
        {
            /*PlayerData.instance.hasWalljump = true;
            PlayerData.instance.canWallJump = true;
            PlayerData.instance.hasDash = true;
            PlayerData.instance.canDash = true;
            PlayerData.instance.hasDoubleJump = true;
            PlayerData.instance.hasSuperDash = true;
            PlayerData.instance.canSuperDash = true;
            PlayerData.instance.hasShadowDash = true;
            PlayerData.instance.canShadowDash = true;
            PlayerData.instance.fireballLevel = 2;
            PlayerData.instance.screamLevel = 2;
            PlayerData.instance.quakeLevel = 2;*/

            if (Randomizer.randomizer)
            {
                if (Randomizer.seed == -1)
                {
                    Randomizer.seed = new System.Random().Next();
                }

                Randomizer.swappedCloak = false;

                Randomizer.SetHardMode(Randomizer.hardMode);
                Randomizer.Randomize(new System.Random(Randomizer.seed));

                //Randomizer.permutation.Add("Isma's Tear", "Fury of the Fallen");
                //Randomizer.permutation.Add("Fury of the Fallen", "Isma's Tear");
            }
        }

        //Delete randomizer save if applicable
        public static void DeleteGame(int profileId)
        {
            if (File.Exists(Application.persistentDataPath + @"\user" + profileId + ".rnd"))
            {
                File.Delete(Application.persistentDataPath + @"\user" + profileId + ".rnd");
            }
        }

        //Load entries for the given mode
        public static void SetHardMode(bool hard)
        {
            //Clear everything beforehand just in case
            Randomizer.hardMode = hard;
            Randomizer.permutation.Clear();
            Randomizer.reverseLookup.Clear();
            Randomizer.entries.Clear();

            //Log any errors that occur
            try
            {
                //TODO: Cleaner implementation than reloading entries every new game/load
                Randomizer.LoadEntriesFromXML(hard, PlayerData.instance.permadeathMode > 0);
            }
            catch (Exception e)
            {
                Randomizer.DebugLog(e.ToString());
            }
        }

        //Loads all entries from XML
        //Entries are assumed to be formatted properly, malformatted XML will likely cause a crash
        public static void LoadEntriesFromXML(bool hard, bool permadeath)
        {
            if (!File.Exists(@"Randomizer\randomizer.xml"))
            {
                return;
            }

            XmlDocument rnd = new XmlDocument();
            rnd.Load(@"Randomizer\randomizer.xml");

            //Load hard mode first because duplicate entries are ignored
            if (hard)
            {
                Randomizer.LoadEntries(rnd.SelectSingleNode("randomizer/hard"), permadeath);
            }

            Randomizer.LoadEntries(rnd.SelectSingleNode("randomizer/easy"), permadeath);
        }

        //Add entry for each node
        public static void LoadEntries(XmlNode nodes, bool permadeath)
        {
            foreach (XmlNode node in nodes.SelectNodes("entry"))
            {
                Randomizer.AddEntry(node, permadeath);
            }
        }

        //Log to file
        public static void DebugLog(string message)
        {
            if (!Randomizer.debug)
            {
                return;
            }
            if (Randomizer.debugWriter == null)
            {
                Randomizer.debugWriter = new StreamWriter(Application.persistentDataPath + "\\randomizer.txt", true);
                Randomizer.debugWriter.AutoFlush = true;
            }
            if (Randomizer.debugWriter != null)
            {
                Randomizer.debugWriter.WriteLine(message);
                Randomizer.debugWriter.Flush();
            }
        }

        //Checks if player is in inventory
        //None of this should ever be null, but checking just in case
        public static bool InInventory()
        {
            GameObject invTop = GameObject.FindGameObjectWithTag("Inventory Top");

            if (invTop != null)
            {
                PlayMakerFSM invFSM = invTop.GetComponent<PlayMakerFSM>();

                if (invFSM != null)
                {
                    FsmBool invOpen = invFSM.FsmVariables.GetFsmBool("Open");

                    if (invOpen != null)
                    {
                        return invOpen.Value;
                    }
                }
            }
            return false;
        }
    }
}
