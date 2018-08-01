using System.Collections;
using System.Collections.Generic;
using UnityEngine.VR;
using UnityEngine;


public static class HoloHelper
{
    public static bool isHololens()
    {

#if UNITY_WSA_10_0 && !UNITY_EDITOR
        Debug.Log("Present: " + UnityEngine.XR.XRDevice.isPresent);
        Debug.Log(UnityEngine.XR.XRSettings.loadedDeviceName);
        return (UnityEngine.XR.XRDevice.isPresent && UnityEngine.XR.XRSettings.loadedDeviceName.Equals("WindowsMR"));
#endif
        return false;
    }
	
}
