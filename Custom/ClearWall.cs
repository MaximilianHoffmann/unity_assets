using UnityEngine;

public class ClearWall: MonoBehaviour
{
    public GameObject myAvatar;
    public Vector3 resetPosition = Vector3.zero; // The position to reset to

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == myAvatar)
        {
            myAvatar.transform.position = resetPosition;
            Debug.Log("myAvatar position has been reset!");
        }
    }
}

