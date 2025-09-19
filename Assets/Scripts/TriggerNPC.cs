using UnityEngine;

public class TriggerNPC : MonoBehaviour
{
    [SerializeField] GameObject UIElement;
    public bool PlayerOnRange;
    void Start()
    {
        if (UIElement != null)
        {
            UIElement.SetActive(false);
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            UIElement.SetActive(true);
            PlayerOnRange = true;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            UIElement.SetActive(false);
            PlayerOnRange = false;
        }
    }
}
