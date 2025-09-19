using System.Collections;
using UnityEngine;

public class Eye : MonoBehaviour
{
    [SerializeField] GameObject targetObject;
    [SerializeField] float toggleDelay;
    [SerializeField] float cooldown;
    void Start()
    {
        targetObject.SetActive(false);
        StartCoroutine(ToggleVisibilityRoutine());
    }
    IEnumerator ToggleVisibilityRoutine()
    {
        while (true)
        {
            targetObject.SetActive(false);
            yield return new WaitForSeconds(toggleDelay);
            targetObject.SetActive(true);
            yield return new WaitForSeconds(toggleDelay);
            targetObject.SetActive(false);
            yield return new WaitForSeconds(toggleDelay);
            targetObject.SetActive(true);
            yield return new WaitForSeconds(toggleDelay);
            targetObject.SetActive(false);
            yield return new WaitForSeconds(cooldown);
        }
    }
}
