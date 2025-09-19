using Unity.VisualScripting;
using UnityEngine;

public class EScript : MonoBehaviour
{
    [SerializeField] Camera Cam3D;

    private void Start()
    {
        if (!Cam3D)
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in cameras)
            {
                if (cam.CompareTag("MainCamera") && cam.gameObject.layer == LayerMask.NameToLayer("P1"))
                {
                    Cam3D = cam;
                    break;
                }
            }
        }

    }
    void Update()
    {
        transform.forward = Cam3D.transform.forward;
    }
}
