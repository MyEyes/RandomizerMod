using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Linq;
using UnityEngine;

namespace RandomizerMod
{
    public static class Randomizer
    {
        public static Dictionary<string, RandomizerEntry> entries = new Dictionary<string, RandomizerEntry>();
        public static Dictionary<string, string> reverseLookup = new Dictionary<string, string>();
        public static Dictionary<string, string> permutation = new Dictionary<string, string>();

        public static bool swappedCloak;
        public static bool randomizer;
        public static bool hardMode;
        public static int seed = -1;

        public static StreamWriter debugWriter = null;
        public static bool debug = false;

        //Int function placeholders, currently unused
        public static int GetPlayerDataInt(GameManager man, string name)
        {
            return 0;
        }

        public static void SetPlayerDataInt(GameManager man, string name, int value)
        {
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
            string text = new StackTrace().ToString();
            if (text.Contains("at HutongGames.PlayMaker.Fsm.Start()") || (name.Contains("gotCharm_") && text.Contains("at HutongGames.PlayMaker.Fsm.DoTransition(HutongGames.PlayMaker.FsmTransition transition, Boolean isGlobal)")))
            {
                return pd.GetBoolInternal(name);
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

                //Set all bools relating to the given entry
                for (int i = 0; i < Randomizer.entries[text].entries.Length; i++)
                {
                    pd.SetBoolInternal(Randomizer.entries[text].entries[i], val);
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
                            int index = random.Next(list2.Count);
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
        public static void SaveGame(int profileId)
        {
            if (Randomizer.randomizer)
            {
                using (StreamWriter streamWriter = new StreamWriter(Application.persistentDataPath + @"\user" + profileId + ".rnd"))
                {
                    streamWriter.WriteLine(Randomizer.seed);
                    streamWriter.WriteLine(Randomizer.swappedCloak);
                    streamWriter.WriteLine(Randomizer.hardMode);
                }
            }
        }

        //Load randomizer save from file if applicable
        public static void LoadGame(int profileId)
        {
            Randomizer.randomizer = false;
            Randomizer.hardMode = false;

            if (File.Exists(Application.persistentDataPath + @"\user" + profileId + ".rnd"))
            {
                using (StreamReader streamReader = new StreamReader(Application.persistentDataPath + @"\user" + profileId + ".rnd"))
                {
                    Randomizer.seed = Convert.ToInt32(streamReader.ReadLine());
                    Randomizer.swappedCloak = Convert.ToBoolean(streamReader.ReadLine());
                    Randomizer.hardMode = Convert.ToBoolean(streamReader.ReadLine());
                }

                Randomizer.SetHardMode(Randomizer.hardMode);
                Randomizer.Randomize(new System.Random(Randomizer.seed));

                //Swap cloaks if player picked up shade cloak first
                if (Randomizer.swappedCloak)
                {
                    Randomizer.Swap("Mothwing Cloak", "Shade Cloak");
                }

                Randomizer.randomizer = true;
            }
        }

        //Set up randomization if applicable
        public static void NewGame()
        {
            if (Randomizer.randomizer)
            {
                if (Randomizer.seed == -1)
                {
                    Randomizer.seed = new System.Random().Next();
                }

                Randomizer.swappedCloak = false;

                Randomizer.SetHardMode(Randomizer.hardMode);
                Randomizer.Randomize(new System.Random(Randomizer.seed));
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
            Randomizer.hardMode = hard;
            Randomizer.permutation.Clear();
            Randomizer.reverseLookup.Clear();
            Randomizer.entries.Clear();

            //Log any errors that occur
            try
            {
                Randomizer.LoadEntriesFromXML(hard, PlayerData.instance.permadeathMode > 0);
            }
            catch (Exception e)
            {
                Randomizer.DebugLog(e.ToString());
            }
        }

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
    }
}
