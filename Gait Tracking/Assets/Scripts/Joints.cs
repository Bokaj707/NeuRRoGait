using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class Joints : MonoBehaviour, FileIO.LoggingButtonHandler  {

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

    bool ankleLocalFrame;

    Text display;

    GameObject[] interpolationPoints;
    Action<Vector3> interpAction;
    GameObject virtualParent;

    GameObject averageCam;
    CameraController camController;

    float lastTime;
    float currentTime;

    int inverter;
    static readonly int RIGHT_LEG_INVERT = 1;
    static readonly int LEFT_LEG_INVERT = -1;
    static readonly int TARGET_FRAME_RATE = 120;

    bool tPosed;
    bool interpolatingPoint;

    static readonly string loggingPrecision = "0.000";

    FileIO logger;

    Puppet skeleton;

    Queue<Vector3> anklePoints;
    int currentPoint;
    int capacity;

    float logTime;

    public enum jointAnglesEnum : int {hipAngle, kneeAngle, ankleAngle};
    public enum jointEnum : int {hipJoint, hipJointPartner, kneeJoint, kneeJointPartner, ankleJoint, ankleJointPartner};
    public enum virtualMarkersEnum : int {ASIS1, ASIS2, PSIS1, PSIS2, FE1, FE2, LM, MM};
    public GameObject[] virtualMarkers;
    public GameObject[] joints;

    Queue<float[]>[] lostPoints;

    private enum headers
    {
        TimeStamp,
        HipPitch, HipRoll, HipYaw,
        KneePitch, KneeRoll, KneeYaw,
        AnklePitch, AnkleRoll, AnkleYaw,
        HjointX, HjointY, HjointZ,
        KjointX, KjointY, KjointZ,
        AjointX, AjointY, AjointZ,
        AnklePathX, AnklePathY, AnklePathZ,
        HipOffPitch, HipOffRoll, HipOffYaw,
        KneeOffPitch, KneeOffRoll, KneeOffYaw,
        AnkleOffPitch, AnkleOffRoll, AnkleOffYaw
    };

    float[] angles;
    float[] offsets;


    void Awake()
    {
        Application.targetFrameRate = TARGET_FRAME_RATE;
    }

    // Use this for initialization
    void Start ()
    {
        camController = GameObject.Find("CameraController").GetComponent<CameraController>();

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

        logTime = 60;
        lastTime = 0;
        currentTime = 0;

        tPosed = false;
        interpolatingPoint = false;

        ankleLocalFrame = false;

        angles = new float[9];
        offsets = new float[9];

        lostPoints = new Queue<float[]>[4];

        for (int i = 0; i < 9; i++)
        {
            angles[i] = offsets[i] = 0f;
        }

        skeleton = GameObject.Find("LegContainer").GetComponent<Puppet>();
        averageCam = new GameObject("CameraFocus");

        GUISafety();

        string[] headers = {"Time Stamp",
            "Hip Pitch", "Hip Roll", "Hip Yaw",
            "Knee Pitch", "Knee Roll", "Knee Yaw",
            "Ankle Pitch","Ankle Roll","Ankle Yaw",
            "Hjoint X","Hjoint Y","Hjoint Z",
            "Kjoint X","Kjoint Y","Kjoint Z",
            "Ajoint X","Ajoint Y","Ajoint Z",
            "Ankle Path x", "Ankle Path y", "Ankle Path z",
            "Hip OffPitch", "Hip OffRoll", "Hip OffYaw",
            "Knee OffPitch", "Knee OffRoll", "Knee OffYaw",
            "Ankle OffPitch","Ankle OffRoll","Ankle OffYaw"};
        
        logger = new FileIO(headers.Length, Application.dataPath + @"\Logs\", string.Format("session-{0:yyyy-MM-dd_hh-mm-ss-tt}.txt", DateTime.Now), 600, headers, this);

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
            Snapshot();
            if(logger.Log())
            {
                logTime -= (currentTime - lastTime);
                UpdateDisplayedTime(logTime.ToString("0.0"));
                if(logTime<=0)
                {
                    LoggingButtonPress();
                    logTime = 0;
                }
            }
        }
    }

    private void bodyLost(int body, Queue<int> columns)
    {
        int currentLine = logger.getLine();
        while(columns.Count>0)
        {
            int c = columns.Dequeue();
            float[] f = new float[3];//(line, column, data)
            f[0] = currentLine;
            f[1] = c;
            f[2] = float.Parse(logger.getData(c));
            lostPoints[body].Enqueue(f);
        }
    }

    private void bodyFound(int body)
    {
        while(lostPoints[body].Count>0)
        {
            float[] f = lostPoints[body].Dequeue();
            int firstLine = (int)f[0];
            int column = (int)f[1];
            float data = f[3];

            int currentLine = logger.getLine();
            int missingPoints = currentLine - firstLine - 1;

            for (int i = 0; i < missingPoints; i++)
            {
                logger.pushCorrections(new FileIO.dataCorrection(currentLine + i, column, interpolateData(data, float.Parse(logger.getData(column)), missingPoints, i+1).ToString(loggingPrecision)));
            }
        }
    }

    private float interpolateData(float lastKnownValue, float currentValue, int numberOfLerps, int currentLerp)
    {
        return Mathf.Lerp(lastKnownValue, currentValue, (float)currentLerp / (float)numberOfLerps);
    }

    private void Snapshot()
    {
        logger.setColumn((int)headers.HjointX, joints[(int)jointEnum.hipJoint].transform.position.x.ToString(loggingPrecision));
        logger.setColumn((int)headers.HjointY, joints[(int)jointEnum.hipJoint].transform.position.y.ToString(loggingPrecision));
        logger.setColumn((int)headers.HjointZ, joints[(int)jointEnum.hipJoint].transform.position.z.ToString(loggingPrecision));

        logger.setColumn((int)headers.KjointX, joints[(int)jointEnum.kneeJoint].transform.position.x.ToString(loggingPrecision));
        logger.setColumn((int)headers.KjointY, joints[(int)jointEnum.kneeJoint].transform.position.y.ToString(loggingPrecision));
        logger.setColumn((int)headers.KjointZ, joints[(int)jointEnum.kneeJoint].transform.position.z.ToString(loggingPrecision));

        logger.setColumn((int)headers.AjointX, joints[(int)jointEnum.ankleJoint].transform.position.x.ToString(loggingPrecision));
        logger.setColumn((int)headers.AjointY, joints[(int)jointEnum.ankleJoint].transform.position.y.ToString(loggingPrecision));
        logger.setColumn((int)headers.AjointZ, joints[(int)jointEnum.ankleJoint].transform.position.z.ToString(loggingPrecision));
    }


    private void UpdateCamera()
    {
        Camera.main.transform.position = pelvis.transform.position + (Vector3.left);
        if (tPosed)
        {
            Camera.main.transform.LookAt(Vector3.Lerp(pelvis.transform.position, new Vector3(pelvis.transform.position.x, 0, pelvis.transform.position.z), 0.5f));
        }
        else
        {
            Camera.main.transform.LookAt((foot.transform.position + thigh.transform.position + pelvis.transform.position + shank.transform.position) / 4);
        }
    }

    private void CalculateJointAngles()
    {
        string[] angleOutputs = new string[3];

        if (!tPosed)
        {
            PoseHipJoint();
            PoseKneeJoint();
            PoseAnkleJoint();
            createVirtualMarkers();

            averageCam.transform.position = (foot.transform.position + thigh.transform.position + pelvis.transform.position + shank.transform.position) / 4;
            averageCam.transform.parent = pelvis.transform;

            skeleton.setSide(inverter);
            GameObject.Find("CameraController").GetComponent<CameraController>().setSide(inverter);
        }

        joints[(int)jointEnum.hipJointPartner].transform.position = joints[(int)jointEnum.hipJoint].transform.position;
        joints[(int)jointEnum.kneeJointPartner].transform.position = joints[(int)jointEnum.kneeJoint].transform.position;
        joints[(int)jointEnum.ankleJointPartner].transform.position = joints[(int)jointEnum.ankleJoint].transform.position;

        Vector3 floatingHip = Vector3.Normalize(Vector3.Cross(joints[(int)jointEnum.hipJoint].transform.right, joints[(int)jointEnum.hipJointPartner].transform.up));
        Vector3 floatingKnee = Vector3.Normalize(Vector3.Cross(joints[(int)jointEnum.kneeJoint].transform.right, joints[(int)jointEnum.kneeJointPartner].transform.up));
        Vector3 floatingAnkle = Vector3.Normalize(Vector3.Cross(joints[(int)jointEnum.ankleJoint].transform.right, joints[(int)jointEnum.ankleJointPartner].transform.up));

        angles[0] = FlexionExtension(joints[(int)jointEnum.hipJoint], floatingHip);
        angles[1] = -inverter * InternalExternatlRotation(joints[(int)jointEnum.hipJointPartner], floatingHip);
        angles[2] = -inverter * AductionAbduction(joints[(int)jointEnum.hipJoint], joints[(int)jointEnum.hipJointPartner]);

        angles[3] = -FlexionExtension(joints[(int)jointEnum.kneeJoint], floatingKnee);
        angles[4] = -InternalExternatlRotation(joints[(int)jointEnum.kneeJointPartner], floatingKnee);
        angles[5] = -inverter * AductionAbduction(joints[(int)jointEnum.kneeJoint], joints[(int)jointEnum.kneeJointPartner]);

        angles[6] = -FlexionExtension(joints[(int)jointEnum.ankleJoint], floatingAnkle);
        angles[7] = -InternalExternatlRotation(joints[(int)jointEnum.ankleJointPartner], floatingAnkle);
        angles[8] =  -inverter * AductionAbduction(joints[(int)jointEnum.ankleJoint], joints[(int)jointEnum.ankleJointPartner]);

        if (tPosed)
        {
            int i = 0;
            foreach (float angle in angles)
            {

                angles[i] -= offsets[i];

                i++;
            }
            angleOutputs[(int)jointAnglesEnum.hipAngle] = angles[0].ToString(loggingPrecision) + "\t" + angles[1].ToString(loggingPrecision) + "\t" + angles[2].ToString(loggingPrecision) + "\t";
            angleOutputs[(int)jointAnglesEnum.kneeAngle] = angles[3].ToString(loggingPrecision) + "\t" + angles[4].ToString(loggingPrecision) + "\t" + angles[5].ToString(loggingPrecision) + "\t";
            angleOutputs[(int)jointAnglesEnum.ankleAngle] = angles[6].ToString(loggingPrecision) + "\t" + angles[7].ToString(loggingPrecision) + "\t" + angles[8].ToString(loggingPrecision) + "\t";

            hipAngleDisplay.GetComponent<Text>().text = angleOutputs[(int)jointAnglesEnum.hipAngle];
            kneeAngleDisplay.GetComponent<Text>().text = angleOutputs[(int)jointAnglesEnum.kneeAngle];
            ankleAngleDisplay.GetComponent<Text>().text = angleOutputs[(int)jointAnglesEnum.ankleAngle];
                int j = 0;
                foreach(float s in angles)
                {
                    logger.setColumn((int)headers.HipPitch + j, s.ToString(loggingPrecision));
                    j++;
                }
                logger.setColumn((int)headers.TimeStamp, UnityEngine.Time.fixedTime.ToString());
                lastTime = currentTime;
                currentTime = UnityEngine.Time.fixedTime;
        }
        else
        {
                ZeroAngles();
                tPosed = true;
                camController.StartController(joints[(int)jointEnum.hipJoint], joints[(int)jointEnum.ankleJoint]);
                GUISafety();
        }

        //Debug.Log(Vector3.Distance(joints[(int)jointEnum.hipJoint].transform.position, joints[(int)jointEnum.kneeJoint].transform.position).ToString());
    }

    public float FlexionExtension(GameObject joint, Vector3 floatingAxis)
    {
        return Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(joint.transform.forward, floatingAxis)) * Math.Sign(Vector3.Dot(joint.transform.up, floatingAxis)); //flex/ex
    }
    public float InternalExternatlRotation(GameObject jointPartner, Vector3 floatingAxis)
    {
        return Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(jointPartner.transform.forward, floatingAxis)) * inverter * -(Math.Sign(Vector3.Dot(jointPartner.transform.right, floatingAxis)));//rot
    }
    public float AductionAbduction(GameObject joint, GameObject jointPartner)
    {
        return Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(joint.transform.right, jointPartner.transform.up)) - inverter * (-Mathf.PI / 2);//ad/ab
    }
    private void createVirtualMarkers()
    {
        int q = 0;
        string vmName = "VMTrunk";
        foreach (GameObject marker in virtualMarkers)
        {
            if (4 < q && q < 7)
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
    }

    private float[] AngleDecomposition(Quaternion angle)//Returns Roll Pitch Yaw
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

        joints[(int)jointEnum.hipJoint].transform.rotation = Quaternion.LookRotation(pelvisAnterior, Vector3.Normalize(Vector3.Cross(pelvisLateral, -pelvisAnterior)));
        joints[(int)jointEnum.hipJointPartner].transform.rotation = Quaternion.LookRotation(Vector3.Normalize(Vector3.Cross(inverter * Vector3.Normalize(virtualMarkers[(int)virtualMarkersEnum.FE1].transform.position - virtualMarkers[(int)virtualMarkersEnum.FE2].transform.position), femur)), femur);
        joints[(int)jointEnum.hipJointPartner].transform.parent = thigh.transform; 
    }
    private void PoseKneeJoint()
    {
        Vector3 femur = Vector3.Normalize(joints[(int)jointEnum.hipJoint].transform.position - joints[(int)jointEnum.kneeJoint].transform.position);
        Vector3 tibia = Vector3.Normalize(joints[(int)jointEnum.kneeJoint].transform.position - joints[(int)jointEnum.ankleJoint].transform.position);
        Vector3 ankleLateral = inverter * Vector3.Normalize(virtualMarkers[(int)virtualMarkersEnum.LM].transform.position - virtualMarkers[(int)virtualMarkersEnum.MM].transform.position);
        Vector3 kneeLateral = inverter * Vector3.Normalize(virtualMarkers[(int)virtualMarkersEnum.FE1].transform.position - virtualMarkers[(int)virtualMarkersEnum.FE2].transform.position);

        joints[(int)jointEnum.kneeJoint].transform.rotation = Quaternion.LookRotation(Vector3.Normalize(Vector3.Cross(kneeLateral, femur)), femur);
        joints[(int)jointEnum.kneeJointPartner].transform.rotation = Quaternion.LookRotation(Vector3.Normalize(Vector3.Cross(ankleLateral, tibia)),tibia);
        joints[(int)jointEnum.kneeJointPartner].transform.parent = shank.transform;
    }
    private void PoseAnkleJoint()
    {
        Vector3 tibia = Vector3.Normalize(joints[(int)jointEnum.kneeJoint].transform.position - joints[(int)jointEnum.ankleJoint].transform.position);
        Vector3 ankleLateral = inverter * Vector3.Normalize(virtualMarkers[(int)virtualMarkersEnum.LM].transform.position - virtualMarkers[(int)virtualMarkersEnum.MM].transform.position);
        Vector3 anklePosterior = Vector3.Normalize(Vector3.Normalize(Vector3.Cross(ankleLateral, tibia)));
        Vector3 footPosterior = Vector3.Normalize(Vector3.Normalize(Vector3.Cross(inverter * Vector3.Normalize(virtualMarkers[(int)virtualMarkersEnum.FE1].transform.position - virtualMarkers[(int)virtualMarkersEnum.FE2].transform.position), tibia)));

        joints[(int)jointEnum.ankleJoint].transform.rotation = Quaternion.LookRotation(anklePosterior, Vector3.Normalize(Vector3.Cross(-ankleLateral, anklePosterior)));
        joints[(int)jointEnum.ankleJointPartner].transform.rotation = Quaternion.LookRotation(footPosterior, tibia);
        joints[(int)jointEnum.ankleJointPartner].transform.parent = foot.transform;
    }
    public void ZeroAngles()
    {
        logger.Log();

        logger.setColumn((int)headers.TimeStamp, UnityEngine.Time.fixedTime.ToString());

        int i = 0;
        foreach (float angle in angles)
        {
            offsets[i] += angle;
            logger.setColumn((int)headers.HipOffPitch + i, angle.ToString(loggingPrecision));
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

        Quaternion rot = Quaternion.LookRotation(Vector3.Normalize(Vector3.Cross(right, Vector3.up)), Vector3.up);
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

        anklePathRendererObject.transform.position = joints[(int)jointEnum.hipJoint].transform.position;
        anklePathRendererObject.transform.rotation = Quaternion.identity;
        Vector3 jointLoc = joints[(int)jointEnum.ankleJoint].transform.position;
        Vector3 framedJointLoc = anklePathRendererObject.transform.InverseTransformPoint(jointLoc);
        if (ankleLocalFrame)
        {
            anklePoints.Enqueue(framedJointLoc);
        }
        else
        {
            anklePoints.Enqueue(jointLoc);
        }
        int i = 0;
        foreach(Vector3 v in anklePoints.ToArray())
        {
            anklePathRendererObject.GetComponent<LineRenderer>().SetPosition(i, v);
            i++;
        }
        logger.setColumn((int)headers.AnklePathX, framedJointLoc.x.ToString(loggingPrecision));
        logger.setColumn((int)headers.AnklePathY, framedJointLoc.y.ToString(loggingPrecision));
        logger.setColumn((int)headers.AnklePathZ, framedJointLoc.z.ToString(loggingPrecision));
    }
    public void toggleAnklePathParentButtonPress()
    {
        ankleLocalFrame = !ankleLocalFrame;
        if(ankleLocalFrame)
        {
            GameObject.Find("TogglePathParentButton").transform.FindChild("Text").GetComponent<Text>().text = "Local Path";
            anklePathRendererObject.GetComponent<LineRenderer>().useWorldSpace = false;
        }
        else
        {
            GameObject.Find("TogglePathParentButton").transform.FindChild("Text").GetComponent<Text>().text = "Global Path";
            anklePathRendererObject.GetComponent<LineRenderer>().useWorldSpace = true;
        }
    }
    public void toggleAnklePathButton()
    {
        anklePathRendererObject.GetComponent<LineRenderer>().enabled = !anklePathRendererObject.GetComponent<LineRenderer>().enabled;
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
        if(logger.toggleLogging())
        {
            GameObject.Find("Logging").GetComponent<Image>().color = Color.green;
            GameObject.Find("FilePathInput").GetComponent<InputField>().interactable = false;
            GameObject.Find("FileNameInput").GetComponent<InputField>().interactable = false;
            GameObject.Find("LogInput").GetComponent<InputField>().interactable = false;
        }
        else
        {
            GameObject.Find("Logging").GetComponent<Image>().color = Color.white;
            GameObject.Find("FilePathInput").GetComponent<InputField>().interactable = true;
            GameObject.Find("FileNameInput").GetComponent<InputField>().interactable = true;
            GameObject.Find("LogInput").GetComponent<InputField>().interactable = true;
        }
    }
    public void VMShowButtonPress()
    {
        if(tPosed)
        {
            foreach(GameObject vm in virtualMarkers)
            {
                vm.GetComponentInChildren<MeshRenderer>().enabled = !vm.GetComponentInChildren<MeshRenderer>().enabled;
            }
            foreach (GameObject vm in joints)
            {
                vm.GetComponentInChildren<MeshRenderer>().enabled = !vm.GetComponentInChildren<MeshRenderer>().enabled;
            }
        }
    }
    public GameObject[] getJoints()
    {
        return joints;
    }
    private void SetSide()
    {
        Vector3 pelvisLateral = Vector3.Normalize(interpolationPoints[0].transform.position - interpolationPoints[1].transform.position);
        Vector3 pelvisAnterior = Vector3.Normalize(Vector3.Lerp(interpolationPoints[0].transform.position, interpolationPoints[1].transform.position, 0.5f) -
            Vector3.Lerp(interpolationPoints[2].transform.position, interpolationPoints[3].transform.position, 0.5f));

        if (Math.Sign(Vector3.Dot(Vector3.Normalize(Vector3.Cross(pelvisLateral, pelvisAnterior)), Vector3.down)) > 0)
        {
            Debug.Log(Vector3.Dot(Vector3.Normalize(Vector3.Cross(pelvisLateral, pelvisAnterior)), Vector3.down));
            inverter = RIGHT_LEG_INVERT;
        }
        else
        {
            Debug.Log(Vector3.Dot(Vector3.Normalize(Vector3.Cross(pelvisLateral, pelvisAnterior)), Vector3.down));
            inverter = LEFT_LEG_INVERT;
        }
    }
    public void SetLogTimer()
    {
        if (GameObject.Find("LogInput").GetComponent<InputField>().IsInteractable())
        {
            string input = GameObject.Find("LogInput").GetComponent<InputField>().text;
            logTime = float.Parse(input);
        }
    }
    public void SetFilePath()
    {
        string input = GameObject.Find("FilePathInput").GetComponent<InputField>().text;
        logger.SetFilePath(input);
    }
    public void SetFileName()
    {
        string input = GameObject.Find("FileNameInput").GetComponent<InputField>().text;
        logger.SetFileName(input);
    }
    public void SetDefaultPaths(string filePath, string fileName)
    {
        GameObject.Find("FilePathInput").GetComponent<InputField>().text = filePath;
        GameObject.Find("FileNameInput").GetComponent<InputField>().text = fileName;
    }
    public void UpdateDisplayedTime(string newTime)
    {
        GameObject.Find("LogInput").GetComponent<InputField>().text = newTime;
    }
    private void GUISafety()
    {
        foreach(GameObject button in GameObject.FindGameObjectsWithTag("PosedDependant"))
        {
            button.GetComponent<Button>().interactable = !button.GetComponent<Button>().interactable;
        }
    }
    internal FileIO getFileIO()
    {
        return logger;
    }
    public void showAnklePathButton()
    {
        anklePathRendererObject.GetComponent<LineRenderer>().enabled = !anklePathRendererObject.GetComponent<LineRenderer>().enabled;
    }
    
}
