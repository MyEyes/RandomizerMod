using System;

namespace RandomizerMod
{
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
        public RandomizerEntry(string n, string[] e, string[][] req)
        {
            this.name = n;
            this.entries = e;
            this.requires = req;
        }

        // Token: 0x040038E7 RID: 14567
        private string name;

        // Token: 0x040038E8 RID: 14568
        public string[] entries;

        // Token: 0x040038E9 RID: 14569
        public string[][] requires;
    }
}
