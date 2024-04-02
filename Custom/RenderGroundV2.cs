using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RenderGroundV2 : MonoBehaviour
{
    [SerializeField] private GameObject groundTilePrefab;
    [SerializeField] private GameObject avatar;
    [SerializeField] private int gridShape;
    [SerializeField] private float tileScale;
    private float tileSize;
    private Material mat;
    private Vector3 playerPosition;
    private Vector3 gridCenterPosition;
    public Color currentColor;
    private static readonly int _Color = Shader.PropertyToID("_Color");
    private List<List<GameObject>> tileGrid = new();
    private MeshRenderer renderer;


    [SerializeField]
    private bool _Visible = false;
    public bool Visible
    {
        get{
            return _Visible;
        }
        
        set{
            _Visible=value;
            SetObjectsVisibility(value);
        }
    }
        

    // Start is called before the first frame update
    void Start()

    
    {
        // #if UNITY_EDITOR
        //     mat = groundTilePrefab.GetComponentInChildren<MeshRenderer>().sharedMaterial;
        //     playerPosition = avatar.transform.position;
        //     gridCenterPosition = playerPosition;
        //     gridCenterPosition.y = 0;
        //     GridShape = gridShape;
        //     Size = tileScale;

        //     SetObjectsVisibility(false);
        //     playerPosition = avatar.transform.position;
        //     gridCenterPosition = playerPosition;
        //     gridCenterPosition.y = 0;
        //     // Size = radius;
        //     mat = groundTilePrefab.GetComponentInChildren<MeshRenderer>().sharedMaterial;
        //     // Color = color;
        
        // #endif
    }

    // Update is called once per frame
    void Update()
    {
        playerPosition = avatar.transform.position; 
        if (playerPosition.z > gridCenterPosition.z + tileSize/2.0f)
        {
            ShiftGridUp();
        }
        else if (playerPosition.z < gridCenterPosition.z - tileSize/2.0f)
        {
            ShiftGridDown();
        }
        else if (playerPosition.x > gridCenterPosition.x + tileSize/2.0f)
        {
            ShiftGridRight();
        }
        else if (playerPosition.x < gridCenterPosition.x - tileSize/2.0f)
        {
            ShiftGridLeft();
        }
    }

    public void OnCreate(Vector3 position, Quaternion rotation, float height, float radius, Color color)
    {

        mat = groundTilePrefab.GetComponentInChildren<MeshRenderer>().sharedMaterial;
        avatar= GameObject.Find("Avatar");
        SetObjectsVisibility(false);
        playerPosition = avatar.transform.position;
        gridCenterPosition = playerPosition;
        gridCenterPosition.y = 0;
        Size = radius;
        mat = groundTilePrefab.GetComponentInChildren<MeshRenderer>().sharedMaterial;
        Color = color;
    }
    
    public Color Color
    {
        get => currentColor;
        set
        {
            currentColor = value;
            mat.SetColor(_Color, value);
        } 
    }

    public float Size
    {
        get => tileScale;
        set
        {
            tileScale = value;
            tileSize = value * 10.0f;
            PaveNewGround();
        }
    }

    public float GridShape
    {
        get => gridShape;
        set
        {
            gridShape = (int)(value/2) * 2 + 1;
            PaveNewGround();
        }
    }
    
    private void PaveNewGround()
    {
        foreach (var row in tileGrid)
        {
            foreach (var tile in row)
            {
                Destroy(tile);
            }
        }
        tileGrid.RemoveAll(x => true);
        
        for (int i = -gridShape/2; i < gridShape/2+1; i++)
        {
            List<GameObject> row = new List<GameObject>();
            for (int j = -gridShape/2; j < gridShape/2+1; j++)
            {
                // tile is 10x10, so we need to shift position up by increment * size
                GameObject tile = GenerateGroundTile(
                    new Vector3(j * tileSize, 0, i * tileSize));
                row.Add(tile);
            }
            tileGrid.Add(row);
        }
    }
    
    private GameObject GenerateGroundTile(Vector3 position)
    {
        GameObject tile = Instantiate(groundTilePrefab, transform, true);
        if(!Visible)
        {
            Transform firstChild = tile.transform.GetChild(0);
            MeshRenderer renderer = firstChild.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
        tile.transform.localPosition = gridCenterPosition + position;
        tile.transform.localRotation = Quaternion.identity;
        tile.transform.localScale = new Vector3(Size, 1, Size);
        
        
        return tile;
    }

    private void ShiftGridUp()
    {
        gridCenterPosition = tileGrid[gridShape/2+1][gridShape/2].transform.localPosition;
        for (int i = gridShape-1; i > -1; i--)
        {
            Destroy(tileGrid[0][i]);
            tileGrid[0].RemoveAt(i);
        }
        tileGrid.RemoveAt(0);

        List<GameObject> row = new List<GameObject>();
        for (int i = -gridShape/2; i < gridShape/2+1; i++)
        {
            GameObject tile = GenerateGroundTile(
                new Vector3(i * tileSize, 0, gridShape/2 * tileSize));
            row.Add(tile);
        }
        tileGrid.Add(row);
    }

    private void ShiftGridDown()
    {
        gridCenterPosition = tileGrid[gridShape/2-1][gridShape/2].transform.localPosition;
        for (int i = gridShape-1; i > -1; i--)
        {
            Destroy(tileGrid[gridShape-1][i]);
            tileGrid[gridShape-1].RemoveAt(i);
        }
        tileGrid.RemoveAt(gridShape-1);

        List<GameObject> row = new List<GameObject>();
        for (int i = -gridShape/2; i < gridShape/2+1; i++)
        {
            GameObject tile = GenerateGroundTile(
                new Vector3(i * tileSize, 0, gridShape/2 * -tileSize));
            row.Add(tile);
        }
        tileGrid.Insert(0, row); 
    }

    private void ShiftGridRight()
    {
        gridCenterPosition = tileGrid[gridShape/2][gridShape/2+1].transform.localPosition;
        for (int i = gridShape-1; i > -1; i--)
        {
            Destroy(tileGrid[i][0]);
            tileGrid[i].RemoveAt(0);
        }

        for (int i = -gridShape/2; i < gridShape/2+1; i++)
        {
            GameObject tile = GenerateGroundTile(
                new Vector3(gridShape/2 * tileSize, 0, i * tileSize));
            tileGrid[i+gridShape/2].Add(tile);
        }
    }   

    private void ShiftGridLeft()
    {
        gridCenterPosition = tileGrid[gridShape/2][gridShape/2-1].transform.localPosition;
        for (int i = gridShape-1; i > -1; i--)
        {
            Destroy(tileGrid[i][gridShape-1]);
            tileGrid[i].RemoveAt(gridShape-1);
        }

        for (int i = -gridShape/2; i < gridShape/2+1; i++)
        {
            GameObject tile = GenerateGroundTile(
                new Vector3(gridShape/2 * -tileSize, 0, i * tileSize));
            tileGrid[i+gridShape/2].Insert(0, tile); 
        }
    }
    private void SetObjectsVisibility(bool visible)
    {
        foreach (List<GameObject> row in tileGrid)
        {
            foreach (GameObject obj in row)
            {
                Transform firstChild = obj.transform.GetChild(0);
                MeshRenderer renderer = firstChild.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }
    }
}
