using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RandomizerMod
{
    // Token: 0x02000921 RID: 2337
    public class GUIController : MonoBehaviour
    {
        // Token: 0x0600312A RID: 12586 RVA: 0x00127240 File Offset: 0x00125440
        public GUIController()
        {
            this.seedString = new System.Random().Next().ToString();
            this.style = GUI.skin.textField;
        }

        // Token: 0x0600312B RID: 12587 RVA: 0x0012727C File Offset: 0x0012547C
        private void Awake()
        {
            if (File.Exists("Randomizer\\logo.png") && File.Exists("Randomizer\\off.png") && File.Exists("Randomizer\\easy.png") && File.Exists("Randomizer\\hard.png"))
            {
                this.textures = new Dictionary<string, Texture2D>();
                Texture2D texture2D = new Texture2D(1, 1);
                Texture2D texture2D2 = new Texture2D(1, 1);
                Texture2D texture2D3 = new Texture2D(1, 1);
                Texture2D texture2D4 = new Texture2D(1, 1);
                texture2D.LoadImage(File.ReadAllBytes("Randomizer\\logo.png"));
                texture2D2.LoadImage(File.ReadAllBytes("Randomizer\\off.png"));
                texture2D3.LoadImage(File.ReadAllBytes("Randomizer\\easy.png"));
                texture2D4.LoadImage(File.ReadAllBytes("Randomizer\\hard.png"));
                this.textures.Add("logo", texture2D);
                this.textures.Add("off", texture2D2);
                this.textures.Add("easy", texture2D3);
                this.textures.Add("hard", texture2D4);
                return;
            }
            throw new FileNotFoundException("Randomizer could not find all menu images");
        }

        // Token: 0x0600312C RID: 12588 RVA: 0x00127384 File Offset: 0x00125584
        public void OnGUI()
        {
            this.style = GUI.skin.textField;
            this.style.fontSize = 64;
            int depth = GUI.depth;
            Matrix4x4 matrix = GUI.matrix;
            Color backgroundColor = GUI.backgroundColor;
            Color contentColor = GUI.contentColor;
            Color color = GUI.color;
            GUI.depth = 1;
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3((float)Screen.width / 1920f, (float)Screen.height / 1080f, 1f));
            if (GameManager.instance.sceneName == "Menu_Title")
            {
                if (UIManager.instance.mainMenuScreen.alpha > 0f)
                {
                    GUI.color = new Color(color.r, color.g, color.b, UIManager.instance.mainMenuScreen.alpha);
                    GUI.DrawTexture(new Rect(1515f, 1020f, 409f, 66f), this.textures["logo"], ScaleMode.ScaleToFit);
                }
                if (UIManager.instance.playModeMenuScreen.isActiveAndEnabled)
                {
                    GUI.color = new Color(color.r, color.g, color.b, UIManager.instance.playModeMenuScreen.screenCanvasGroup.alpha);
                    if (!Randomizer.randomizer && this.TextureButton(this.textures["off"], new Rect(590f, 757f, 740f, 80f)))
                    {
                        Randomizer.randomizer = true;
                        Randomizer.hardMode = false;
                    }
                    else if (Randomizer.randomizer && !Randomizer.hardMode && this.TextureButton(this.textures["easy"], new Rect(590f, 757f, 740f, 80f)))
                    {
                        Randomizer.randomizer = true;
                        Randomizer.hardMode = true;
                    }
                    else if (Randomizer.randomizer && Randomizer.hardMode && this.TextureButton(this.textures["hard"], new Rect(590f, 757f, 740f, 80f)))
                    {
                        Randomizer.randomizer = false;
                        Randomizer.hardMode = false;
                    }
                    if (Randomizer.randomizer)
                    {
                        this.seedString = GUI.TextField(new Rect(200f, 757f, 330f, 82f), this.seedString, 9, this.style);
                        this.seedString = Regex.Replace(this.seedString, "[^0-9]", "");
                        int.TryParse(this.seedString, out Randomizer.seed);
                    }
                }
            }
            GUI.depth = depth;
            GUI.matrix = matrix;
            GUI.backgroundColor = backgroundColor;
            GUI.contentColor = contentColor;
            GUI.color = color;
        }

        // Token: 0x0600312D RID: 12589 RVA: 0x000246BC File Offset: 0x000228BC
        private bool TextureButton(Texture2D tex, Rect rect)
        {
            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
            return GUI.Button(rect, "", GUIStyle.none);
        }

        // Token: 0x040038EA RID: 14570
        private Dictionary<string, Texture2D> textures;

        // Token: 0x040038EB RID: 14571
        private string seedString;

        // Token: 0x040038EC RID: 14572
        public GUIStyle style;
    }
}
