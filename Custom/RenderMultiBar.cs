using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Data;

// Don't forget to change the class name when using this as a template
// for implementing new objects with custom behaviors
public class RenderMultiBar : MonoBehaviour
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
        #if UNITY_EDITOR
        Vector3 pos = new Vector3(0,0,0);
        Vector3 scale = new Vector3(10,0,10);
        Color white = new Color(1,1,1,1);

        KeyValuePair<string, object>[]  kwargs=new KeyValuePair<string, object>[] 
        {
            new KeyValuePair<string, object>("Solidangle", Mathf.PI/2f),
            new KeyValuePair<string, object>("Angle", 180f),
            new KeyValuePair<string, object>("Distance", 100f)
        };
        OnCreate(pos, DefaultOrientation, scale, white,kwargs);
        Visible = true;
        #endif
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
        string logMessage = $"Time: {Time.time}, Position: {transform.position}, Rotation: {transform.rotation}, Scale: {transform.localScale}\n";
        File.AppendAllText(logFilePath, logMessage);
    }

    public void OnCreate(Vector3 position, Quaternion rotation, Vector3 scale, Color color, params KeyValuePair<string, object>[] kwargs)
    {
        logFilePath = SetROSBridge.LogFilePath + "_multibar.csv";
        if (!File.Exists(logFilePath))
        {
            File.Create(logFilePath).Close();
        }

        foreach (var param in kwargs)
        {
            if (param.Key == "Solidangle") { Solidangle = (float)param.Value; }
            if (param.Key == "Angle") { Angle = (float)param.Value; }
            if (param.Key == "Distance") { Distance = (float)param.Value; }
        }

        renderer = GetComponent<MeshRenderer>();
        renderer.enabled = false;
        avatar = GameObject.Find("Avatar").transform;


        
        Vector3 direction = new Vector3(Mathf.Sin(Angle * Mathf.Deg2Rad), 0, Mathf.Cos(Angle * Mathf.Deg2Rad));
        DefaultOffset = direction * Distance;
        transform.localRotation = DefaultOrientation * Quaternion.Euler(Angle, 0, 0);
        // Adjust the scale based on the solid angle
        float scaleFactor = Mathf.Sqrt(Solidangle);
        transform.localScale = scale * scaleFactor;
        lastScale = transform.localScale;

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
    
    }

    public float Angle = 0;
    public float Solidangle = 20;

    public float Distance = 20;

    private Quaternion DefaultOrientation  = Quaternion.Euler(0,90,90);
    private Vector3 DefaultOffset  = new Vector3 (10,10,0);

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

