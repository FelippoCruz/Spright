using UnityEngine;

public class NPCScript : MonoBehaviour
{
    Animator currentAnimator;

    void Start()
    {
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        foreach (Animator anim in animators)
        {
            if (anim.gameObject.activeInHierarchy)
            {
                currentAnimator = anim;
                anim.updateMode = AnimatorUpdateMode.UnscaledTime; // works with paused Time.timeScale
                Debug.Log("Active NPC animator found: " + anim.gameObject.name);
                break;
            }
        }

        // Subscribe to dialogue events
        DialogueScript.OnPlayerNameCalled += PlayTalkingAnimation;
        DialogueScript.OnTypeCalled += PlayTalkingAnimation;
    }

    void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks
        DialogueScript.OnPlayerNameCalled -= PlayTalkingAnimation;
        DialogueScript.OnTypeCalled -= PlayTalkingAnimation;
    }

    void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        DialogueScript.OnPlayerNameCalled -= PlayTalkingAnimation;
        DialogueScript.OnTypeCalled -= PlayTalkingAnimation;
    }

    void PlayTalkingAnimation()
    {
        if (currentAnimator != null)
        {
            currentAnimator.SetTrigger("Talking");
        }
    }
}
