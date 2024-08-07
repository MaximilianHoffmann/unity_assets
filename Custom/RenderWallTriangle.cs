using Unity.VisualScripting;
using UnityEngine;
using System.IO;
using System;
using System.Data;
using UnityEngine.Rendering;

public class RenderWallTriangle : MonoBehaviour
{
    private Transform avatarTransform;
    private GameObject avatar;
    private Material mat;
    private MeshRenderer renderer;
    private Vector3 lastScale;
    private string logFilePath;
    private static readonly int _TimeStep = Shader.PropertyToID("_TimeStep");

    public float colliderRadius = 0.99f;

    public float TimeStep
    {
        get => mat.GetFloat(_TimeStep);
        set => mat.SetFloat(_TimeStep, value);
    }

    [SerializeField]
    private bool _EncloseFlag = true;


    public bool EncloseFlag
    {
        get
        {
            return _EncloseFlag;
        }

        set
        {
            _EncloseFlag = value;
        }
    }

    private bool _Visible = true;
    public bool Visible


    {
        get
        {
            return _Visible;
        }

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
        
        Vector3 pos=new Vector3(0,0,0);
        Quaternion rot = new Quaternion(0,0,0,1);
        Color white= new Color(1,1,1,1);
        OnCreate(pos,rot,40,40,white);
        renderer.enabled = true;
#endif

    }

    bool PointInTriangle(Vector2 p, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        float denominator = 1 / ((v2.y - v3.y) * (v1.x - v3.x) + (v3.x - v2.x) * (v1.y - v3.y));
        float alpha = ((v2.y - v3.y) * (p.x - v3.x) + (v3.x - v2.x) * (p.y - v3.y)) * denominator;
        float beta = ((v3.y - v1.y) * (p.x - v3.x) + (v1.x - v3.x) * (p.y - v3.y)) * denominator;
        float gamma = 1 - alpha - beta;

        return alpha > 0 && beta > 0 && gamma > 0;
    }

    float DistancePointToLine(Vector2 p, Vector2 v1, Vector2 v2)
    {
        Vector2 v = v2 - v1;
        Vector2 w = p - v1;

        float c1 = Vector2.Dot(w, v);
        float c2 = Vector2.Dot(v, v);

        float b = c1 / c2;

        Vector2 pb = v1 + b * v;
        return Vector2.Distance(p, pb);
    }

    Vector2 GetNearestPointOnTriangleBoundary(Vector2 p, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        float d1 = DistancePointToLine(p, v1, v2);
        float d2 = DistancePointToLine(p, v2, v3);
        float d3 = DistancePointToLine(p, v3, v1);

        float minDistance = Mathf.Min(d1, Mathf.Min(d2, d3));

        if (minDistance == d1)
            return ClosestPointOnLineSegment(p, v1, v2);
        else if (minDistance == d2)
            return ClosestPointOnLineSegment(p, v2, v3);
        else
            return ClosestPointOnLineSegment(p, v3, v1);
    }

    Vector2 ClosestPointOnLineSegment(Vector2 p, Vector2 v1, Vector2 v2)
    {
        Vector2 v = v2 - v1;
        Vector2 w = p - v1;

        float c1 = Vector2.Dot(w, v);
        if (c1 <= 0)
            return v1;

        float c2 = Vector2.Dot(v, v);
        if (c2 <= c1)
            return v2;

        float b = c1 / c2;
        Vector2 pb = v1 + b * v;
        return pb;
    }

    bool evaluateEnclosure()
    {

        float delta = 1.0f - colliderRadius;
        float angle30 = 30.0f * Mathf.Deg2Rad;
        float angle60 = 60.0f * Mathf.Deg2Rad;

        float delta2 = delta * Mathf.Sin(angle60) / Mathf.Sin(angle30);
        float delta3 = delta / Mathf.Sin(angle30);

        Vector2 point1 = new Vector2(colliderRadius * (Mathf.Sqrt(3.00f) - 1.00f) - delta3, 0.00f);
        Vector2 point2 = new Vector2(-colliderRadius + delta, -colliderRadius + delta2);
        Vector2 point3 = new Vector2(-colliderRadius + delta, colliderRadius - delta2);

        //Vector2 point2 = new Vector2(-0.942f, -1f);
        //Vector2 point3 = new Vector2(-0.942f, 1f);

        Vector3 localPosition = transform.InverseTransformPoint(avatarTransform.position);

        Vector2 pointp = new Vector2(localPosition.x, localPosition.z);
        float norm = Mathf.Sqrt(localPosition.x * localPosition.x + localPosition.z * localPosition.z);
        //Debug.Log("Relative Position of avatar in enclusore" + localPosition);
        if (PointInTriangle(pointp, point1, point2, point3))
        {
            Debug.Log("I am inside");
            Debug.Log("Relative Position of avatar in enclusore" + localPosition);
            return false;
        }
        else
        {
            Debug.Log("I am outside");
            Debug.Log("Relative Position of avatar in enclusore" + localPosition);
            return true;
        }
    }

