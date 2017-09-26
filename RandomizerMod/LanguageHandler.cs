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

            string stack = new StackTrace().ToString();

            if (!stack.Contains("at HutongGames.PlayMaker.Fsm.DoTransition(HutongGames.PlayMaker.FsmTransition transition, Boolean isGlobal)") && (!stack.Contains("at HutongGames.PlayMaker.Fsm.UpdateState(HutongGames.PlayMaker.FsmState state)") || key.Contains("CHARM_NAME_") || key.Contains("INV_NAME_TRINKET")))
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

                if (key.Contains("INV_NAME_TRINKET") && !Randomizer.InInventory())
                {
                    return Language.Language.GetInternal("INV_NAME_TRINKET" + Randomizer.GetTrinketForScene(), sheet);
                }
            }

            return Language.Language.GetInternal(key, sheet);
        }
    }
}
