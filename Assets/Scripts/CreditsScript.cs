using UnityEngine;
using System.Collections;

public class CreditsRoll : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] RectTransform creditsContent;
    [SerializeField] float scrollSpeed = 110f;
    [SerializeField] float topY = 3000f;

    [Header("Logo")]
    [SerializeField] CanvasGroup companyLogoGroup;
    [SerializeField] float fadeDuration = 2f;
    [SerializeField] float logoPause = 4f;

    [Header("Scenes")]
    [SerializeField] string menuScene = "StartScene";
    [SerializeField] string fallbackScene = "StartScene";

    readonly string BEAT_KEY = "GameCompleted";
    readonly string PREV_SCENE_KEY = "PreviousScene";

    LevelLoader LevelLoader;

    private float escHoldTime = 0f;          // <-- Added

    void Start() => StartCoroutine(Roll());

    private void Update()
    {
        // Speed boost with Space
        if (Input.GetKey(KeyCode.Space))
            scrollSpeed = 250;
        else
            scrollSpeed = 110;

        // ESC HOLD LOGIC ---------------------------------------
        if (Input.GetKey(KeyCode.Escape))
        {
            escHoldTime += Time.unscaledDeltaTime;

            if (escHoldTime >= 3f)  // held for 3 seconds
            {
                LoadReturnScene();
            }
        }
        else
        {
            escHoldTime = 0f;
        }
    }

    IEnumerator Roll()
    {
        if (companyLogoGroup != null)
        {
            companyLogoGroup.alpha = 0f;
            companyLogoGroup.gameObject.SetActive(false);
        }

        while (creditsContent.anchoredPosition.y < topY)
        {
            creditsContent.anchoredPosition +=
                Vector2.up * (scrollSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        if (companyLogoGroup != null)
        {
            companyLogoGroup.gameObject.SetActive(true);

            float t = 0f;
            while (t < fadeDuration)
            {
                companyLogoGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            companyLogoGroup.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(logoPause);

        LoadReturnScene();
    }

    // ------------------ Helper Method ------------------
    private void LoadReturnScene()
    {
        bool beaten = PlayerPrefs.GetInt(BEAT_KEY, 0) == 1;
        string returnScene = beaten
            ? menuScene
            : PlayerPrefs.GetString(PREV_SCENE_KEY, fallbackScene);

        LevelLoader.Instance.LoadNextLevel(returnScene);
    }
}