using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.WebCam;
using UnityEngine.XR.WSA.Persistence;
using UnityEngine.XR.WSA;
using System.Runtime.InteropServices;

enum MatrixType { MarkerfOrigin = 0, HeadfCamera = 1, HeadfOrigin = 2, RightToLeft = 3, ViewMatrix = 4,
    CurrentFrameCandidate = 5, CurrentFrameConfirmed = 6, ComputationFrameCandidate = 7, ComputationFrameConfirmed = 8};

public class TransformStoration
{
    public Vector3 position;
    public Quaternion rotation;
    public TransformStoration()
    {
        position = new Vector3();
        rotation = new Quaternion();
    }
}

public class TestDLL : MonoBehaviour {
#if !UNITY_EDITOR
    static IntPtr nativeLibraryPtr;

    [DllImport("OpenCVAruco.dll")]
    public static extern void StartCameraModule();

    [DllImport("OpenCVAruco.dll")]
    public static extern void DetectMarkersAruco();

    [DllImport("OpenCVAruco.dll")]
    private static extern void RegisterDebugCallback(DebugCallback callback);

    [DllImport("OpenCVAruco.dll")]
    private static extern void RegisterPassToUnityMatrixCallback(PassToUnityMatrixCallback callback);

    [DllImport("OpenCVAruco.dll")]
    private static extern void RegisterNotifyFromNativeCallback(NotifyFromNativeCallback callback);

    [DllImport("OpenCVAruco.dll")]
    public static extern void PassInMatrix(double[] arr, int rows, int cols, int type);



    public delegate void DebugCallback(string str);
    public delegate void PassToUnityMatrixCallback(IntPtr arr_ptr, int mtype);
    public delegate void NotifyFromNativeCallback(int mtype);
    //public delegate void StartCameraModule();

    Matrix4x4 HeadfCamera;
    Matrix4x4 MarkerfOrigin;
    Matrix4x4 HeadfOrigin;
    Matrix4x4 RightToLeft;
    Matrix4x4 WorldfCamera;

    Matrix4x4 HeadfWorld;

    GameObject CameraChild;
    GameObject PreviousCamera;
    TransformStoration PreviousCameraLatestFrameConfirmedStore;
    TransformStoration PreviousCameraLatestFrameCandidateStore;
    TransformStoration PreviousCameraComputationCandidateStore;
    TransformStoration PreviousCameraComputationConfirmedStore;
    TransformStoration MainCameraStore;
    private object lock_main_camera;
    private object lock_lastest_frame;
    private object lock_computation_frame;
    
    PhotoCapture photoCaptureObject;
    private bool stable = false;
    private bool cmd_update_switch = false;

    //void Awake()
    //{
    //    if (nativeLibraryPtr != IntPtr.Zero) return;
    //    nativeLibraryPtr = NativeHelper.LoadLibrary("OpenCVAruco.dll");
    //    if(nativeLibraryPtr == IntPtr.Zero)
    //    {
    //        Debug.Log("Failed to load native library");
    //    }
    //}

    //void OnApplicationQuit()
    //{
    //    if (nativeLibraryPtr == IntPtr.Zero) return;

    //    Debug.Log(NativeHelper.FreeLibrary(nativeLibraryPtr)
    //                  ? "Native library successfully unloaded."
    //                  : "Native library could not be unloaded.");
    //}

	// Use this for initialization
	void Start () {
        PreviousCameraLatestFrameCandidateStore = new TransformStoration();
        PreviousCameraLatestFrameConfirmedStore = new TransformStoration();
        PreviousCameraComputationCandidateStore = new TransformStoration();
        PreviousCameraComputationConfirmedStore = new TransformStoration();
        MainCameraStore = new TransformStoration();
        PreviousCamera = new GameObject("PreviousCamera");
        CameraChild = new GameObject("CameraChild");
        lock_main_camera = new object();
        lock_lastest_frame = new object();
        lock_computation_frame = new object();
        //CameraChild.transform.parent = Camera.main.transform;
        CameraChild.transform.parent = PreviousCamera.transform;

        RegisterDebugCallback(new DebugCallback(debugFunction));
        RegisterPassToUnityMatrixCallback(new PassToUnityMatrixCallback(passToUnityMatrixFunction));
        RegisterNotifyFromNativeCallback(new NotifyFromNativeCallback(notifyFromNativeFunction));

        setRightToLeft();
        setHeadfCamera();
        PassInMatrix(HeadfCamera, MatrixType.HeadfCamera);
        PassInMatrix(RightToLeft, MatrixType.RightToLeft);
        Debug.Log("set head finished");

        StartCameraModule();
        //NativeHelper.Invoke<StartCameraModule>(nativeLibraryPtr);
        StartCoroutine(CallTrackingMethod());





    }

