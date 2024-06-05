using System;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Utilla;
using UnityEngine.Networking;
using Newtonsoft.Json;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using TTboard;

namespace TTboard
{
    /// <summary>
    /// This is your mod's main class.
    /// </summary>

    /* This attribute tells Utilla to look for [ModdedGameJoin] and [ModdedGameLeave] */
    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static GameObject TTBoard;
        private string filePath;
        public string txtcon;
        private bool showWindow = false;
        private string username = "";
        bool allowlivecounter = false;
        private float updatesecs = 3.35f;
        private float timer = 0f;
        public static int followersCount;
        public static bool canUpdate;
        public static bool usernameIsalreadySet;
        public static bool inModded = false;

        void Start()
        {
            /* A lot of Gorilla Tag systems will not be set up when start is called */
            /* Put code in OnGameInitialized to avoid null references */
            LoadAssets();
            filePath = System.IO.Path.Combine(Application.dataPath, "Gtag", "TTUsernameConfig.txt");
            if (File.Exists(filePath))
            {
                txtcon = File.ReadAllText(filePath);
            }
            else
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
                File.CreateText(filePath).Close();
            }
            Utilla.Events.GameInitialized += OnGameInitialized;
        }

        [ModdedGamemodeJoin]
        public void OnJoin(string gamemode)
        {
            /* Activate your mod here */
            /* This code will run regardless of if the mod is enabled */
            TTBoard.SetActive(true);
            inModded = true;
            TTBoard.transform.position = new Vector3(-63.2354f, 11.8859f, -82.111f);
            TTBoard.transform.rotation = Quaternion.Euler(6.1233f, 230.3577f, 355.5288f);
            TTBoard.transform.localScale = new Vector3(.5f, .5f, .5f);
        }

        [ModdedGamemodeLeave]
        public void OnLeave(string gamemode)
        {
            /* Deactivate your mod here */
            /* This code will run regardless of if the mod is enabled */
            TTBoard.transform.position = new Vector3(0, 0, 0);
            TTBoard.SetActive(true);
            inModded = false;
        }

        void OnEnable()
        {
            /* Set up your mod here */
            /* Code here runs at the start and whenever your mod is enabled */
            HarmonyPatches.ApplyHarmonyPatches();
        }

        void OnDisable()
        {
            /* Undo mod setup here */
            /* This provides support for toggling mods with ComputerInterface, please implement it :) */
            /* Code here runs whenever your mod is disabled (including if it disabled on startup) */
            HarmonyPatches.RemoveHarmonyPatches();
        }

        void OnGameInitialized(object sender, EventArgs e)
        {
            TTBoard = Instantiate(TTBoard);
            TTBoard.transform.position = new Vector3(0, 0, 0);
            TTBoard.SetActive(true);
            StartCoroutine(Username2Followers());
        }

        public static void LoadAssets()
        {
            AssetBundle bundle = LoadAssetBundle("TTBoard.plum");
            TTBoard = bundle.LoadAsset<GameObject>("TiktokBoard");
        }

        public static AssetBundle LoadAssetBundle(string path)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            AssetBundle bundle = AssetBundle.LoadFromStream(stream);
            stream.Close();
            return bundle;
        }

        void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 100, 30), "User Window"))
            {
                showWindow = true;
            }
            if (usernameIsalreadySet)
            {
                GUI.Label(new Rect(120, 10, 200, 30), "(Account Name Is Already Set!)");
            }
            if (GUI.Button(new Rect(10, 35, 100, 30), $"Live Counter: {(allowlivecounter ? "T" : "F")}"))
            {
                allowlivecounter = !allowlivecounter;
            }
            if (GUI.Button(new Rect(10, 60, 100, 30), "Update Counter"))
            {
                StartCoroutine(Username2Followers());
            }
            if (showWindow)
            {
                int windowWidth = 200;
                int windowHeight = 120;
                int windowX = (Screen.width - windowWidth) / 2;
                int windowY = (Screen.height - windowHeight) / 2;
                GUI.Window(0, new Rect(windowX, windowY, windowWidth, windowHeight), WindowFunction, "Enter Username");
            }
        }

        void WindowFunction(int windowID)
        {
            GUI.Label(new Rect(10, 30, 180, 20), "Enter Username:");
            username = GUI.TextField(new Rect(10, 50, 180, 20), username);
            if (GUI.Button(new Rect(10, 80, 180, 30), "Set Username"))
            {
                L("Entered Username: " + username);
                File.WriteAllText(filePath, username);
                StartCoroutine(Username2Followers());
                showWindow = false;
            }
        }

        IEnumerator Username2Followers()
        {
            txtcon = File.ReadAllText(filePath);
            string url = "https://mixerno.space/api/tiktok-user-counter/user/" + txtcon;

            if (string.IsNullOrEmpty(txtcon))
            {
                GameObject.Find("tttext").GetComponent<TextMesh>().text = "NO ACC";
                L("No Account Set! Set with GUI!");
                usernameIsalreadySet = false;
            }
            else
            {
                usernameIsalreadySet = true;
                UnityWebRequest webRequest = UnityWebRequest.Get(url);
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string responseData = webRequest.downloadHandler.text;
                    var data = JsonConvert.DeserializeObject<MixernoUserData>(responseData);
                    followersCount = data?.counts?.FirstOrDefault(item => item.value == "followers")?.count ?? 0;
                    L($"Amount of Followers: {followersCount}");
                }
                else
                {
                    L($"Error: {webRequest.error}");
                }

                webRequest.Dispose();
            }
        }

        void L(string msg)
        {
            UnityEngine.Debug.Log(msg);
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= updatesecs && allowlivecounter)
            {
                StartCoroutine(Username2Followers());
                timer = 0f;
            }
            if (followersCount == 0 || string.IsNullOrEmpty(txtcon))
            {
                canUpdate = false;
            }
            else
            {
                canUpdate = true;
            }
            if (canUpdate)
            {
                GameObject.Find("tttext").GetComponent<TextMesh>().text = $"{txtcon.ToUpper()}\n\n{followersCount}";
            }
            if (!PhotonNetwork.InRoom)
            {
                TTBoard.SetActive(true);
                TTBoard.transform.position = new Vector3(-63.2354f, 11.8859f, -82.111f);
                TTBoard.transform.rotation = Quaternion.Euler(6.1233f, 230.3577f, 355.5288f);
                TTBoard.transform.localScale = new Vector3(.5f, .5f, .5f);
            }
            else if (!inModded)
            {
                TTBoard.transform.position = new Vector3(0, 0, 0);
            }
        }
    }
}

public class MixernoUserData
{
    public long t { get; set; }
    public List<Count> counts { get; set; }
    public List<UserDetail> user { get; set; }
}

public class Count
{
    public string value { get; set; }
    public int count { get; set; }
}

public class UserDetail
{
    public string value { get; set; }
    public string count { get; set; }
}
