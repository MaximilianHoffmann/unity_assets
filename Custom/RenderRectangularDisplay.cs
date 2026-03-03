using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class RenderRectangularDisplay : MonoBehaviour
{
    private Material mat;
    private MeshRenderer renderer;
    private Texture2D _currentTexture;

    private static readonly int _Color1ID = Shader.PropertyToID("_Color1");
    private static readonly int _Color2ID = Shader.PropertyToID("_Color2");
    private static readonly int _VerticalArmThicknessID = Shader.PropertyToID("_VerticalArmThickness");
    private static readonly int _HorizontalArmThicknessID = Shader.PropertyToID("_HorizontalArmThickness");
    private static readonly int _VerticalArmLengthID = Shader.PropertyToID("_VerticalArmLength");
    private static readonly int _HorizontalArmLengthID = Shader.PropertyToID("_HorizontalArmLength");
    private static readonly int _RotationID = Shader.PropertyToID("_Rotation");
    private static readonly int _GratingDensityID = Shader.PropertyToID("_GratingDensity");
    private static readonly int _SwitchOnGratingID = Shader.PropertyToID("_SwitchOnGrating");
    private static readonly int _SwitchOnTextureID = Shader.PropertyToID("_SwitchOnTexture");

    public Color Color1
    {
        get => mat.GetColor(_Color1ID);
        set => mat.SetColor(_Color1ID, value);
    }

    public Color Color2
    {
        get => mat.GetColor(_Color2ID);
        set => mat.SetColor(_Color2ID, value);
    }

    public float VerticalArmThickness
    {
        get => mat.GetFloat(_VerticalArmThicknessID);
        set => mat.SetFloat(_VerticalArmThicknessID, value);
    }

    public float HorizontalArmThickness
    {
        get => mat.GetFloat(_HorizontalArmThicknessID);
        set => mat.SetFloat(_HorizontalArmThicknessID, value);
    }

    public float VerticalArmLength
    {
        get => mat.GetFloat(_VerticalArmLengthID);
        set => mat.SetFloat(_VerticalArmLengthID, value);
    }

    public float HorizontalArmLength
    {
        get => mat.GetFloat(_HorizontalArmLengthID);
        set => mat.SetFloat(_HorizontalArmLengthID, value);
    }

    public float PatternRotation
    {
        get => mat.GetFloat(_RotationID);
        set => mat.SetFloat(_RotationID, value);
    }

    public float GratingDensity
    {
        get => mat.GetFloat(_GratingDensityID);
        set => mat.SetFloat(_GratingDensityID, value);
    }

    public bool SwitchOnGrating
    {
        get => mat.GetFloat(_SwitchOnGratingID) > 0.5f;
        set => mat.SetFloat(_SwitchOnGratingID, value ? 1f : 0f);
    }

    public bool SwitchOnTexture
    {
        get => mat.GetFloat(_SwitchOnTextureID) > 0.5f;
        set => mat.SetFloat(_SwitchOnTextureID, value ? 1f : 0f);
    }

    public string CurrentTexture
    {
        get { return "texture"; }
        set
        {
            string trimmed = value.TrimStart();
            if (!trimmed.StartsWith("["))
            {
                // File path — load image from disk
                if (!System.IO.File.Exists(value))
                {
                    Debug.LogError($"RenderRectangularDisplay.CurrentTexture file not found: {value}");
                    return;
                }
                byte[] fileData = System.IO.File.ReadAllBytes(value);
                Texture2D texture = new Texture2D(2, 2);
                texture.filterMode = FilterMode.Point;
                ImageConversion.LoadImage(texture, fileData);
                _currentTexture = texture;
                mat.mainTexture = texture;
                Debug.Log($"RenderRectangularDisplay: Loaded texture from file {value} ({texture.width}x{texture.height})");
                return;
            }

            // JSON array path (backward compatible)
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
                        var ch = jsonArray[y][x].ToObject<float[]>();
                        float a = ch.Length > 3 ? ch[3] : 1f;
                        pixels[y * width + x] = new Color(ch[0], ch[1], ch[2], a);
                    }
                }
                Texture2D texture = new Texture2D(width, height);
                texture.filterMode = FilterMode.Point;
                texture.SetPixels(pixels);
                texture.Apply();
                _currentTexture = texture;
                mat.mainTexture = texture;
            }
        }
    }

    public void OnCreate(Vector3 position, Quaternion rotation, Vector3 scale, Color color, KeyValuePair<string, object>[] kvlist)
    {
        renderer = GetComponent<MeshRenderer>();
        renderer.enabled = true;

        if (renderer != null)
        {
            mat = renderer.material;
        }

        transform.localScale = scale;
        Color1 = color;
    }
}
