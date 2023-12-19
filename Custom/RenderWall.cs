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
    private Transform avatar_transform;
    private GameObject avatar;
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
        OnCreate(pos,rot,40,40,white);
        renderer.enabled = true;
        #endif
     
    }

    void Update()
    {        
     
    }

    public float colliderRadialPos = 1.5f;
    public Vector3 colliderSize = new Vector3(0.01f, 1f, 0.01f);
 
    public void OnCreate(Vector3 position, Quaternion rotation, float height, float radius, Color color)
    {
        
        
        renderer = GetComponent<MeshRenderer>();
        renderer.enabled=false;
        avatar_transform = GameObject.Find("Avatar").transform;
        avatar= GameObject.Find("Avatar");
        transform.localPosition = position;
        transform.localRotation = rotation;
        transform.localScale = new Vector3(radius, height, radius);
        lastScale=transform.localScale;
        if (GetComponent<MeshRenderer>() != null)
        {
            mat = GetComponent<MeshRenderer>().material;


        }

       
        for (int i = 0; i < 180; i++)
        {
            float angle = i * 2 * Mathf.PI / 180;
            Vector3 pos = new Vector3(Mathf.Sin(angle) * colliderRadialPos, 0, Mathf.Cos(angle) * colliderRadialPos);
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            // collider.size = new Vector3(Mathf.Cos(angle) *colliderVar.x + colliderSize.x,colliderSize.y, Mathf.Sin(angle) * colliderVar.z + colliderSize.z);
            collider.size=colliderSize;
            collider.center = pos;
            collider.isTrigger=true;
            
        }

    }

 
      
        public static (float r, float theta) CartesianToPolar(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            float theta = Mathf.Atan2(z, x) + Mathf.PI/2;

            return (r, theta);
        }



    public Vector3 resetPosition = Vector3.zero; // The position to reset to
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == avatar)
        {   
           
            //myAvatar.transform.position = new Vector3( myAvatar.transform.position.x - 1f , myAvatar.transform.position.y, myAvatar.transform.position.z -1);
             var (r, theta) = CartesianToPolar(avatar_transform.position.x , avatar_transform.position.z);
            Debug.Log("From:" + avatar_transform.position.x+  " " +   avatar_transform.position.z);
            Debug.Log("To" + .95f*transform.localScale[0]*Mathf.Sin(theta) +  " " +  0.95f*transform.localScale[1]*Mathf.Cos(theta));
            avatar_transform.position = new Vector3(0.95f*transform.localScale[0]*Mathf.Sin(theta) , avatar_transform.position.y,-0.95f*transform.localScale[1]*Mathf.Cos(theta));
           
        }
    }

}


