using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.Persistence;
using UnityEngine.XR.WSA;
public class OriginPosition : MonoBehaviour {
    Vector3 offset = new Vector3(0, -0.15f, 0.4f);

    //WorldAnchorStore anchorStore;
    //string anchorName = "laparo_anchor";
    public bool stable;
    public bool anchor_change;
    public Vector3 offset_vec;
    private Matrix4x4 HeadfCamera;
    GameObject CameraChild;
    // Use this for initialization
    void Start () {
        stable = true;
        //offset_vec = new Vector3();

        //HeadfCamera = new Matrix4x4();
        ////HeadfCamera.SetRow(0, new Vector4(0.9999533f, 0.002300503f, -0.00933247f, -0.001294257f));
        ////HeadfCamera.SetRow(1, new Vector4(-0.002309966f, 0.9999966f, -0.001006001f, 0.02103565f));
        ////HeadfCamera.SetRow(2, new Vector4(0.009330086f, 0.001027526f, 0.9999559f, 0.065153553f));
        ////HeadfCamera.SetRow(3, new Vector4(0f, 0f, 0f, 1f));


        //HeadfCamera.SetRow(0, new Vector4(0.999901500f, -0.013928400f, -0.000545241f, -0.001265314f));
        //HeadfCamera.SetRow(1, new Vector4(0.013928410f, 0.999900000f, 0.000278913f, 0.020772190f));
        //HeadfCamera.SetRow(2, new Vector4(0.000540711f, -0.000287881f, 1.000002000f, 0.064527340f));
        //HeadfCamera.SetRow(3, new Vector4(0f, 0f, 0f, 1f));
        //Matrix4x4 CamerafHead = HeadfCamera.inverse;
        //offset_vec = new Vector3(CamerafHead[0, 3], CamerafHead[1, 3], CamerafHead[2, 3]);
        //Debug.Log("Result Vector" + offset_vec.ToString("F4"));
        //CameraChild = new GameObject("CameraChild");
        //CameraChild.transform.parent = Camera.main.transform;
    }
	
	// Update is called once per frame
	void Update () {
        if (!stable)
        {
            Vector3 camera_angle = Camera.main.transform.eulerAngles;
            Vector3 self_angle = this.transform.eulerAngles;
            this.transform.eulerAngles = new Vector3(self_angle.x, camera_angle.y, self_angle.z);
            this.transform.position = Camera.main.transform.position +
                Quaternion.AngleAxis(camera_angle.y, Vector3.up) * offset;
        }
        if (anchor_change)
        {
            if (stable)
            {
                //CameraChild.transform.position = this.transform.position;
                //CameraChild.transform.localPosition += offset_vec;
                //this.transform.position = CameraChild.transform.position; 
                addAnchor();
            }
            else
            {
                removeAnchor();
            }
            anchor_change = false;
        }

    }
    public void addAnchor()
    {
        WorldAnchor anchor = this.gameObject.AddComponent<WorldAnchor>();
        Debug.Log("anchor is added");
    }

    public void removeAnchor()
    {
        Destroy(gameObject.GetComponent<WorldAnchor>());
        Debug.Log("anchor is removed");
    }

    private Quaternion ConvertRotMatToQuat(Matrix4x4 TransMat)
    {
        float qw = (float)Math.Sqrt(1.00 + TransMat[0, 0] + TransMat[1, 1] + TransMat[2, 2]) / (float)2;
        float qx = (TransMat[2, 1] - TransMat[1, 2]) / (4 * qw);
        float qy = (TransMat[0, 2] - TransMat[2, 0]) / (4 * qw);
        float qz = (TransMat[1, 0] - TransMat[0, 1]) / (4 * qw);
        Quaternion quat = new Quaternion(qx, qy, qz, qw);
        return quat;
    }

}
