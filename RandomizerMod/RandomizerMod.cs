using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using Modding;
using UnityEngine.SceneManagement;

namespace RandomizerMod
{
    public class RandomizerMod : Mod<SaveSettings>
    {
        private string xmlVer = "UNKNOWN";

        public static RandomizerMod instance;

        //Attach to the modded dll
        public override void Initialize()
        {
            Log("Randomizer Mod initializing!");

            instance = this;

            ModHooks.Instance.GetPlayerBoolHook += Randomizer.GetPlayerDataBool;
            ModHooks.Instance.SetPlayerBoolHook += Randomizer.SetPlayerDataBool;
            ModHooks.Instance.GetPlayerIntHook += Randomizer.GetPlayerDataInt;
            ModHooks.Instance.SetPlayerIntHook += Randomizer.SetPlayerDataInt;
            ModHooks.Instance.SavegameLoadHook += LogSetting;
            ModHooks.Instance.SavegameClearHook += Randomizer.DeleteGame;
            ModHooks.Instance.NewGameHook += Randomizer.NewGame;

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneHandler.CheckForChanges;

            ModHooks.Instance.LanguageGetHook += LanguageHandler.Get;
            UnityEngine.GameObject UIObj = new UnityEngine.GameObject();
            UIObj.AddComponent<GUIController>();
            UnityEngine.GameObject.DontDestroyOnLoad(UIObj);

            //Get version from XML
            if (File.Exists(@"Randomizer\randomizer.xml"))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(@"Randomizer\randomizer.xml");
                XmlAttribute ver = doc.SelectSingleNode("randomizer").Attributes["version"];
                if (ver != null) xmlVer = ver.Value;
            }

            try
            {
                Randomizer.LoadEntriesFromXML();
                Randomizer.xmlLoaded = true;
            }
            catch (Exception e)
            {
                LogError("Failed to load XML:\n" + e);
            }

            Log("Randomizer Mod initialized!");
        }

        private void LogSetting(int id)
        {
            Log("Loading save file (Randomizer: " + (Settings.randomizer ? (Settings.hardMode ? "hard" : "easy") : "off") + ")");
            Log("Permutations:");

            foreach (KeyValuePair<string, string> perm in Settings.StringValues)
            {
                Log(perm.Key + " = " + perm.Value);
            }
        }

        public override string GetVersion()
        {
            return "1.5.0 (XML Version: " + xmlVer + ")";
        }

        public override bool IsCurrent()
        {
            return true;
        }
    }
}
