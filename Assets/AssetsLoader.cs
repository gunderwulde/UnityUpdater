using UnityEngine;
#if UNITY_EDITOR	
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;

public class AssetsLoader : MonoBehaviour
{
    public string baseURL = "https://bsiminstaller.bkool.com/bsim-installer/";

    public static AssetsLoader Instance { get; private set; }

    public static string localPath;
    public static string urlPath;
    public delegate void callback(TaskBundle task);

    public static bool updatingMode = true;

    public class TaskBundle {
        public string url { get; private set; }
        public string name { get; private set; }
        public callback callback { get; set; }

        public AssetBundle bundle { get; private set; }
        public WWW www { get; private set; }

        public T Load<T>(string name) where T : UnityEngine.Object {
            return bundle.LoadAsset<T>(name);
        }

        public AssetBundleRequest LoadAsync<T>(string name) where T : UnityEngine.Object {
            return bundle.LoadAssetAsync<T>(name);
        }

        public TaskBundle(string url, callback callback = null, bool wating = false) {
            this.url = url;
            this.name = this.url.Substring(url.LastIndexOf('/') + 1);
            this.callback = callback;
        }

        public bool Update() {
            if (www != null)
            {
                if (www.isDone)
                {
                    if (string.IsNullOrEmpty(www.error))
                    {

                        Debug.Log(">>>> " + www.url + " " + www.text);

                        bundle = www.assetBundle;
                        if (url.Contains("http")) {
                            UpdateTask(this);
                            if (bundle != null) bundle.Unload(true);
                        }
                        else
                            bundles.Add(name, this);
                    }
                    else
                        www = null;
                    if (callback != null) callback(this);
                    www = null;
                    return true;
                }
                else{
                    currentPercent = www.progress;
                }
            }
            else
            {
                if (updatingMode && !url.Contains("http") && name!= "version_list.txt") {
                    // Mueve la tarea al final de la lista.
                    tasks.RemoveAt(0);
                    tasks.Add(this);

                }
                else {
                    if(updatingMode) currentLabel = "Descargando fichero " + this.name;
                    else currentLabel = "Cargando fichero " + this.name;
                    www = new WWW(this.url);
                }
            }
            return false;
        }
    }

    static List<TaskBundle> tasks = new List<TaskBundle>();
    static Dictionary<string, TaskBundle> bundles = new Dictionary<string, TaskBundle>();

    public static void AddTask(string name, callback callback = null) {
        TaskBundle old = null;
        if (!name.Contains("://")) name = "file://" + localPath + name;
        else old = tasks.Find(a => a.name == name);

        tasks.Add(new TaskBundle(name, callback, old != null));
        Instance.enabled = true;
    }

    public static TaskBundle GetPack(string name) {
        if(!name.Contains(".pack")) name += ".pack";
        if (bundles.ContainsKey(name))
            return bundles[name];
        return null;
    }

    static List<string> localFiles;
    public static void UpdateTask(TaskBundle task) {
        if (task.name != "version_list.txt" && !string.IsNullOrEmpty(task.url)) {
            string current = localFiles.Find(file => file.Contains(task.name));
            if (current != null) localFiles.Remove(current);
            localFiles.Add(task.url);

            // Save bundle.
            System.IO.File.WriteAllBytes(localPath + task.name, task.www.bytes );
            // FLUSH FILE!
            System.IO.File.WriteAllLines(localPath + "version_list.txt", localFiles.ToArray());
        }        
    }

    // Use this for initialization
    void Awake()
    {
        Instance = this;
        updatingMode = true;
        localPath = Application.streamingAssetsPath;
        if (Application.isEditor){
            localPath = Application.persistentDataPath;//Application.streamingAssetsPath;
        }
        else if (Application.isMobilePlatform || Application.isConsolePlatform) {
            localPath = Application.persistentDataPath;//Application.streamingAssetsPath;
        }
        else {
            localPath = Application.persistentDataPath;//Application.streamingAssetsPath;
        }

        

        localPath = localPath.Replace('\\', '/') + "/";
        urlPath = baseURL + GetPlatformFolderForAssetBundles() + "/";
    }

