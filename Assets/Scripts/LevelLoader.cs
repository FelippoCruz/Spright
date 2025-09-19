using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

    [SerializeField] Animator transition;
    [SerializeField] float transitionTime = 1.0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (transition != null)
            transition.updateMode = AnimatorUpdateMode.UnscaledTime;

        // Register to sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Clean up listener in case object is ever destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (transition != null)
        {
            Debug.Log($"[LevelLoader] Scene {scene.name} loaded. Rebinding animator...");
            transition.Rebind();
            transition.Update(0f);
        }
    }

    public void LoadNextLevel(string scene)
    {
        StartCoroutine(LoadLevel(scene));
    }

    IEnumerator LoadLevel(string scene)
    {
        if (transition != null)
        {
            transition.SetTrigger("Start");
        }
        else
        {
            Debug.LogWarning("LevelLoader: No transition Animator assigned!");
        }

        yield return new WaitForSecondsRealtime(transitionTime);
        SceneManager.LoadScene(scene);
    }
}
