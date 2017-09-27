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

            //Experimental change, keeping the previous check commented here in case we need it back
            //if (!stack.Contains("at HutongGames.PlayMaker.Fsm.DoTransition(HutongGames.PlayMaker.FsmTransition transition, Boolean isGlobal)") && (!stack.Contains("at HutongGames.PlayMaker.Fsm.UpdateState(HutongGames.PlayMaker.FsmState state)") || key.Contains("CHARM_NAME_") || key.Contains("INV_NAME_TRINKET")))
            if (!Randomizer.InInventory())
            {
                //Switch locales based on loaded XML data
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

            //Ruins1_05b is Lemm's room, checking to make sure we don't randomize his UI strings
            if (key.Contains("INV_NAME_TRINKET") && !Randomizer.InInventory() && GameManager.instance.GetSceneNameString() != "Ruins1_05b")
            {
                return Language.Language.GetInternal("INV_NAME_TRINKET" + Randomizer.GetTrinketForScene(), sheet);
            }

            return Language.Language.GetInternal(key, sheet);
        }
    }
}
