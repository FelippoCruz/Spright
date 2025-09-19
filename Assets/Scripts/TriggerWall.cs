using UnityEngine;

public class TriggerWall : MonoBehaviour
{
    [SerializeField] GameObject AttackPrompt;
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            AttackPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            AttackPrompt.SetActive(false);
        }
    }
}