    void Update()
    {
        //Debug.Log(this.gameObject.transform.position.ToString("F6"));
        if (!stable)
        {
            lock (lock_main_camera)
            {
                MainCameraStore.position = Camera.main.transform.position;
                MainCameraStore.rotation = Camera.main.transform.rotation;
            }
            lock (lock_computation_frame)
            {
                PreviousCamera.transform.position = PreviousCameraComputationConfirmedStore.position;
                PreviousCamera.transform.rotation = PreviousCameraComputationConfirmedStore.rotation;
            }
        }
        
        if (cmd_update_switch)
        {
            if (stable)
            {
                WorldAnchor anchor = this.gameObject.AddComponent<WorldAnchor>();
            }
            else
            {
                Destroy(gameObject.GetComponent<WorldAnchor>());
            }
            cmd_update_switch = false;
        }

    }

    public void StableObject()
    {
        stable = true;
        cmd_update_switch = true;
    }

    public void TrackObject()
    {
        stable = false;
        cmd_update_switch = true;
    }

    IEnumerator CallTrackingMethod()
    {
        while (true)
        {
            //Debug.Log("coroutine called");
            DetectMarkersAruco();
            yield return new WaitForSeconds(1.0f / 3);
        }
    }

    void debugFunction(string str)
    {
        Debug.Log(str);
    }

    void passToUnityMatrixFunction(IntPtr arr_ptr, int mtype )
    {
        
        if (mtype == (int)MatrixType.HeadfOrigin)
        {
            double[] arr_copy = new double[16];
            Marshal.Copy(arr_ptr, arr_copy, 0, 16);
            HeadfOrigin = ConvertArrayToMatrix(arr_copy);
            //Debug.Log("matrix call back called" + '\n' + HeadfOrigin.ToString());
            if (!stable)
            {
                CameraChild.transform.localRotation = ConvertRotMatToQuat(HeadfOrigin);
                CameraChild.transform.localPosition = new Vector3(HeadfOrigin[0, 3], HeadfOrigin[1, 3], HeadfOrigin[2, 3]);
                this.gameObject.transform.rotation = CameraChild.transform.rotation;
                this.gameObject.transform.position = CameraChild.transform.position;
            }
            
        }
        else if(mtype == (int)MatrixType.ViewMatrix){
            //WorldfCamera = ConvertArrayToMatrix(arr_copy);
            //Debug.Log("matrix call back called" + '\n' + WorldfCamera.ToString());
            //WorldfCamera = RightToLeft * WorldfCamera * RightToLeft;
            //Debug.Log("convert from right hand to left hand" + '\n' + WorldfCamera.ToString());
            //Matrix4x4 HeadfWorld = Camera.main.transform.localToWorldMatrix.inverse;
            //Debug.Log("head from world" + '\n' + HeadfWorld.ToString());
            //Matrix4x4 HeadfCamera = HeadfWorld * WorldfCamera;
            //Debug.Log("head from camera" + '\n' + HeadfCamera.ToString());

        }
    }

    void notifyFromNativeFunction(int mtype)
    {
        if(mtype == (int)MatrixType.CurrentFrameCandidate)
        {
            
            lock (lock_main_camera)
            {
                //Debug.Log("Recieve store frame ntification");
                PreviousCameraLatestFrameCandidateStore.position = MainCameraStore.position;
                PreviousCameraLatestFrameCandidateStore.rotation = MainCameraStore.rotation;
            }

        }
        else if(mtype == (int)MatrixType.CurrentFrameConfirmed)
        {
            lock (lock_lastest_frame)
            {
                PreviousCameraLatestFrameConfirmedStore.position = PreviousCameraLatestFrameCandidateStore.position;
                PreviousCameraLatestFrameConfirmedStore.rotation = PreviousCameraLatestFrameCandidateStore.rotation;
            }
        }
        else if(mtype == (int)MatrixType.ComputationFrameCandidate)
        {
            lock (lock_lastest_frame)
            {
                PreviousCameraComputationCandidateStore.position = PreviousCameraLatestFrameConfirmedStore.position;
                PreviousCameraComputationCandidateStore.rotation = PreviousCameraLatestFrameConfirmedStore.rotation;
            }
            
        }
        else if(mtype == (int)MatrixType.ComputationFrameConfirmed)
        {
            lock (lock_computation_frame)
            {
                PreviousCameraComputationConfirmedStore.position = PreviousCameraComputationCandidateStore.position;
                PreviousCameraComputationConfirmedStore.rotation = PreviousCameraComputationCandidateStore.rotation;
            }
        }
        return;
    }

   

    void PassInMatrix(Matrix4x4 mat, MatrixType mtype)
    {
        double[] data;
        data = ConvertMatrixToArray(mat);
        PassInMatrix(data, 4, 4, (int)mtype);
    }

