using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Linq;
using System.Reflection;
using System.Web.Extensions;
using UnityEngine;
using HutongGames.PlayMaker;
using GlobalEnums;
using HutongGames.PlayMaker.Actions;
using Modding;

namespace RandomizerMod
{
    public static class Randomizer
    {
        public static Dictionary<string, RandomizerEntry> entries = new Dictionary<string, RandomizerEntry>();
        public static Dictionary<string, string> reverseLookup = new Dictionary<string, string>();

        public static string keyItems;

        public static bool loadedSave;
        public static bool xmlLoaded;

        public static int GetPlayerDataInt(string name)
        {
            PlayerData pd = PlayerData.instance;

            //Don't run randomizer code in non-randomizer saves
            if (!RandomizerMod.instance.Settings.randomizer)
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
            if (name == "_true")
            {
                return 2;
            }
            else if (name == "_fireballLevel")
            {
                return RandomizerMod.instance.Settings.FireballLevel();
            }
            else if (name == "_quakeLevel")
            {
                return RandomizerMod.instance.Settings.QuakeLevel();
            }
            else if (name == "_screamLevel")
            {
                return RandomizerMod.instance.Settings.ScreamLevel();
            }

            string key;
            string key2;

            //Don't run randomizer if int is not in the loaded data
            if (!reverseLookup.TryGetValue(name, out key) || !RandomizerMod.instance.Settings.StringValues.TryGetValue(key, out key2))
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
                    if (key2 == "Vengeful Spirit") return Convert.ToInt32(RandomizerMod.instance.Settings.fireball1) * 2;
                    else if (key2 == "Shade Soul") return Convert.ToInt32(RandomizerMod.instance.Settings.fireball2) * 2;
                    else if (key2 == "Desolate Dive") return Convert.ToInt32(RandomizerMod.instance.Settings.quake1) * 2;
                    else if (key2 == "Descending Dark") return Convert.ToInt32(RandomizerMod.instance.Settings.quake2) * 2;
                    else if (key2 == "Howling Wraiths") return Convert.ToInt32(RandomizerMod.instance.Settings.scream1) * 2;
                    else if (key2 == "Abyss Shriek") return Convert.ToInt32(RandomizerMod.instance.Settings.scream2) * 2;

                    return pd.GetIntInternal(var.name) >= (int)var.value ? 2 : 0;
                }
            }
        }

        //Used only for relics currently
        public static void SetPlayerDataInt(string name, int value)
        {
            PlayerData pd = PlayerData.instance;

            if (!RandomizerMod.instance.Settings.randomizer)
            {
                SetIntInternal(name, value);
                
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
                    SetIntInternal(name, value);
                    return;
                }

                int trinketNum = GetTrinketForScene();

                SetIntInternal("trinket" + trinketNum, PlayerData.instance.GetIntInternal("trinket" + trinketNum) + 1);
                return;
            }


            //Begin copy/pasted code from set bool
            string key;
            string text;

            //Check if var is in data before running randomizer code
            if (reverseLookup.TryGetValue(nameVal, out key) && RandomizerMod.instance.Settings.StringValues.TryGetValue(key, out text))
            {
                //Randomizer breaks progression, so we need to ensure the player never gets shade cloak before mothwing cloak
                if (text == "Shade Cloak" && !pd.hasDash && !pd.canDash)
                {
                    RandomizerMod.instance.Settings.Swap("Shade Cloak", "Mothwing Cloak");
                    text = "Mothwing Cloak";
                }

                //Similar checks for dream nail
                if (text == "Dream Gate" && !pd.hasDreamNail)
                {
                    RandomizerMod.instance.Settings.Swap("Dream Nail", "Dream Gate");
                    text = "Dream Nail";
                }

                if (text == "Awoken Dream Nail" && !pd.hasDreamNail)
                {
                    RandomizerMod.instance.Settings.Swap("Dream Nail", "Awoken Dream Nail");
                    text = "Dream Nail";
                }

                //Similar checks for spells
                if (text == "Shade Soul" && RandomizerMod.instance.Settings.FireballLevel() == 0)
                {
                    RandomizerMod.instance.Settings.Swap("Vengeful Spirit", "Shade Soul");
                    text = "Vengeful Spirit";
                }

                if (text == "Descending Dark" && RandomizerMod.instance.Settings.QuakeLevel() == 0)
                {
                    RandomizerMod.instance.Settings.Swap("Desolate Dive", "Descending Dark");
                    text = "Desolate Dive";
                }

                if (text == "Abyss Shriek" && RandomizerMod.instance.Settings.ScreamLevel() == 0)
                {
                    RandomizerMod.instance.Settings.Swap("Howling Wraiths", "Abyss Shriek");
                    text = "Howling Wraiths";
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
                        if (text == "Vengeful Spirit") RandomizerMod.instance.Settings.fireball1 = value > 0;
                        else if (text == "Shade Soul") RandomizerMod.instance.Settings.fireball2 = value > 0;
                        else if (text == "Desolate Dive") RandomizerMod.instance.Settings.quake1 = value > 0;
                        else if (text == "Descending Dark") RandomizerMod.instance.Settings.quake2 = value > 0;
                        else if (text == "Howling Wraiths") RandomizerMod.instance.Settings.scream1 = value > 0;
                        else if (text == "Abyss Shriek") RandomizerMod.instance.Settings.scream2 = value > 0;
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

            SetIntInternal(name, value);
        }


        private static void SetIntInternal(string name, int value)
        {
            PlayerData.instance.SetIntInternal(name, value);

            //If other mods want to be compatible with Randomizer logic, but don't want to implement randomizer logic, that can find out what's set this way.
            if (_SetPlayerIntHook != null)
            {
                try
                {
                    _SetPlayerIntHook(name, value);
                }
                catch (Exception ex)
                {
                    RandomizerMod.instance.LogError(ex);
                }
            }
        }

        //Randomize trinkets based on scene name and seed
        public static int GetTrinketForScene()
        {
            //Adding all chars from scene name to seed works well enough because there's only two places with multiple trinkets in a scene
            char[] sceneCharArray = GameManager.instance.GetSceneNameString().ToCharArray();
            int[] sceneNumbers = sceneCharArray.Select(c => Convert.ToInt32(c)).ToArray();

            int modifiedSeed = RandomizerMod.instance.Settings.seed;

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
	        if (!RandomizerMod.instance.Settings.randomizer)
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

            if (name == "_true")
            {
                return true;
            }
            else if (name == "_false")
            {
                return false;
            }
            else if (name == "hasAcidArmour")
            {
                return GameManager.instance.GetSceneNameString() == "Waterways_13" ? false : PlayerData.instance.hasAcidArmour;
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

                if (name.Contains("gotCharm_") && (stack.Contains("at HutongGames.PlayMaker.Fsm.DoTransition(HutongGames.PlayMaker.FsmTransition transition, Boolean isGlobal)") || InInventory()))
                {
                    return pd.GetBoolInternal(name);
                }
            }

            string key;
            string key2;

            //Don't run randomizer if bool is not in the loaded data
            if (!reverseLookup.TryGetValue(name, out key) || !RandomizerMod.instance.Settings.StringValues.TryGetValue(key, out key2))
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
                    if (key2 == "Vengeful Spirit") return RandomizerMod.instance.Settings.fireball1;
                    else if (key2 == "Shade Soul") return RandomizerMod.instance.Settings.fireball2;
                    else if (key2 == "Desolate Dive") return RandomizerMod.instance.Settings.quake1;
                    else if (key2 == "Descending Dark") return RandomizerMod.instance.Settings.quake2;
                    else if (key2 == "Howling Wraiths") return RandomizerMod.instance.Settings.scream1;
                    else if (key2 == "Abyss Shriek") return RandomizerMod.instance.Settings.scream2;
                    return pd.GetIntInternal(var.name) >= (int)var.value;
                }
            }
        }

        //Override for PlayerData.SetBool
        public static void SetPlayerDataBool(string name, bool val)
        {
            PlayerData pd = PlayerData.instance;

            //Don't run randomizer code in non-randomizer saves
	        if (!RandomizerMod.instance.Settings.randomizer)
	        {
	            SetBoolInternal(name, val);

                return;
	        }

            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            
            string key;
            string text;

            //Check if bool is in data before running randomizer code
            if (reverseLookup.TryGetValue(name, out key) && RandomizerMod.instance.Settings.StringValues.TryGetValue(key, out text))
            {
                //Randomizer breaks progression, so we need to ensure the player never gets shade cloak before mothwing cloak
                if (text == "Shade Cloak" && !pd.hasDash && !pd.canDash)
                {
                    RandomizerMod.instance.Settings.Swap("Shade Cloak", "Mothwing Cloak");
                    text = "Mothwing Cloak";
                }

                //Similar checks for dream nail
                if (text == "Dream Gate" && !pd.hasDreamNail)
                {
                    RandomizerMod.instance.Settings.Swap("Dream Nail", "Dream Gate");
                    text = "Dream Nail";
                }

                if (text == "Awoken Dream Nail" && !pd.hasDreamNail)
                {
                    RandomizerMod.instance.Settings.Swap("Dream Nail", "Awoken Dream Nail");
                    text = "Dream Nail";
                }

                //Similar checks for spells
                if (text == "Shade Soul" && RandomizerMod.instance.Settings.FireballLevel() == 0)
                {
                    RandomizerMod.instance.Settings.Swap("Vengeful Spirit", "Shade Soul");
                    text = "Vengeful Spirit";
                }

                if (text == "Descending Dark" && RandomizerMod.instance.Settings.QuakeLevel() == 0)
                {
                    RandomizerMod.instance.Settings.Swap("Desolate Dive", "Descending Dark");
                    text = "Desolate Dive";
                }

                if (text == "Abyss Shriek" && RandomizerMod.instance.Settings.ScreamLevel() == 0)
                {
                    RandomizerMod.instance.Settings.Swap("Howling Wraiths", "Abyss Shriek");
                    text = "Howling Wraiths";
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
                        SetBoolInternal(var.name, val);
                    }
                    else
                    {
                        if (text == "Vengeful Spirit") RandomizerMod.instance.Settings.fireball1 = val;
                        else if (text == "Shade Soul") RandomizerMod.instance.Settings.fireball2 = val;
                        else if (text == "Desolate Dive") RandomizerMod.instance.Settings.quake1 = val;
                        else if (text == "Descending Dark") RandomizerMod.instance.Settings.quake2 = val;
                        else if (text == "Howling Wraiths") RandomizerMod.instance.Settings.scream1 = val;
                        else if (text == "Abyss Shriek") RandomizerMod.instance.Settings.scream2 = val;
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

            SetBoolInternal(name, val);

            
        }

        private static void SetBoolInternal(string name, bool value)
        {
            PlayerData.instance.SetBoolInternal(name, value);

            //If other mods want to be compatible with Randomizer logic, but don't want to implement randomizer logic, that can find out what's set this way.
            if (_SetPlayerBoolHook != null)
            {
                try
                {
                    _SetPlayerBoolHook(name, value);
                }
                catch (Exception ex)
                {
                    RandomizerMod.instance.LogError(ex);
                }
            }
        }
        //Adds data to the randomizer dictionaries
        public static void AddEntry(XmlNode node)
        {
            RandomizerEntry entry = new RandomizerEntry(node);

            foreach (RandomizerVar var in entry.entries)
            {
                if (typeof(PlayerData).GetField(var.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) == null)
                {
                    return;
                }
            }

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

        public static List<RandomizerEntry> GetNewReachableItems(List<RandomizerEntry> reachable, List<RandomizerEntry> replaced, List<RandomizerEntry> obtained)
        {
            List<RandomizerEntry> candidates = (from item in entries.Values.AsEnumerable() where !(reachable.Contains(item) || replaced.Contains(item)) select item).ToList();
            List<RandomizerEntry> newEntries = new List<RandomizerEntry>();

            foreach (RandomizerEntry candidate in candidates)
            {
                if (candidate.IsReachable(obtained))
                {
                    newEntries.Add(candidate);
                    RandomizerMod.instance.Log(candidate.name + " is now reachable");
                }
            }

            return newEntries;
        }

        //Randomization algorithm
        public static void Randomize(System.Random random)
        {
            RandomizerMod.instance.Log("----------------------------------------------------------");
            RandomizerMod.instance.Log("Beginning randomization with seed " + NewGameSettings.seed);
            RandomizerMod.instance.Settings.StringValues.Clear();

            List<RandomizerEntry> unsorted = new List<RandomizerEntry>();
            List<RandomizerEntry> sorted = new List<RandomizerEntry>();
            List<RandomizerEntry> reachable = (from entry in entries.Values.AsEnumerable() where (entry.IsReachable(sorted)) select entry).ToList();
            List<RandomizerEntry> replaced = new List<RandomizerEntry>();
            unsorted.AddRange(entries.Values);

            foreach (RandomizerEntry entry in reachable)
            {
                RandomizerMod.instance.Log("" + entry.name + " is reachable");
            }

            //Loop until we've run out of places to put things at
            while (reachable.Count > 0)
            {
                RandomizerEntry newItem = default(RandomizerEntry);

                //Need to select an item that isn't a dead end if we're almost out of options
                if (reachable.Count == 1 && unsorted.Count > 1)
                {
                    List<RandomizerEntry> candidates = new List<RandomizerEntry>();
                    List<float> weights = new List<float>();
                    float totalWeights = 0;
                    foreach (RandomizerEntry entry in unsorted)
                    {
                        int leadCount = entry.LeadsTo(entries.Values.ToList(), sorted, reachable.Union(replaced).ToList()).Count;
                        if (leadCount > 0)
                        {
                            totalWeights += (float)1.0 + Mathf.Log(leadCount);
                            weights.Add(totalWeights);
                            candidates.Add(entry);
                        }
                    }

                    if (candidates.Count > 0)
                    {
                        bool itemAssigned = false;
                        float weight = (float)random.NextDouble() * totalWeights;

                        for (int i = 0; i < weights.Count; i++)
                        {
                            if (weight <= weights.ElementAt(i))
                            {
                                newItem = candidates.ElementAt(i);
                                itemAssigned = true;
                                break;
                            }
                        }

                        if (!itemAssigned)
                        {
                            RandomizerMod.instance.LogWarn("Weighted randomness has failed, picking full random value");
                            newItem = candidates.ElementAt(random.Next(candidates.Count));
                        }
                        RandomizerMod.instance.Log("Running out of options, " + newItem.name + " should prevent hard lock");
                    }
                    else
                    {
                        newItem = unsorted.ElementAt(random.Next(unsorted.Count));
                        RandomizerMod.instance.LogWarn("Running out of options, " + newItem.name + " will probably not help anything");
                    }
                }
                else
                {
                    if (RandomizerMod.instance.Settings.hardMode)
                    {
                        List<RandomizerEntry> candidates = new List<RandomizerEntry>();
                        foreach (RandomizerEntry entry in unsorted)
                        {
                            if (random.Next(100) < 35 || entry.LeadsTo(entries.Values.ToList(), sorted, reachable.Union(replaced).ToList()).Count == 0)
                            {
                                candidates.Add(entry);
                            }
                        }

                        if (candidates.Count > 0)
                        {
                            newItem = candidates.ElementAt(random.Next(candidates.Count));
                        }
                        else
                        {
                            newItem = unsorted.ElementAt(random.Next(unsorted.Count));
                        }
                    }
                    else
                    {
                        newItem = unsorted.ElementAt(random.Next(unsorted.Count));
                    }
                }

                //Update list of items that need to be placed still
                sorted.Add(newItem);
                unsorted.Remove(newItem);

                //Randomly place the chosen item among the reachable elements
                //No need to check requirements, we can assume the new item isn't required for the replace location since it's already reachable
                RandomizerEntry replaceAt = reachable.ElementAt(random.Next(reachable.Count));
                replaced.Add(replaceAt);
                reachable.Remove(replaceAt);

                RandomizerMod.instance.Log("Adding permutation: " + replaceAt.name + " = " + newItem.name);

                RandomizerMod.instance.Settings.StringValues.Add(replaceAt.name, newItem.name);
                reachable.AddRange(GetNewReachableItems(reachable, replaced, sorted));
            }

            //Restart if the algorithm fails
            //Hopefully it never does
            if (unsorted.Count != 0)
            {
                RandomizerMod.instance.LogWarn("Randomization has somehow failed");
                foreach (RandomizerEntry entry in entries.Values.ToList().FindAll(item => !replaced.Contains(item)))
                {
                    RandomizerMod.instance.Log(entry.name + " is unreachable");
                }
                Randomize(new System.Random(random.Next()));
            }
            
        }

        //Call randomization without starting the game
        public static void LogRandomization()
        {
            try
            {
                SetHardMode(NewGameSettings.hardMode);
                Randomize(new System.Random(NewGameSettings.seed));
            }
            catch (Exception e)
            {
                RandomizerMod.instance.LogError(e);
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

            RandomizerMod.instance.Settings.Reset();

            
            if (NewGameSettings.randomizer)
            {
                if (NewGameSettings.seed == -1)
                {
                    NewGameSettings.seed = new System.Random().Next();
                }

                RandomizerMod.instance.Settings.seed = NewGameSettings.seed;
                RandomizerMod.instance.Settings.randomizer = NewGameSettings.randomizer;
                RandomizerMod.instance.Settings.hardMode = NewGameSettings.hardMode;

                SetHardMode(NewGameSettings.hardMode);
                Randomize(new System.Random(NewGameSettings.seed));
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
            RandomizerMod.instance.Settings.hardMode = hard;

            //Log any errors that occur
            if (!xmlLoaded)
            {
                try
                {
                    LoadEntriesFromXML();
                    xmlLoaded = true;
                }
                catch (Exception e)
                {
                    RandomizerMod.instance.LogError("Failed to load XML:\n" + e.ToString());
                }
            }
        }

        //Loads all entries from XML
        //Entries are assumed to be formatted properly, malformatted XML will likely cause a crash
        public static void LoadEntriesFromXML()
        {
            if (!File.Exists(@"Randomizer\randomizer.xml"))
            {
                return;
            }

            XmlDocument rnd = new XmlDocument();
            rnd.Load(@"Randomizer\randomizer.xml");

            LoadEntries(rnd.SelectSingleNode("randomizer"));
        }

        //Add entry for each node
        public static void LoadEntries(XmlNode nodes)
        {
            List<string> keyItemsList = new List<string>();
            foreach (XmlNode node in nodes.SelectNodes("keyitems/item"))
            {
                keyItemsList.Add(node.InnerText);
            }

            keyItems = "(";
            for (int i = 0; i < keyItemsList.Count; i++)
            {
                keyItems += "(" + keyItemsList[i] + ")";
                if (i != keyItemsList.Count - 1) keyItems += " + ";
            }
            keyItems += ")";

            foreach (XmlNode node in nodes.SelectNodes("entry"))
            {
                AddEntry(node);
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

        /// <summary>
        /// Called when anything in the game tries to set a bool in player data after the randomizer has alterned what was set.
        /// </summary>
        /// <remarks>PlayerData.SetBool</remarks>
        /// <see cref="SetBoolProxy"/>
        public static event SetBoolProxy SetPlayerBoolHook
        {
            add
            {
                RandomizerMod.instance.LogDebug($"[{value.Method.DeclaringType?.Name}] - Adding Randomizier.SetPlayerBoolHook");
                _SetPlayerBoolHook += value;

            }
            remove
            {
                RandomizerMod.instance.LogDebug($"[{value.Method.DeclaringType?.Name}] - Removing Randomizier.SetPlayerBoolHook");
                _SetPlayerBoolHook -= value;
            }
        }

        private static event SetBoolProxy _SetPlayerBoolHook;

        /// <summary>
        /// Called when anything in the game tries to set an int in player data, after the randomizer has changed its value.
        /// </summary>
        /// <remarks>PlayerData.SetInt</remarks>
        public static event SetIntProxy SetPlayerIntHook
        {
            add
            {
                RandomizerMod.instance.LogDebug($"[{value.Method.DeclaringType?.Name}] - Adding.Randomizier SetPlayerIntHook");
                _SetPlayerIntHook += value;

            }
            remove
            {
                RandomizerMod.instance.LogDebug($"[{value.Method.DeclaringType?.Name}] - Removing.Randomizier SetPlayerIntHook");
                _SetPlayerIntHook -= value;
            }
        }

        private static event SetIntProxy _SetPlayerIntHook;
    }
}
