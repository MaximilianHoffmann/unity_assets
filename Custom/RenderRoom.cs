using Unity.VisualScripting;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.IO;
using System;
using System.Data;
using System.Linq;


public class RenderRoom : MonoBehaviour
{
    private const float PlaneHalfSize = 5f; // Unity plane spans [-5, 5] in local space

    private Transform avatar;
    private Material floorMaterialInstance;
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
    private static readonly int _UseBandNoiseID = Shader.PropertyToID("_UseBandNoise");
    private static readonly int _SeedID = Shader.PropertyToID("_Seed");
    private static readonly int _BandFrequenciesID = Shader.PropertyToID("_BandFrequencies");
    private static readonly int _BandAmplitudesID = Shader.PropertyToID("_BandAmplitudes");
    private static Material fallbackWallMaterial;
    private bool wallMaterialDirty = true;
    private bool wallMaterialAppliedOnce = false;
    private Color _appliedWallColor1;
    private Color _appliedWallColor2;
    private float _appliedNoiseScale;
    private bool _appliedUseBandNoise;
    private Vector3 _appliedBandFrequencies;
    private Vector3 _appliedBandAmplitudes;
    private MaterialPropertyBlock wallPropertyBlock;
    private bool wallMaterialSupportsProperties = false;
    private bool wallNeedsPostTextureApply = false;
    private bool _applyWallProperties = false;

    // Cached properties for atomic update
    private Color _cachedWallColor1 = new Color(0.2f, 0.2f, 0.2f, 1.0f);
    private Color _cachedWallColor2 = new Color(0.8f, 0.8f, 0.8f, 1.0f);
    private float _cachedNoiseScale = 1.0f;
    private bool _cachedUseBandNoise = false;
    private Vector3 _cachedBandFrequencies = new Vector3(0.05f, 0.12f, 0.30f);
    private Vector3 _cachedBandAmplitudes = new Vector3(0.6f, 0.8f, 0.5f);
    private string _cachedTexture = null;
    private string _cachedCollider = null;
    private string _cachedWalls = null;
    private bool _hasWallPropertyCache = false;

