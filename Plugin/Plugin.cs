using BepInEx;
using BepInEx.Configuration;
using Bounce.Unmanaged;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(LordAshes.FileAccessPlugin.Guid)]
    [BepInDependency(LordAshes.StatMessaging.Guid)]
    [BepInDependency(RadialUI.RadialUIPlugin.Guid)]
    public partial class HSVPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "HSV Plug-In";              
        public const string Guid = "org.lordashes.plugins.hsv";
        public const string Version = "1.2.0.0";

        public bool menuOpen = false;
        public int applying = 0;

        public Dictionary<String, Texture2D> originalTextures = new Dictionary<String, Texture2D>();

        public string data = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase.ToString().Replace("file:///", ""));

        private float h = 100.0f;
        private float s = 100.0f;
        private float v = 100.0f;

        private static BaseUnityPlugin self;

        private static KeyboardShortcut manualTrigger;
        private static bool useAutomaticTriggers = true;

        private static Queue<object[]> transformations = new Queue<object[]>();
        private static int transformationsInProgress = 0;
        private static int maxtTransfromationsInProgress = 0;

        public enum MenuItemLocation
        { 
            MainMenu = 0,
            GMMenu = 1
        }

        void Awake()
        {
            Debug.Log("HSV Plugin: Active.");

            self = this;

            var harmony = new Harmony(HSVPlugin.Guid);
            harmony.PatchAll();

            if (Config.Bind("Settings", "HSV Menu Location", MenuItemLocation.GMMenu).Value == MenuItemLocation.GMMenu)
            {
                RadialUI.RadialUIPlugin.AddCustomButtonGMSubmenu(RadialUI.RadialUIPlugin.Guid + ".HSL", new MapMenu.ItemArgs()
                {
                    Action = HSVMenu,
                    CloseMenuOnActivate = true,
                    FadeName = true,
                    Icon = FileAccessPlugin.Image.LoadSprite("Paintbrush.png"),
                    Title = "HSV"
                }
                , (a, b) => { return true; });
            }
            else
            {
                RadialUI.RadialUIPlugin.AddCustomButtonOnCharacter(RadialUI.RadialUIPlugin.Guid + ".HSL", new MapMenu.ItemArgs()
                {
                    Action = HSVMenu,
                    CloseMenuOnActivate = true,
                    FadeName = true,
                    Icon = FileAccessPlugin.Image.LoadSprite("Paintbrush.png"),
                    Title = "HSV"
                }
                , (a, b) => { return true; });
            }

            manualTrigger = Config.Bind("Keyboard", "Manual Trigger To Apply HSV Transfromations", new KeyboardShortcut(KeyCode.H, KeyCode.RightControl)).Value;
            useAutomaticTriggers = Config.Bind("Settings", "Use Automatic HSV Application Triggers", true).Value;
            maxtTransfromationsInProgress = Config.Bind("Settings", "Maximum Simultaneous Transformations", 5).Value;

            StatMessaging.Subscribe(HSVPlugin.Guid, StatMessagingTransformationRequest);

            Utility.PostOnMainPage(this.GetType());
        }

        void OnGUI()
        {
            if (menuOpen)
            {
                GUIStyle gs1 = new GUIStyle();
                gs1.alignment = TextAnchor.MiddleRight;
                gs1.fontSize = 16;
                gs1.fontStyle = FontStyle.Bold;
                gs1.normal.textColor = Color.white;
                GUIStyle gs2 = new GUIStyle();
                gs2.alignment = TextAnchor.MiddleLeft;
                gs2.fontSize = 16;
                gs2.fontStyle = FontStyle.Bold;
                gs2.normal.textColor = Color.white;
                GUI.Label(new Rect(1920 / 2 - 105, 1080 / 2 - 40, 100, 30), "Hue:", gs1);
                GUI.Label(new Rect(1920 / 2 + 5, 1080 / 2 - 40, 30, 30), h.ToString("0.0") + "%", gs2);
                h = GUI.HorizontalSlider(new Rect(1920 / 2 + 60, 1080 / 2 - 30, 300, 30), h, 0.0F, 200.0F);
                GUI.Label(new Rect(1920 / 2 - 105, 1080 / 2 - 0, 100, 30), "Saturation:", gs1);
                GUI.Label(new Rect(1920 / 2 + 5, 1080 / 2 - 0, 30, 30), s.ToString("0.0") + "%", gs2);
                s = GUI.HorizontalSlider(new Rect(1920 / 2 + 60, 1080 / 2 + 10, 300, 30), s, 0.0F, 200.0F);
                GUI.Label(new Rect(1920 / 2 - 105, 1080 / 2 + 40, 100, 30), "Value:", gs1);
                GUI.Label(new Rect(1920 / 2 + 5, 1080 / 2 + 40, 30, 30), v.ToString("0.0") + "%", gs2);
                v = GUI.HorizontalSlider(new Rect(1920 / 2 + 60, 1080 / 2 + 50, 300, 30), v, 0.0F, 200.0F);
                if (GUI.Button(new Rect(1920 / 2 + 175, 1080 / 2 + 100, 20, 20), "^"))
                {
                    h = 100f;
                    s = 100f;
                    v = 100f;
                }
                if (applying == 0)
                {
                    if (GUI.Button(new Rect(1920 / 2 - 65, 1080 / 2 + 80, 60, 30), "Apply"))
                    {
                        applying = 1;
                    }
                    if (GUI.Button(new Rect(1920 / 2 + 5, 1080 / 2 + 80, 60, 30), "Exit"))
                    {
                        menuOpen = false;
                    }
                }
                else
                {
                    applying++;
                    GUI.Label(new Rect(1920 / 2 - 15, 1080 / 2 + 80, 60, 30), "Applying...", gs1);
                    if (applying == 10)
                    { 
                        applying++;
                        StatMessaging.SetInfo(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()), HSVPlugin.Guid, h +"," + s +","+v);
                    }
                }
            }
        }

        void Update()
        {
            if(Utility.isBoardLoaded())
            {
                if (Utility.StrictKeyCheck(manualTrigger))
                {
                    SystemMessage.DisplayInfoText("Applying HSV Transfromations");
                    Debug.Log("HSV Plugin: Manual HSV Application Triggered");
                    foreach (CreatureBoardAsset asset in CreaturePresenter.AllCreatureAssets)
                    {
                        Debug.Log("HSV Plugin: Refreshing HSV On " + asset.Creature.CreatureId + ".");
                        StartCoroutine("RefreshHSV", new object[] { asset.Creature.CreatureId });
                    }
                }
            }
            if(transformationsInProgress<maxtTransfromationsInProgress && transformations.Count>0)
            {
                Debug.Log("HSV Plugin: Dequeing Next Transformation");
                object[] transformation = transformations.Dequeue();
                StartCoroutine("ProcessChange", transformation);
            }
        }

        private void HSVMenu(MapMenuItem arg1, object arg2)
        {
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()), out asset);
            if (asset != null)
            {
                string[] values = StatMessaging.ReadInfo(asset.Creature.CreatureId, HSVPlugin.Guid).Split(',');
                try
                {
                    h = float.Parse(values[0]);
                    s = float.Parse(values[1]);
                    v = float.Parse(values[2]);
                }
                catch(Exception)
                {
                    h = 100f;
                    s = 100f;
                    v = 100f;
                }
                menuOpen = true;
            }
        }

        private void StatMessagingTransformationRequest(StatMessaging.Change[] changes)
        {
            Debug.Log("HSV Plugin: Received "+changes.Length+" Updates");
            foreach (StatMessaging.Change change in changes)
            {
                Debug.Log("HSV Plugin: Processing " + change.cid.ToString() + " -> " + change.value + " (" + change.action + ")");
                string[] values = change.value.Split(',');
                float delay = (change.action == StatMessaging.ChangeType.added) ? Config.Bind("Settings", "Delay HSV Application On New Asset", 1.0f).Value : 0.1f;
                StartCoroutine("DelayTransformationQueuing", new object[] { change.cid, float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]), delay });
            }
        }

        private IEnumerator DelayTransformationQueuing(object[] inputs)
        {
            // Cid, H, S, V, delay
            yield return new WaitForSeconds((float)inputs[4]);
            Debug.Log("HSV Plugin: Enqueing Transformation.");
            transformations.Enqueue(inputs);
        }

        private IEnumerator ProcessChange(object[] inputs)
        {
            Debug.Log("HSV Plugin: Processing HSV Transformation");
            transformationsInProgress++;
            yield return new WaitForSeconds(0.1f);
            CreatureGuid cid = (CreatureGuid)inputs[0];
            float dh = (float)inputs[1];
            float ds = (float)inputs[2];
            float dv = (float)inputs[3];
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset(cid, out asset);
            string phase = "";
            if (asset != null)
            {
                try
                {
                    Debug.Log("HSV Plugin: Processing HSV Transformation For "+StatMessaging.GetCreatureName(asset)+" (H" + dh + ":S" + ds + ":V" + dv + ")");
                    phase = "Get Mini Reference";
                    GameObject go = asset.CreatureLoaders[0].gameObject;
                    phase = "Get Mini Render Reference";
                    Renderer rend = go.GetComponentInChildren<Renderer>();

                    List<Texture2D> textures = new List<Texture2D>();

                    if (rend.materials.Length == 1)
                    {
                        if (!originalTextures.ContainsKey(cid.ToString()))
                        {
                            Debug.Log("HSV Plugin: Registering Only Original Texture '"+ rend.material.mainTexture.name + "'.");
                            phase = "Get Mini Original Texture Reference";
                            originalTextures.Add(cid.ToString(), (Texture2D)rend.material.mainTexture);
                        }
                        Debug.Log("HSV Plugin: Adding Original Texture To Processing List.");
                        textures.Add(originalTextures[cid.ToString()]);
                    }
                    else
                    {
                        for(int i=0; i<rend.materials.Length; i++)
                        {
                            if (!originalTextures.ContainsKey(cid+"."+i))
                            {
                                Debug.Log("HSV Plugin: Registering Original Texture "+(i+1)+" of " + rend.materials.Length + " '"+ rend.materials[i].mainTexture.name + "'.");
                                phase = "Get Mini Original Texture Reference";
                                originalTextures.Add(cid+"."+i, (Texture2D)rend.materials[i].mainTexture);
                            }
                            Debug.Log("HSV Plugin: Adding Original Texture "+(i+1)+" To Processing List.");
                            textures.Add(originalTextures[cid+"."+i]);
                        }
                    }

                    foreach (Texture2D tex in textures)
                    {
                        Debug.Log("HSV Plugin: Processing Texture '" + tex.name + "' (Is Readable: " + tex.isReadable + ")");
                        phase = "Creature New Texture Object";
                        byte[] bytes;
                        Texture2D alt;
                        if (!tex.isReadable)
                        {
                            phase = "Extract Non-Readable Texture Object";
                            RenderTexture tmp = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                            Graphics.Blit(tex, tmp);
                            RenderTexture previous = RenderTexture.active;
                            RenderTexture.active = tmp;
                            alt = new Texture2D(tex.width, tex.height);
                            alt.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                            alt.Apply();
                            RenderTexture.active = previous;
                            RenderTexture.ReleaseTemporary(tmp);
                        }
                        else
                        {
                            alt = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false);
                            phase = "Read Original Texture Object";
                            Color[] c = tex.GetPixels(0, 0, tex.width - 1, tex.height - 1);
                            phase = "Copy Texture To New Texture Object";
                            alt.SetPixels(0, 0, tex.width - 1, tex.height - 1, c);
                        }
                        bytes = alt.EncodeToPNG();
                        phase = "Write Uncompressed Texture Object";
                        System.IO.File.WriteAllBytes(data + "/CustomData/Images/" + asset.Creature.CreatureId + ".png", bytes);
                        Debug.Log("HSV Plugin: Applying Transformation.");
                        phase = "Apply Transformation To Texture Object";
                        System.Diagnostics.Process transformation = new System.Diagnostics.Process()
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo()
                            {
                                FileName = data + "/ImageAdjust.exe",
                                WorkingDirectory = data,
                                Arguments = "\"" + data + "/CustomData/Images/" + asset.Creature.CreatureId + ".png\" " + dh + " " + ds + " " + dv,
                                CreateNoWindow = true,
                                RedirectStandardOutput = true,
                                UseShellExecute = false
                            }
                        };
                        transformation.Start();
                        string output = transformation.StandardOutput.ReadToEnd();
                        transformation.WaitForExit();
                        Debug.Log("HSV Plugin: ImageAdjust Output = " + output);
                        phase = "Apply New Texture";
                        Debug.Log("HSV Plugin: Loading Transformation.");
                        rend.material.mainTexture = LoadTexture(data + "/CustomData/Images/" + asset.Creature.CreatureId + ".png");
                        // try { System.IO.File.Delete(data + "/CustomData/Images/" + asset.Creature.CreatureId + ".png"); } catch (Exception) {; }
                    }
                }
                catch(Exception x)
                {
                    SystemMessage.DisplayInfoText(StatMessaging.GetCreatureName(asset)+":\r\nAsset Texture Not Readable.\r\nThe Asset Is Not Compatible With HSV Plugin.\r\nEnsure Texture Is Set To Read/Write.");
                    Debug.Log("HSV Plugin: Exception Processing Asset ("+StatMessaging.GetCreatureName(asset)+") Texture At Phase '"+phase+"'");
                    Debug.LogException(x);
                }
            }
            applying = 0;
            transformationsInProgress--;
        }

        private Texture2D LoadTexture(string source)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.LoadImage(System.IO.File.ReadAllBytes(source));
            return tex;
        }
    }
}
