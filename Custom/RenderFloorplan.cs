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

    private Texture2D currentTexture;  // Add this field at class level

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

    void Update()
    {   
        if(_attachToPlayer)
        {
            transform.position = avatar.position + DefaultOffset;
        }
        
        // Calculate UV coordinates after any position updates
        Vector3 localPos = transform.InverseTransformPoint(avatar.position);
        
        // Flip the Z coordinate for proper UV mapping
        Vector2 textureCoord = new Vector2(
            ((-localPos.x / 5f) + 1.0f) / 2.0f,
            ((-localPos.z / 5f) + 1.0f) / 2.0f  // Note the negative sign here
        );
        
        if (currentTexture != null)
        {
            Color pixelColor = SampleTexture(textureCoord);
            Debug.Log($"Local X: {localPos.x}, Local Z: {localPos.z}, X: {avatar.position.x}, Z: {avatar.position.z}");
            Debug.Log($"Avatar at texture coordinate ({textureCoord.x:F2}, {textureCoord.y:F2}), Color: {pixelColor}");
            Debug.Log($"Local Scale: {transform.localScale}");
        }
    }

    private Color SampleTexture(Vector2 uv)
    {
        if (currentTexture == null) return Color.black;
        
        // Clamp UV coordinates to [0,1]
        uv.x = Mathf.Clamp01(uv.x);
        uv.y = Mathf.Clamp01(uv.y);
        
        // Convert to pixel coordinates
        int x = Mathf.FloorToInt(uv.x * (currentTexture.width - 1));
        int y = Mathf.FloorToInt(uv.y * (currentTexture.height - 1));
        
        return currentTexture.GetPixel(x, y);  // Fixed typo: 'ax' to 'x'
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

                currentTexture = texture;  // Save reference to texture
                GetComponent<Renderer>().material.mainTexture = texture;
            }
        }

        if (GetComponent<MeshRenderer>() != null)
        {
            mat = GetComponent<MeshRenderer>().material;

            
        }


      
        
    
    }

    private Vector3 DefaultOffset  = new Vector3 (0,-1,0);

}