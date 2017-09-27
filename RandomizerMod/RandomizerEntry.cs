using System;
using System.Collections.Generic;
using System.Xml;

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
        public string[] entries;
        public string[][] requires;
        public RandomizerType type;
        public string[] localeNames;

        //Get index of entry so we can change the matching index on the swapped item
        public int GetIndex(string s)
        {
            for (int i = 0; i < this.entries.Length; i++)
            {
                if (s == this.entries[i])
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
            XmlNodeList entriesXml = xml.SelectNodes("bools/bool");
            XmlNodeList requiresXml = xml.SelectNodes("requirements/requirementSet");
            this.type = RandomizerEntry.GetTypeFromString(xml.SelectSingleNode("type").InnerText);
            XmlNodeList localesXml = xml.SelectNodes("locales/locale");

            this.entries = new string[entriesXml.Count];
            for (int i = 0; i < entriesXml.Count; i++)
            {
                this.entries[i] = entriesXml[i].InnerText;
            }

            this.requires = new string[requiresXml.Count][];
            for (int i = 0; i < requiresXml.Count; i++)
            {
                XmlNodeList reqsSetXml = requiresXml[i].SelectNodes("requirement");
                this.requires[i] = new string[reqsSetXml.Count];
                for (int j = 0; j < reqsSetXml.Count; j++)
                {
                    this.requires[i][j] = reqsSetXml[j].InnerText;
                }
            }

            this.localeNames = new string[localesXml.Count];
            for (int i = 0; i < localesXml.Count; i++)
            {
                this.localeNames[i] = localesXml[i].InnerText;
            }
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
}
