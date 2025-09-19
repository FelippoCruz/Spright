using UnityEngine;

public class Boss1AreaTrigger : MonoBehaviour
{
    LevelLoader LevelLoader;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LevelLoader.Instance.LoadNextLevel("CreditsScene");
        }
    }
}