    private void setHeadfCamera()
    {
        //photoCaptureObject = null;
        //PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);


        HeadfCamera = new Matrix4x4();
        //HeadfCamera.SetRow(0, new Vector4(0.9999533f, 0.002300503f, -0.00933247f, -0.001294257f));
        //HeadfCamera.SetRow(1, new Vector4(-0.002309966f, 0.9999966f, -0.001006001f, 0.02103565f));
        //HeadfCamera.SetRow(2, new Vector4(0.009330086f, 0.001027526f, 0.9999559f, 0.065153553f));
        //HeadfCamera.SetRow(3, new Vector4(0f, 0f, 0f, 1f));


        HeadfCamera.SetRow(0, new Vector4(0.999901500f, -0.013928400f, -0.000545241f, -0.001265314f));
        HeadfCamera.SetRow(1, new Vector4(0.013928410f, 0.999900000f, 0.000278913f, 0.020772190f));
        HeadfCamera.SetRow(2, new Vector4(0.000540711f, -0.000287881f, 1.000002000f, 0.064527340f));
        HeadfCamera.SetRow(3, new Vector4(0f, 0f, 0f, 1f));


        //Debug.Log("Test: " + (HeadfCamera * new Vector4(0, 0, 0, 1)).ToString("F6"));

        return;
    }

    private void setRightToLeft()
    {
        //TODO:Change back to y axis
        RightToLeft = new Matrix4x4();
        RightToLeft.SetRow(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
        RightToLeft.SetRow(1, new Vector4(0.0f, -1.0f, 0.0f, 0.0f));
        RightToLeft.SetRow(2, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
        RightToLeft.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

    }

    private void setMarkerfOrigin()
    {

    }

    private double[] ConvertMatrixToArray(Matrix4x4 mat)
    {
        double[] res = new double[16];
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                res[i * 4 + j] = (double)mat[i, j];
            }
        }
        return res;
    }

    private Matrix4x4 ConvertArrayToMatrix(double[] arr)
    {
        Matrix4x4 res = new Matrix4x4();
        for(int i = 0; i < 16; i++)
        {
            res[i / 4, i % 4] = (float)arr[i];
        }
        return res;
    }

    private Quaternion ConvertRotMatToQuat(Matrix4x4 TransMat)
    {
        float qw = (float)Math.Sqrt(1.00 + TransMat[0, 0] + TransMat[1, 1] + TransMat[2, 2])/(float)2;
        float qx = (TransMat[2, 1] - TransMat[1, 2]) / (4 * qw);
        float qy = (TransMat[0, 2] - TransMat[2, 0]) / (4 * qw);
        float qz = (TransMat[1, 0] - TransMat[0, 1]) / (4 * qw);
        Quaternion quat = new Quaternion(qx, qy, qz, qw);
        return quat;
    }

    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        photoCaptureObject = captureObject;
        Resolution cameraResolution = new Resolution();
        cameraResolution.width = 1280;
        cameraResolution.height = 720;
        CameraParameters c = new CameraParameters();
        c.hologramOpacity = 0.0f;
        c.cameraResolutionWidth = cameraResolution.width;
        c.cameraResolutionHeight = cameraResolution.height;
        c.pixelFormat = CapturePixelFormat.BGRA32;

        captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            Debug.Log("world from head" + '\n' + Camera.main.transform.localToWorldMatrix.ToString());
            HeadfWorld = Camera.main.transform.localToWorldMatrix.inverse;
        }
        else
        {
            Debug.LogError("Unable to start photo mode!");
        }
    }
    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        Matrix4x4 WorldfCamera = new Matrix4x4();
        Matrix4x4 WorldfCameraV2 = new Matrix4x4();
        photoCaptureFrame.TryGetCameraToWorldMatrix(out WorldfCamera);
        WorldfCameraV2 = RightToLeft * WorldfCamera * RightToLeft;
        WorldfCamera.m02 = -1.0f * WorldfCamera.m02;
        WorldfCamera.m12 = -1.0f * WorldfCamera.m12;
        WorldfCamera.m22 = -1.0f * WorldfCamera.m22;
        Debug.Log("world from camera" + '\n' + WorldfCamera.ToString());
        Debug.Log("world from camera version 2" + '\n' + WorldfCameraV2.ToString());
        Debug.Log("head from world" + '\n' + HeadfWorld.ToString());
        Matrix4x4 HeadfCamera = HeadfWorld * WorldfCamera;
        Matrix4x4 HeadfCameraV2 = HeadfWorld * WorldfCameraV2;
        Debug.Log("head from camera" + '\n' + HeadfCamera.ToString("F9"));
        Debug.Log("head from camera version 2" + '\n' + HeadfCameraV2.ToString("F9"));
    }
#else
#endif

}
