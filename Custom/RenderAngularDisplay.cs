using Unity.VisualScripting;
using UnityEngine;
using System.IO;
using System;
using System.Data;

// Don't forget to change the class name when using this as a template
// for implementing new objects with custom behaviors
public class RenderAngularDisplay : MonoBehaviour
{
    private Transform avatar;
    private Material mat;
    private MeshRenderer renderer;
    private Vector3 lastScale;
    private string logFilePath;
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
    private static readonly int _GratingFrequency1 = Shader.PropertyToID("_GratingFrequency1");
    private static readonly int _GratingFrequency2 = Shader.PropertyToID("_GratingFrequency2");
    private static readonly int _GratingOrientation1 = Shader.PropertyToID("_GratingOrientation1");
    private static readonly int _GratingOrientation2 = Shader.PropertyToID("_GratingOrientation2");
    private static readonly int _Aspect = Shader.PropertyToID("_Aspect");
    private NormalRandom normalRandom = new NormalRandom(12345);
    private bool _AngularNoiseOn = false;
    private float _NoiseStd = 0.0F;
    private int _NoiseFreq = 320; // Number of frames to wait
    private float _SpinSpeed = 100f; // Speed of the rotation in degrees per second
    private bool _attachToPlayer = true;
    public bool AngularNoiseOn{
        get{ return _AngularNoiseOn;}
        set{_AngularNoiseOn= value;}
    }

    public float NoiseStd{
        get{ return _NoiseStd;}
        set{_NoiseStd = value;}
    }
    public float NoiseFreq{
        get{ return _NoiseFreq;}
        set{_NoiseFreq = Mathf.RoundToInt(value);}
    }
    public float SpinSpeed{
        get { return _SpinSpeed; }
        set { _SpinSpeed = value; }
    }
    private bool _IsSpinning = false; // When set to true, the cylinder will start spinning
    
    public bool IsSpinning
    {
        get { return _IsSpinning; }
        set { _IsSpinning = value; }
    }
     
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
        #endif
     
    }

    // Update is called once per frame
    void Update()
    {   
        if(_attachToPlayer)
            {transform.position = avatar.position;}
           if (IsSpinning)
        {
            Spin();
        }

        

        // if ((Time.frameCount % _NoiseFreq == 0)&_AngularNoiseOn)
        if (_AngularNoiseOn)
       
        {   
            Vector3 rand_rot=new Vector3 (0, _NoiseStd*(float) normalRandom.NextDouble(),0);
            transform.rotation=transform.rotation*Quaternion.Euler(rand_rot);
            Debug.Log(rand_rot);
            }

        
       
         if (transform.localScale != lastScale)
         {
            Aspect= transform.localScale.x/transform.localScale.y;
            lastScale=transform.localScale;
            
         }
        
        LogTransformDetails();
    }

      private void LogTransformDetails()
    {
        string logMessage = $"Time: {Time.time}, Position: {transform.position}, Rotation: {transform.rotation}, Scale: {transform.localScale}\n";
        
        File.AppendAllText(logFilePath, logMessage);
    }

    
    // OnCreate() is required to properly instantiate the object with the correct properties
    // The method must take the listed inputs -- no more, no less (even if some are not used)
    public void OnCreate(Vector3 position, Quaternion rotation, float height, float radius, Color color)
    {
        
        
        logFilePath = SetROSBridge.LogFilePath + "_angular_display.csv";

        if (!File.Exists(logFilePath))
        {
            File.Create(logFilePath).Close();
        }
        
        renderer = GetComponent<MeshRenderer>();
        renderer.enabled=false;
        // // Uncomment to add player collisions with this object
        // // Add RigidBody and MeshCollider components, both are required for enabling collisions
        // // Since the ground is not always Active nor collision-enabled,
        // // useGravity and isKinematic must both be False to avoid falling into the abyss.
        // Rigidbody rigidBody = this.AddComponent<Rigidbody>();
        // rigidBody.useGravity = false;
        // rigidBody.isKinematic = false;
        // this.AddComponent<MeshCollider>();
        avatar = GameObject.Find("Avatar").transform;
        transform.localPosition = position;
        transform.localRotation = rotation;
        transform.localScale = new Vector3(radius, height, radius);
        lastScale=transform.localScale;
        

        
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
  void Spin()
    {
        // Rotate around the up axis (Y-axis by default) of the cylinder
        transform.Rotate(Vector3.up * _SpinSpeed * Time.deltaTime);
    }
}

