using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

enum MatrixType { MarkerfOrigin = 0, HeadfCamera = 1, HeadfOrigin = 2};

public class TestDLL : MonoBehaviour {
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
    public delegate void PassMatrix4x4Callback(IntPtr arr_ptr);

    Matrix4x4 HeadfCamera;
    Matrix4x4 MarkerfOrigin;
    Matrix4x4 HeadfOrigin;

	// Use this for initialization
	void Start () {

        RegisterDebugCallback(new DebugCallback(debugFunction));
        RegisterPassMatrix4x4Callback(new PassMatrix4x4Callback(passMatrix4x4Function));

        setHeadfCamera();
        PassInMatrix(HeadfCamera, MatrixType.HeadfCamera);
        Debug.Log("set head finished");
        
        StartCameraModule();
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

    void passMatrix4x4Function(IntPtr arr_ptr)
    {
        double[] arr_copy = new double[16];
        Marshal.Copy(arr_ptr, arr_copy, 0, 16);
        HeadfOrigin = ConvertArrayToMatrix(arr_copy);
        Debug.Log("matrix call back called" + '\n' + HeadfOrigin.ToString());
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

    void PassInMatrix(Matrix4x4 mat, MatrixType mtype)
    {
        double[] data;
        if (mtype != MatrixType.HeadfOrigin)
        {
            data = ConvertMatrixToArray(mat);
            PassInMatrix(data, 4, 4, (int)mtype);
        }
        else
        {
            data = new double[16];
            PassInMatrix(data, 4, 4, (int)mtype);
            HeadfOrigin = ConvertArrayToMatrix(data);
        }
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
            Debug.Log(i);
        }
        return res;
    }

    private void setHeadfCamera()
    {
        HeadfCamera = new Matrix4x4();
        HeadfCamera.SetRow(0, new Vector4(0.9999533f, 0.002300503f, -0.00933247f, -0.001294257f));
        HeadfCamera.SetRow(1, new Vector4(-0.002309966f, 0.9999966f, -0.001006001f, 0.02103565f));
        HeadfCamera.SetRow(2, new Vector4(0.009330086f, 0.001027526f, 0.9999559f, 0.065153553f));
        HeadfCamera.SetRow(3, new Vector4(0f, 0f, 0f, 1f));
        //Debug.Log("Test: " + (HeadfCamera * new Vector4(0, 0, 0, 1)).ToString("F6"));
        return;
    }

    private void setMarkerfOrigin()
    {

    }
}
