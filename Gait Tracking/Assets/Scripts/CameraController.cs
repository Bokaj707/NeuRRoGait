using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class CameraController : MonoBehaviour {

    GameObject orthoCam;
    GameObject hipJoint;
    GameObject ankleJoint;
    Action planeUpdate;

    bool cameraSet;
    bool showingMenu;
    bool templateSet;
    bool spanned;
    bool attatchedToHip;

    float x, y, z, minX, maxX, minY, maxY, minZ, maxZ;

    float bounding;
    float[] orthoSizes;
    GameObject[] centers;
    int sideIs;
    

    // Use this for initialization
    void Start ()
    {
        orthoCam = GameObject.Find("OrthoCam");
        cameraSet = false;
        showingMenu = false;
        templateSet = false;
        spanned = false;
        sideIs = 0;
        attatchedToHip = true;
        bounding = 1;

        x = y = z = 0;

        orthoSizes = new float[3];
        centers = new GameObject[3];
        int i = 0;
        foreach(GameObject g in centers)
        {
            centers[i] = new GameObject("CameraCenter" + i);
            i++;
        }
    }
	
	// Update is called once per frame
	void Update ()
    {
	    if(cameraSet)
        {
            planeUpdate();
        }
	}

    public void StartController(GameObject hipJoint, GameObject ankleJoint)
    {
        this.hipJoint = hipJoint;
        this.ankleJoint = ankleJoint;
        setPlane(0);
    }

    public void setPlane(int plane)
    {
        cameraSet = false;
        switch(plane)
        {
            case 0:
                {
                    planeUpdate = TrackSagital;
                    break;
                }
            case 1:
                {
                    planeUpdate = TrackFrontal;
                    break;
                }
            case 2:
                {
                    planeUpdate = TrackTransverse;
                    break;
                }
        }
        planeUpdate();
    }

    private void TrackSagital()
    {
        if(!templateSet)
        {
            orthoCam.transform.position = new Vector3(hipJoint.transform.position.x - 1 * -sideIs, hipJoint.transform.position.y / 2, hipJoint.transform.position.z);
            orthoCam.transform.rotation = Quaternion.LookRotation(Vector3.left * sideIs, Vector3.up);
        }
        else if (!cameraSet)
        {
            orthoCam.transform.position = new Vector3(hipJoint.transform.position.x - 1 * -sideIs, centers[0].transform.position.y, centers[0].transform.position.z);
            orthoCam.transform.rotation = Quaternion.LookRotation(Vector3.left * sideIs, Vector3.up);
            orthoCam.GetComponent<Camera>().orthographicSize = orthoSizes[0];
            if (attatchedToHip)
            {
                orthoCam.transform.parent = GameObject.Find("Template").transform;
            }
            else
            {
                orthoCam.transform.parent = null;
            }
            cameraSet = true;
        }
    }
    private void TrackFrontal()
    {
        if (!templateSet)
        {
            orthoCam.transform.position = new Vector3(hipJoint.transform.position.x, hipJoint.transform.position.y / 2, hipJoint.transform.position.z + 1);
            orthoCam.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
        }
        else if(!cameraSet)
        {
            orthoCam.transform.position = new Vector3(centers[1].transform.position.x, centers[1].transform.position.y, hipJoint.transform.position.z + 1);
            orthoCam.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            orthoCam.GetComponent<Camera>().orthographicSize = orthoSizes[1];
            if (attatchedToHip)
            {
                orthoCam.transform.parent = GameObject.Find("Template").transform;
            }
            else
            {
                orthoCam.transform.parent = null;
            }
            cameraSet = true;
        }
    }

    private void TrackTransverse()
    {
        if (!templateSet)
        {
            orthoCam.transform.position = new Vector3(hipJoint.transform.position.x, hipJoint.transform.position.y + 1, hipJoint.transform.position.z);
            orthoCam.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        }
        else if(!cameraSet)
        {
            orthoCam.transform.position = new Vector3(centers[2].transform.position.x, hipJoint.transform.position.y + 1, centers[2].transform.position.z);
            orthoCam.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            orthoCam.GetComponent<Camera>().orthographicSize = orthoSizes[2];
            if (attatchedToHip)
            {
                orthoCam.transform.parent = GameObject.Find("Template").transform;
            }
            else
            {
                orthoCam.transform.parent = null;
            }
            cameraSet = true;
        }
    }

    public void SwapCameras()
    {
        Rect swapRect = orthoCam.GetComponent<Camera>().rect;
        float swapOrder = orthoCam.GetComponent<Camera>().depth;
        orthoCam.GetComponent<Camera>().rect = Camera.main.rect;
        orthoCam.GetComponent<Camera>().depth = Camera.main.depth;
        Camera.main.rect = swapRect;
        Camera.main.depth = swapOrder;
    }

    public void ShowMenu()
    {
        if(showingMenu)
        {
            GameObject.Find("CameraMenu").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, Screen.height*2);
            showingMenu = false;
        }
        else
        {
            GameObject.Find("CameraMenu").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            showingMenu = true;
        }
    }

    public void setTemplateBounds(float maxX, float minX, float maxY, float minY, float maxZ, float minZ, bool attatchToHip)
    {
        if (!attatchToHip)
        {
            foreach (GameObject g in centers)
            {
                g.transform.parent = null;
            }
        }

        x = maxX - minX; 
        y = maxY - minY;
        z = maxZ - minZ;
        this.minX = minX;
        this.minY = minY;
        this.minZ = minZ;
        this.maxX = maxX;
        this.maxY = maxY;
        this.maxZ = maxZ;
        attatchedToHip = attatchToHip;
        calculateBounds();
    }
    private void calculateBounds()
    {
        if (y > z) //sagital
        {
            orthoSizes[0] = y;
        }
        else
        {
            orthoSizes[0] = ((Screen.height * z) / (Screen.width)) / orthoCam.GetComponent<Camera>().rect.width;
        }
        centers[0].transform.position = new Vector3(0, minY + (y / 2), minZ + (z / 2));
        if (y > x) //frontal
        {
            orthoSizes[1] = y;
        }
        else
        {
            orthoSizes[1] = ((Screen.height * x) / (Screen.width)) / orthoCam.GetComponent<Camera>().rect.width;
        }
        centers[1].transform.position = new Vector3(minX + x / 2, minY + y / 2, 0);
        if (z > x) //transverse
        {
            orthoSizes[2] = z;
        }
        else
        {
            orthoSizes[2] = ((Screen.height * x) / (Screen.width)) / orthoCam.GetComponent<Camera>().rect.width;
        }

        int i = 0;
        foreach (float j in orthoSizes)
        {
            orthoSizes[i] /= 2;
            orthoSizes[i] *= bounding;
            i++;
        }

        centers[2].transform.position = new Vector3(0, minY + y / 2, minZ + z / 2);
        if (attatchedToHip)
        {
            foreach (GameObject g in centers)
            {
                g.transform.position = GameObject.Find("Template").transform.TransformPoint(g.transform.position);
                g.transform.parent = GameObject.Find("Template").transform;
            }
        }

        Debug.Log(orthoSizes[0] + " " + orthoSizes[1] + " " + orthoSizes[2]);
        templateSet = true;
        if (planeUpdate != null)
        {
            cameraSet = false;
            planeUpdate();
        }
    }
    public void setCameraBounding()
    {
        int i = 0;
        foreach(float f in orthoSizes)
        {
            orthoSizes[i] = f / bounding;
            i++;
        }
        bounding = 1+float.Parse(GameObject.Find("ThresholdInputField").GetComponent<InputField>().text);
        i = 0;
        foreach (float f in orthoSizes)
        {
            orthoSizes[i] = f * bounding;
            i++;
        }
        if(planeUpdate != null)
        {
            cameraSet = false;
            planeUpdate();
        }
    }

    public void spanViews()
    {
        if(!spanned)
        {
            orthoCam.GetComponent<Camera>().rect = new Rect(0, 0, .5f, 1);
            Camera.main.rect = new Rect(0.5f, 0, .5f, 1);
            if(templateSet)
            {
                int i = 0;
                foreach (float f in orthoSizes)
                {
                    orthoSizes[i] = f / orthoCam.GetComponent<Camera>().rect.width;
                    i++;
                }
            }

            spanned = true;
        }
        else
        {
            int i = 0;
            if (templateSet)
            {
                foreach (float f in orthoSizes)
                {
                    orthoSizes[i] = f * orthoCam.GetComponent<Camera>().rect.width;
                    i++;
                }
            }
            orthoCam.GetComponent<Camera>().rect = Camera.main.rect = new Rect(0, 0, 1f, 1f);
            spanned = false;
        }
        if(templateSet)
        {
            calculateBounds();
        }
    }

    public void setSide(int side)
    {
        sideIs = side;
    }
}
