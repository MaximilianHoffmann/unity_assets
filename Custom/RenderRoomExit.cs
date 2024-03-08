
using UnityEngine;
using System.IO;
using System;
using System.Collections;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.UnityVrDriverInterfaces;
using UnityEngine.UIElements;
using System.Threading.Tasks;

public class RenderRoomExit: MonoBehaviour
{
    private Transform avatarTransform;
    private ROSConnection rosConnection;
    [SerializeField]
    private string vrTaskTopic="VRTask";
    private GameObject avatar;
    private Material mat;
    private MeshRenderer renderer;
    private Vector3 lastScale;
    private string logFilePath;
    private string logFilePathTask;
    private static readonly int _Color1 = Shader.PropertyToID("_Color1");
    private static readonly int _Color2 = Shader.PropertyToID("_Color2");
    private static readonly int _Color3 = Shader.PropertyToID("_Color3");
    private static readonly int _CapThreshold = Shader.PropertyToID("_CapThreshold");
    private static readonly int _AngularSize1 = Shader.PropertyToID("_AngularSize1");
    private static readonly int _AngularSize2 = Shader.PropertyToID("_AngularSize2");
    private static readonly int _AngularDistance = Shader.PropertyToID("_AngularDistance");
    private static readonly int _Offset = Shader.PropertyToID("_Offset");
    private static readonly int _SwitchOnGrating1 = Shader.PropertyToID("_SwitchOnGrating1");
    private static readonly int _SwitchOnGrating2 = Shader.PropertyToID("_SwitchOnGrating2");
    private static readonly int _SwitchOnNoise1 = Shader.PropertyToID("_SwitchOnNoise1");
    private static readonly int _GridSize_X= Shader.PropertyToID("_GridSize_X");
    private static readonly int _GridSize_Y= Shader.PropertyToID("_GridSize_Y");
    private static readonly int _GratingFrequency1 = Shader.PropertyToID("_GratingFrequency1");
    private static readonly int _GratingFrequency2 = Shader.PropertyToID("_GratingFrequency2");
    private static readonly int _GratingOrientation1 = Shader.PropertyToID("_GratingOrientation1");
    private static readonly int _GratingOrientation2 = Shader.PropertyToID("_GratingOrientation2");
    private static readonly int _Aspect = Shader.PropertyToID("_Aspect");
    private NormalRandom normalRandom = new NormalRandom(12345);

    
    private bool _TrialEnded = false;
    public bool TrialEnded
    {
        get { return _TrialEnded; }

         
        set{
            if (value)
            {
                CompleteTrial();
            }
            else
            {
                StartTrial();
            }
        }
    }

    private bool _TaskActive= false;
    public bool TaskActive
    {
        get { return _TaskActive; } 
        set{
            _TaskActive=value;
            if (!value){
                StopCoroutine(ScheduleTrial());
            }
            }
        }
    private ushort trialNumber = 0;
    public string taskName="confined_exit";
    [SerializeField]
    public float colliderRadius = 0.99f;
    [SerializeField]
   private float _InterTrialInterval= 10.0f;

    public float InterTrialInterval
    { get{ return _InterTrialInterval;}
    set{ _InterTrialInterval=value;}
    }
    
    [SerializeField]
    private float _PreExitInterval= 10.0f;
    public float PreExitInterval
    { get{ return _PreExitInterval;}
    set{ _PreExitInterval=value;}
    }

    [SerializeField]
    private bool _EncloseFlag = false;
    private bool _attachToPlayer = false;
    
    public bool EncloseFlag
    {
        get{
            return _EncloseFlag;
        }
        
        set{
            _EncloseFlag=value;
        }
    }
    
    [SerializeField]
    private bool _Visible = false;
    public bool Visible


