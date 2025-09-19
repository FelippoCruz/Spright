using UnityEngine;
using System.Collections;

public class CharacterSelectedTrigger : MonoBehaviour
{
    [SerializeField] private GameObject WorldCanvas;
    [SerializeField] private Transform movingParent;
    [SerializeField] private Transform targetPosition;
    [SerializeField] private float moveDuration = 2f;

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            int chosenIndex = (other.transform.position.x <= -0.7f) ? 0 :
                              (other.transform.position.x >= 0.7f) ? 1 : -1;

            if (chosenIndex != -1)
            {
                CharacterChosen(chosenIndex, other.GetComponent<CharacterController>());
            }
        }
    }

    void CharacterChosen(int v, CharacterController controller)
    {
        PlayerPrefs.SetInt("CharacterChosen", v);
        PlayerPrefs.Save();
        StartCoroutine(DestructionProcess(controller));
    }

    IEnumerator DestructionProcess(CharacterController controller)
    {
        Destroy(WorldCanvas);

        if (controller != null)
            controller.enabled = false;

        // Animate parent moving object
        if (movingParent != null && targetPosition != null)
        {
            Vector3 startPos = movingParent.position;
            Vector3 endPos = targetPosition.position;
            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                movingParent.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            movingParent.position = endPos; // Snap to exact position
        }

        if (controller != null)
            controller.enabled = true;

        Destroy(this);
    }
}
