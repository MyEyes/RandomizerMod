using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerMod
{
    public static class RoomChanger
    {
        //Not really useful for anything but testing
        public static string ChangeRoom(string sceneName)
        {
            return sceneName;

            //Debug code, be sure this never runs in released versions
            if (new System.Diagnostics.StackTrace().ToString().Contains("LoadSceneAdditive"))
            {
                GameManager.instance.entryGateName = "left1";
                return "Waterways_18";
            }
            else return sceneName;
        }
    }
}
