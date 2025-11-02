using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Data;
using System.Linq;


public class RenderRoom : MonoBehaviour
{
    private const float PlaneHalfSize = 5f; // Unity plane spans [-5, 5] in local space

    private Transform avatar;
    private Material mat;
    private MeshRenderer renderer;
    private string logFilePath;

    // Wall management
    private GameObject wallContainer;
    private List<GameObject> wallQuads = new List<GameObject>();
    [SerializeField]
    private Material wallMaterial;
    private Material wallMaterialInstance; // Instance of wall material for this room
    private List<WallSegment> _currentWallSegments = new List<WallSegment>(); // Pre-computed wall segments
    private Vector3 _lastRoomScale = Vector3.zero;

    // Shader property IDs for performance
    private static readonly int _Color1ID = Shader.PropertyToID("_Color1");
    private static readonly int _Color2ID = Shader.PropertyToID("_Color2");
    private static readonly int _NoiseScaleID = Shader.PropertyToID("_NoiseScale");
    private static readonly int _SeedID = Shader.PropertyToID("_Seed");
    private static Material fallbackWallMaterial;
    private bool wallMaterialDirty = true;
    private bool wallMaterialAppliedOnce = false;
    private Color _appliedWallColor1;
    private Color _appliedWallColor2;
    private float _appliedNoiseScale;
    private MaterialPropertyBlock wallPropertyBlock;

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
            Debug.Log($"Visible property setter called with value={value}, old value={_Visible}");
            _Visible = value;
            if (renderer != null)
            {
                renderer.enabled = _Visible;
            }

