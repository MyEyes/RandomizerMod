using System;
using System.Diagnostics;

namespace RandomizerMod
{
    public static class LanguageHandler
    {
        public static string Get(string key, string sheet)
        {
            if (!Randomizer.randomizer)
            {
                return Language.Language.GetInternal(key, sheet);
            }

            if (!new StackTrace().ToString().Contains("at HutongGames.PlayMaker.Fsm.DoTransition(HutongGames.PlayMaker.FsmTransition transition, Boolean isGlobal)"))
            {
                string pickupName;
                if (Randomizer.reverseLookup.TryGetValue(sheet + "." + key, out pickupName))
                {
                    string switchedPickup;
                    if (Randomizer.permutation.TryGetValue(pickupName, out switchedPickup))
                    {
                        RandomizerEntry switchedEntry;
                        if (Randomizer.entries.TryGetValue(switchedPickup, out switchedEntry))
                        {
                            string[] switchedLocale = switchedEntry.localeNames[0].Split('.');
                            return Language.Language.GetInternal(switchedLocale[1], switchedLocale[0]);
                        }
                    }
                }
            }

            return Language.Language.GetInternal(key, sheet);
        }
    }
}
