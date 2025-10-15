using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class TimeWaiter : MonoBehaviour
{
    [Header("Timing Settings")]
    [SerializeField] float subtitleDelay = 1f;     // Pause between lines
    [SerializeField] float sceneChangeDelay = 5f;  // Delay after input before changing scene
    [SerializeField] float fadeDuration = 0.5f;    // Fade speed for skip effects
    [SerializeField] float audioFadeDuration = 1f; // Separate from text fade speed
    [SerializeField] float backgroundFadeDuration = 0.75f; // speed of background fade in/out


    [Header("References")]
    [SerializeField] SubtitlesManager subtitlesManager;
    [SerializeField] LocalizedAudioPlayer localizedAudioPlayer;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] GameObject subtitleBackground;

    private AudioSource audioSource;
    private string playerInput;

    private bool skipCurrentLine = false;
    private bool skipAll = false;
    private bool isFading = false;

    void Start()
    {
        if (subtitlesManager != null && subtitlesManager.backgroundImage != null)
            subtitleBackground = subtitlesManager.backgroundImage;

        inputField.gameObject.SetActive(false);
        audioSource = localizedAudioPlayer.GetComponent<AudioSource>();

        StartCoroutine(PlayIntroSequence());
    }

    IEnumerator PlayIntroSequence()
    {
        // Fade in background once
        yield return StartCoroutine(FadeInBackground());

        int totalLines = 4; // or subtitlesManager.lineKeys.Length if dynamic
        for (int i = 0; i < totalLines; i++)
        {
            if (skipAll) break;

            // Reset and show subtitle text
            subtitlesManager.ResetText();
            subtitlesManager.ShowSubtitle(i);

            // Play audio and fade-in text simultaneously
            yield return StartCoroutine(PlaySubtitleAndAudio(i));

            if (skipAll) break;

            // Wait subtitleDelay before next line, but keep background visible
            yield return new WaitForSecondsRealtime(subtitleDelay);
        }

        // Fade out background at the very end
        yield return StartCoroutine(FadeOutBackground());

        // Show input field after everything
        subtitlesManager.ResetText(); // just to be sure text is cleared
        inputField.gameObject.SetActive(true);
        inputField.onSubmit.AddListener(OnInputSubmit);
    }

    IEnumerator PlaySubtitleAndAudio(int index)
    {
        skipCurrentLine = false;
        isFading = false;

        // Start monitoring input for skipping
        Coroutine inputMonitor = StartCoroutine(SkipInputMonitor());

        // Start audio and text
        Coroutine audioRoutine = StartCoroutine(PlayLocalizedClipAtIndex(index));
        Coroutine fadeInRoutine = StartCoroutine(FadeInText());

        // Wait until typing + audio are finished or player skips
        while (!skipCurrentLine && !skipAll && (audioSource.isPlaying || !IsTypingDone()))
            yield return null;

        StopCoroutine(inputMonitor);

        if (skipCurrentLine && !skipAll)
        {
            // Immediately stop typing
            subtitlesManager.SkipTyping();

            // Fade text only
            yield return StartCoroutine(FadeOutText());
        }
        else if (skipAll)
        {
            subtitlesManager.SkipTyping();
            // Fade text and background immediately
            yield return StartCoroutine(FadeOutText());
            yield return StartCoroutine(FadeOutBackground());
        }
        else
        {
            // Normal playback finished, just fade out text
            yield return StartCoroutine(FadeOutText());
        }

        // Reset text, keep background active
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

            // Fade audio if playing
            if (audioSource != null && audioSource.isPlaying)
                audioSource.volume = Mathf.Lerp(1f, 0f, t);

            time += Time.unscaledDeltaTime;
            yield return null;
        }

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        // Reset text and audio for next line
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
        if (subtitleBackground == null) yield break;

        CanvasGroup group = subtitleBackground.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = subtitleBackground.AddComponent<CanvasGroup>();
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
        subtitleBackground.SetActive(false);
    }

    IEnumerator FadeInBackground()
    {
        if (subtitleBackground == null) yield break;

        CanvasGroup group = subtitleBackground.GetComponent<CanvasGroup>();
        if (group == null)
            group = subtitleBackground.AddComponent<CanvasGroup>();

        subtitleBackground.SetActive(true);
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
        if (code == "pt" || code == "pt-BR")
            return typeof(LocalizedAudioPlayer)
                .GetField("portugueseClips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(localizedAudioPlayer) as AudioClip[];

        return typeof(LocalizedAudioPlayer)
            .GetField("englishClips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(localizedAudioPlayer) as AudioClip[];
    }

    void OnInputSubmit(string input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            playerInput = input;
            PlayerPrefs.SetString("playerName", playerInput);
            PlayerPrefs.Save();

            inputField.onSubmit.RemoveListener(OnInputSubmit);
            inputField.gameObject.SetActive(false);

            StartCoroutine(LoadNextSceneAfterDelay());
        }
    }

    IEnumerator LoadNextSceneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(sceneChangeDelay);
        LevelLoader.Instance.LoadNextLevel("3DTutorialScene");
    }
}
