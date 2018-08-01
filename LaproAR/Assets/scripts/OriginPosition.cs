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
    // Use this for initialization
    void Start () {
        stable = true;
	}
	
	// Update is called once per frame
	void Update () {
        if (!stable) {
            Vector3 camera_angle = Camera.main.transform.eulerAngles;
            Vector3 self_angle = this.transform.eulerAngles;
            this.transform.eulerAngles = new Vector3(self_angle.x, camera_angle.y, self_angle.z);
            this.transform.position = Camera.main.transform.position +
                Quaternion.AngleAxis(camera_angle.y, Vector3.up) * offset;
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
    
}
