using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modding;

namespace RandomizerMod
{
    public class RandomizerMod : Mod
    {
        public override void Initialize()
        {
            ModHooks.ModLog("Randomizer Mod initializing!");
            ModHooks.Instance.GetPlayerBoolHook += Randomizer.GetPlayerDataBool;
            ModHooks.Instance.SetPlayerBoolHook += Randomizer.SetPlayerDataBool;
            ModHooks.Instance.SavegameLoadHook += Randomizer.LoadGame;
            ModHooks.Instance.SavegameSaveHook += Randomizer.SaveGame;
            ModHooks.Instance.NewGameHook += Randomizer.NewGame;
            ModHooks.Instance.SceneChanged += SceneHandler.CheckForChanges;
            UnityEngine.GameObject UIObj = new UnityEngine.GameObject();
            UIObj.AddComponent<GUIController>();
            ModHooks.ModLog("Randomizer Mod initialized!");
        }
    }
}
