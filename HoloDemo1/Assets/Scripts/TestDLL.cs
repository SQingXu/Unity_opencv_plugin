using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class TestDLL : MonoBehaviour {
    [DllImport("OpenCVAruco.dll")]
    public static extern void StartCameraModule();

    [DllImport("OpenCVAruco.dll")]
    public static extern void DetectMarkersAruco();

    [DllImport("OpenCVAruco.dll")]
    private static extern void RegisterDebugCallback(DebugCallback callback);



    public delegate void DebugCallback(string str); 

	// Use this for initialization
	void Start () {
        RegisterDebugCallback(new DebugCallback(debugFunction));
        Debug.Log("function start");
        StartCameraModule();
        Debug.Log("start function called");
        StartCoroutine(CallTrackingMethod());
	}
	
	// Update is called once per frame
	void Update () {
        //Debug.Log("Keep update");
	}
    void Awake()
    {

    }

    void debugFunction(string str)
    {
        Debug.Log(str);
    }

    IEnumerator CallTrackingMethod()
    {
        while (true)
        {
            //Debug.Log("coroutine called");
            DetectMarkersAruco();
            yield return new WaitForSeconds(1.0f);

        }
    }

}
