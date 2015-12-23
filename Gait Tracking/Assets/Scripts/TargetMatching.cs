using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class TargetMatching : MonoBehaviour {

    FileIO logger;
    Vector3[] template;

    GameObject pelvis;
    GameObject footJoint;

    LineRenderer templateRenderer;
    LineRenderer ankleRenderer;

    CameraController cameraController;

    Joints joints;
    bool matching;
    private int sideIs;

    // Use this for initialization
    void Start ()
    {
        cameraController = GameObject.Find("CameraController").GetComponent<CameraController>();
        templateRenderer = GameObject.Find("Template").GetComponent<LineRenderer>();
        ankleRenderer = GameObject.Find("AnklePathRendererObject").GetComponent<LineRenderer>();

        pelvis = GameObject.Find("PELVIS");
        
        joints = GameObject.Find("RigidBodyStruct").GetComponent<Joints>();
        matching = false;
    }	
	// Update is called once per frame
	void Update ()
    {
	    if(matching)
        {
            if(footJoint == null)
            {
                footJoint = joints.getJoints()[(int)Joints.jointEnum.ankleJoint];
            }
            //Vector3[] minPoints = getMinPoints(localTemplate, footJoint.transform.position); //ToDo local/global template
            //float endWidth = Vector3.Distance(footJoint.transform.position, closestPointOnLine(minPoints[0], minPoints[1], footJoint.transform.position));
            //ankleRenderer.SetWidth(0.01f, endWidth);
        }
	}
    private void populateTemplate()
    {
        templateRenderer.SetVertexCount(template.Length);
        int i = 0;
        foreach (Vector3 v in template)
        {
            templateRenderer.SetPosition(i, v);
            i++;
        }
        setCameraBounds();
    }
    public void loadTemplate()
    {
        logger = GameObject.Find("RigidBodyStruct").GetComponent<Joints>().getFileIO();

        Queue<string[]> data = logger.ImportGrid();
        Vector3[] vectors = new Vector3[data.Count];
        int i = 0;
        while(data.Count>0)
        {
            string[] vectorString = data.Dequeue();
            vectors[i] = new Vector3(float.Parse(vectorString[2]), float.Parse(vectorString[1]), float.Parse(vectorString[0])); //ToDo switch back to 0,1,2
            i++;
        }

        template = vectors;
        populateTemplate();
        matching = true;
    }

    private void setCameraBounds()
    {
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity, maxZ = float.NegativeInfinity;
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity, minZ = float.PositiveInfinity;

        foreach (Vector3 v in template)
        {
            if (v.x > maxX)
            {
                maxX = v.x;
            }
            else if (v.x < minX)
            {
                minX = v.x;
            }

            if (v.y > maxY)
            {
                maxY = v.y;
            }
            else if(v.y < minY)
            {
                minY = v.y;
            }

            if (v.z > maxZ)
            {
                maxZ = v.z;
            }
            else if(v.z < minZ)
            {
                minZ = v.z;
            }
        }

        Debug.Log(minX.ToString("0.000") + " " + minY.ToString("0.000") + " " + minZ.ToString("0.000"));
        Debug.Log(maxX.ToString("0.000") + " " + maxY.ToString("0.000") + " " + maxZ.ToString("0.000"));

        if (templateRenderer.useWorldSpace == false)
        {
            cameraController.setTemplateBounds(maxX, minX, maxY, minY, maxZ, minZ, true);
        }
        else
        {
            cameraController.setTemplateBounds(maxX, minX, maxY, minY, maxZ, minZ, false);
        }

    }

    private Vector3[] getMinPoints(Vector3[] points, Vector3 point)
    {
        int minPoint = -1;
        int secondMinPoint = -1;

        int i = 0;
        foreach(Vector3 v in points)
        {
            if(minPoint == -1)
            {
                minPoint = i;
            }
            else if(secondMinPoint == -1)
            {
                if(Vector3.Distance(point, v) >= Vector3.Distance(point, points[minPoint]))
                {
                    secondMinPoint = i;
                }
                else
                {
                    secondMinPoint = minPoint;
                    minPoint = i;
                }
            }
            else
            {
                if(Vector3.Distance(point, v) < Vector3.Distance(point, points[minPoint]))
                {
                    secondMinPoint = minPoint;
                    minPoint = i;
                }
                else if(Vector3.Distance(point, v) == Vector3.Distance(point, points[minPoint]) || Vector3.Distance(point, v) < Vector3.Distance(point, points[secondMinPoint]))
                {
                    secondMinPoint = i;
                }
            }
            i++;
        }

       Vector3[] results = { points[minPoint], points[secondMinPoint] };
        return results;
    }

    private Vector3 closestPointOnLine(Vector3 vA, Vector3 vB, Vector3 vPoint)
    {
            Vector3 vVector1 = vPoint - vA;
            Vector3 vVector2 = (vB - vA).normalized;

            float d = Vector3.Distance(vA, vB);
            float t = Vector3.Dot(vVector2, vVector1);

            if (t <= 0)
        {
            return vA;
        }


            if (t >= d)
        {
            return vB;
        }

            Vector3 vVector3 = vVector2 * t;

            Vector3 vClosestPoint = vA + vVector3;

            return vClosestPoint;
    }

    public void TemplateTypeButton()
    {
        int selection = GameObject.Find("TemplateTypeDropdown").GetComponent<Dropdown>().value;
        if(selection == 0)
        {
            templateRenderer.useWorldSpace = false;
        }
        else if(selection == 1)
        {
            templateRenderer.useWorldSpace = true;
        }
        setCameraBounds();
    }
}
