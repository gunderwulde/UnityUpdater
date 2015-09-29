using UnityEngine;
using UnityEngine.VR;
using System.Collections;

public class EntryPoint : MonoBehaviour
{

    public bool isPresent;
    public string family;
    public string model;

    public bool enabledDevice;
    VRDeviceType loadedDevice;
    public float renderScale;
    public bool showDeviceView;

    public bool forceEnabled;

    // Use this for initialization
    void Start()
    {
        AssetsLoader.AddTask("velodromo", (www)=> {
            Application.LoadLevel("Velodromo_android");
        });
        forceEnabled = VRSettings.enabled;
    }

    // Update is called once per frame
    void Update()
    {
        isPresent = VRDevice.isPresent;
        family = VRDevice.family;
        model = VRDevice.model;

        enabledDevice = VRSettings.enabled;
        loadedDevice = VRSettings.loadedDevice;
        renderScale = VRSettings.renderScale;
        showDeviceView = VRSettings.showDeviceView;

        VRSettings.enabled = forceEnabled;
        if (Input.GetKeyDown(KeyCode.H))
        {
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;
            Destroy(GameObject.Find("Main Camera"));
            GameObject go = new GameObject("New camera", typeof(Camera));
            go.transform.position = pos;
            go.transform.rotation = rot;
        }

    }
}
