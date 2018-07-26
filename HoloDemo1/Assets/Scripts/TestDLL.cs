using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.WebCam;
using System.Runtime.InteropServices;

enum MatrixType { MarkerfOrigin = 0, HeadfCamera = 1, HeadfOrigin = 2, RightToLeft = 3, ViewMatrix = 4, CamerafWorld = 5};

public class TestDLL : MonoBehaviour {
    static IntPtr nativeLibraryPtr;

    [DllImport("OpenCVAruco.dll")]
    public static extern void StartCameraModule();

    [DllImport("OpenCVAruco.dll")]
    public static extern void DetectMarkersAruco();

    [DllImport("OpenCVAruco.dll")]
    private static extern void RegisterDebugCallback(DebugCallback callback);

    [DllImport("OpenCVAruco.dll")]
    private static extern void RegisterPassMatrix4x4Callback(PassMatrix4x4Callback callback);

    [DllImport("OpenCVAruco.dll")]
    public static extern void PassInMatrix(double[] arr, int rows, int cols, int type);



    public delegate void DebugCallback(string str);
    public delegate void PassMatrix4x4Callback(IntPtr arr_ptr, int mtype);
    //public delegate void StartCameraModule();

    Matrix4x4 HeadfCamera;
    Matrix4x4 MarkerfOrigin;
    Matrix4x4 HeadfOrigin;
    Matrix4x4 RightToLeft;
    Matrix4x4 WorldfCamera;

    Matrix4x4 HeadfWorld;

    GameObject CameraChild;
    GameObject PreviousCamera;
    PhotoCapture photoCaptureObject;

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

        RegisterDebugCallback(new DebugCallback(debugFunction));
        RegisterPassMatrix4x4Callback(new PassMatrix4x4Callback(passMatrix4x4Function));

        setHeadfCamera();
        setRightToLeft();
        PassInMatrix(HeadfCamera, MatrixType.HeadfCamera);
        PassInMatrix(RightToLeft, MatrixType.RightToLeft);
        Debug.Log("set head finished");

        StartCameraModule();
        //NativeHelper.Invoke<StartCameraModule>(nativeLibraryPtr);
        StartCoroutine(CallTrackingMethod());
        PreviousCamera = new GameObject("PreviousCamera");
        CameraChild = new GameObject("CameraChild");
        CameraChild.transform.parent = Camera.main.transform;
        //CameraChild.transform.parent = PreviousCamera.transform;


    }

    IEnumerator CallTrackingMethod()
    {
        while (true)
        {
            //Debug.Log("coroutine called");
            DetectMarkersAruco();
            yield return new WaitForSeconds(1.0f/5);

        }
    }

    void debugFunction(string str)
    {
        Debug.Log(str);
    }

    void passMatrix4x4Function(IntPtr arr_ptr, int mtype )
    {
        double[] arr_copy = new double[16];
        Marshal.Copy(arr_ptr, arr_copy, 0, 16);
        if (mtype == (int)MatrixType.HeadfOrigin)
        {
            HeadfOrigin = ConvertArrayToMatrix(arr_copy);
            //Debug.Log("matrix call back called" + '\n' + HeadfOrigin.ToString());
            CameraChild.transform.localRotation = ConvertRotMatToQuat(HeadfOrigin);
            CameraChild.transform.localPosition = new Vector3(HeadfOrigin[0, 3], HeadfOrigin[1, 3], HeadfOrigin[2, 3]);
            this.gameObject.transform.rotation = CameraChild.transform.rotation;
            this.gameObject.transform.position = CameraChild.transform.position;
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
        else if(mtype == (int)MatrixType.CamerafWorld)
        {
            PreviousCamera.transform.position = Camera.main.transform.position;
            PreviousCamera.transform.rotation = Camera.main.transform.rotation;
        }
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
        HeadfCamera.SetRow(0, new Vector4(0.9999533f, 0.002300503f, -0.00933247f, -0.001294257f));
        HeadfCamera.SetRow(1, new Vector4(-0.002309966f, 0.9999966f, -0.001006001f, 0.02103565f));
        HeadfCamera.SetRow(2, new Vector4(0.009330086f, 0.001027526f, 0.9999559f, 0.065153553f));
        HeadfCamera.SetRow(3, new Vector4(0f, 0f, 0f, 1f));
        //Debug.Log("Test: " + (HeadfCamera * new Vector4(0, 0, 0, 1)).ToString("F6"));
        
        return;
    }

    private void setRightToLeft()
    {
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
        for(int i = 0; i < 4; i++)
        {
            for(int j = 0; j < 4; j++)
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
            Debug.Log("world from head" + '\n' + Camera.main.transform.localToWorldMatrix);
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
        photoCaptureFrame.TryGetCameraToWorldMatrix(out WorldfCamera);
        WorldfCamera.m02 = -1.0f * WorldfCamera.m02;
        WorldfCamera.m12 = -1.0f * WorldfCamera.m12;
        WorldfCamera.m22 = -1.0f * WorldfCamera.m22;
        Debug.Log("world from camera" + '\n' + WorldfCamera.ToString());
        Debug.Log("head from world" + '\n' + HeadfWorld.ToString());
        Matrix4x4 HeadfCamera = HeadfWorld * WorldfCamera;
        Debug.Log("head from camera" + '\n' + HeadfCamera.ToString("F9"));
    }

}
