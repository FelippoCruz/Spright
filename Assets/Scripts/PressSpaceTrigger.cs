using UnityEngine;

public class PressSpaceTrigger : MonoBehaviour
{
    [SerializeField] GameObject JumpPrompt;
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            JumpPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            JumpPrompt.SetActive(false);
        }
    }
}
