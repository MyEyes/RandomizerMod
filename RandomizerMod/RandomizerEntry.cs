using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

namespace RandomizerMod
{
    //Type of pickup
    //Not currently used much but it is in the XML and loaded, mostly for future proofing
    public enum RandomizerType
    {
        ABILITY,
        CHARM,
        SPELL,
        RELIC,
        INVALID
    };

    public struct RandomizerEntry
    {
        public string name;
        public RandomizerVar[] entries;
        public string[][] requiresEasy;
        public string[][] requiresHard;
        public string[][] requiresHardPermadeath;
        public RandomizerType type;
        public string[] localeNames;

        //Get index of entry so we can change the matching index on the swapped item
        public int GetIndex(string s)
        {
            for (int i = 0; i < this.entries.Length; i++)
            {
                if (s == this.entries[i].name)
                {
                    return i;
                }
            }
            return -1;
        }

        public string[][] GetRequires()
        {
            if (Randomizer.hardMode)
            {
                if (PlayerData.instance.permadeathMode > 0)
                {
                    return this.requiresHardPermadeath;
                }

                return this.requiresHard;
            }

            return this.requiresEasy;
        }

        //Load entry from XML
        //TODO: Add error checking for malformatted XML
        public RandomizerEntry(XmlNode xml)
        {
            this.name = xml.SelectSingleNode("name").InnerText;
            XmlNodeList entriesXml = xml.SelectSingleNode("vars").ChildNodes;
            XmlNodeList requiresXml = xml.SelectNodes("requirements/requirementSet");
            this.type = RandomizerEntry.GetTypeFromString(xml.SelectSingleNode("type").InnerText);
            XmlNodeList localesXml = xml.SelectNodes("locales/locale");

            this.entries = new RandomizerVar[entriesXml.Count];
            for (int i = 0; i < entriesXml.Count; i++)
            {
                string value = entriesXml[i].Attributes["value"] == null ? "" : entriesXml[i].Attributes["value"].Value;

                this.entries[i] = new RandomizerVar(entriesXml[i].InnerText, entriesXml[i].Name, value);
            }

            List<List<string>> easy = new List<List<string>>();
            List<List<string>> hard = new List<List<string>>();
            List<List<string>> hardPermaDeath = new List<List<string>>();

            for (int i = 0; i < requiresXml.Count; i++)
            {
                XmlNodeList reqsSetXml = requiresXml[i].SelectNodes("requirement");
                List<string> reqSetList = new List<string>();

                for (int j = 0; j < reqsSetXml.Count; j++)
                {
                    reqSetList.Add(reqsSetXml[j].InnerText);
                }

                if (Convert.ToBoolean(requiresXml[i].Attributes["easy"].Value))
                {
                    easy.Add(reqSetList);
                }

                if (Convert.ToBoolean(requiresXml[i].Attributes["hard"].Value))
                {
                    hard.Add(reqSetList);
                }

                if (Convert.ToBoolean(requiresXml[i].Attributes["hardpermadeath"].Value))
                {
                    hardPermaDeath.Add(reqSetList);
                }
            }

            this.requiresEasy = easy.Select(l => l.ToArray()).ToArray();
            this.requiresHard = hard.Select(l => l.ToArray()).ToArray();
            this.requiresHardPermadeath = hardPermaDeath.Select(l => l.ToArray()).ToArray();

            this.localeNames = new string[localesXml.Count];
            for (int i = 0; i < localesXml.Count; i++)
            {
                this.localeNames[i] = localesXml[i].InnerText;
            }
        }

        public List<RandomizerEntry> LeadsTo(List<RandomizerEntry> entries, List<RandomizerEntry> obtained, List<RandomizerEntry> reachable)
        {
            foreach (RandomizerEntry item in reachable)
            {
                entries.Remove(item);
            }

            List<RandomizerEntry> l = new List<RandomizerEntry>();
            foreach (RandomizerEntry entry in entries)
            {
                foreach (string[] reqSet in entry.GetRequires())
                {
                    if (l.Contains(entry))
                    {
                        break;
                    }

                    bool flag = true;

                    foreach (string req in reqSet)
                    {
                        if (req != this.name)
                        {
                            bool hasItem = false;

                            foreach(RandomizerEntry item in obtained)
                            {
                                if (req == item.name)
                                {
                                    hasItem = true;
                                }
                            }

                            if (!hasItem)
                            {
                                flag = false;
                            }
                        }
                    }

                    if (flag)
                    {
                        l.Add(entry);
                    }
                }
            }

            return l;
        }

        //Helper function for loading XML
        public static RandomizerType GetTypeFromString(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return RandomizerType.INVALID;
            }

            if (type.ToLower() == "ability")
            {
                return RandomizerType.ABILITY;
            }
            else if (type.ToLower() == "charm")
            {
                return RandomizerType.CHARM;
            }
            else if (type.ToLower() == "spell")
            {
                return RandomizerType.SPELL;
            }
            else if (type.ToLower() == "relic")
            {
                return RandomizerType.RELIC;
            }

            return RandomizerType.INVALID;
        }
    }

    public struct RandomizerVar
    {
        public string name;
        public Type type;
        public object value;

        public RandomizerVar(string n, string t, string v = "")
        {
            this.name = n;

            if (t == "int")
            {
                this.type = typeof(int);
            }
            else
            {
                this.type = typeof(bool);
            }

            if (this.type == typeof(int))
            {
                value = Convert.ToInt32(v);
            }
            else
            {
                value = null;
            }
        }
    }
}
