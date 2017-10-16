using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using UnityEngine.SceneManagement;

namespace RandomizerMod
{
    public class RandomizerMod : Mod
    {
        //Attach to the modded dll
        public override void Initialize()
        {
            ModHooks.ModLog("Randomizer Mod initializing!");
            ModHooks.Instance.GetPlayerBoolHook += Randomizer.GetPlayerDataBool;
            ModHooks.Instance.SetPlayerBoolHook += Randomizer.SetPlayerDataBool;
            ModHooks.Instance.GetPlayerIntHook += Randomizer.GetPlayerDataInt;
            ModHooks.Instance.SetPlayerIntHook += Randomizer.SetPlayerDataInt;
            ModHooks.Instance.SavegameLoadHook += Randomizer.LoadGame;
            ModHooks.Instance.SavegameSaveHook += Randomizer.SaveGame;
            ModHooks.Instance.SavegameClearHook += Randomizer.DeleteGame;
            ModHooks.Instance.NewGameHook += Randomizer.NewGame;

            //ModHooks.Instance.SceneChanged += SceneHandler.CheckForChanges;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneHandler.CheckForChanges;

            ModHooks.Instance.LanguageGetHook += LanguageHandler.Get;
            ModHooks.Instance.BeforeSceneLoadHook += RoomChanger.ChangeRoom;
            UnityEngine.GameObject UIObj = new UnityEngine.GameObject();
            UIObj.AddComponent<GUIController>();
            UnityEngine.GameObject.DontDestroyOnLoad(UIObj);
            ModHooks.ModLog("Randomizer Mod initialized!");
        }

        public override string GetVersion()
        {
            return "1.0.0";
        }
    }
}