    void resetPosition()
    {
        Vector3 localPosition = transform.InverseTransformPoint(avatarTransform.position);
        float norm = Mathf.Sqrt(localPosition.x * localPosition.x + localPosition.z * localPosition.z);
        float delta = 1.0f - colliderRadius;
        float angle30 = 30.0f * Mathf.Deg2Rad;
        float angle60 = 60.0f * Mathf.Deg2Rad;

        float delta2 = delta * Mathf.Sin(angle60) / Mathf.Sin(angle30);
        float delta3 = delta / Mathf.Sin(angle30);

        Vector2 point1 = new Vector2(colliderRadius * (Mathf.Sqrt(3.00f) - 1.00f) - delta3, 0.00f);
        Vector2 point2 = new Vector2(-colliderRadius + delta, -colliderRadius + delta2);
        Vector2 point3 = new Vector2(-colliderRadius + delta, colliderRadius - delta2);

        Vector2 pointp = new Vector2(localPosition.x, localPosition.z);

        Vector2 nearest = GetNearestPointOnTriangleBoundary(pointp, point1, point2, point3).normalized * colliderRadius;
        //Vector3 newLocalPosition = new Vector3(nearest.x, localPosition.y, nearest.y);

        //Vector3 newLocalPosition = new Vector3(localPosition.x / norm * factor, localPosition.y, localPosition.z / norm * factor);
        Vector3 newLocalPosition = new Vector3(localPosition.x * colliderRadius, localPosition.y, localPosition.z * colliderRadius);

        Vector3 newPosition = transform.TransformPoint(newLocalPosition);
        Debug.Log("Collider Radius" + colliderRadius);
        Debug.Log("New Local Position" + newLocalPosition);
        Debug.Log("New Avatar Position" + newPosition);
        avatarTransform.position = newPosition;
    }

    void Update()
    {

        if (EncloseFlag)
        {
            bool outside = evaluateEnclosure();
            Debug.Log(outside);
            if (outside)
            {
                Debug.Log("RESET POSITION");
                resetPosition();
            }
        }
    }
    // public float colliderRadialPos = 1.5f;
    // public Vector3 colliderSize = new Vector3(0.01f, 1f, 0.01f);

    public void OnCreate(Vector3 position, Quaternion rotation, float height, float radius, Color color)
    {


        renderer = GetComponent<MeshRenderer>();
        renderer.enabled = true;
        avatarTransform = GameObject.Find("Avatar").transform;
        avatar = GameObject.Find("Avatar");
        transform.localPosition = position;
        transform.localRotation = rotation;
        transform.localScale = new Vector3(radius, height, radius);
        lastScale = transform.localScale;
        if (GetComponent<MeshRenderer>() != null)
        {
            mat = GetComponent<MeshRenderer>().material;


        }


        // for (int i = 0; i < 180; i++)
        // {
        //     float angle = i * 2 * Mathf.PI / 180;
        //     Vector3 pos = new Vector3(Mathf.Sin(angle) * colliderRadialPos, 0, Mathf.Cos(angle) * colliderRadialPos);
        //     BoxCollider collider = gameObject.AddComponent<BoxCollider>();
        //     // collider.size = new Vector3(Mathf.Cos(angle) *colliderVar.x + colliderSize.x,colliderSize.y, Mathf.Sin(angle) * colliderVar.z + colliderSize.z);
        //     collider.size=colliderSize;
        //     collider.center = pos;
        //     collider.isTrigger=true;

        // }

    }



    public static (float r, float theta) CartesianToPolar(float x, float z)
    {
        float r = Mathf.Sqrt(x * x + z * z);
        float theta = Mathf.Atan2(z, x) + Mathf.PI / 2;

        return (r, theta);
    }



    // public Vector3 resetPosition = Vector3.zero; // The position to reset to
    // private void OnTriggerEnter(Collider other)
    // {
    //     if (other.gameObject == avatar)
    //     {   

    //         //myAvatar.transform.position = new Vector3( myAvatar.transform.position.x - 1f , myAvatar.transform.position.y, myAvatar.transform.position.z -1);
    //          var (r, theta) = CartesianToPolar(avatar_transform.position.x , avatar_transform.position.z);
    //         Debug.Log("From:" + avatar_transform.position.x+  " " +   avatar_transform.position.z);
    //         Debug.Log("To" + .95f*transform.localScale[0]*Mathf.Sin(theta) +  " " +  0.95f*transform.localScale[1]*Mathf.Cos(theta));
    //         avatar_transform.position = new Vector3(0.95f*transform.localScale[0]*Mathf.Sin(theta) , avatar_transform.position.y,-0.95f*transform.localScale[1]*Mathf.Cos(theta));

    //     }
    // }

}