            // Update wall visibility
            UpdateWallVisibility();
        }
    }

    [SerializeField]
    private bool _WallsVisible = false;
    public bool WallsVisible
    {
        get { return _WallsVisible; }
        set
        {
            Debug.Log($"WallsVisible property setter called with value={value}, old value={_WallsVisible}");
            _WallsVisible = value;
            UpdateWallVisibility();
        }
    }

    // Track previous visibility state to detect changes via reflection
    private bool _lastVisibleState = false;
    private bool _lastWallsVisibleState = false;

    // Wall height is now stored per-segment in WallSegment struct
    // This property is kept for backwards compatibility but not used internally

    private Color _wallColor1 = new Color(0.2f, 0.2f, 0.2f, 1.0f);
    public Color WallColor1
    {
        get { return _wallColor1; }
        set
        {
            Debug.Log($"WallColor1 setter called: {value}");
            _wallColor1 = value;
            wallMaterialDirty = true;
            ApplyWallMaterialProperties();
        }
    }

    private Color _wallColor2 = new Color(0.8f, 0.8f, 0.8f, 1.0f);
    public Color WallColor2
    {
        get { return _wallColor2; }
        set
        {
            Debug.Log($"WallColor2 setter called: {value}");
            _wallColor2 = value;
            wallMaterialDirty = true;
            ApplyWallMaterialProperties();
        }
    }

    private float _noiseScale = 1.0f;
    public float NoiseScale
    {
        get { return _noiseScale; }
        set
        {
            Debug.Log($"NoiseScale setter called: {value}");
            _noiseScale = value;
            wallMaterialDirty = true;
            ApplyWallMaterialProperties();
        }
    }

    // Backwards compatibility: WallColor sets both colors to the same value
    public Color WallColor
    {
        get { return _wallColor1; }
        set
        {
            WallColor1 = value;
            WallColor2 = value;
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

    public string Walls
    {
        get { return "walls"; }
        set {
            // Expected format: Array of wall segments
            // Each segment: {"x1": float, "z1": float, "x2": float, "z2": float, "height": float}
            var jsonArray = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(value);
            _currentWallSegments.Clear();

            foreach (var segmentData in jsonArray)
            {
                WallSegment segment = new()
                {
                    x1 = segmentData["x1"].ToObject<float>(),
                    z1 = segmentData["z1"].ToObject<float>(),
                    x2 = segmentData["x2"].ToObject<float>(),
                    z2 = segmentData["z2"].ToObject<float>(),
                    height = segmentData["height"].ToObject<float>(),
                    nx = segmentData["nx"] != null ? segmentData["nx"].ToObject<float>() : 0f,
                    nz = segmentData["nz"] != null ? segmentData["nz"].ToObject<float>() : 0f
                };
                _currentWallSegments.Add(segment);
            }

            // Regenerate wall geometry
            GenerateWallQuads();
        }
    }

    private Vector3 lastValidPosition;
    private bool wasLastPositionValid = false;
    private Vector3 _defaultLocalPosition = new Vector3(0f, 0f, 0f);
    public Vector3 DefaultLocalPosition
    {
        get { return _defaultLocalPosition; }
        set { _defaultLocalPosition = value; }
    }
    
    [SerializeField]
    private bool _EnableCollider = false;
    public bool EnableCollider
    {
        get { return _EnableCollider; }
        set { _EnableCollider = value; }
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
        double unity_time = DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000d;
        if (File.Exists(logFilePath)) 
        { 
            File.AppendAllText(logFilePath, 
                $"{unity_time:F3},{PublishVRPosition.LastTimestamp:F3}," +
                $"{avatar.position.x:F3},{avatar.position.z:F3},{avatar.position.y:F3}," + 
                $"{worldPos.x:F3},{worldPos.z:F3},{worldPos.y:F3}\n" ) ;   }

    }

    void Update()
    {
        // Early return if avatar not yet initialized
        if (avatar == null)
            return;

        // Check if visibility has changed (handles reflection-based property setting from ROS)
        if (_Visible != _lastVisibleState || _WallsVisible != _lastWallsVisibleState)
        {
            _lastVisibleState = _Visible;
            _lastWallsVisibleState = _WallsVisible;
            UpdateWallVisibility();

            // Also update floor renderer
            if (renderer != null)
            {
                renderer.enabled = _Visible;
            }
        }

        if (!ApproximatelyEqual(transform.localScale, _lastRoomScale))
        {
            _lastRoomScale = transform.localScale;
            ConfigureWallContainerTransform();
        }

        DetectExternalWallMaterialChanges();
        ApplyWallMaterialProperties();

        if(_attachToPlayer)
        {
             transform.position = avatar.position + DefaultOffset;
        }

        Vector3 localPos = transform.InverseTransformPoint(avatar.position);
        float u = Mathf.InverseLerp(PlaneHalfSize, -PlaneHalfSize, localPos.x);
        float v = Mathf.InverseLerp(PlaneHalfSize, -PlaneHalfSize, localPos.z);
        Vector2 textureCoord = new Vector2(u, v);
        
       
        bool isValidPosition = !_EnableCollider || SampleCollider(textureCoord);

        if (isValidPosition)
        {
            lastValidPosition = avatar.position;
            wasLastPositionValid = true;
        }
        else if (wasLastPositionValid)
        {
            

            double unity_time = DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000d;
            if (File.Exists(logFilePath)) 
            { 
                File.AppendAllText(logFilePath, 
                    $"{unity_time:F3},{PublishVRPosition.LastTimestamp:F3}," + 
                    $"{avatar.position.x:F3},{avatar.position.z:F3},{avatar.position.y:F3}," +
                    $"{lastValidPosition.x:F3},{lastValidPosition.z:F3},{lastValidPosition.y:F3}\n" ) ;   }
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
            // Debug.Log($"Local X: {localPos.x}, Local Z: {localPos.z}, X: {avatar.position.x}, Z: {avatar.position.z}");
            // Debug.Log($"Avatar at texture coordinate ({textureCoord.x:F2}, {textureCoord.y:F2}), Color: {pixelColor}");
            // Debug.Log($"Avatar at collider coordinate ({textureCoord.x:F2}, {textureCoord.y:F2}), Color: {colliderColor}");
            // Debug.Log($"Local Scale: {transform.localScale}");
        }
    }

    private Color SampleTexture(Vector2 uv)
    {
        if (_currentTexture == null) return Color.black;
        
        int width = _currentTexture.width;
        int height = _currentTexture.height;

        int x = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(uv.x) * width), 0, width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(uv.y) * height), 0, height - 1);

        return _currentTexture.GetPixel(x, y);  // Fixed typo: 'ax' to 'x'
    }

    private bool SampleCollider(Vector2 uv)
    {
        if (_currentCollider == null) return false;

        int width = _currentCollider.GetLength(1);
        int height = _currentCollider.GetLength(0);

        int x = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(uv.x) * width), 0, width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(uv.y) * height), 0, height - 1);

        return _currentCollider[y, x] == 1;
    }

    public void OnCreate(Vector3 position, Quaternion rotation, Vector3 scale, Color color, KeyValuePair<string, object>[] kvlist)
    {
       
        transform.localScale=scale;
     
        renderer = GetComponent<MeshRenderer>();
        renderer.enabled = false;
        avatar = GameObject.Find("Avatar").transform;


        string gameObjectName = gameObject.name;
        
             
        
        logFilePath = SetROSBridge.LogFilePath + "_" + gameObjectName+".csv";
        Debug.Log($"Log file path: {logFilePath}");
        if (SetROSBridge.LogFilePath != "")
        {
            File.WriteAllText(logFilePath, 
                "unity_time,last_timestamp," +
                "old_proper_position_x,old_proper_position_y,old_proper_position_z," +
                "new_proper_position_x,new_proper_position_y,new_proper_position_z" +
                "\n");
        }

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

        // Check for wall segment data
        var wallsData = kvlist.FirstOrDefault(kv => kv.Key == "walls");
        if (wallsData.Value != null)
        {
            var jsonArray = wallsData.Value as Newtonsoft.Json.Linq.JArray;
            if (jsonArray != null)
            {
                _currentWallSegments.Clear();

                foreach (var segmentData in jsonArray)
                {
                    WallSegment segment = new()
                    {
                        x1 = segmentData["x1"].ToObject<float>(),
                        z1 = segmentData["z1"].ToObject<float>(),
                        x2 = segmentData["x2"].ToObject<float>(),
                        z2 = segmentData["z2"].ToObject<float>(),
                        height = segmentData["height"].ToObject<float>(),
                        nx = segmentData["nx"] != null ? segmentData["nx"].ToObject<float>() : 0f,
                        nz = segmentData["nz"] != null ? segmentData["nz"].ToObject<float>() : 0f
                    };
                    _currentWallSegments.Add(segment);
                }

                // Generate wall geometry after all data is loaded
                GenerateWallQuads();
            }
        }

        if (GetComponent<MeshRenderer>() != null)
        {
            mat = GetComponent<MeshRenderer>().material;
        }
    }

    private Vector3 DefaultOffset  = new Vector3 (0,-1,0);

    // Helper struct for wall segments
    private struct WallSegment
    {
        public float x1;      // Start X coordinate (world space)
        public float z1;      // Start Z coordinate (world space)
        public float x2;      // End X coordinate (world space)
        public float z2;      // End Z coordinate (world space)
        public float height;  // Wall height
        public float nx;      // Normal X component (points toward walkable area)
        public float nz;      // Normal Z component (points toward walkable area)
    }


    /// <summary>
    /// Generates wall quad GameObjects from pre-computed wall segments.
    /// Segments are provided in world coordinates (local to the RenderRoom transform).
    /// </summary>
    private void GenerateWallQuads()
    {
        // Clean up existing walls
        DestroyWalls();

        if (_currentWallSegments == null || _currentWallSegments.Count == 0)
        {
            Debug.Log("No wall segments to render");
            return;
        }

        // Create wall container if needed
        if (wallContainer == null)
        {
            wallContainer = new GameObject("Walls");
            wallContainer.transform.SetParent(transform, false);
        }

        ConfigureWallContainerTransform();

        // Create or find wall material instance
        if (wallMaterialInstance == null)
        {
            if (wallMaterial != null)
            {
                // Create instance from assigned material
                wallMaterialInstance = new Material(wallMaterial);
            }
            else
            {
                // Try to find RoomWall shader
                Shader roomWallShader = Shader.Find("Custom/RoomWall");
                if (roomWallShader != null)
                {
                    wallMaterialInstance = new Material(roomWallShader);
                    Debug.Log("Created wall material from RoomWall shader");
                }
                else
                {
                    // Fallback to standard shader
                    wallMaterialInstance = new Material(Shader.Find("Standard"));
                    Debug.LogWarning("RoomWall shader not found, using Standard shader");
                }
            }

            // Set initial colors and noise parameters
            wallMaterialInstance.SetFloat(_SeedID, UnityEngine.Random.value * 1000f);
            wallMaterialDirty = true;
            ApplyWallMaterialProperties();
        }

        foreach (var segment in _currentWallSegments)
        {
            GameObject wallQuad = CreateWallQuad(segment);
            wallQuad.transform.SetParent(wallContainer.transform, false);
            wallQuads.Add(wallQuad);
        }

        Debug.Log($"Generated {wallQuads.Count} wall quads from {_currentWallSegments.Count} segments");

        // Update wall visibility to match current settings
        UpdateWallVisibility();
    }

    /// <summary>
    /// Creates a vertical quad GameObject between two points on the ground.
    /// </summary>
    private GameObject CreateWallQuad(WallSegment segment)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "WallQuad";

        Vector3 start = new Vector3(segment.x1, 0f, segment.z1);
        Vector3 end = new Vector3(segment.x2, 0f, segment.z2);

        Vector3 direction = end - start;
        float length = direction.magnitude;
        if (length < 1e-4f)
        {
            length = 1e-4f;
            direction = Vector3.right;
        }

        Vector3 center = (start + end) * 0.5f;
        center.y = segment.height * 0.5f;

        quad.transform.localPosition = center;
        quad.transform.localScale = new Vector3(length, segment.height, 1f);

        Vector3 normal = new Vector3(segment.nx, 0f, segment.nz);
        if (normal.sqrMagnitude < 1e-6f)
        {
            normal = Vector3.Cross(Vector3.up, direction).normalized;
            if (normal.sqrMagnitude < 1e-6f)
            {
                normal = Vector3.forward;
            }
        }
        else
        {
            normal.Normalize();
        }
        normal = -normal; // Ensure front face points toward interior

        Vector3 tangent = direction.normalized;
        if (tangent.sqrMagnitude < 1e-6f)
        {
            tangent = Vector3.right;
        }

        Quaternion rotation = Quaternion.LookRotation(normal, Vector3.up);
        Vector3 currentRight = rotation * Vector3.right;
        float alignAngle = Vector3.SignedAngle(currentRight, tangent, normal);
        rotation = Quaternion.AngleAxis(alignAngle, normal) * rotation;
        quad.transform.localRotation = rotation;

        // Apply the shared wall material instance
        MeshRenderer quadRenderer = quad.GetComponent<MeshRenderer>();
        if (wallMaterialInstance != null)
        {
            quadRenderer.sharedMaterial = wallMaterialInstance;
        }
        else
        {
            // Fallback: Default gray material
            if (fallbackWallMaterial == null)
            {
                fallbackWallMaterial = new Material(Shader.Find("Standard"))
                {
                    color = new Color(0.5f, 0.5f, 0.5f, 1f)
                };
            }
            quadRenderer.sharedMaterial = fallbackWallMaterial;
        }

        // Make visible based on visibility settings
        quadRenderer.enabled = _Visible && _WallsVisible;
        Debug.Log($"Created wall quad with renderer.enabled={quadRenderer.enabled} (_Visible={_Visible}, _WallsVisible={_WallsVisible})");

        if (wallMaterialInstance != null)
        {
            ApplyWallMaterialProperties();
        }

        return quad;
    }

    private void ApplyWallMaterialProperties()
    {
        if (wallMaterialInstance == null || !wallMaterialDirty)
        {
            return;
        }

        Debug.Log($"Applying wall material properties: Color1={_wallColor1}, Color2={_wallColor2}, NoiseScale={_noiseScale}");
        wallMaterialInstance.SetColor(_Color1ID, _wallColor1);
        wallMaterialInstance.SetColor(_Color2ID, _wallColor2);
        wallMaterialInstance.SetFloat(_NoiseScaleID, _noiseScale);
        wallMaterialDirty = false;
        wallMaterialAppliedOnce = true;
        _appliedWallColor1 = _wallColor1;
        _appliedWallColor2 = _wallColor2;
        _appliedNoiseScale = _noiseScale;
        UpdateWallPropertyBlocks();
    }

    private void DetectExternalWallMaterialChanges()
    {
        if (!wallMaterialAppliedOnce)
        {
            wallMaterialDirty = true;
            return;
        }

        if (_wallColor1 != _appliedWallColor1 ||
            _wallColor2 != _appliedWallColor2 ||
            !Mathf.Approximately(_noiseScale, _appliedNoiseScale))
        {
            wallMaterialDirty = true;
        }
    }

    private void UpdateWallPropertyBlocks()
    {
        if (wallQuads.Count == 0)
        {
            return;
        }

        if (wallPropertyBlock == null)
        {
            wallPropertyBlock = new MaterialPropertyBlock();
        }

        wallPropertyBlock.SetColor(_Color1ID, _wallColor1);
        wallPropertyBlock.SetColor(_Color2ID, _wallColor2);
        wallPropertyBlock.SetFloat(_NoiseScaleID, _noiseScale);

        foreach (var wall in wallQuads)
        {
            if (wall == null)
            {
                continue;
            }
            var renderer = wall.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                continue;
            }
            renderer.SetPropertyBlock(wallPropertyBlock);
        }
    }

    private void ConfigureWallContainerTransform()
    {
        if (wallContainer == null)
        {
            return;
        }

        wallContainer.transform.localPosition = Vector3.zero;
        wallContainer.transform.localRotation = Quaternion.identity;

        Vector3 scale = transform.localScale;

        float invX = Mathf.Approximately(scale.x, 0f) ? 1f : 1f / scale.x;
        float invY = Mathf.Approximately(scale.y, 0f) ? 1f : 1f / scale.y;
        float invZ = Mathf.Approximately(scale.z, 0f) ? 1f : 1f / scale.z;

        wallContainer.transform.localScale = new Vector3(invX, invY, invZ);
    }

    private static bool ApproximatelyEqual(Vector3 a, Vector3 b)
    {
        return Vector3.SqrMagnitude(a - b) < 1e-6f;
    }

    /// <summary>
    /// Updates visibility of all wall quads based on room and wall visibility settings.
    /// </summary>
    private void UpdateWallVisibility()
    {
        bool shouldBeVisible = _Visible && _WallsVisible;
        Debug.Log($"UpdateWallVisibility called: _Visible={_Visible}, _WallsVisible={_WallsVisible}, shouldBeVisible={shouldBeVisible}, wallQuads.Count={wallQuads.Count}");

        foreach (var wall in wallQuads)
        {
            if (wall != null)
            {
                var wallRenderer = wall.GetComponent<MeshRenderer>();
                if (wallRenderer != null)
                {
                    wallRenderer.enabled = shouldBeVisible;
                    Debug.Log($"Set wall renderer enabled to {shouldBeVisible}");
                }
            }
        }
    }

    /// <summary>
    /// Destroys all existing wall quads.
    /// </summary>
    private void DestroyWalls()
    {
        foreach (var wall in wallQuads)
        {
            if (wall != null)
                Destroy(wall);
        }
        wallQuads.Clear();
    }

}
