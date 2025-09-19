using UnityEngine.SceneManagement;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private LevelLoader levelLoader;
    [SerializeField] private UIManager uiManager;

    [Header("Audio")]
    [Tooltip("AudioSource routed to your mixer for death/game over sounds.")]
    public AudioSource AudioSource;

    [Tooltip("Clip to play at the start of GameOverScene.")]
    [SerializeField] private AudioClip deathSound;

    private int timesTalkedToMainNPC = 0;
    private int amountOfBossesDefeated = 0;

    public int TimesTalkedToMainNPC => timesTalkedToMainNPC;
    public int AmountOfBossesDefeated => amountOfBossesDefeated;

    public SaveData LoadedSaveData { get; private set; }
    public string TargetSceneAfterLoad { get; set; }

    private string sceneName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (AudioSource == null)
        {
            Debug.LogWarning("GameManager: No AudioSource assigned! Death sounds won’t play.");
        }

        Time.timeScale = 1.0f;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
        sceneName = SceneManager.GetActiveScene().name;

        bool isMenuScene =
            sceneName == "LanguageSelectorScene" ||
            sceneName == "BrightnessSelectorScene" ||
            sceneName == "StartScene" ||
            sceneName == "LoadingScene" ||
            sceneName == "GameOverScene";

        bool uiWantsCursor =
            (uiManager != null &&
            (uiManager.Chatbox.activeSelf ||
             uiManager.PausePanel.activeSelf ||
             uiManager.OptionsPanel.activeSelf) || Time.timeScale == 0f);

        if (isMenuScene || uiWantsCursor)
        {
            SetCursorState(true);
        }
        else
        {
            SetCursorState(false);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void TriggerGameOver()
    {
        if (levelLoader != null)
        {
            levelLoader.LoadNextLevel("GameOverScene");
        }
        else
        {
            Debug.LogError("GameManager: Cannot load GameOverScene because LevelLoader is missing!");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameOverScene")
        {
            if (deathSound != null && AudioSource != null)
            {
                AudioSource.PlayOneShot(deathSound);
            }
        }
    }

    public void IncrementTimesTalkedToMainNPC() => timesTalkedToMainNPC++;
    public void IncrementBossesDefeated() => amountOfBossesDefeated++;
    public void SetTimesTalkedToMainNPC(int value) => timesTalkedToMainNPC = Mathf.Max(0, value);
    public void SetAmountOfBossesDefeated(int value) => amountOfBossesDefeated = Mathf.Max(0, value);

    public void SetLoadedSaveData(SaveData data) => LoadedSaveData = data;
    public void ClearLoadedSaveData() => LoadedSaveData = null;

    public void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