    void Start() {
        // Primero leemos el fichero local de version.
        tasks.Add(new TaskBundle("file://" + localPath + "version_list.txt", (task) => {
            if (task.www != null) {
                localFiles = new List<string>(task.www.text.Replace("\r","").Split('\n'));
                // Filtra ficheros mal formados.
                for( int i=0;i< localFiles.Count;) {
                    if (localFiles[i].Length < 3)
                        localFiles.RemoveAt(i);
                    else
                        i++;
                }
            }
            else localFiles = new List<string>();

            currentLabel = "Comprobando si hay actualizaciones";

            tasks.Add(new TaskBundle(urlPath + "version_list.txt", (task2) => {
                string[] files = task2.www.text.Trim('\r').Split('\n');
                int toUpdate = 0;
                foreach (var file in files) {
                    if (!string.IsNullOrEmpty(file)) {                        
                        string current = localFiles.Find(a => { return a == file; });
                        string name = file.Substring(file.LastIndexOf('/') + 1);
                        if (current == null || !System.IO.File.Exists(localPath + name))
                        {
                            tasks.Add(new TaskBundle(file));
                            toUpdate++;
                        }
                    }
                }
                
                if (toUpdate > 0){
                    // Añade un callback a la ultima tarea
                    tasks[tasks.Count - 1].callback = (p) =>{
                        updatingMode = false;
                        currentLabel = "Todo descargado";
                    };
                }
                else{
                    updatingMode = false;
                    currentLabel = "No hay nada que actualizar.";
                }
                
            }));
        }));
    }

    public static string currentLabel { get; set; }
    public static float currentPercent { get; set; }
    void OnGUI()
    { // Constrain all drawing to be within a pixel area .
        Rect rect;
        GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
            GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    rect = GUILayoutUtility.GetRect(new GUIContent(currentLabel), GUI.skin.box, GUILayout.MinWidth(400));
                    GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(32);
            GUILayout.EndVertical();
        GUILayout.EndArea();
        if (Event.current.type == EventType.Repaint)
        {
            Rect nrect = rect;
            nrect.width *= currentPercent;
            Graphics.DrawTexture(nrect, GUI.skin.box.normal.background, new Rect(0, 0, 1, 1), 4, 4, 4, 4, new Color(1,0,0,0.5f) );
        }
        GUI.Box(rect, currentLabel);
    }




    // Update is called once per frame
    void Update() {
        if (tasks.Count > 0)
            if (tasks[0].Update())
            {
                tasks.RemoveAt(0);
                if (tasks.Count == 0)
                    Instance.enabled = false;
            }
    }

    public static string GetPlatformFolderForAssetBundles()
    {
#if UNITY_EDITOR
        switch (EditorUserBuildSettings.activeBuildTarget) {
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.iOS:
                return "iOS";
            case BuildTarget.WebPlayer:
                return "WebPlayer";
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return "Windows";
            case BuildTarget.StandaloneOSXIntel:
            case BuildTarget.StandaloneOSXIntel64:
            case BuildTarget.StandaloneOSXUniversal:
                return "OSX";
            // Add more build targets for your own.
            // If you add more targets, don't forget to add the same platforms to GetPlatformFolderForAssetBundles(RuntimePlatform) function.
            default:
                return null;
        }
#else
        switch (Application.platform) {
            case RuntimePlatform.Android:
                return "Android";
            case RuntimePlatform.IPhonePlayer:
                return "iOS";
            case RuntimePlatform.WindowsWebPlayer:
            case RuntimePlatform.OSXWebPlayer:
                return "WebPlayer";
            case RuntimePlatform.WindowsPlayer:
                return "Windows";
            case RuntimePlatform.OSXPlayer:
                return "OSX";
            // Add more build platform for your own.
            // If you add more platforms, don't forget to add the same targets to GetPlatformFolderForAssetBundles(BuildTarget) function.
            default:
                return null;
        }
#endif
    }
}



