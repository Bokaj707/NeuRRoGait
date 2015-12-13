using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class Joints : MonoBehaviour {

    GameObject stylus;
    GameObject stylusPoint;

    LineRenderer thighRenderer;
    LineRenderer shankRenderer;
    LineRenderer pelvisRenderer;
    LineRenderer footRenderer;

    GameObject thigh;
    GameObject shank;
    GameObject pelvis;
    GameObject foot;

    GameObject kneeAngleDisplay;
    GameObject hipAngleDisplay;
    GameObject ankleAngleDisplay;
    GameObject anklePathRendererObject;

    Text display;

    Vector3 hipToKnee;
    Vector3 ankleToKnee;
    Vector3 footToAnkle;
    Vector3 trunkToHip;

    GameObject[] interpolationPoints;
    Action<Vector3> interpAction;
    GameObject virtualParent;

    GameObject averageCam;

    int inverter;
    static int RIGHT_LEG_INVERT = 1;
    static int LEFT_LEG_INVERT = -1;
    static int TARGET_FRAME_RATE = 120;

    bool tPosed;
    bool calculate;
    bool interpolatingPoint;
    bool logging;

    Logger logger;

    Puppet skeleton;

    Queue<Vector3> anklePoints;
    int currentPoint;
    int capacity;

    public enum jointAnglesEnum : int {hipAngle, kneeAngle, ankleAngle};
    public enum jointEnum : int {hipJoint, hipJointPartner, kneeJoint, kneeJointPartner, ankleJoint, ankleJointPartner};
    public enum virtualMarkersEnum : int {ASIS1, ASIS2, PSIS1, PSIS2, FE1, FE2, LM, MM};
    public GameObject[] virtualMarkers;
    public GameObject[] joints;

    float[] angles;
    float[] offsets;

    void Awake()
    {
        Application.targetFrameRate = TARGET_FRAME_RATE;
    }

    // Use this for initialization
    void Start ()
    {
        inverter = 0;
        virtualMarkers = new GameObject[8];
        joints = new GameObject[6];
        interpolationPoints = new GameObject[4];

        stylus = GameObject.Find("Stylus");
        thigh = GameObject.Find("THIGH");
        shank = GameObject.Find("SHANK");
        foot = GameObject.Find("FOOT");
        pelvis = GameObject.Find("PELVIS");

        footRenderer = foot.GetComponent<LineRenderer>();
        pelvisRenderer = pelvis.GetComponent<LineRenderer>();
        shankRenderer = shank.GetComponent<LineRenderer>();
        thighRenderer = thigh.GetComponent<LineRenderer>();

        hipAngleDisplay = GameObject.Find("HipAngleDisplay");
        kneeAngleDisplay = GameObject.Find("KneeAngleDisplay");
        ankleAngleDisplay = GameObject.Find("AnkleAngleDisplay");
        display = GameObject.Find("Display").GetComponent<Text>();

        tPosed = false;
        interpolatingPoint = false;
        calculate = false;

        angles = new float[9];
        offsets = new float[9];

        for(int i = 0; i < 9; i++)
        {
            angles[i] = offsets[i] = 0f;
        }

        skeleton = GameObject.Find("LegContainer").GetComponent<Puppet>();
        averageCam = new GameObject("CameraFocus");  
        
        string[] headers = { "Time Stamp",
            "Hip Pitch", "Hip Roll", "Hip Yaw",
            "Knee Pitch", "Knee Roll", "Knee Yaw",
            "Ankle Pitch","Ankle Roll","Ankle Yaw",
            "Hjoint X","Hjoint Y","Hjoint Z",
            "Kjoint X","Kjoint Y","Kjoint Z",
            "Ajoint X","Ajoint Y","Ajoint Z",
            "Ankle Path",
            "Hip OffPitch", "Hip OffRoll", "Hip OffYaw",
            "Knee OffPitch", "Knee OffRoll", "Knee OffYaw",
            "Ankle OffPitch","Ankle OffRoll","Ankle OffYaw",};

        logger = new Logger(headers.Length, Application.dataPath + @"\Logs\", string.Format("session-{0:yyyy-MM-dd_hh-mm-ss-tt}.txt", DateTime.Now), 600, headers);
        logging = false;

        anklePoints = new Queue<Vector3>();
        currentPoint = 0;
        capacity = 240;

        anklePathRendererObject = GameObject.Find("AnklePathRendererObject");
        anklePathRendererObject.GetComponent<LineRenderer>().SetVertexCount(capacity);

        for(int i = 0; i < capacity; i++)
        {
            anklePathRendererObject.GetComponent<LineRenderer>().SetPosition(i, Vector3.zero);
            i++;
        }        
    }

    // Update is called once per frame
    void Update()
    {
        
        if (interpolatingPoint && Input.GetKeyDown(KeyCode.Mouse1))
        {
            Debug.Log("setting markers");
            if (interpolationPoints[0]== null)
            {
                Console.Out.WriteLine("setting 1");
                GameObject point = new GameObject("Temp Marker 1");
                point.transform.position = stylusPoint.transform.position;
                point.transform.parent = virtualParent.transform;
                interpolationPoints[0] = point;
                if (interpAction == SetHipJoint)
                {
                    display.text = "ASIS2";
                }
                else if (interpAction == SetKneeJoint)
                {
                    display.text = "FE2";
                }
                else
                {
                    display.text = "MM";
                }

            }
            else if (interpolationPoints[1] == null)
            {
                Debug.Log("setting 2");
                GameObject point = new GameObject("Temp Marker 2");
                point.transform.position = stylusPoint.transform.position;
                point.transform.parent = virtualParent.transform;
                interpolationPoints[1] = point;
                if (interpAction != SetHipJoint)
                {
                    InterpolatePoint(interpolationPoints, interpAction);
                    interpolatingPoint = false;
                    display.text = "";
                }
                else
                {
                    display.text = "PSIS1";
                }
            }
            else if (interpolationPoints[2] == null)
            {
                Debug.Log("setting 3");
                GameObject point = new GameObject("Temp Marker 3");
                point.transform.position = stylusPoint.transform.position;
                point.transform.parent = virtualParent.transform;
                interpolationPoints[2] = point;
                display.text = "PSIS2";
            }
            else if (interpolationPoints[3] == null)
            {
                Debug.Log("setting 4");
                GameObject point = new GameObject("Temp Marker 4");
                point.transform.position = stylusPoint.transform.position;
                point.transform.parent = virtualParent.transform;
                interpolationPoints[3] = point;
                SetSide();
                CalculateOffsetPoint(interpolationPoints, 0.12f, 0.11f, 0.21f, interpAction); //TODO: variable %s try 34, 32, 22 : 12, 11, 21
                interpolatingPoint = false;
                display.text = "";
            }
        }
    

        if (tPosed)
        {
            thighRenderer.SetPosition(0, joints[(int)jointEnum.hipJoint].transform.position);
            thighRenderer.SetPosition(1, joints[(int)jointEnum.kneeJoint].transform.position);
            shankRenderer.SetPosition(0, joints[(int)jointEnum.kneeJoint].transform.position);
            shankRenderer.SetPosition(1, joints[(int)jointEnum.ankleJoint].transform.position);
            pelvisRenderer.SetPosition(0, joints[(int)jointEnum.hipJoint].transform.position + joints[(int)jointEnum.hipJoint].transform.up * 0.05f);
            pelvisRenderer.SetPosition(1, joints[(int)jointEnum.hipJoint].transform.position);
            footRenderer.SetPosition(0, joints[(int)jointEnum.ankleJoint].transform.position);
            footRenderer.SetPosition(1, joints[(int)jointEnum.ankleJoint].transform.position + joints[(int)jointEnum.ankleJoint].transform.forward * 0.05f);
        }
        else
        {
            thighRenderer.SetPosition(0, thigh.transform.position);
            thighRenderer.SetPosition(1, shank.transform.position);
            shankRenderer.SetPosition(0, shank.transform.position);
            shankRenderer.SetPosition(1, foot.transform.position);
            pelvisRenderer.SetPosition(0, pelvis.transform.position);
            pelvisRenderer.SetPosition(1, thigh.transform.position);
            footRenderer.SetPosition(0, foot.transform.position);
            footRenderer.SetPosition(1, foot.transform.position);

            bool b = true;
            foreach (GameObject obj in joints)
            {
                if(obj == null)
                {
                    b = false;
                    break;
                }
            }
            if (b == true)
            {
                CalculateJointAngles();
            }

        }
        UpdateCamera();
    }

    void FixedUpdate()
    {
        if (tPosed)
        {
            CalculateJointAngles();
            updateAnklePath();
            if (logging)
            {
                logger.Log();
            }
        }
    }


    private void UpdateCamera()
    {
        Camera.main.transform.position = pelvis.transform.position + (Vector3.left);
        if (tPosed)
        {
            Camera.main.transform.LookAt(Vector3.Lerp(pelvis.transform.position, new Vector3(pelvis.transform.position.x, 0, pelvis.transform.position.z), 0.5f));
            //hipAngleDisplay.transform.rotation = Quaternion.LookRotation(Vector3.Normalize(hipAngleDisplay.transform.position - Camera.main.transform.position), Vector3.up);
            //kneeAngleDisplay.transform.rotation = Quaternion.LookRotation(Vector3.Normalize(kneeAngleDisplay.transform.position - Camera.main.transform.position), Vector3.up);
            //ankleAngleDisplay.transform.rotation = Quaternion.LookRotation(Vector3.Normalize(ankleAngleDisplay.transform.position - Camera.main.transform.position), Vector3.up);
        }
        else
        {
            Camera.main.transform.LookAt((foot.transform.position + thigh.transform.position + pelvis.transform.position + shank.transform.position) / 4);
        }
    }

    private void CalculateJointAngles()
    {
        bool logSkip = false;
        string[] angleOutputs = new string[3];

        if (!tPosed)
        {
            PoseHipJoint();
            PoseKneeJoint();
            PoseAnkleJoint();

            int q = 0;
            string vmName = "VMTrunk";
            foreach(GameObject marker in virtualMarkers)
            {   
                if( 4 < q && q < 7)
                {
                    vmName = "VMThigh";
                }
                else if (q >= 7)
                {
                    vmName = "VMShank";
                }
                GameObject vm = SetPoint(marker.transform.parent.gameObject, marker, marker.transform.position);
                vm.transform.FindChild("PointSphere").GetComponent<MeshRenderer>().material = GameObject.Find(vmName).GetComponent<MeshRenderer>().material;
            }

            //hipAngleDisplay.transform.position = joints[(int)jointEnum.hipJoint].transform.position + Vector3.forward * 0.5f;
            //hipAngleDisplay.transform.parent = joints[(int)jointEnum.hipJoint].transform;
            //kneeAngleDisplay.transform.position = joints[(int)jointEnum.kneeJoint].transform.position + Vector3.forward * 0.5f;
            //kneeAngleDisplay.transform.parent = joints[(int)jointEnum.kneeJoint].transform;
            //ankleAngleDisplay.transform.position = joints[(int)jointEnum.ankleJoint].transform.position + Vector3.forward * 0.5f;
            //ankleAngleDisplay.transform.parent = joints[(int)jointEnum.ankleJoint].transform;

            averageCam.transform.position = (foot.transform.position + thigh.transform.position + pelvis.transform.position + shank.transform.position) / 4;
            averageCam.transform.parent = pelvis.transform;

            skeleton.setSide(inverter);

            logSkip = true;
        }

        joints[(int)jointEnum.hipJointPartner].transform.position = joints[(int)jointEnum.hipJoint].transform.position;
        joints[(int)jointEnum.kneeJointPartner].transform.position = joints[(int)jointEnum.kneeJoint].transform.position;
        joints[(int)jointEnum.ankleJointPartner].transform.position = joints[(int)jointEnum.ankleJoint].transform.position;

        Vector3 floatingHip = Vector3.Cross(joints[(int)jointEnum.hipJoint].transform.right, joints[(int)jointEnum.hipJointPartner].transform.up);
        Vector3 floatingKnee = Vector3.Cross(joints[(int)jointEnum.kneeJoint].transform.right, joints[(int)jointEnum.kneeJointPartner].transform.up);
        Vector3 floatingAnkle = Vector3.Cross(joints[(int)jointEnum.ankleJoint].transform.right, joints[(int)jointEnum.ankleJointPartner].transform.up);

        angles[0] = Mathf.Acos(Vector3.Dot(joints[(int)jointEnum.hipJoint].transform.forward, floatingHip)) * Math.Sign(Vector3.Dot(joints[(int)jointEnum.hipJoint].transform.up, floatingHip));
        angles[1]= Mathf.Acos(Vector3.Dot(joints[(int)jointEnum.hipJointPartner].transform.forward, floatingHip)) * Math.Sign(Vector3.Dot(joints[(int)jointEnum.hipJoint].transform.up, floatingHip)) * inverter * -(Math.Sign(Vector3.Dot(joints[(int)jointEnum.hipJointPartner].transform.right, floatingHip)));
        angles[2]= Mathf.Acos(Vector3.Dot(joints[(int)jointEnum.hipJoint].transform.right, joints[(int)jointEnum.hipJointPartner].transform.up)) - inverter * (-Mathf.PI/2);

        angles[3] = Mathf.Acos(Vector3.Dot(joints[(int)jointEnum.kneeJoint].transform.forward, floatingKnee)) * Math.Sign(Vector3.Dot(joints[(int)jointEnum.kneeJoint].transform.up, floatingKnee)); //flex/ex
        angles[4] = Mathf.Acos(Vector3.Dot(joints[(int)jointEnum.kneeJointPartner].transform.forward, floatingKnee)) * Math.Sign(Vector3.Dot(joints[(int)jointEnum.kneeJoint].transform.up, floatingKnee)) * inverter * -(Math.Sign(Vector3.Dot(joints[(int)jointEnum.kneeJointPartner].transform.right, floatingKnee))); //rotation
        angles[5] = Mathf.Acos(Vector3.Dot(joints[(int)jointEnum.kneeJoint].transform.right, joints[(int)jointEnum.kneeJointPartner].transform.up)) - inverter * (-Mathf.PI/2); //ad/ab

        angles[6] = Mathf.Acos(Vector3.Dot(joints[(int)jointEnum.ankleJoint].transform.forward, floatingAnkle) * Math.Sign(Vector3.Dot(joints[(int)jointEnum.ankleJoint].transform.up, floatingAnkle)));
        angles[7] = Mathf.Acos(Vector3.Dot(joints[(int)jointEnum.ankleJointPartner].transform.forward, floatingAnkle)) * Math.Sign(Vector3.Dot(joints[(int)jointEnum.ankleJoint].transform.up, floatingAnkle)) * inverter * -(Math.Sign(Vector3.Dot(joints[(int)jointEnum.ankleJointPartner].transform.right, floatingAnkle)));
        angles[8] = Mathf.Acos(Vector3.Dot(joints[(int)jointEnum.ankleJoint].transform.right, joints[(int)jointEnum.ankleJointPartner].transform.up)) - inverter * (-Mathf.PI/2);

        int i = 0;
        foreach(float angle in angles)
        {
            angles[i] = Mathf.Rad2Deg * angles[i];
            if(tPosed)
            {
                angles[i] -= offsets[i];
            }
            i++;   
        }

        if(!logSkip)
        {
            
            angleOutputs[(int)jointAnglesEnum.hipAngle] = angles[0].ToString("0.0") + "\t" + angles[1].ToString("0.0") + "\t" + angles[2].ToString("0.0") + "\t";
            angleOutputs[(int)jointAnglesEnum.kneeAngle] = angles[3].ToString("0.0") + "\t" + angles[4].ToString("0.0") + "\t" + angles[5].ToString("0.0") + "\t";
            angleOutputs[(int)jointAnglesEnum.ankleAngle] = angles[6].ToString("0.0") + "\t" + angles[7].ToString("0.0") + "\t" + angles[8].ToString("0.0") + "\t";

            hipAngleDisplay.GetComponent<Text>().text = angleOutputs[(int)jointAnglesEnum.hipAngle];
            kneeAngleDisplay.GetComponent<Text>().text = angleOutputs[(int)jointAnglesEnum.kneeAngle];
            ankleAngleDisplay.GetComponent<Text>().text = angleOutputs[(int)jointAnglesEnum.ankleAngle];
        }

        if(!tPosed)
        {
            ZeroAngles();
            tPosed = true;
        }

        if (logging && !logSkip)
        {
            int j = 0;
            foreach (string s in angleOutputs)
            {
                logger.setColumn(j + 1, angleOutputs[j]);
                j++;
            }

            logger.setColumn(0, UnityEngine.Time.fixedTime.ToString());
        }
    }

    private float [] AngleDecomposition(Quaternion angle)//Returns Roll Pitch Yaw
    {
        float w = angle.w;
        float y = angle.y;
        float z = angle.z;
        float x = angle.x;

        float[] rpy = new float[3];
        rpy[0] = Mathf.Atan2(2 * y * w - 2 * x * z, 1 - 2 * y * y - 2 * z * z) * 180 / Mathf.PI;
        rpy[1] = Mathf.Atan2(2 * x * w - 2 * y * z, 1 - 2 * x * x - 2 * z * z) * 180 / Mathf.PI;
        rpy[2] = Mathf.Asin(2 * x * y + 2 * z * w) * 180 / Mathf.PI;

        return rpy;
    }
    private void PoseHipJoint()
    {
        Vector3 femur = Vector3.Normalize(joints[(int)jointEnum.hipJoint].transform.position - joints[(int)jointEnum.kneeJoint].transform.position);
        Vector3 pelvisLateral = inverter * Vector3.Normalize(virtualMarkers[(int)virtualMarkersEnum.ASIS1].transform.position - virtualMarkers[(int)virtualMarkersEnum.ASIS2].transform.position);
        Vector3 pelvisAnterior = Vector3.Normalize(Vector3.Lerp(virtualMarkers[(int)virtualMarkersEnum.ASIS1].transform.position, virtualMarkers[(int)virtualMarkersEnum.ASIS2].transform.position, 0.5f) -
            Vector3.Lerp(virtualMarkers[(int)virtualMarkersEnum.PSIS1].transform.position, virtualMarkers[(int)virtualMarkersEnum.PSIS2].transform.position, 0.5f));

        joints[(int)jointEnum.hipJoint].transform.rotation = Quaternion.LookRotation(pelvisAnterior, Vector3.Cross(pelvisLateral, -pelvisAnterior));
        joints[(int)jointEnum.hipJointPartner].transform.rotation = Quaternion.LookRotation(Vector3.Cross(inverter * Vector3.Normalize(virtualMarkers[(int)virtualMarkersEnum.FE1].transform.position - virtualMarkers[(int)virtualMarkersEnum.FE2].transform.position), femur), femur);
        joints[(int)jointEnum.hipJointPartner].transform.parent = thigh.transform; 
    }
    private void PoseKneeJoint()
    {
        Vector3 femur = Vector3.Normalize(joints[(int)jointEnum.hipJoint].transform.position - joints[(int)jointEnum.kneeJoint].transform.position);
        Vector3 tibia = Vector3.Normalize(joints[(int)jointEnum.kneeJoint].transform.position - joints[(int)jointEnum.ankleJoint].transform.position);
        Vector3 kneeLateral = inverter * Vector3.Normalize(virtualMarkers[(int)virtualMarkersEnum.FE1].transform.position - virtualMarkers[(int)virtualMarkersEnum.FE2].transform.position);

        joints[(int)jointEnum.kneeJoint].transform.rotation = Quaternion.LookRotation(Vector3.Cross(kneeLateral, femur), femur);
        joints[(int)jointEnum.kneeJointPartner].transform.rotation = Quaternion.LookRotation(Vector3.Cross(kneeLateral, tibia),tibia);
        joints[(int)jointEnum.kneeJointPartner].transform.parent = shank.transform;
    }
    private void PoseAnkleJoint()
    {
        Vector3 tibia = Vector3.Normalize(joints[(int)jointEnum.kneeJoint].transform.position - joints[(int)jointEnum.ankleJoint].transform.position);
        Vector3 ankleLateral = inverter * Vector3.Normalize(virtualMarkers[(int)virtualMarkersEnum.LM].transform.position - virtualMarkers[(int)virtualMarkersEnum.MM].transform.position);
        Vector3 anklePosterior = Vector3.Normalize(Vector3.Cross(ankleLateral, tibia));
        Vector3 footPosterior = Vector3.Normalize(Vector3.Cross(inverter * Vector3.Normalize(virtualMarkers[(int)virtualMarkersEnum.FE1].transform.position - virtualMarkers[(int)virtualMarkersEnum.FE2].transform.position), tibia));

        joints[(int)jointEnum.ankleJoint].transform.rotation = Quaternion.LookRotation(anklePosterior, Vector3.Cross(-ankleLateral, anklePosterior));
        joints[(int)jointEnum.ankleJointPartner].transform.rotation = Quaternion.LookRotation(footPosterior, tibia);
        joints[(int)jointEnum.ankleJointPartner].transform.parent = foot.transform;
    }
    public void ZeroAngles()
    {
        logger.Log();

        logger.setColumn(0, UnityEngine.Time.fixedTime.ToString());

        int i = 0;
        foreach (float angle in angles)
        {
            offsets[i] = angle + offsets[i];
            logger.setColumn(i+20, angle.ToString("0.00"));
            i++;
        }

        logger.Log();
    }
    private void InterpolatePoint(GameObject[] interpolationPoints, Action<Vector3> setCertainJoint)
    {
        Vector3 interp = Vector3.Lerp(interpolationPoints[0].transform.position, interpolationPoints[1].transform.position, 0.5f);
        setCertainJoint(interp);
    }
    private void CalculateOffsetPoint(GameObject[] interpolationPoints, float distalPercent, float medialPercent, float posteriorPercent, Action<Vector3> setCertainJoint)
    {
        Vector3 lateral = Vector3.Normalize(interpolationPoints[0].transform.position - interpolationPoints[1].transform.position);
        Vector3 posterior = Vector3.Normalize(Vector3.Cross(inverter * lateral, Vector3.down));

        float distance = Vector3.Distance(interpolationPoints[0].transform.position, interpolationPoints[1].transform.position);

        Vector3 medialInterp = Vector3.Lerp(interpolationPoints[0].transform.position, interpolationPoints[1].transform.position, medialPercent);
        Vector3 medialToDistalInterp = Vector3.Lerp(medialInterp, medialInterp + Vector3.down * distance, distalPercent);
        Vector3 finalInterp = Vector3.Lerp(medialToDistalInterp, medialInterp + posterior * distance, posteriorPercent);

        setCertainJoint(finalInterp);
    }
    public void SetHipJoint()
    {
        Debug.Log("setting hip");
        interpolationPoints[0] = interpolationPoints[1] = interpolationPoints[2] = interpolationPoints[3] = null;
        interpAction = SetHipJoint;
        virtualParent = pelvis;
        interpolatingPoint = true;
        display.text = "ASIS1";
    }
    public void SetHipJoint(Vector3 lerpPoint)
    {

        GameObject[] joints = SetJoint(pelvis, this.joints[(int)jointEnum.hipJoint], this.joints[(int)jointEnum.hipJointPartner], lerpPoint, interpolationPoints[1].transform.position - interpolationPoints[0].transform.position); //TODO: Right and up may need to be parameters
        virtualMarkers[(int)virtualMarkersEnum.ASIS1] = Instantiate(interpolationPoints[0], interpolationPoints[0].transform.position, Quaternion.identity) as GameObject;
        virtualMarkers[(int)virtualMarkersEnum.ASIS1].name = "ASIS1";
        virtualMarkers[(int)virtualMarkersEnum.ASIS2] = Instantiate(interpolationPoints[1], interpolationPoints[1].transform.position, Quaternion.identity) as GameObject;
        virtualMarkers[(int)virtualMarkersEnum.ASIS2].name = "ASIS2";
        virtualMarkers[(int)virtualMarkersEnum.PSIS1] = Instantiate(interpolationPoints[2], interpolationPoints[2].transform.position, Quaternion.identity) as GameObject;
        virtualMarkers[(int)virtualMarkersEnum.PSIS1].name = "PSIS1";
        virtualMarkers[(int)virtualMarkersEnum.PSIS2] = Instantiate(interpolationPoints[3], interpolationPoints[3].transform.position, Quaternion.identity) as GameObject;
        virtualMarkers[(int)virtualMarkersEnum.PSIS2].name = "PSIS2";
        virtualMarkers[(int)virtualMarkersEnum.ASIS1].transform.parent = 
            virtualMarkers[(int)virtualMarkersEnum.ASIS2].transform.parent = 
            virtualMarkers[(int)virtualMarkersEnum.PSIS1].transform.parent = 
            virtualMarkers[(int)virtualMarkersEnum.PSIS2].transform.parent = pelvis.transform;
        this.joints[(int)jointEnum.hipJoint] = joints[0];
        this.joints[(int)jointEnum.hipJointPartner] = joints[1];

        int i = 0;
        foreach(GameObject obj in interpolationPoints)
        {
            DestroyImmediate(obj);
            interpolationPoints[i] = null;
            i++;
        }

    }
    public void SetKneeJoint()
    {
        interpolationPoints[0] = interpolationPoints[1] = interpolationPoints[2] = interpolationPoints[3] = null;
        interpAction = SetKneeJoint;
        virtualParent = shank;
        interpolatingPoint = true;
        display.text = "FE1";
    }
    public void SetKneeJoint(Vector3 lerpPoint)
    {
        GameObject[] joints = SetJoint(thigh, this.joints[(int)jointEnum.kneeJoint], this.joints[(int)jointEnum.kneeJointPartner], lerpPoint, interpolationPoints[1].transform.position - interpolationPoints[0].transform.position);
        virtualMarkers[(int)virtualMarkersEnum.FE1] = Instantiate(interpolationPoints[0], interpolationPoints[0].transform.position, Quaternion.identity) as GameObject;
        virtualMarkers[(int)virtualMarkersEnum.FE1].name = "FE1";
        virtualMarkers[(int)virtualMarkersEnum.FE2] = Instantiate(interpolationPoints[0], interpolationPoints[1].transform.position, Quaternion.identity) as GameObject;
        virtualMarkers[(int)virtualMarkersEnum.FE2].name = "FE2";
        virtualMarkers[(int)virtualMarkersEnum.FE1].transform.parent = virtualMarkers[(int)virtualMarkersEnum.FE2].transform.parent = thigh.transform;
        this.joints[(int)jointEnum.kneeJoint] = joints[0];
        this.joints[(int)jointEnum.kneeJointPartner] = joints[1];

        int i = 0;
        foreach (GameObject obj in interpolationPoints)
        {
            DestroyImmediate(obj);
            interpolationPoints[i] = null;
            i++;
        }
    }
    public void SetAnkleJoint()
    {
        interpolationPoints[0] = interpolationPoints[1] = interpolationPoints[2] = interpolationPoints[3] = null;
        interpAction = SetAnkleJoint;
        virtualParent = shank;
        interpolatingPoint = true;
        display.text = "LM";
    }
    public void SetAnkleJoint(Vector3 lerpPoint)
    {
        GameObject[] joints = SetJoint(shank, this.joints[(int)jointEnum.ankleJoint], this.joints[(int)jointEnum.ankleJointPartner], lerpPoint, interpolationPoints[1].transform.position - interpolationPoints[0].transform.position);
        virtualMarkers[(int)virtualMarkersEnum.LM] = Instantiate(interpolationPoints[0], interpolationPoints[0].transform.position, Quaternion.identity) as GameObject;
        virtualMarkers[(int)virtualMarkersEnum.LM].name = "LM";
        virtualMarkers[(int)virtualMarkersEnum.MM] = Instantiate(interpolationPoints[1], interpolationPoints[1].transform.position, Quaternion.identity) as GameObject;
        virtualMarkers[(int)virtualMarkersEnum.MM].name = "MM";
        virtualMarkers[(int)virtualMarkersEnum.LM].transform.parent = virtualMarkers[(int)virtualMarkersEnum.MM].transform.parent = shank.transform;
        this.joints[(int)jointEnum.ankleJoint] = joints[0];
        this.joints[(int)jointEnum.ankleJointPartner] = joints[1];

        int i = 0;
        foreach (GameObject obj in interpolationPoints)
        {
            DestroyImmediate(obj);
            interpolationPoints[i] = null;
            i++;
        }

        anklePathRendererObject.transform.position = this.joints[(int)jointEnum.ankleJoint].transform.position;
        anklePathRendererObject.transform.parent = pelvis.transform;
    }
    private GameObject SetPoint(GameObject parent, GameObject point)
    {
        string name = null;
        if (point != null)
        {
            name = point.name;
            DestroyImmediate(point);
        }

        GameObject p = new GameObject("Point");
        p.transform.position = stylusPoint.transform.position;
        if(name!=null)
        {
            p.name = name;
        }
        GameObject psphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        psphere.transform.position = p.transform.position;
        psphere.name = "PointSphere";
        psphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        psphere.transform.parent = p.transform;
        p.transform.parent = parent.transform;

        return p;
    }
    private GameObject SetPoint(GameObject parent, GameObject point, Vector3 position)
    {
        point.transform.position = position;
        GameObject psphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        psphere.transform.position = point.transform.position;
        psphere.name = "PointSphere";
        psphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        psphere.transform.parent = point.transform;
        point.transform.parent = parent.transform;

        return point;
    }
    private GameObject[] SetJoint(GameObject parent, GameObject jointPoint, GameObject rotationPartner,  Vector3 position, Vector3 right)
    {
        if (jointPoint != null)
        {
            DestroyImmediate(jointPoint);
            DestroyImmediate(rotationPartner);
        }

        GameObject p = new GameObject();
        GameObject rp = new GameObject();
        p.name = "JointPoint";
        rp.name = "JointPointPartner";

        p.transform.position = position;
        rp.transform.position = position;

        Quaternion rot = Quaternion.LookRotation(Vector3.Cross(right, Vector3.up), Vector3.up);
        p.transform.rotation = rot;
        rp.transform.rotation = rot;

        GameObject psphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        GameObject rpsphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        psphere.name = "JointSphere";
        rpsphere.name = "JointSpherePartner";

        Vector3 scale = new Vector3(0.01f, 0.01f, 0.01f);
        psphere.transform.localScale = scale;
        rpsphere.transform.localScale = scale;
        psphere.transform.position = p.transform.position;
        rpsphere.transform.position = rp.transform.position;
        psphere.transform.parent = p.transform;
        rpsphere.transform.parent = rp.transform;

        p.transform.parent = parent.transform;
        rp.transform.parent = parent.transform;

        GameObject[] jp = new GameObject[2];
        jp[0] = p;
        jp[1] = rp;

        return jp;
    }
    private void updateAnklePath()
    {
        anklePathRendererObject.transform.rotation = Quaternion.identity;
        Vector3 newLoc = joints[(int)jointEnum.ankleJoint].transform.position;
        if (currentPoint < capacity)
        {
            currentPoint++;
        }
        else
        {
            while(currentPoint >= capacity)
            {
                anklePoints.Dequeue();
                currentPoint--;
            }
            currentPoint++;
        }
        anklePoints.Enqueue(anklePathRendererObject.transform.InverseTransformPoint(newLoc));

        int i = 0;
        foreach(Vector3 v in anklePoints.ToArray())
        {
            anklePathRendererObject.GetComponent<LineRenderer>().SetPosition(i, v);
            i++;
        }
    }
    public void ChangeTrailLength()
    {
        string trailSize = GameObject.Find("TrailInput").GetComponent<InputField>().text;
        Debug.Log(trailSize);
        capacity = int.Parse(trailSize);
        anklePathRendererObject.GetComponent<LineRenderer>().SetVertexCount(capacity);
    }

    void OnApplicationQuit()
    {
        logger.close();
    }

    public void setStylusPoint(GameObject point)
    {
        stylusPoint = point;
    }

    public void LoggingButtonPress()
    {
        logging = !logging;
        if(logging)
        {
            GameObject.Find("Logging").GetComponent<Image>().color = Color.green;
        }
        else
        {
            GameObject.Find("Logging").GetComponent<Image>().color = Color.white;
        }
    }
    public void VMShowButtonPress()
    {
        if(tPosed)
        {
            foreach(GameObject vm in virtualMarkers)
            {
                vm.GetComponent<MeshRenderer>().enabled = !vm.GetComponent<MeshRenderer>().enabled;
            }
        }
    }
    public GameObject[] getJoints()
    {
        return joints;
    }
    public void ToggleModelButtonPress()
    {
            foreach(GameObject model in GameObject.FindGameObjectsWithTag("SkeletonModel"))
            {
                model.GetComponent<MeshRenderer>().enabled = !model.GetComponent<MeshRenderer>().enabled;
            }
    }
    private void SetSide()
    {
        Vector3 pelvisLateral = Vector3.Normalize(interpolationPoints[0].transform.position - interpolationPoints[1].transform.position);
        Vector3 pelvisAnterior = Vector3.Normalize(Vector3.Lerp(interpolationPoints[0].transform.position, interpolationPoints[1].transform.position, 0.5f) -
            Vector3.Lerp(interpolationPoints[2].transform.position, interpolationPoints[3].transform.position, 0.5f));

        if (Math.Sign(Vector3.Dot(Vector3.Cross(pelvisLateral, pelvisAnterior), Vector3.down)) > 0)
        {
            Debug.Log(Vector3.Dot(Vector3.Cross(pelvisLateral, pelvisAnterior), Vector3.down));
            inverter = RIGHT_LEG_INVERT;
        }
        else
        {
            Debug.Log(Vector3.Dot(Vector3.Cross(pelvisLateral, pelvisAnterior), Vector3.down));
            inverter = LEFT_LEG_INVERT;
        }
    }
    public static Vector3 ProjectVectorOnPlane(Vector3 planeNormal, Vector3 vector)
    {

        return vector - (Vector3.Dot(vector, planeNormal) * planeNormal);
    }

}
