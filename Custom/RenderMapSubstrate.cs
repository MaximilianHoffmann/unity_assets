using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderMapSubstrate : MonoBehaviour
{
    public Texture2D grayscaleImage;
    private Color[] pixelColors;
    private Transform avatarTransform;
    private GameObject avatar;
    private Vector3 lastNonZeroPosition;

    void Start()
    {
        if (grayscaleImage == null)
        {
            Debug.LogError("Grayscale image not assigned.");
            return;
        }

        if (!grayscaleImage.isReadable)
        {
            Debug.LogError("Grayscale image is not readable.");
            return;
        }

        // Get the pixel colors from the texture
        pixelColors = grayscaleImage.GetPixels();

        avatarTransform = GameObject.Find("Avatar").transform;
        avatar = GameObject.Find("Avatar");

        // Set the initial last non-zero position to the starting position of the avatar
        lastNonZeroPosition = avatarTransform.position;

        // Apply the pixel colors to the plane
        ApplyTextureToFloor();
    }

    void Update()
    {
        Vector3 localPosition = transform.InverseTransformPoint(avatarTransform.position);
        float fieldValue = GetFieldValue(localPosition);

        Debug.Log("Update - Field Value: " + fieldValue);

        if (fieldValue == 0)
        {
            Debug.Log("Field value is zero, moving to last non-zero position or closest non-zero field.");
            Vector3 targetPosition = FindClosestNonZeroField(localPosition);
            avatarTransform.position = targetPosition;
        }
        else
        {
            lastNonZeroPosition = avatarTransform.position;
        }
    }

    void ApplyTextureToFloor()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Material material = renderer.material;

        // Create a new texture to apply to the floor
        Texture2D texture = new Texture2D(grayscaleImage.width, grayscaleImage.height);
        texture.SetPixels(pixelColors);
        texture.Apply();

        // Set the texture to the material
        material.mainTexture = texture;
    }

    public float GetFieldValue(Vector3 position)
    {
        // Convert local position to texture coordinates
        int x = grayscaleImage.width - Mathf.FloorToInt((position.z / 8 + 0.5f) * grayscaleImage.width);
        int y = grayscaleImage.height - Mathf.FloorToInt((position.x / 8 + 0.5f) * grayscaleImage.height);

        // Ensure the coordinates are within bounds
        x = Mathf.Clamp(x, 0, grayscaleImage.width - 1);
        y = Mathf.Clamp(y, 0, grayscaleImage.height - 1);

        // Get the pixel color at the specified coordinates
        Color pixelColor = grayscaleImage.GetPixel(y, x);

        // Convert the color to a grayscale value
        float fieldValue = pixelColor.grayscale * 255;

        return fieldValue;
    }

    private Vector3 FindClosestNonZeroField(Vector3 localPosition)
    {
        int searchRadius = 1;
        while (true)
        {
            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius; dy++)
                {
                    int x = Mathf.FloorToInt((localPosition.z / 8 + 0.5f) * grayscaleImage.width) + dx;
                    int y = Mathf.FloorToInt((localPosition.x / 8 + 0.5f) * grayscaleImage.height) + dy;

                    // Ensure the coordinates are within bounds
                    x = Mathf.Clamp(x, 0, grayscaleImage.width - 1);
                    y = Mathf.Clamp(y, 0, grayscaleImage.height - 1);

                    // Get the pixel color at the specified coordinates
                    Color pixelColor = grayscaleImage.GetPixel(y, x);
                    float fieldValue = pixelColor.grayscale * 255;

                    if (fieldValue != 0)
                    {
                        float worldX = (y / (float)grayscaleImage.height - 0.5f) * 8;
                        float worldZ = (x / (float)grayscaleImage.width - 0.5f) * 8;
                        return transform.TransformPoint(new Vector3(worldX, 0, worldZ));
                    }
                }
            }
            searchRadius++;
        }
    }
}
