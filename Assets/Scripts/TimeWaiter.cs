using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class TimeWaiter : MonoBehaviour
{
    [Header("Timing Settings")]
    [SerializeField] float subtitleDelay = 1f;
    [SerializeField] float sceneChangeDelay = 0.5f;
    [SerializeField] float fadeDuration = 0.5f;
    [SerializeField] float audioFadeDuration = 1f;
    [SerializeField] float backgroundFadeDuration = 0.75f;

    [Header("References")]
    [SerializeField] SubtitlesManager subtitlesManager;
    [SerializeField] LocalizedAudioPlayer localizedAudioPlayer;

    [Header("UI")]
    [SerializeField] GameObject pressEnterImage;   // NEW – image shown at the end

    private AudioSource audioSource;

    private bool skipCurrentLine = false;
    private bool skipAll = false;
    private bool isFading = false;
    private bool waitingForContinue = false;        // NEW

    void Start()
    {
        if (subtitlesManager != null && subtitlesManager.backgroundImage != null)
            subtitlesManager.backgroundImage.SetActive(false);

        if (pressEnterImage != null)
            pressEnterImage.SetActive(false);

        audioSource = localizedAudioPlayer.GetComponent<AudioSource>();

        StartCoroutine(PlayIntroSequence());
    }

    IEnumerator PlayIntroSequence()
    {
        // Fade in subtitle background
        yield return StartCoroutine(FadeInBackground());

        int totalLines = 4; // adjust as needed

        for (int i = 0; i < totalLines; i++)
        {
            if (skipAll) break;

            subtitlesManager.ResetText();
            subtitlesManager.ShowSubtitle(i);

            // Audio + typing logic
            yield return StartCoroutine(PlaySubtitleAndAudio(i));

            if (skipAll) break;

            yield return new WaitForSecondsRealtime(subtitleDelay);
        }

        // Fade out background before showing prompt
        yield return StartCoroutine(FadeOutBackground());

        // All subtitles done -> show "Press Enter" prompt
        ShowPressEnterPrompt();

        // Wait for ENTER
        yield return StartCoroutine(WaitForEnter());

        // Load next scene
        StartCoroutine(LoadNextSceneAfterDelay());
    }

    void ShowPressEnterPrompt()
    {
        if (pressEnterImage != null)
            pressEnterImage.SetActive(true);

        waitingForContinue = true;
    }

    IEnumerator WaitForEnter()
    {
        while (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
            yield return null;

        waitingForContinue = false;
    }

    IEnumerator PlaySubtitleAndAudio(int index)
    {
        skipCurrentLine = false;
        isFading = false;

        Coroutine inputMonitor = StartCoroutine(SkipInputMonitor());

        Coroutine audioRoutine = StartCoroutine(PlayLocalizedClipAtIndex(index));
        Coroutine fadeInRoutine = StartCoroutine(FadeInText());

        while (!skipCurrentLine && !skipAll && (audioSource.isPlaying || !IsTypingDone()))
            yield return null;

        StopCoroutine(inputMonitor);

        if (skipCurrentLine && !skipAll)
        {
            subtitlesManager.SkipTyping();
            yield return StartCoroutine(FadeOutText());
        }
        else if (skipAll)
        {
            subtitlesManager.SkipTyping();
            yield return StartCoroutine(FadeOutText());
            yield return StartCoroutine(FadeOutBackground());
        }
        else
        {
            yield return StartCoroutine(FadeOutText());
        }

        subtitlesManager.ResetText();
    }

    IEnumerator PlayLocalizedClipAtIndex(int index)
    {
        yield return LocalizationSettings.InitializationOperation;

        AudioClip[] clips = GetLocalizedClips();
        if (clips == null || index >= clips.Length) yield break;

        AudioClip clip = clips[index];
        if (clip == null) yield break;

        audioSource.clip = clip;
        audioSource.volume = 1f;
        audioSource.Play();

        float timer = 0f;
        while (timer < clip.length)
        {
            if (skipCurrentLine || skipAll)
                yield break;

            timer += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    IEnumerator SkipInputMonitor()
    {
        float escHoldTime = 0f;

        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                skipCurrentLine = true;
                yield break;
            }

            if (Input.GetKey(KeyCode.Escape))
            {
                escHoldTime += Time.unscaledDeltaTime;
                if (escHoldTime >= 3f)
                {
                    skipAll = true;
                    skipCurrentLine = true;
                    yield break;
                }
            }
            else escHoldTime = 0f;

            yield return null;
        }
    }

    IEnumerator FadeOutText()
    {
        if (isFading) yield break;
        isFading = true;

        var text = subtitlesManager.GetComponentInChildren<TextMeshProUGUI>();
        if (text == null) { isFading = false; yield break; }

        Color original = text.color;
        float startAlpha = original.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            float t = time / fadeDuration;
            Color faded = original;
            faded.a = Mathf.Lerp(startAlpha, 0f, t);
            text.color = faded;

            if (audioSource != null && audioSource.isPlaying)
                audioSource.volume = Mathf.Lerp(1f, 0f, t);

            time += Time.unscaledDeltaTime;
            yield return null;
        }

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        original.a = 1f;
        text.color = original;
        if (audioSource != null)
            audioSource.volume = 1f;

        isFading = false;
    }

    IEnumerator FadeInText()
    {
        var text = subtitlesManager.GetComponentInChildren<TextMeshProUGUI>();
        if (text == null) yield break;

        Color color = text.color;
        color.a = 0f;
        text.color = color;

        float time = 0f;

        while (time < fadeDuration)
        {
            float t = time / fadeDuration;
            color.a = Mathf.Lerp(0f, 1f, t);
            text.color = color;
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        color.a = 1f;
        text.color = color;
    }

    IEnumerator FadeOutBackground()
    {
        if (subtitlesManager.backgroundImage == null) yield break;

        CanvasGroup group = subtitlesManager.backgroundImage.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = subtitlesManager.backgroundImage.AddComponent<CanvasGroup>();
            group.alpha = 1f;
        }

        float time = 0f;
        float startAlpha = group.alpha;

        while (time < backgroundFadeDuration)
        {
            group.alpha = Mathf.Lerp(startAlpha, 0f, time / backgroundFadeDuration);
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        group.alpha = 0f;
        subtitlesManager.backgroundImage.SetActive(false);
    }

    IEnumerator FadeInBackground()
    {
        if (subtitlesManager.backgroundImage == null) yield break;

        CanvasGroup group = subtitlesManager.backgroundImage.GetComponent<CanvasGroup>();
        if (group == null)
            group = subtitlesManager.backgroundImage.AddComponent<CanvasGroup>();

        subtitlesManager.backgroundImage.SetActive(true);
        group.alpha = 0f;

        float time = 0f;
        while (time < backgroundFadeDuration)
        {
            group.alpha = Mathf.Lerp(0f, 1f, time / backgroundFadeDuration);
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        group.alpha = 1f;
    }

    bool IsTypingDone()
    {
        return subtitlesManager != null && subtitlesManager.typingFinished;
    }

    AudioClip[] GetLocalizedClips()
    {
        string code = LocalizationSettings.SelectedLocale.Identifier.Code;
        if (code.StartsWith("pt"))
            return typeof(LocalizedAudioPlayer)
                .GetField("portugueseClips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(localizedAudioPlayer) as AudioClip[];

        return typeof(LocalizedAudioPlayer)
            .GetField("englishClips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(localizedAudioPlayer) as AudioClip[];
    }

    IEnumerator LoadNextSceneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(sceneChangeDelay);
        LevelLoader.Instance.LoadNextLevel("3DTutorialScene");
    }
}
