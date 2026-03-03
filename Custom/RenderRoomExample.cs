using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Example script demonstrating how to use RenderRoom with wall rendering.
/// Attach this to a GameObject with RenderRoom component for testing.
/// </summary>
public class RenderRoomExample : MonoBehaviour
{
    private RenderRoom renderRoom;

    void Start()
    {
        renderRoom = GetComponent<RenderRoom>();

        // Uncomment one of these examples to test:

        // Example 1: Simple box room
        // Example1_SimpleBox();

        // Example 2: Room with internal walls
        // Example2_InternalWalls();

        // Example 3: Variable height walls (doorways)
        // Example3_VariableHeights();
    }

    /// <summary>
    /// Example 1: Create a simple box room with 4 outer walls
    /// </summary>
    void Example1_SimpleBox()
    {
        Debug.Log("Example 1: Simple Box Room");

        // Define 4 walls forming a box
        var walls = new[]
        {
            new { x1 = -5f, z1 = -5f, x2 = 5f, z2 = -5f, height = 3f },  // South wall
            new { x1 = 5f, z1 = -5f, x2 = 5f, z2 = 5f, height = 3f },    // East wall
            new { x1 = 5f, z1 = 5f, x2 = -5f, z2 = 5f, height = 3f },    // North wall
            new { x1 = -5f, z1 = 5f, x2 = -5f, z2 = -5f, height = 3f }   // West wall
        };

        // Create simple texture (checkerboard)
        float[][][] texture = CreateCheckerboardTexture(5, 5);
        renderRoom.CurrentTexture = JsonConvert.SerializeObject(texture);

        // Set collider (all walkable inside)
        int[,] collider = new int[,]
        {
            {0, 0, 0, 0, 0},
            {0, 1, 1, 1, 0},
            {0, 1, 1, 1, 0},
            {0, 1, 1, 1, 0},
            {0, 0, 0, 0, 0}
        };
        renderRoom.CurrentCollider = JsonConvert.SerializeObject(collider);

        // Set walls
        renderRoom.Walls = JsonConvert.SerializeObject(walls);

        // Configure appearance
        renderRoom.Visible = true;
        renderRoom.WallsVisible = true;
        renderRoom.WallColor = new Color(0.8f, 0.8f, 0.8f);

        Debug.Log("Created box room with 4 walls");
    }

    /// <summary>
    /// Example 2: Room with internal dividing walls
    /// </summary>
    void Example2_InternalWalls()
    {
        Debug.Log("Example 2: Room with Internal Walls");

        // Outer walls + internal dividers
        var walls = new[]
        {
            // Outer walls
            new { x1 = -5f, z1 = -5f, x2 = 5f, z2 = -5f, height = 3f },
            new { x1 = 5f, z1 = -5f, x2 = 5f, z2 = 5f, height = 3f },
            new { x1 = 5f, z1 = 5f, x2 = -5f, z2 = 5f, height = 3f },
            new { x1 = -5f, z1 = 5f, x2 = -5f, z2 = -5f, height = 3f },

            // Internal dividers
            new { x1 = -2f, z1 = -5f, x2 = -2f, z2 = 0f, height = 2.5f },  // Vertical divider (partial)
            new { x1 = 2f, z1 = 0f, x2 = 2f, z2 = 5f, height = 2.5f },     // Vertical divider (partial)
            new { x1 = -5f, z1 = 0f, x2 = 0f, z2 = 0f, height = 2.5f }     // Horizontal divider (partial)
        };

        float[][][] texture = CreateCheckerboardTexture(7, 7);
        renderRoom.CurrentTexture = JsonConvert.SerializeObject(texture);

        int[,] collider = new int[,]
        {
            {0, 0, 0, 0, 0, 0, 0},
            {0, 1, 0, 1, 1, 0, 0},
            {0, 1, 0, 1, 1, 1, 0},
            {0, 0, 0, 1, 1, 1, 0},
            {0, 1, 1, 1, 1, 1, 0},
            {0, 1, 1, 1, 1, 1, 0},
            {0, 0, 0, 0, 0, 0, 0}
        };
        renderRoom.CurrentCollider = JsonConvert.SerializeObject(collider);

        renderRoom.Walls = JsonConvert.SerializeObject(walls);
        renderRoom.Visible = true;
        renderRoom.WallsVisible = true;
        renderRoom.WallColor = new Color(0.6f, 0.7f, 0.9f);

        Debug.Log("Created room with internal walls");
    }

    /// <summary>
    /// Example 3: Variable height walls (e.g., with doorways)
    /// </summary>
    void Example3_VariableHeights()
    {
        Debug.Log("Example 3: Variable Height Walls");

        // Walls with different heights to create doorways
        var walls = new[]
        {
            // South wall with doorway (lower height in middle)
            new { x1 = -5f, z1 = -5f, x2 = -1f, z2 = -5f, height = 3f },   // Left part (full height)
            new { x1 = -1f, z1 = -5f, x2 = 1f, z2 = -5f, height = 2.2f },  // Doorway (lower height)
            new { x1 = 1f, z1 = -5f, x2 = 5f, z2 = -5f, height = 3f },     // Right part (full height)

            // Other walls (normal height)
            new { x1 = 5f, z1 = -5f, x2 = 5f, z2 = 5f, height = 3f },
            new { x1 = 5f, z1 = 5f, x2 = -5f, z2 = 5f, height = 3f },
            new { x1 = -5f, z1 = 5f, x2 = -5f, z2 = -5f, height = 3f }
        };

        float[][][] texture = CreateCheckerboardTexture(5, 5);
        renderRoom.CurrentTexture = JsonConvert.SerializeObject(texture);

        int[,] collider = new int[,]
        {
            {0, 0, 0, 0, 0},
            {0, 1, 1, 1, 0},
            {0, 1, 1, 1, 0},
            {0, 1, 1, 1, 0},
            {0, 0, 0, 0, 0}
        };
        renderRoom.CurrentCollider = JsonConvert.SerializeObject(collider);

        renderRoom.Walls = JsonConvert.SerializeObject(walls);
        renderRoom.Visible = true;
        renderRoom.WallsVisible = true;
        renderRoom.WallColor = new Color(0.9f, 0.8f, 0.6f);

        Debug.Log("Created room with doorway (variable height walls)");
    }

    /// <summary>
    /// Helper: Create a simple checkerboard texture pattern
    /// </summary>
    private float[][][] CreateCheckerboardTexture(int width, int height)
    {
        float[][][] texture = new float[height][][];

        for (int y = 0; y < height; y++)
        {
            texture[y] = new float[width][];
            for (int x = 0; x < width; x++)
            {
                // Checkerboard pattern
                bool isLight = (x + y) % 2 == 0;
                float brightness = isLight ? 0.8f : 0.3f;

                texture[y][x] = new float[] { brightness, brightness, brightness };
            }
        }

        return texture;
    }
}
