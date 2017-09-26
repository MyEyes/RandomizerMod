using System;
using System.Collections.Generic;
using System.Xml;

namespace RandomizerMod
{
    public enum RandomizerType
    {
        ABILITY,
        CHARM,
        SPELL,
        RELIC,
        INVALID
    };

    // Token: 0x02000920 RID: 2336
    public struct RandomizerEntry
    {
        // Token: 0x06003128 RID: 12584 RVA: 0x0012720C File Offset: 0x0012540C
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

        // Token: 0x06003129 RID: 12585 RVA: 0x000246A5 File Offset: 0x000228A5
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

        // Token: 0x040038E7 RID: 14567
        public string name;

        // Token: 0x040038E8 RID: 14568
        public string[] entries;

        // Token: 0x040038E9 RID: 14569
        public string[][] requires;

        public RandomizerType type;

        public string[] localeNames;
    }
}