    public bool ApplyWallProperties
    {
        get { return _applyWallProperties; }
        set
        {
            _applyWallProperties = false;
            if (value)
            {
                StartCoroutine(ApplyCachedWallPropertiesCoroutine());
            }
        }
    }

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
            Debug.Log($"WallColor1 setter called: {value} (caching only, not applying immediately)");
            _cachedWallColor1 = value;
            _hasWallPropertyCache = true;
            // Do NOT set wallMaterialDirty here - only cache the value
            // Properties will be applied atomically via ApplyWallProperties trigger
        }
    }

    private Color _wallColor2 = new Color(0.8f, 0.8f, 0.8f, 1.0f);
    public Color WallColor2
    {
        get { return _wallColor2; }
        set
        {
            Debug.Log($"WallColor2 setter called: {value} (caching only, not applying immediately)");
            _cachedWallColor2 = value;
            _hasWallPropertyCache = true;
            // Do NOT set wallMaterialDirty here - only cache the value
            // Properties will be applied atomically via ApplyWallProperties trigger
        }
    }

    private float _noiseScale = 1.0f;
    public float NoiseScale
    {
        get { return _noiseScale; }
        set
        {
            Debug.Log($"NoiseScale setter called: {value} (caching only, not applying immediately)");
            _cachedNoiseScale = value;
            _hasWallPropertyCache = true;
            // Do NOT set wallMaterialDirty here - only cache the value
            // Properties will be applied atomically via ApplyWallProperties trigger
        }
    }

    [SerializeField]
    private bool _useBandNoise = false;
    public bool UseBandNoise
    {
        get { return _useBandNoise; }
        set
        {
            Debug.Log($"UseBandNoise setter called: {value} (caching only, not applying immediately)");
            _cachedUseBandNoise = value;
            _hasWallPropertyCache = true;
            // Do NOT set wallMaterialDirty here - only cache the value
            // Properties will be applied atomically via ApplyWallProperties trigger
        }
    }

    private Vector3 _bandFrequencies = new Vector3(0.05f, 0.12f, 0.30f);
    public Vector3 BandFrequencies
    {
        get { return _bandFrequencies; }
        set
        {
            Debug.Log($"BandFrequencies setter called: {value} (caching only, not applying immediately)");
            _cachedBandFrequencies = value;
            _hasWallPropertyCache = true;
            // Do NOT set wallMaterialDirty here - only cache the value
            // Properties will be applied atomically via ApplyWallProperties trigger
        }
    }

    private Vector3 _bandAmplitudes = new Vector3(0.6f, 0.8f, 0.5f);
    public Vector3 BandAmplitudes
    {
        get { return _bandAmplitudes; }
        set
        {
            Debug.Log($"BandAmplitudes setter called: {value} (caching only, not applying immediately)");
            _cachedBandAmplitudes = value;
            _hasWallPropertyCache = true;
            // Do NOT set wallMaterialDirty here - only cache the value
            // Properties will be applied atomically via ApplyWallProperties trigger
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
        set
        {
            _cachedTexture = value;
            _hasWallPropertyCache = true;
        }
    }

    private IEnumerator ApplyCurrentTextureCoroutine(string value)
    {
        // Detect file path vs JSON array
        string trimmed = value.TrimStart();
        if (!trimmed.StartsWith("["))
        {
            // File path — read bytes on background thread, decompress on main thread
            if (!System.IO.File.Exists(value))
            {
                Debug.LogError($"RenderRoom.CurrentTexture file not found: {value}");
                yield break;
            }

            byte[] fileData = null;
            bool ioComplete = false;
            bool ioError = false;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { fileData = File.ReadAllBytes(value); }
                catch (Exception ex)
                {
                    Debug.LogError($"RenderRoom background file read failed: {ex.Message}");
                    ioError = true;
                }
                ioComplete = true;
            });

            // Wait for background I/O without blocking the render thread
            while (!ioComplete) yield return null;

            if (ioError || fileData == null) yield break;

            // Decompress and upload on main thread (Unity API requirement)
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            ImageConversion.LoadImage(texture, fileData);

            if (_currentTexture != null)
            {
                if (Application.isPlaying) Destroy(_currentTexture);
                else DestroyImmediate(_currentTexture);
            }
            _currentTexture = texture;

            if (!EnsureFloorMaterialInstance())
            {
                Debug.LogWarning("RenderRoom.CurrentTexture could not access floor material");
                yield break;
            }
            floorMaterialInstance.mainTexture = _currentTexture;
            Debug.Log($"ApplyCurrentTexture SUCCESS: Loaded texture from file {value} ({texture.width}x{texture.height})");
            yield break;
        }

        // JSON array path (backward compatible, synchronous)
        try
        {
            var jsonArray = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(value);
            if (jsonArray == null || jsonArray.Count == 0)
            {
                Debug.LogWarning("RenderRoom.CurrentTexture received empty payload");
                yield break;
            }

            int height = jsonArray.Count;
            int width = (jsonArray[0] as Newtonsoft.Json.Linq.JArray)?.Count ?? 0;
            if (width == 0)
            {
                Debug.LogWarning("RenderRoom.CurrentTexture received rows with zero length");
                yield break;
            }

            if (_currentTexture == null || _currentTexture.width != width || _currentTexture.height != height)
            {
                if (_currentTexture != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(_currentTexture);
                    }
                    else
                    {
                        DestroyImmediate(_currentTexture);
                    }
                }

                _currentTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            var pixels = _currentTexture.GetPixels();
            if (pixels.Length != width * height)
            {
                pixels = new Color[width * height];
            }

            int index = 0;
            for (int y = 0; y < height; y++)
            {
                var row = jsonArray[y] as Newtonsoft.Json.Linq.JArray;
                if (row == null || row.Count != width)
                {
                    Debug.LogWarning($"RenderRoom.CurrentTexture row {y} has unexpected length");
                    yield break;
                }

                for (int x = 0; x < width; x++, index++)
                {
                    var rgbToken = row[x];
                    var rgb = rgbToken?.ToObject<float[]>();
                    pixels[index] = (rgb != null && rgb.Length >= 3)
                        ? new Color(rgb[0], rgb[1], rgb[2], 1f)
                        : Color.black;
                }
            }

            _currentTexture.SetPixels(pixels);
            _currentTexture.Apply(false);

            if (!EnsureFloorMaterialInstance())
            {
                Debug.LogWarning("RenderRoom.CurrentTexture could not access floor material");
                yield break;
            }

            floorMaterialInstance.mainTexture = _currentTexture;
            Debug.Log($"ApplyCurrentTexture SUCCESS: Set floor texture to {width}x{height} texture");
        }
        catch (Exception ex)
        {
            Debug.LogError($"RenderRoom.ApplyCurrentTexture failed: {ex}");
        }
    }
        
    private Texture2D _currentTexture;
    public string CurrentCollider
    {
        get { return "collider"; }
        set
        {
            _cachedCollider = value;
            _hasWallPropertyCache = true;
        }
    }

    private IEnumerator ApplyCurrentColliderCoroutine(string value)
    {
        // Parse JSON on background thread
        int[,] parsed = null;
        bool done = false;
        string error = null;

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                var jsonArray = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(value);
                int height = jsonArray.Count;
                int width = jsonArray[0].Count();
                var result = new int[height, width];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        result[y, x] = jsonArray[y][x].ToObject<int>();
                parsed = result;
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            done = true;
        });

        while (!done) yield return null;

        if (error != null)
        {
            Debug.LogError($"ApplyCurrentCollider background parse failed: {error}");
            yield break;
        }

        _currentCollider = parsed;
        wasLastPositionValid = false;
    }

    private int[,] _currentCollider;

    private bool EnsureFloorMaterialInstance()
    {
        if (renderer == null)
        {
            renderer = GetComponent<MeshRenderer>();
        }

        if (renderer == null)
        {
            return false;
        }

        if (floorMaterialInstance == null)
        {
            var shared = renderer.sharedMaterial;
            if (shared != null)
            {
                floorMaterialInstance = new Material(shared);
            }
            else
            {
                floorMaterialInstance = new Material(Shader.Find("Standard"));
            }
        }

        var sharedMaterials = renderer.sharedMaterials;
        if (sharedMaterials == null || sharedMaterials.Length == 0)
        {
            renderer.sharedMaterial = floorMaterialInstance;
        }
        else
        {
            if (!ReferenceEquals(sharedMaterials[0], floorMaterialInstance))
            {
                sharedMaterials[0] = floorMaterialInstance;
                renderer.sharedMaterials = sharedMaterials;
            }
        }

        renderer.enabled = _Visible;

        return floorMaterialInstance != null;
    }

    private bool EnsureWallMaterialInstance()
    {
        if (wallMaterialInstance == null)
        {
            Material sourceMaterial = wallMaterial;
            if (sourceMaterial != null && sourceMaterial.shader != null && sourceMaterial.shader.name == "Custom/RoomWall")
            {
                wallMaterialInstance = new Material(sourceMaterial);
            }
            else
            {
                if (sourceMaterial != null && sourceMaterial.shader != null)
                {
                    Debug.LogWarning($"RenderRoom: Assigned wall material uses shader '{sourceMaterial.shader.name}'. Expected 'Custom/RoomWall'. Falling back to shader lookup.");
                }

                Shader roomWallShader = Shader.Find("Custom/RoomWall");
                if (roomWallShader != null)
                {
                    wallMaterialInstance = new Material(roomWallShader);
                    Debug.Log("RenderRoom: Created wall material from Custom/RoomWall shader");
                }
                else
                {
                    Debug.LogWarning("RenderRoom: Custom/RoomWall shader not found. Wall properties will be disabled.");
                }
            }

            if (wallMaterialInstance != null)
            {
                wallMaterialSupportsProperties =
                    wallMaterialInstance.HasProperty(_Color1ID) &&
                    wallMaterialInstance.HasProperty(_Color2ID) &&
                    wallMaterialInstance.HasProperty(_NoiseScaleID) &&
                    wallMaterialInstance.HasProperty(_UseBandNoiseID) &&
                    wallMaterialInstance.HasProperty(_BandFrequenciesID) &&
                    wallMaterialInstance.HasProperty(_BandAmplitudesID);

                if (!wallMaterialSupportsProperties)
                {
                    Debug.LogWarning("RenderRoom: Wall material is missing expected shader properties; disabling wall property updates.");
                }
                else if (wallMaterialInstance.HasProperty(_SeedID))
                {
                    wallMaterialInstance.SetFloat(_SeedID, UnityEngine.Random.value * 1000f);
                    wallMaterialDirty = true;
                }
            }
            else
            {
                wallMaterialSupportsProperties = false;
            }
        }

        return wallMaterialInstance != null;
    }

    public string Walls
    {
        get { return "walls"; }
        set
        {
            _cachedWalls = value;
            _hasWallPropertyCache = true;
        }
    }

    private IEnumerator ApplyWallsCoroutine(string value)
    {
        // Parse JSON on background thread
        List<WallSegment> segments = null;
        bool parseDone = false;
        string parseError = null;

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                var jsonArray = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(value);
                var list = new List<WallSegment>();
                foreach (var segmentData in jsonArray)
                {
                    list.Add(new WallSegment
                    {
                        x1 = segmentData["x1"].ToObject<float>(),
                        z1 = segmentData["z1"].ToObject<float>(),
                        x2 = segmentData["x2"].ToObject<float>(),
                        z2 = segmentData["z2"].ToObject<float>(),
                        height = segmentData["height"].ToObject<float>(),
                        nx = segmentData["nx"] != null ? segmentData["nx"].ToObject<float>() : 0f,
                        nz = segmentData["nz"] != null ? segmentData["nz"].ToObject<float>() : 0f
                    });
                }
                segments = list;
            }
            catch (Exception ex)
            {
                parseError = ex.Message;
            }
            parseDone = true;
        });

        while (!parseDone) yield return null;

        if (parseError != null)
        {
            Debug.LogError($"ApplyWalls background parse failed: {parseError}");
            yield break;
        }

        _currentWallSegments.Clear();
        _currentWallSegments.AddRange(segments);

        // Destroy old walls and yield a frame for cleanup
        DestroyWalls();
        yield return null;

        if (_currentWallSegments.Count == 0)
        {
            Debug.Log("No wall segments to render");
            yield break;
        }

        // Create wall container if needed
        if (wallContainer == null)
        {
            wallContainer = new GameObject("Walls");
            wallContainer.transform.SetParent(transform, false);
        }

        ConfigureWallContainerTransform();

        bool haveWallMaterial = EnsureWallMaterialInstance();
        if (haveWallMaterial && wallMaterialSupportsProperties)
        {
            wallMaterialDirty = true;
            ApplyWallMaterialProperties();
        }

        // Create quads in batches to stay under VR frame budget
        const int BATCH_SIZE = 20;
        for (int i = 0; i < _currentWallSegments.Count; i++)
        {
            GameObject wallQuad = CreateWallQuad(_currentWallSegments[i]);
            wallQuad.transform.SetParent(wallContainer.transform, false);
            wallQuads.Add(wallQuad);

            if ((i + 1) % BATCH_SIZE == 0)
                yield return null;
        }

        Debug.Log($"Generated {wallQuads.Count} wall quads from {_currentWallSegments.Count} segments");

        UpdateWallPropertyBlocks();
        UpdateWallVisibility();
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

      
            // Also update floor renderer
        if (renderer != null)
        {
            renderer.enabled = _Visible;
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
        Vector2 textureCoord = new Vector2(
            ((-localPos.x / PlaneHalfSize) + 1.0f) / 2.0f,
            ((-localPos.z / PlaneHalfSize) + 1.0f) / 2.0f
        );
        
       
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

        // if (_currentTexture != null)
        // {
        //     Color pixelColor = SampleTexture(textureCoord);
        //     bool colliderColor = SampleCollider(textureCoord);
        //     // Debug.Log($"Local X: {localPos.x}, Local Z: {localPos.z}, X: {avatar.position.x}, Z: {avatar.position.z}");
        //     // Debug.Log($"Avatar at texture coordinate ({textureCoord.x:F2}, {textureCoord.y:F2}), Color: {pixelColor}");
        //     // Debug.Log($"Avatar at collider coordinate ({textureCoord.x:F2}, {textureCoord.y:F2}), Color: {colliderColor}");
        //     // Debug.Log($"Local Scale: {transform.localScale}");
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
        if (renderer != null)
        {
            renderer.enabled = false;
        }
        avatar = GameObject.Find("Avatar")?.transform;


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

        // Find the texture data in kvlist and cache via property setters
        var textureData = kvlist.FirstOrDefault(kv => kv.Key == "texture");
        if (textureData.Value is Newtonsoft.Json.Linq.JArray textureArray)
        {
            CurrentTexture = textureArray.ToString(Newtonsoft.Json.Formatting.None);
        }

        var colliderData = kvlist.FirstOrDefault(kv => kv.Key == "collider");
        if (colliderData.Value is Newtonsoft.Json.Linq.JArray colliderArray)
        {
            CurrentCollider = colliderArray.ToString(Newtonsoft.Json.Formatting.None);
        }

        // Check for wall segment data
        var wallsData = kvlist.FirstOrDefault(kv => kv.Key == "walls");
        if (wallsData.Value != null)
        {
            var jsonArray = wallsData.Value as Newtonsoft.Json.Linq.JArray;
            if (jsonArray != null)
            {
                Walls = jsonArray.ToString(Newtonsoft.Json.Formatting.None);
            }
        }

        // Atomically apply all cached properties
        // ApplyWallProperties = true;

        EnsureFloorMaterialInstance();
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

        bool haveWallMaterial = EnsureWallMaterialInstance();
        if (haveWallMaterial && wallMaterialSupportsProperties)
        {
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

        // Ensure all wall renderers have the latest material properties applied
        UpdateWallPropertyBlocks();

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
        if (EnsureWallMaterialInstance() && wallMaterialInstance != null)
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

        return quad;
    }

    private IEnumerator ApplyCachedWallPropertiesCoroutine()
    {
        if (!_hasWallPropertyCache) yield break;

        // Step 1: Apply collider (JSON parsed on background thread)
        if (!string.IsNullOrEmpty(_cachedCollider))
            yield return StartCoroutine(ApplyCurrentColliderCoroutine(_cachedCollider));

        yield return null;

        // Step 2: Apply walls (spread across frames)
        if (!string.IsNullOrEmpty(_cachedWalls))
            yield return StartCoroutine(ApplyWallsCoroutine(_cachedWalls));

        // Step 3: Apply wall shader properties
        if (EnsureWallMaterialInstance() && wallMaterialInstance != null)
        {
            _wallColor1 = _cachedWallColor1;
            _wallColor2 = _cachedWallColor2;
            _noiseScale = _cachedNoiseScale;
            _useBandNoise = _cachedUseBandNoise;
            _bandFrequencies = _cachedBandFrequencies;
            _bandAmplitudes = _cachedBandAmplitudes;

            if (wallMaterialSupportsProperties)
            {
                wallMaterialInstance.SetColor(_Color1ID, _wallColor1);
                wallMaterialInstance.SetColor(_Color2ID, _wallColor2);
                wallMaterialInstance.SetFloat(_NoiseScaleID, _noiseScale);
                wallMaterialInstance.SetFloat(_UseBandNoiseID, _useBandNoise ? 1f : 0f);
                wallMaterialInstance.SetVector(_BandFrequenciesID, _bandFrequencies);
                wallMaterialInstance.SetVector(_BandAmplitudesID, _bandAmplitudes);

                wallMaterialAppliedOnce = true;
                _appliedWallColor1 = _wallColor1;
                _appliedWallColor2 = _wallColor2;
                _appliedNoiseScale = _noiseScale;
                _appliedUseBandNoise = _useBandNoise;
                _appliedBandFrequencies = _bandFrequencies;
                _appliedBandAmplitudes = _bandAmplitudes;

                UpdateWallPropertyBlocks();
            }
        }

        yield return null;

        // Step 4: Apply texture
        if (!string.IsNullOrEmpty(_cachedTexture))
            yield return StartCoroutine(ApplyCurrentTextureCoroutine(_cachedTexture));

        wallMaterialDirty = false;
        wallNeedsPostTextureApply = false;
    }

    private void ApplyWallMaterialProperties()
    {
        if (!wallMaterialDirty)
        {
            return;
        }

        if (!EnsureWallMaterialInstance() || wallMaterialInstance == null || !wallMaterialSupportsProperties)
        {
            wallMaterialDirty = false;
            return;
        }

        wallMaterialInstance.SetColor(_Color1ID, _wallColor1);
        wallMaterialInstance.SetColor(_Color2ID, _wallColor2);
        wallMaterialInstance.SetFloat(_NoiseScaleID, _noiseScale);
        wallMaterialInstance.SetFloat(_UseBandNoiseID, _useBandNoise ? 1f : 0f);
        wallMaterialInstance.SetVector(_BandFrequenciesID, _bandFrequencies);
        wallMaterialInstance.SetVector(_BandAmplitudesID, _bandAmplitudes);
        var matColor = wallMaterialInstance.GetColor(_Color1ID);
        Debug.Log($"Wall material now has Color1 = {matColor}");
        wallMaterialDirty = false;
        wallMaterialAppliedOnce = true;
        _appliedWallColor1 = _wallColor1;
        _appliedWallColor2 = _wallColor2;
        _appliedNoiseScale = _noiseScale;
        _appliedUseBandNoise = _useBandNoise;
        _appliedBandFrequencies = _bandFrequencies;
        _appliedBandAmplitudes = _bandAmplitudes;
        UpdateWallPropertyBlocks();

        if (_currentTexture != null && EnsureFloorMaterialInstance())
        {
            floorMaterialInstance.mainTexture = _currentTexture;
        }
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
            !Mathf.Approximately(_noiseScale, _appliedNoiseScale) ||
            _useBandNoise != _appliedUseBandNoise ||
            _bandFrequencies != _appliedBandFrequencies ||
            _bandAmplitudes != _appliedBandAmplitudes)
        {
            wallMaterialDirty = true;
        }
    }

    private void UpdateWallPropertyBlocks()
    {
        if (!wallMaterialSupportsProperties || wallQuads.Count == 0 || wallMaterialInstance == null)
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
        wallPropertyBlock.SetFloat(_UseBandNoiseID, _useBandNoise ? 1f : 0f);
        wallPropertyBlock.SetVector(_BandFrequenciesID, _bandFrequencies);
        wallPropertyBlock.SetVector(_BandAmplitudesID, _bandAmplitudes);

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