    {
        get{
            return _Visible;
        }
        
        set{
           if (renderer != null)
           {
            _Visible=value;
            renderer.enabled = _Visible;
           }
        }
    }
  

       
    
    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        
        Vector3 pos=new Vector3(0,0,0);
        Quaternion rot = new Quaternion(0,0,0,1);
        Color white= new Color(1,1,1,1);
        OnCreate(pos,rot,5,20,white);
        SetupPublisher();
        StartTrial();
        #endif
     
    }

    bool EvaluateEnclosure()
    {   
        Vector3 localPosition = transform.InverseTransformPoint(avatarTransform.position);
        (float r, float theta) =CartesianToPolar(localPosition.x,localPosition.z);


        float norm = Mathf.Sqrt(localPosition.x*localPosition.x + localPosition.z*localPosition.z);
        // #float exit_to_theta=(ExitLocation*2*Mathf.PI + Mathf.PI)% (2*Mathf.PI)-Mathf.PI;
        float theta_two_pi=(theta+2*Mathf.PI)%(2*Mathf.PI);
        float ang_distance = Mathf.Abs(Mathf.Asin(MathF.Sin(theta_two_pi-ExitLocation*2*Mathf.PI)));


        // Debug.Log("Relative Position wof avatar in enclosure" + localPosition);
        Debug.Log("Theta: " + theta);
        Debug.Log("Angular Deviation From Exit: " +  ang_distance+ "  Allowed Deviation:"+(ExitSize*Mathf.PI));
     
        if (norm > colliderRadius)
        {   if (!ExitOn&&(Mathf.Abs(ang_distance)<(ExitSize*Mathf.PI))){
            CompleteTrial();
            return false;
        }
            Debug.Log("RESET");
            return true;
        }
        else
        {
            return false;
        }
    }

     void WallCollide()
    {
        Vector3 oldPosition = avatarTransform.position;
        Vector3 localPosition = transform.InverseTransformPoint(oldPosition);
        (float r, float theta) =CartesianToPolar(localPosition.x,localPosition.z);
        Vector3 newLocalPosition = new Vector3(MathF.Cos(theta)*colliderRadius,localPosition.y,MathF.Sin(theta)*colliderRadius);
        // Debug.Log("NewLocalPosition: " + newLocalPosition + " OldLocalPosition: " + localPosition + " Norm: " +  Mathf.Sqrt(newLocalPosition.x*newLocalPosition.x + newLocalPosition.z*newLocalPosition.z));
        Vector3 newPosition = transform.TransformPoint(newLocalPosition);
        avatarTransform.position = newPosition;
        double unity_time = DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000d;
        if (File.Exists(logFilePath)) 
        { 
            File.AppendAllText(logFilePath, 
                $"{unity_time:F3},{PublishVRPosition.LastTimestamp:F3}," + 
                $"{oldPosition.x:F3},{oldPosition.z:F3},{oldPosition.y:F3}," + 
                 $"{newPosition.x:F3},{newPosition.z:F3},{newPosition.y:F3}\n" ) ;   }

    }



    // Update is called once per frame
    void Update()
    {   
        if(_attachToPlayer)
            {transform.position = avatarTransform.position;}
          

        if (EncloseFlag)
            {
            bool outside=EvaluateEnclosure();
            Debug.Log(outside);
            if (outside)
            {
                Debug.Log("RESET POSITION");
                WallCollide();
            }  
            }
        if (TaskActive){
            PublishVRTaskMsg();
        }
         
           }

    public void OnCreate(Vector3 position, Quaternion rotation, float height, float radius, Color color)
    {
        
        SetupPublisher();
     


        renderer = GetComponent<MeshRenderer>();
        renderer.enabled=false;
      

        avatarTransform = GameObject.Find("Avatar").transform;
        avatar= GameObject.Find("Avatar");
        transform.localPosition = position;
        transform.localRotation = rotation;
        transform.localScale = new Vector3(radius, height, radius);
        lastScale=transform.localScale;
        if (GetComponent<MeshRenderer>() != null)
        {
            mat = GetComponent<MeshRenderer>().material;


        }
        string gameObjectName = gameObject.name;

              
        logFilePath = SetROSBridge.LogFilePath + "_" + gameObjectName+"_resets.csv";
        if (SetROSBridge.LogFilePath != "")
        {
            File.WriteAllText(logFilePath, 
                "unity_time,last_timestamp," +
                "old_proper_position_x,old_proper_position_y,old_proper_position_z," +
                "new_proper_position_x,new_proper_position_y,new_proper_position_z" +
                "\n");
        }




 
        
        // Instantiate variables in OnCreate() instead of Start()
        // since they are called in the same frame and thus their call order may not be consistent.
        // i.e. Assign the proper object (Renderer material) to variable (mat)
        // before changing its color via Property Set.
        if (GetComponent<MeshRenderer>() != null)
        {
            mat = GetComponent<MeshRenderer>().material;
            Aspect= transform.localScale.x/transform.localScale.y;

        }

    }

