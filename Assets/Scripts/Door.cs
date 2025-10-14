using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Player Settings")]
    public float interactDistance = 3f;
    public Camera playerCamera;
    public KeyCode interactKey = KeyCode.E;
    [SerializeField] GameObject EPrompt;

    [Header("Door Settings")]
    public string DoorTag = "Door";
    public string LockedDoorTag = "Locked Door";

    private Animator doorAnim;
    private Collider physicalCollider;  // solid collider
    private Collider triggerCollider;   // trigger used for raycast detection

    private void Awake()
    {
        doorAnim = GetComponent<Animator>();

        // Find colliders among children
        foreach (var col in GetComponents<Collider>())
        {
            if (col.isTrigger)
                triggerCollider = col;
            else
                physicalCollider = col;
        }

        if (doorAnim == null)
            Debug.LogError("No Animator found on Door root.");
        if (physicalCollider == null)
            Debug.LogError("No physical collider found on door.");
        if (triggerCollider == null)
            Debug.LogError("No trigger collider found on door.");
    }

    private void Update()
    {
        bool isOpen = doorAnim.GetBool("IsOpened");
        if (Input.GetKeyDown(interactKey))
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, ~0, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.CompareTag(DoorTag) || hit.collider.CompareTag(LockedDoorTag))
                {
                    Debug.Log("Hi yo");

                    if (CompareTag(LockedDoorTag))
                    {
                        doorAnim.SetTrigger("TryOpen");
                    }
                    else if (CompareTag(DoorTag))
                    {
                        if (!isOpen)
                        {
                            // Open
                            doorAnim.SetTrigger("OpenDoor");
                            doorAnim.ResetTrigger("CloseDoor");
                            doorAnim.SetBool("IsOpened", true);

                            // Player can pass
                            physicalCollider.enabled = false;
                            Debug.Log(isOpen);
                        }
                        else
                        {
                            // Close
                            doorAnim.SetTrigger("CloseDoor");
                            doorAnim.ResetTrigger("OpenDoor");
                            doorAnim.SetBool("IsOpened", false);

                            // Player blocked again
                            physicalCollider.enabled = true;
                            Debug.Log(isOpen);
                        }
                    }
                }
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            EPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            EPrompt.SetActive(false);
        }
    }
}