using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Data;
using System.Linq;


public class RenderFloorplan : MonoBehaviour
{
    private Transform avatar;
    private Material mat;
    private MeshRenderer renderer;
    private string logFilePath;
    [SerializeField]
    private bool _attachToPlayer = false;
    private bool _resetPosition = false;
    public bool ResetPosition
    {
        get { return _resetPosition; }
        set { _resetPosition = false; 
            if (value)
            {
               RecenterAvatar();
            }
        }
    }
    public bool AttachToPlayer
    {
        get { return _attachToPlayer; }
        set { _attachToPlayer = value; }
    }
    [SerializeField]
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
    
    public string CurrentTexture
    {
        get { return "texture"; }
        set { 
            // Parse the JSON string into a JArray
            var jsonArray = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(value);
            if (jsonArray != null)
            {
                int height = jsonArray.Count;
                int width = jsonArray[0].Count();
                Color[] pixels = new Color[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var rgb = jsonArray[y][x].ToObject<float[]>();
                        pixels[y * width + x] = new Color(rgb[0], rgb[1], rgb[2], 1f);
                  
                    }
                }
                Texture2D texture = new Texture2D(width, height);
                texture.filterMode = FilterMode.Point;  
                texture.SetPixels(pixels);
                texture.Apply();
                _currentTexture = texture;  
                GetComponent<Renderer>().material.mainTexture = texture;
            }
        }}
        
    private Texture2D _currentTexture; 
    public string CurrentCollider
    {
        get { return "collider"; }
        set { 
           var jsonArray = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(value);
            int height = jsonArray.Count;
            int width = jsonArray[0].Count();
            _currentCollider = new int[height, width];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    _currentCollider[y, x] = jsonArray[y][x].ToObject<int>();
                }
            }

            wasLastPositionValid = false;
            }
        }
    private int[,] _currentCollider; 
    private Vector3 lastValidPosition;
    private bool wasLastPositionValid = false;
    private Vector3 _defaultLocalPosition = new Vector3(0f, 0f, 0f);
    public Vector3 DefaultLocalPosition
    {
        get { return _defaultLocalPosition; }
        set { _defaultLocalPosition = value; }
    }
    
    [SerializeField]
    private bool _enableCollider = true;
    public bool EnableCollider
    {
        get { return _enableCollider; }
        set { _enableCollider = value; }
    }
    
    void Start()
    {
        // #if UNITY_EDITOR
        // avatar = GameObject.Find("Avatar").transform;
        
        // // Get the renderer component
        // renderer = GetComponent<MeshRenderer>();
        // if (renderer == null)
        // {
        //     Debug.LogError("No MeshRenderer found!");
        //     return;
        // }

        // // Make sure the renderer is visible
        // renderer.enabled = true;
        
        // // Create a 2D texture (for example 256x256)
        // int width = 6;
        // int height = 6;
        // Texture2D texture = new Texture2D(width, height);
        
        // // Create and fill the pixel array
        // Color[] pixels = new Color[width * height];
        // System.Random random = new System.Random();
        // for (int y = 0; y < height; y++)
        // {
        //     for (int x = 0; x < width; x++)
        //     {
        //         // Create random RGB colors
        //         float r = (float)random.NextDouble();
        //         float g = (float)random.NextDouble();
        //         float b = (float)random.NextDouble();
        //         pixels[y * width + x] = new Color(r, g, b, 1f);
        //     }
        // }
        
        // // Apply the pixels to the texture
        // texture.SetPixels(pixels);
        // texture.filterMode = FilterMode.Point;
        // texture.Apply();
        
        // // Create a new material using the standard shader
        // Material material = new Material(Shader.Find("Standard"));
        // material.mainTexture = texture;
        
        // // Assign the material to the renderer
        // renderer.material = material;
        
        // Debug.Log("Texture applied to material");
        // #endif
    }
    void RecenterAvatar()
    {
        Vector3 worldPos = transform.TransformPoint(new Vector3(
            DefaultLocalPosition.x * 5f,  
            avatar.position.y,
            DefaultLocalPosition.z * 5f   
        ));
        avatar.position = worldPos;
        Debug.Log($"DefaultLocalPosition {DefaultLocalPosition}");
        Debug.Log($"Recentered avatar to {worldPos}");
    }

    void Update()
    {   
        if(_attachToPlayer)
        {
             transform.position = avatar.position + DefaultOffset;
        }
                
        Vector3 localPos = transform.InverseTransformPoint(avatar.position);
        Vector2 textureCoord = new Vector2(
            ((-localPos.x / 5f) + 1.0f) / 2.0f,
            ((-localPos.z / 5f) + 1.0f) / 2.0f 
        );
        
       
        bool isValidPosition = !_enableCollider || SampleCollider(textureCoord);

        if (isValidPosition)
        {
            lastValidPosition = avatar.position;
            wasLastPositionValid = true;
        }
        else if (wasLastPositionValid)
        {
            avatar.position = lastValidPosition;
        }
        else
        {
            RecenterAvatar();
        }

        if (_currentTexture != null)
        {
            Color pixelColor = SampleTexture(textureCoord);
            bool colliderColor = SampleCollider(textureCoord);
            Debug.Log($"Local X: {localPos.x}, Local Z: {localPos.z}, X: {avatar.position.x}, Z: {avatar.position.z}");
            Debug.Log($"Avatar at texture coordinate ({textureCoord.x:F2}, {textureCoord.y:F2}), Color: {pixelColor}");
            Debug.Log($"Avatar at collider coordinate ({textureCoord.x:F2}, {textureCoord.y:F2}), Color: {colliderColor}");
            Debug.Log($"Local Scale: {transform.localScale}");
        }
    }

    private Color SampleTexture(Vector2 uv)
    {
        if (_currentTexture == null) return Color.black;
        
        // Clamp UV coordinates to [0,1]
        uv.x = Mathf.Clamp01(uv.x);
        uv.y = Mathf.Clamp01(uv.y);
        
        // Convert to pixel coordinates
        int x = Mathf.FloorToInt(uv.x * (_currentTexture.width - 1));
        int y = Mathf.FloorToInt(uv.y * (_currentTexture.height - 1));
        
        return _currentTexture.GetPixel(x, y);  // Fixed typo: 'ax' to 'x'
    }

    private bool SampleCollider(Vector2 uv)
    {
        if (_currentCollider == null) return false;
        
        // Clamp UV coordinates to [0,1]
        uv.x = Mathf.Clamp01(uv.x);
        uv.y = Mathf.Clamp01(uv.y);
        
        // Convert to grid coordinates
        int x = Mathf.FloorToInt(uv.x * (_currentCollider.GetLength(1) - 1));
        int y = Mathf.FloorToInt(uv.y * (_currentCollider.GetLength(0) - 1));
        
        return _currentCollider[y, x] == 1;
    }

    public void OnCreate(Vector3 position, Quaternion rotation, Vector3 scale, Color color, KeyValuePair<string, object>[] kvlist)
    {
       
        transform.localScale=scale;
     
        renderer = GetComponent<MeshRenderer>();
        renderer.enabled = false;
        avatar = GameObject.Find("Avatar").transform;

        // Find the texture data in kvlist
        var textureData = kvlist.FirstOrDefault(kv => kv.Key == "texture");
        if (textureData.Value != null)
        {
            // Convert the JSON array structure to a C# array
            var jsonArray = textureData.Value as Newtonsoft.Json.Linq.JArray;
            if (jsonArray != null)
            {
                int height = jsonArray.Count;
                int width = jsonArray[0].Count();
                Color[] pixels = new Color[width * height];
                
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var rgb = jsonArray[y][x].ToObject<float[]>();
                        pixels[y * width + x] = new Color(rgb[0], rgb[1], rgb[2], 1f);
                  
                    }
                }

                // Create and apply the texture
                Texture2D texture = new Texture2D(width, height);
                texture.filterMode = FilterMode.Point;  // For nearest neighbor sampling
                texture.SetPixels(pixels);
                texture.Apply();

                _currentTexture = texture;  // Save reference to texture
                GetComponent<Renderer>().material.mainTexture = texture;
            }
        }
        
        var colliderData = kvlist.FirstOrDefault(kv => kv.Key == "collider");
        if (colliderData.Value != null)
        {
            var jsonArray = colliderData.Value as Newtonsoft.Json.Linq.JArray;
            if (jsonArray != null)
            {
                int height = jsonArray.Count;
                int width = jsonArray[0].Count();
                _currentCollider = new int[height, width];
                
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        _currentCollider[y, x] = jsonArray[y][x].ToObject<int>();
                    }
                }

           
            }
        }
            

        if (GetComponent<MeshRenderer>() != null)
        {
            mat = GetComponent<MeshRenderer>().material;

            
        }


      
        
    
    }

    private Vector3 DefaultOffset  = new Vector3 (0,-1,0);

}