public static (float r, float theta) CartesianToPolar(float x, float z)
    {
        float r = Mathf.Sqrt(x * x + z * z);
        float theta = Mathf.Atan2(z, x);

        return (r, theta);
    }

public class NormalRandom
{
    private System.Random random;

    public NormalRandom(int seed)
    {
        this.random = new System.Random(seed);
    }

    public double NextDouble()
    {
        // Use Box-Muller transform to generate two independent standard normal distributed numbers
        double u1 = 1.0 - random.NextDouble(); // Uniform(0,1] random doubles
        double u2 = 1.0 - random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                     Math.Sin(2.0 * Math.PI * u2); // Random normal(0,1)
        return randStdNormal;
    }
}
    
        public Color Color1
    {
        get => mat.GetColor(_Color1);
        set => mat.SetColor(_Color1, value);
    }

    public Color Color2
    {
        get => mat.GetColor(_Color2);
        set => mat.SetColor(_Color2, value);
    }

    public Color Color3
    {
        get => mat.GetColor(_Color3);
        set => mat.SetColor(_Color3, value);
    }

    public float CapThreshold
    {
        get => mat.GetFloat(_CapThreshold);
        set => mat.SetFloat(_CapThreshold, value);
    }

    public float AngularSize1
    {
        get => mat.GetFloat(_AngularSize1);
        set => mat.SetFloat(_AngularSize1, value);
    }

    public float AngularSize2
    {
        get => mat.GetFloat(_AngularSize2);
        set => mat.SetFloat(_AngularSize2, value);
    }

    public float AngularDistance
    {
        get => mat.GetFloat(_AngularDistance);
        set => mat.SetFloat(_AngularDistance, value);
    }

    public float Offset
    {
        get => mat.GetFloat(_Offset);
        set => mat.SetFloat(_Offset, value);
    }

    public bool SwitchOnGrating1
    {
        get => mat.GetFloat(_SwitchOnGrating1) > 0.5f;
        set => mat.SetFloat(_SwitchOnGrating1, value ? 1.0f : 0.0f);
    }
    public bool SwitchOnNoise1
    {
        get => mat.GetFloat(_SwitchOnNoise1) > 0.5f;
        set => mat.SetFloat(_SwitchOnNoise1, value ? 1.0f : 0.0f);
    }
   

    public bool SwitchOnGrating2
    {
        get => mat.GetFloat(_SwitchOnGrating2) > 0.5f;
        set => mat.SetFloat(_SwitchOnGrating2, value ? 1.0f : 0.0f);
    }

    public float GratingFrequency1
    {
        get => mat.GetFloat(_GratingFrequency1);
        set => mat.SetFloat(_GratingFrequency1, value);
    }

    public float GratingFrequency2
    {
        get => mat.GetFloat(_GratingFrequency2);
        set => mat.SetFloat(_GratingFrequency2, value);
    }

    public float GridSizeX
    {
        get => mat.GetFloat(_GridSize_X);
        set => mat.SetFloat(_GridSize_X, value);
    }
    public float GridSizeY
    {
        get => mat.GetFloat(_GridSize_Y);
        set => mat.SetFloat(_GridSize_Y, value);
    }

    public float GratingOrientation1
    {
        get => mat.GetFloat(_GratingOrientation1);
        set => mat.SetFloat(_GratingOrientation1, value);
    }

    public float GratingOrientation2
    {
        get => mat.GetFloat(_GratingOrientation2);
        set => mat.SetFloat(_GratingOrientation2, value);
    }

    public float Aspect
    {
        get => mat.GetFloat(_Aspect);
        set => mat.SetFloat(_Aspect, value);
    }

    public bool AttachToPlayer    {
        get { return _attachToPlayer; }
        set { _attachToPlayer = value; }
    }

public float ExitSize
{
    get {
        return AngularSize2;
        }
    set {
        AngularSize1 = 1.0f;
        AngularSize2 = value;
    }
}

public bool ExitOn {
    get {
        return SwitchOnGrating2;
    }
    set {
        SwitchOnGrating2 = !value;
    }
}

public float ExitLocation
{
    get {
        return AngularDistance;
        }
    set {
        AngularDistance = value;

    }

}

