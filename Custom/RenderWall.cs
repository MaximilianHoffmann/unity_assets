using Unity.VisualScripting;
using UnityEngine;
using System.IO;
using System;
using System.Data;
using UnityEngine.Rendering;

// Don't forget to change the class name when using this as a template
// for implementing new objects with custom behaviors
public class RenderWall : MonoBehaviour
{
    private Transform avatar;
    private Material mat;
    private MeshRenderer renderer;
    private Vector3 lastScale;
    private string logFilePath;
    private static readonly int _TimeStep = Shader.PropertyToID("_TimeStep");

   
    public float TimeStep
        {
            get => mat.GetFloat(_TimeStep);
            set => mat.SetFloat(_TimeStep, value);
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
        
       
    void Start()
    {
        #if UNITY_EDITOR
        
        Vector3 pos=new Vector3(0,0,0);
        Quaternion rot = new Quaternion(0,0,0,1);
        Color white= new Color(1,1,1,1);
        OnCreate(pos,rot,5,20,white);
        #endif
     
    }

    void Update()
    {        
     
    }
 
    public void OnCreate(Vector3 position, Quaternion rotation, float height, float radius, Color color)
    {
        
        
        renderer = GetComponent<MeshRenderer>();
        renderer.enabled=false;
        avatar = GameObject.Find("Avatar").transform;
        transform.localPosition = position;
        transform.localRotation = rotation;
        transform.localScale = new Vector3(radius, height, radius);
        lastScale=transform.localScale;
        if (GetComponent<MeshRenderer>() != null)
        {
            mat = GetComponent<MeshRenderer>().material;


        }

    }
}


