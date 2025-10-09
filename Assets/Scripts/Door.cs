using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Player Settings")]
    public float interactDistance = 3f; // How close the player needs to be
    public Camera playerCamera; // Assign your player camera in the inspector

    [Header("Door Settings")]
    public string DoorTag = "Door"; // Tag to identify the door
    public KeyCode interactKey = KeyCode.E;

    public string LockedDoorTag = "Locked Door";

    private void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactDistance))
            {
                if (hit.collider.CompareTag(DoorTag) || hit.collider.CompareTag(LockedDoorTag))
                {
                    Animator doorAnim = hit.collider.GetComponent<Animator>();
                    if (doorAnim != null && hit.collider.CompareTag(LockedDoorTag))
                    {
                        doorAnim.SetTrigger("TryOpen");
                    }
                    else if (doorAnim != null && hit.collider.CompareTag(DoorTag))
                    {
                        if (doorAnim.GetCurrentAnimatorStateInfo(0).IsName("Idle") || doorAnim.GetCurrentAnimatorStateInfo(0).IsName("Closed"))
                        {
                            doorAnim.SetTrigger("OpenDoor");
                            doorAnim.ResetTrigger("CloseDoor");
                        }
                        else if (doorAnim.GetCurrentAnimatorStateInfo(0).IsName("Open"))
                        {
                            doorAnim.SetTrigger("CloseDoor");
                            doorAnim.ResetTrigger("OpenDoor");
                        }
                    }
                }
            }
        }
    }
}