void SetupPublisher(){
  rosConnection = ROSConnection.GetOrCreateInstance();
    if (SetROSBridge.RosNamespace != "" & SetROSBridge.RosNamespace != "/")
    {
        vrTaskTopic = SetROSBridge.RosNamespace + "/" + vrTaskTopic;
    }
    
    rosConnection.RegisterPublisher<VRTaskMsg>(vrTaskTopic);


    logFilePathTask = SetROSBridge.LogFilePath + "_" +  gameObject.name+"_task.csv";
        if (SetROSBridge.LogFilePath != "")
        {
            File.WriteAllText(logFilePathTask, 
               "task_name,trial_number,trial_ended,local_x,local_y,exit_on,exit_size,exit_angle,enclosure_radius\n");
        }
        
    // logFilePath = SetROSBridge.LogFilePath + "_vrtask.csv";
    // if (SetROSBridge.LogFilePath != "")
    // {
    //     File.WriteAllText(logFilePath, 
    //         "unity_time,last_timestamp," +
    //         "proper_position_x,proper_position_y,proper_position_z," +
    //         "proper_heading,last_heading," + 
    //         "last_delta_x,last_delta_y," + 
    //         "last_position_x,last_position_y\n");
    // }

}

public void PublishVRTaskMsg()
    {

        Vector3 localPosition = transform.InverseTransformPoint(avatarTransform.position);
   

        VRTaskMsg vrTask = new VRTaskMsg
        {
            task_name = taskName,
            trial_number = trialNumber,
            trial_ended = _TrialEnded,
            data_header = new string[] { "local_x", "local_y" ,"exit_on","exit_size", "exit_angle","enclosure_radius"},
            data=  new float[] { localPosition.x ,localPosition.z, ExitOn ? 1f : 0f, ExitSize, ExitLocation, transform.localScale.x}

        };



        vrTask.header.stamp.sec = (int)Math.Truncate(PublishVRPosition.LastTimestamp);
        vrTask.header.stamp.nanosec = (uint)((PublishVRPosition.LastTimestamp - Math.Truncate(PublishVRPosition.LastTimestamp)) * 1e9);
        vrTask.header.frame_id = Time.frameCount.ToString();
        rosConnection.Publish(vrTaskTopic, vrTask);
        
        if (File.Exists(logFilePathTask)) 
        { 
            File.AppendAllText(logFilePathTask, 
                $"{taskName},{trialNumber},{_TrialEnded},{localPosition.x:F3},{localPosition.z:F3},{ExitOn},{ExitSize:F3},{ExitLocation:F3},{transform.localScale.x:F3}\n");
        }
    }

void StartTrial()
{
    if(TaskActive){
        
    
    trialNumber += 1;
    _TrialEnded = false;
    EncloseFlag=true;
    ExitOn =false;
    Visible=true;
    ExitSize = 0.05f;
    StartCoroutine(ScheduleExit());
    float[] values = {0f, 0.25f, 0.5f, 0.75f};
    int index = UnityEngine.Random.Range(0, values.Length);
    ExitLocation = values[index];//UnityEngine.Random.Range(0.0f,1.0f);
    Debug.Log("Trial Startet");
    Debug.Log("ExitLocation: " + ExitLocation);

    Vector3 oldPosition = avatarTransform.position;
    Vector3 localPosition = transform.InverseTransformPoint(oldPosition);
    Vector3 newLocalPosition = new Vector3(0,localPosition.y,0);
    Vector3 newPosition = transform.TransformPoint(newLocalPosition);
    avatarTransform.position = newPosition;
    double unity_time = DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000d;
    // if (File.Exists(logFilePath)) 
    // { 
    //     File.AppendAllText(logFilePath, 
    //         $"{unity_time:F3},{PublishVRPosition.LastTimestamp:F3}," + 
    //         $"{oldPosition.x:F3},{oldPosition.z:F3},{oldPosition.y:F3}," + 
    //             $"{newPosition.x:F3},{newPosition.z:F3},{newPosition.y:F3}\n" ) ; 
    // }
    }
}

void CompleteTrial(){
    _TrialEnded = true;
    EncloseFlag=false;
    Visible=false;
    Debug.Log("Trial Ended");
    StartCoroutine(ScheduleTrial());
}

  
  
    IEnumerator ScheduleTrial()
        {
            yield return new WaitForSeconds(InterTrialInterval);
            StartTrial();

}
  IEnumerator ScheduleExit()
        {
            yield return new WaitForSeconds(PreExitInterval);
            ExitOn = true;

}
}



