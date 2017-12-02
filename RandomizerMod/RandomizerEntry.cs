using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Runtime.InteropServices;

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
        private readonly string reqString;

        public string name;
        public RandomizerVar[] entries;
        public RandomizerType type;
        public string[] localeNames;

        public bool IsReachable(List<RandomizerEntry> obtained)
        {
            //RandomizerMod.instance.Log("Checking if " + this.name + " is reachable");

            List<string> names = new List<string>();
            foreach (RandomizerEntry entry in obtained)
            {
                names.Add(entry.name);
                //RandomizerMod.instance.Log("Have " + entry.name);
            }
            
            string logic = string.Copy(reqString).Replace("HARD", RandomizerMod.instance.Settings.hardMode ? "true" : "false").Replace("KEYITEMS", Randomizer.keyItems);

            string logic2 = string.Copy(logic);
            List<int> quoteIndices = logic.AllIndexesOf("\"");
            for (int i = 0; i < quoteIndices.Count - 1; i += 2)
            {
                string itemName = logic2.Substring(quoteIndices[i] + 1, quoteIndices[i + 1] - quoteIndices[i] - 1);
                logic = logic.Replace("\"" + itemName + "\"", names.Contains(itemName) ? "true" : "false");
            }

            return ParseLogicString(logic);
        }

        private bool ParseLogicString(string logic)
        {
            //RandomizerMod.instance.Log("Parsing for " + this.name + ": " + logic);
            while (true)
            {
                int idx = logic.LastIndexOf("(");
                if (idx != -1)
                {
                    int endIdx = logic.IndexOf(")", idx);

                    logic = logic.Replace("(" + logic.Substring(idx + 1, endIdx - idx - 1) + ")", ParseLogicString(logic.Substring(idx + 1, endIdx - idx - 1)) ? "true" : "false");
                }
                else break;
            }

            string[] logicArr = logic.Split(' ');

            bool and = false;
            bool or = false;
            bool ret = true;

            foreach (string str in logicArr)
            {
                switch (str)
                {
                    case "true":
                        if (and)
                        {
                            ret = ret && true;
                            and = false;
                        }
                        else if (or)
                        {
                            ret = ret || true;
                            or = false;
                        }
                        else ret = true;
                        break;
                    case "false":
                        if (and)
                        {
                            ret = ret && false;
                            and = false;
                        }
                        else if (or)
                        {
                            ret = ret || false;
                            or = false;
                        }
                        else ret = false;
                        break;
                    case "+":
                        and = true;
                        break;
                    case "|":
                        or = true;
                        break;
                }
            }

            return ret;
        }

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

        //Load entry from XML
        //TODO: Add error checking for malformatted XML
        public RandomizerEntry(XmlNode xml)
        {
            this.name = xml.SelectSingleNode("name").InnerText;
            XmlNodeList entriesXml = xml.SelectSingleNode("vars").ChildNodes;
            XmlNode requiresXml = xml.SelectSingleNode("requirements");
            this.type = RandomizerEntry.GetTypeFromString(xml.SelectSingleNode("type").InnerText);
            XmlNodeList localesXml = xml.SelectNodes("locales/locale");

            this.entries = new RandomizerVar[entriesXml.Count];
            for (int i = 0; i < entriesXml.Count; i++)
            {
                string value = entriesXml[i].Attributes["value"] == null ? "" : entriesXml[i].Attributes["value"].Value;

                this.entries[i] = new RandomizerVar(entriesXml[i].InnerText, entriesXml[i].Name, value);
            }

            this.reqString = requiresXml.InnerText;

            this.localeNames = new string[localesXml.Count];
            for (int i = 0; i < localesXml.Count; i++)
            {
                this.localeNames[i] = localesXml[i].InnerText;
            }

            GC.Collect();
        }

        public List<RandomizerEntry> LeadsTo(List<RandomizerEntry> entries, List<RandomizerEntry> obtained, List<RandomizerEntry> reachable)
        {
            foreach (RandomizerEntry item in reachable)
            {
                entries.Remove(item);
            }

            List<RandomizerEntry> obtainedCopy = obtained.ToList();
            obtainedCopy.Add(this);

            List<RandomizerEntry> l = new List<RandomizerEntry>();
            foreach (RandomizerEntry entry in entries)
            {
                if (entry.IsReachable(obtainedCopy))
                {
                    l.Add(entry);
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
