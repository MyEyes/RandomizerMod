using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HutongGames.PlayMaker;
using GlobalEnums;

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
        public static bool swappedFireball;
        public static bool swappedQuake;
        public static bool swappedScream;
        public static bool randomizer;
        public static bool hardMode;
        public static int seed = -1;

        public static int _fireball1 = 0;
        public static int _quake1 = 0;
        public static int _scream1 = 0;
        public static int _fireball2 = 0;
        public static int _quake2 = 0;
        public static int _scream2 = 0;

        public static bool loadedSave = false;

        public static StreamWriter debugWriter = null;
        public static bool debug = false;

        public static bool xmlLoaded = false;

        public static int GetPlayerDataInt(string name)
        {
            PlayerData pd = PlayerData.instance;

            //Don't run randomizer code in non-randomizer saves
            if (!randomizer)
            {
                if (name.StartsWith("_"))
                {
                    name = name.Substring(1);
                }
                return pd.GetIntInternal(name);
            }

            if (string.IsNullOrEmpty(name))
            {
                return 0;
            }

            if (name == "_fireballLevel")
            {
                return _fireball1 + _fireball2;
            }
            else if (name == "_quakeLevel")
            {
                return _quake1 + _quake2;
            }
            else if (name == "_screamLevel")
            {
                return _scream1 + _scream2;
            }

            string key;
            string key2;

            //Don't run randomizer if int is not in the loaded data
            if (!reverseLookup.TryGetValue(name, out key) || !permutation.TryGetValue(key, out key2))
            {
                return pd.GetIntInternal(name);
            }
            else
            {
                int index = entries[key].GetIndex(name);
                RandomizerEntry randomizerEntry = entries[key2];

                RandomizerVar var;

                //Return the matching var or the first one if there is no matching index
                if (randomizerEntry.entries.Length > index)
                {
                    var = randomizerEntry.entries[index];
                }
                else
                {
                    var = randomizerEntry.entries[0];
                }

                if (var.type == typeof(bool))
                {
                    return pd.GetBoolInternal(var.name) ? 2 : 0;
                }
                else
                {
                    if (key2 == "Vengeful Spirit") return _fireball1 * 2;
                    else if (key2 == "Shade Soul") return _fireball2 * 2;
                    else if (key2 == "Desolate Dive") return _quake1 * 2;
                    else if (key2 == "Descending Dark") return _quake2 * 2;
                    else if (key2 == "Howling Wraiths") return _scream1 * 2;
                    else if (key2 == "Abyss Shriek") return _scream2 * 2;

                    return pd.GetIntInternal(var.name) >= (int)var.value ? 2 : 0;
                }
            }
        }

        //Used only for relics currently
        public static void SetPlayerDataInt(string name, int value)
        {
            PlayerData pd = PlayerData.instance;

            if (!randomizer)
            {
                PlayerData.instance.SetIntInternal(name, value);
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            string nameVal = name + value;

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

                int trinketNum = GetTrinketForScene();

                PlayerData.instance.SetIntInternal("trinket" + trinketNum, PlayerData.instance.GetIntInternal("trinket" + trinketNum) + 1);
                return;
            }


            //Begin copy/pasted code from set bool
            string key;
            string text;

            //Check if var is in data before running randomizer code
            if (reverseLookup.TryGetValue(nameVal, out key) && permutation.TryGetValue(key, out text))
            {
                //Randomizer breaks progression, so we need to ensure the player never gets shade cloak before mothwing cloak
                if (text == "Shade Cloak" && !pd.hasDash && !pd.canDash)
                {
                    Swap("Shade Cloak", "Mothwing Cloak");
                    text = "Mothwing Cloak";
                    swappedCloak = true;
                }

                //Similar checks for dream nail
                if (text == "Dream Gate" && !pd.hasDreamNail)
                {
                    Swap("Dream Nail", "Dream Gate");
                    text = "Dream Nail";
                    swappedGate = true;
                }

                if (text == "Awoken Dream Nail" && !pd.hasDreamNail)
                {
                    Swap("Dream Nail", "Awoken Dream Nail");
                    text = "Dream Nail";
                    swappedAwoken = true;
                }

                //Similar checks for spells
                if (text == "Shade Soul" && (_fireball1 + _fireball2) == 0)
                {
                    Swap("Vengeful Spirit", "Shade Soul");
                    text = "Vengeful Spirit";
                    swappedFireball = true;
                }

                if (text == "Descending Dark" && (_quake1 + _quake2) == 0)
                {
                    Swap("Desolate Dive", "Descending Dark");
                    text = "Desolate Dive";
                    swappedQuake = true;
                }

                if (text == "Abyss Shriek" && (_scream1 + _scream2) == 0)
                {
                    Swap("Howling Wraiths", "Abyss Shriek");
                    text = "Howling Wraiths";
                    swappedScream = true;
                }

                //FSM variable is probably tracked separately, need to make sure it's accurate
                if (name == "hasDreamGate" && !PlayerData.instance.hasDreamGate)
                {
                    FSMUtility.LocateFSM(HeroController.instance.gameObject, "Dream Nail").FsmVariables.GetFsmBool("Dream Warp Allowed").Value = false;
                }

                //Set all bools relating to the given entry
                for (int i = 0; i < entries[text].entries.Length; i++)
                {
                    RandomizerVar var = entries[text].entries[i];

                    if (var.type == typeof(bool))
                    {
                        pd.SetBoolInternal(var.name, value > 0);
                    }
                    else
                    {
                        if (text == "Vengeful Spirit") _fireball1 = value > 0 ? 1 : 0;
                        else if (text == "Shade Soul") _fireball2 = value > 0 ? 1 : 0;
                        else if (text == "Desolate Dive") _quake1 = value > 0 ? 1 : 0;
                        else if (text == "Descending Dark") _quake2 = value > 0 ? 1 : 0;
                        else if (text == "Howling Wraiths") _scream1 = value > 0 ? 1 : 0;
                        else if (text == "Abyss Shriek") _scream2 = value > 0 ? 1 : 0;
                    }

                    //FSM variable is probably tracked separately, need to make sure it's accurate
                    if (entries[text].entries[i].name == "hasDreamGate")
                    {
                        FSMUtility.LocateFSM(HeroController.instance.gameObject, "Dream Nail").FsmVariables.GetFsmBool("Dream Warp Allowed").Value = true;
                    }

                    //Need to make the charms page accessible if the player gets their first charm from a non-charm pickup
                    if (entries[text].type == RandomizerType.CHARM && entries[key].type != RandomizerType.CHARM) pd.hasCharm = true;
                }
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

            int modifiedSeed = seed;

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
	        if (!randomizer)
	        {
	            return pd.GetBoolInternal(name);
	        }

            if (GameManager.instance.GetSceneNameString() != "RestingGrounds_07" && GameManager.instance.GetSceneNameString() != "RestingGrounds_04")
            {
                if (name == "hasDreamGate" || name == "dreamNailUpgraded" || name == "hasDreamNail")
                {
                    return pd.GetBoolInternal(name);
                }
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
            if (!reverseLookup.TryGetValue(name, out key) || !permutation.TryGetValue(key, out key2))
            {
                return pd.GetBoolInternal(name);
            }
            else
            {
                int index = entries[key].GetIndex(name);
                RandomizerEntry randomizerEntry = entries[key2];

                RandomizerVar var;

                //Return the matching bool or the first one if there is no matching index
                if (randomizerEntry.entries.Length > index)
                {
                    var = randomizerEntry.entries[index];
                }
                else
                {
                    var = randomizerEntry.entries[0];
                }

                if (var.type == typeof(bool))
                {
                    return pd.GetBoolInternal(var.name);
                }
                else
                {
                    if (key2 == "Vengeful Spirit") return _fireball1 > 0;
                    else if (key2 == "Shade Soul") return _fireball2 > 0;
                    else if (key2 == "Desolate Dive") return _quake1 > 0;
                    else if (key2 == "Descending Dark") return _quake2 > 0;
                    else if (key2 == "Howling Wraiths") return _scream1 > 0;
                    else if (key2 == "Abyss Shriek") return _scream2 > 0;
                    return pd.GetIntInternal(var.name) >= (int)var.value;
                }
            }
        }

        //Override for PlayerData.SetBool
        public static void SetPlayerDataBool(string name, bool val)
        {
            PlayerData pd = PlayerData.instance;

            //Don't run randomizer code in non-randomizer saves
	        if (!randomizer)
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
            if (reverseLookup.TryGetValue(name, out key) && permutation.TryGetValue(key, out text))
            {
                //Randomizer breaks progression, so we need to ensure the player never gets shade cloak before mothwing cloak
                if (text == "Shade Cloak" && !pd.hasDash && !pd.canDash)
                {
                    Swap("Shade Cloak", "Mothwing Cloak");
                    text = "Mothwing Cloak";
                    swappedCloak = true;
                }

                //Similar checks for dream nail
                if (text == "Dream Gate" && !pd.hasDreamNail)
                {
                    Swap("Dream Nail", "Dream Gate");
                    text = "Dream Nail";
                    swappedGate = true;
                }

                if (text == "Awoken Dream Nail" && !pd.hasDreamNail)
                {
                    Swap("Dream Nail", "Awoken Dream Nail");
                    text = "Dream Nail";
                    swappedAwoken = true;
                }

                //Similar checks for spells
                if (text == "Shade Soul" && (_fireball1 + _fireball2) == 0)
                {
                    Swap("Vengeful Spirit", "Shade Soul");
                    text = "Vengeful Spirit";
                    swappedFireball = true;
                }

                if (text == "Descending Dark" && (_quake1 + _quake2) == 0)
                {
                    Swap("Desolate Dive", "Descending Dark");
                    text = "Desolate Dive";
                    swappedQuake = true;
                }

                if (text == "Abyss Shriek" && (_scream1 + _scream2) == 0)
                {
                    Swap("Howling Wraiths", "Abyss Shriek");
                    text = "Howling Wraiths";
                    swappedScream = true;
                }

                //FSM variable is probably tracked separately, need to make sure it's accurate
                if (name == "hasDreamGate" && !PlayerData.instance.hasDreamGate)
                {
                    FSMUtility.LocateFSM(HeroController.instance.gameObject, "Dream Nail").FsmVariables.GetFsmBool("Dream Warp Allowed").Value = false;
                }

                //Set all bools relating to the given entry
                for (int i = 0; i < entries[text].entries.Length; i++)
                {
                    RandomizerVar var = entries[text].entries[i];

                    if (var.type == typeof(bool))
                    {
                        pd.SetBoolInternal(var.name, val);
                    }
                    else
                    {
                        if (text == "Vengeful Spirit") _fireball1 = val ? 1 : 0;
                        else if (text == "Shade Soul") _fireball2 = val ? 1 : 0;
                        else if (text == "Desolate Dive") _quake1 = val ? 1 : 0;
                        else if (text == "Descending Dark") _quake2 = val ? 1 : 0;
                        else if (text == "Howling Wraiths") _scream1 = val ? 1 : 0;
                        else if (text == "Abyss Shriek") _scream2 = val ? 1 : 0;
                    }

                    //FSM variable is probably tracked separately, need to make sure it's accurate
                    if (entries[text].entries[i].name == "hasDreamGate")
                    {
                        FSMUtility.LocateFSM(HeroController.instance.gameObject, "Dream Nail").FsmVariables.GetFsmBool("Dream Warp Allowed").Value = true;
                    }

                    //Need to make the charms page accessible if the player gets their first charm from a non-charm pickup
                    if (entries[text].type == RandomizerType.CHARM && entries[key].type != RandomizerType.CHARM) pd.hasCharm = true;
                }
                return;
            }

            pd.SetBoolInternal(name, val);
        }

        //Adds data to the randomizer dictionaries
        public static void AddEntry(XmlNode node, bool permadeath)
        {
            RandomizerEntry entry = new RandomizerEntry(node);

            if (!entries.ContainsKey(entry.name))
            {
                entries.Add(entry.name, entry);

                //Build reverse lookup list for quickly finding pickup name from attributes
                for (int i = 0; i < entry.entries.Length; i++)
                {
                    string val = entry.entries[i].value == null ? "" : entry.entries[i].value.ToString();

                    reverseLookup.Add(entry.entries[i].name + val, entry.name);
                }

                for (int i = 0; i < entry.localeNames.Length; i++)
                {
                    if (i != 1 && i != 2)
                    {
                        reverseLookup.Add(entry.localeNames[i], entry.name);
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
                permutation.Clear();

                List<string> list = new List<string>();
                List<string> list2 = new List<string>();
                List<string> list3 = new List<string>();

                list.AddRange(entries.Keys);
                list2.AddRange(entries.Keys);

                //Loop until all items have been added to randomizer
                while (list.Count > 0)
                {
                    flag = false;

                    //Check for reachable pickups and assign them a random item
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (IsReachable(list3, entries[list[i]]))
                        {
                            flag = true;

                            bool itemPicked = false;
                            int index = -1;

                            //Terrible way to make abilities show up less frequently
                            while (!itemPicked)
                            {
                                itemPicked = true;
                                index = random.Next(list2.Count);

                                if (entries[list2[index]].type == RandomizerType.ABILITY && random.Next(1, 4) != 1)
                                {
                                    itemPicked = false;
                                }
                            }

                            permutation.Add(list[i], list2[index]);
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

            foreach (KeyValuePair<string, string> perm in permutation)
            {
                Modding.ModHooks.ModLog("[RANDOMIZER] " + perm.Key + " = " + perm.Value);
            }
        }

        //Checks requirements to see if an entry is reachable
        public static bool IsReachable(List<string> reachable, RandomizerEntry entry)
        {
            //Loop through requirement sets
            for (int i = 0; i < entry.GetRequires().Length; i++)
            {
                bool flag = true;

                //Loop through requirements in sets
                for (int j = 0; j < entry.GetRequires()[i].Length; j++)
                {
                    if (!reachable.Contains(entry.GetRequires()[i][j]))
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
            return entry.GetRequires().Length == 0;
        }

        //Swap two given entries
        public static void Swap(string entry1, string entry2)
        {
            try
            {
                string key = permutation.FirstOrDefault((KeyValuePair<string, string> x) => x.Value == entry1).Key;
                string key2 = permutation.FirstOrDefault((KeyValuePair<string, string> x) => x.Value == entry2).Key;
                permutation[key] = entry2;
                permutation[key2] = entry1;
            }
            catch (Exception)
            {
                Modding.ModHooks.ModLog("[RANDOMIZER] Could not swap entries " + entry1 + " and " + entry2);
            }
        }

        //Write randomizer save to file if applicable
        //TODO: Hook GameManager.SaveGame to write save to the same file as everything else
        public static void SaveGame(int profileId)
        {
            if (randomizer)
            {
                using (StreamWriter streamWriter = new StreamWriter(Application.persistentDataPath + @"\user" + profileId + ".rnd"))
                {
                    streamWriter.WriteLine(seed);
                    streamWriter.WriteLine(swappedCloak);
                    streamWriter.WriteLine(hardMode);
                    streamWriter.WriteLine(swappedGate);
                    streamWriter.WriteLine(swappedAwoken);
                    streamWriter.WriteLine(swappedFireball);
                    streamWriter.WriteLine(swappedQuake);
                    streamWriter.WriteLine(swappedScream);
                    streamWriter.WriteLine(_fireball1);
                    streamWriter.WriteLine(_fireball2);
                    streamWriter.WriteLine(_quake1);
                    streamWriter.WriteLine(_quake2);
                    streamWriter.WriteLine(_scream1);
                    streamWriter.WriteLine(_scream2);
                }
            }
        }

        //Load randomizer save from file if applicable
        public static void LoadGame(int profileId)
        {
            randomizer = false;
            hardMode = false;
            loadedSave = true;

            if (File.Exists(Application.persistentDataPath + @"\user" + profileId + ".rnd"))
            {
                using (StreamReader streamReader = new StreamReader(Application.persistentDataPath + @"\user" + profileId + ".rnd"))
                {
                    seed = Convert.ToInt32(streamReader.ReadLine());
                    swappedCloak = Convert.ToBoolean(streamReader.ReadLine());
                    hardMode = Convert.ToBoolean(streamReader.ReadLine());
                    swappedGate = Convert.ToBoolean(streamReader.ReadLine());
                    swappedAwoken = Convert.ToBoolean(streamReader.ReadLine());
                    swappedFireball = Convert.ToBoolean(streamReader.ReadLine());
                    swappedQuake = Convert.ToBoolean(streamReader.ReadLine());
                    swappedScream = Convert.ToBoolean(streamReader.ReadLine());
                    _fireball1 = Convert.ToInt32(streamReader.ReadLine());
                    _fireball2 = Convert.ToInt32(streamReader.ReadLine());
                    _quake1 = Convert.ToInt32(streamReader.ReadLine());
                    _quake2 = Convert.ToInt32(streamReader.ReadLine());
                    _scream1 = Convert.ToInt32(streamReader.ReadLine());
                    _scream2 = Convert.ToInt32(streamReader.ReadLine());
                }

                SetHardMode(hardMode);
                Randomize(new System.Random(seed));

                //Swap cloaks if player picked up shade cloak first
                if (swappedCloak)
                {
                    Swap("Mothwing Cloak", "Shade Cloak");
                }

                //Similar checks for dream nail
                if (swappedGate)
                {
                    Swap("Dream Nail", "Dream Gate");
                }

                if (swappedAwoken)
                {
                    Swap("Dream Nail", "Awoken Dream Nail");
                }

                //Similar checks for spells
                if (swappedFireball)
                {
                    Swap("Vengeful Spirit", "Shade Soul");
                }

                if (swappedQuake)
                {
                    Swap("Desolate Dive", "Descending Dark");
                }

                if (swappedScream)
                {
                    Swap("Howling Wraiths", "Abyss Shriek");
                }

                randomizer = true;
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

            if (randomizer)
            {
                if (seed == -1)
                {
                    seed = new System.Random().Next();
                }

                swappedCloak = false;
                swappedAwoken = false;
                swappedCloak = false;
                swappedGate = false;
                swappedFireball = false;
                swappedQuake = false;
                swappedScream = false;
                _fireball1 = 0;
                _fireball2 = 0;
                _quake1 = 0;
                _quake2 = 0;
                _scream1 = 0;
                _scream2 = 0;

                SetHardMode(hardMode);
                Randomize(new System.Random(seed));

                //permutation.Add("Isma's Tear", "Fury of the Fallen");
                //permutation.Add("Fury of the Fallen", "Isma's Tear");
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
            hardMode = hard;

            //Log any errors that occur
            if (!xmlLoaded)
            {
                try
                {
                    LoadEntriesFromXML(hard, PlayerData.instance.permadeathMode > 0);
                    xmlLoaded = true;
                }
                catch (Exception e)
                {
                    Modding.ModHooks.ModLog("[RANDOMIZER] Failed to load XML:\n" + e.ToString());
                }
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

            LoadEntries(rnd.SelectSingleNode("randomizer"), permadeath);
        }

        //Add entry for each node
        public static void LoadEntries(XmlNode nodes, bool permadeath)
        {
            foreach (XmlNode node in nodes.SelectNodes("entry"))
            {
                AddEntry(node, permadeath);
            }
        }

        //Log to file
        public static void DebugLog(string message)
        {
            if (!debug)
            {
                return;
            }
            if (debugWriter == null)
            {
                debugWriter = new StreamWriter(Application.persistentDataPath + "\\txt", true);
                debugWriter.AutoFlush = true;
            }
            if (debugWriter != null)
            {
                debugWriter.WriteLine(message);
                debugWriter.Flush();
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
