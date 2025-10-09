using UnityEngine;

public class FirstPersonView : MonoBehaviour
{
    [Header("Mouse Settings")]
    public float mouseSensitivity = 100f;

    [Header("References")]
    public Transform playerBody; // The player/capsule object

    private float xRotation = 0f;

    void Start()
    {
        // Lock cursor in the middle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Vertical rotation (camera only)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // prevents over-rotation
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal rotation (player body)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
