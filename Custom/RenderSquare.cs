using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Data;


public class RenderSquare : MonoBehaviour
{
    private Transform avatar;
    private Material mat;
    private MeshRenderer renderer;
    private Vector3 lastScale;
    private string logFilePath;
    private static readonly int _Color1 = Shader.PropertyToID("_Color1");
    private static readonly int _Color2 = Shader.PropertyToID("_Color2");
    private static readonly int _VerticalArmThickness = Shader.PropertyToID("_VerticalArmThickness");
    private static readonly int _HorizontalArmThickness = Shader.PropertyToID("_HorizontalArmThickness");
    private static readonly int _VerticalArmLength = Shader.PropertyToID("_VerticalArmLength");
    private static readonly int _HorizontalArmLength = Shader.PropertyToID("_HorizontalArmLength");
    private static readonly int _Rotation = Shader.PropertyToID("_Rotation");
    private static readonly int _GratingDensity = Shader.PropertyToID("_GratingDensity");

    private bool _attachToPlayer = true;

    public bool AttachToPlayer
    {
        get { return _attachToPlayer; }
        set { _attachToPlayer = value; }
    }

    private bool _Visible = false;
    public bool Visible 
    {
        get { return _Visible; }
        set 
        {
            if (renderer != null)
            {
                _Visible = value;
                renderer.enabled = _Visible;
            }
        }
    }

    void Start()
    {
        // #if UNITY_EDITOR
        // Vector3 pos = new Vector3(0,0,0);
        // Vector3 scale = new Vector3(10,0,10);
        // Color white = new Color(1,1,1,1);

        // KeyValuePair<string, object>[]  kwargs=new KeyValuePair<string, object>[] 
        // {
        //     new KeyValuePair<string, object>("Solidangle", Mathf.PI/2f),
        //     new KeyValuePair<string, object>("Angle", 180f),
        //     new KeyValuePair<string, object>("Distance", 100f)
        // };
        // OnCreate(pos, DefaultOrientation, scale, white,kwargs);
        // Visible = true;
        // #endif
    }

    void Update()
    {   
        if(_attachToPlayer)
        {
            transform.position = avatar.position+DefaultOffset;
        }
        LogTransformDetails();
    }

    private void LogTransformDetails()
    {
        string logMessage = $"{Time.time}, {transform.position},{transform.rotation},  {transform.localScale}\n";
        File.AppendAllText(logFilePath, logMessage);
    }

    public void OnCreate(Vector3 position, Quaternion rotation, Vector3 scale, Color color, params KeyValuePair<string, object>[] kwargs)
    {
        logFilePath = SetROSBridge.LogFilePath + "_" + gameObject.name + "_multibar.csv";
        if (!File.Exists(logFilePath))
        {
            File.Create(logFilePath).Close();
        }

        string logMessage = $"Time , Position x, Position y, Position z, Rotation w, Rotation x, Rotation y, Rotation z, Scale x, Scale y, Scale z\n";
        File.AppendAllText(logFilePath, logMessage);
        transform.localScale=scale;
        OrigScale=scale;

        Angle=180;
        Solidangle=30;

        foreach (var param in kwargs)
        {
            if (param.Key == "Solidangle") { Solidangle = (float)param.Value; }
            if (param.Key == "Angle") { Angle = (float)param.Value; }
            if (param.Key == "Distance") { Distance = (float)param.Value; }
        }

        renderer = GetComponent<MeshRenderer>();
        renderer.enabled = false;
        avatar = GameObject.Find("Avatar").transform;


        if (GetComponent<MeshRenderer>() != null)
        {
            mat = GetComponent<MeshRenderer>().material;
        }


      
        VerticalArmThickness =   0.5F;      
        HorizontalArmThickness = 1.0F;
        VerticalArmLength = 1.0F;
        HorizontalArmLength = 0.0F;
        Rotation = 0F;
        Color2 = color;
        Visible =   false;
    
    }

    
    private float _angle = 180;
    public float Angle
    {
        get => _angle;
        set
        {
            _angle = value;
            Vector3 direction = new Vector3(Mathf.Sin(_angle * Mathf.Deg2Rad), 0, Mathf.Cos(_angle * Mathf.Deg2Rad));
            DefaultOffset = direction * Distance;
            Debug.Log("Default Offset: " + DefaultOffset);
            transform.rotation = DefaultQuaternion * Quaternion.AngleAxis(-_angle , Vector3.forward);
        }
    }
    private float _solidangle = 30;
        public float Solidangle
        {
            get => _solidangle;
            set
            {
                _solidangle = value;
                if (transform != null)
                {
                    float scaleFactor = Mathf.Sqrt(_solidangle);
                    transform.localScale = new Vector3 (OrigScale.x * scaleFactor * Distance,0F,1000);
                }
                Debug.Log("Scale: " + transform.localScale);
                Debug.Log("Solidangle: " + _solidangle);
            }
        }

    public float Distance = 400;
    public float distance
    {
        get => Distance;
        set
        {
            Distance = value;
            Angle = _angle;      // Trigger Angle setter to update position
            Solidangle = _solidangle;  // Trigger Solidangle setter to update scale
        }
    }

    private Vector3 OrigScale = new Vector3(1,1,1);
    private Vector3 DefaultOffset  = new Vector3 (10,10,0);

    public Color Color1
    {
        get => mat.GetColor(_Color1);
        set => mat.SetColor(_Color1, value);
    }
    private Quaternion DefaultQuaternion = Quaternion.Euler(90, 0, 0);

    public Color Color2
    {
        get => mat.GetColor(_Color2);
        set => mat.SetColor(_Color2, value);
    }

    public float VerticalArmThickness
    {
        get => mat.GetFloat(_VerticalArmThickness);
        set => mat.SetFloat(_VerticalArmThickness, value);
    }

    public float HorizontalArmThickness
    {
        get => mat.GetFloat(_HorizontalArmThickness);
        set => mat.SetFloat(_HorizontalArmThickness, value);
    }

    public float VerticalArmLength
    {
        get => mat.GetFloat(_VerticalArmLength);
        set => mat.SetFloat(_VerticalArmLength, value);
    }

    public float HorizontalArmLength
    {
        get => mat.GetFloat(_HorizontalArmLength);
        set => mat.SetFloat(_HorizontalArmLength, value);
    }

    public float Rotation
    {
        get => mat.GetFloat(_Rotation);
        set => mat.SetFloat(_Rotation, value);
    }

    public float GratingDensity
    {
        get => mat.GetFloat(_GratingDensity);
        set => mat.SetFloat(_GratingDensity, value);
    }
}

