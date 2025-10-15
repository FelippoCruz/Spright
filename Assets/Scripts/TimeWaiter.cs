using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class TimeWaiter : MonoBehaviour
{
    [Header("Timing Settings")]
    [SerializeField] float subtitleDelay = 1f; // delay between lines
    [SerializeField] float sceneChangeDelay = 5f; // delay after submitting input
    [SerializeField] float fadeDuration = 0.5f;   // fade time for skip

    [Header("References")]
    [SerializeField] SubtitlesManager subtitlesManager;
    [SerializeField] LocalizedAudioPlayer localizedAudioPlayer;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] GameObject subtitleBackground; // auto-linked from subtitlesManager

    private string playerInput;
    private AudioSource audioSource;
    private bool skipCurrentLine = false;
    private bool skipAll = false;
    private bool isFading = false;

    void Start()
    {
        // Auto-assign subtitle background from SubtitlesManager
        if (subtitlesManager != null && subtitlesManager.backgroundImage != null)
            subtitleBackground = subtitlesManager.backgroundImage;

        inputField.gameObject.SetActive(false);
        audioSource = localizedAudioPlayer.GetComponent<AudioSource>();
        StartCoroutine(PlayIntroSequence());
    }

    IEnumerator PlayIntroSequence()
    {
        for (int i = 0; i < 4; i++)
        {
            if (skipAll) break;

            subtitlesManager.ResetText();
            subtitlesManager.ShowSubtitle(i);

            yield return StartCoroutine(PlaySubtitleAndAudio(i));

            if (skipAll) break;

            yield return new WaitForSecondsRealtime(subtitleDelay);
            subtitlesManager.EndSubtitles();
        }

        // End all and show input field
        subtitlesManager.EndSubtitles();
        inputField.gameObject.SetActive(true);
        inputField.onSubmit.AddListener(OnInputSubmit);
    }

    IEnumerator PlaySubtitleAndAudio(int index)
    {
        skipCurrentLine = false;
        isFading = false;

        Coroutine audioRoutine = StartCoroutine(PlayLocalizedClipAtIndex(index));
        Coroutine subtitleRoutine = StartCoroutine(subtitlesManager.WaitForTypingComplete());
        Coroutine inputCheck = StartCoroutine(SkipInputMonitor());

        while (!skipCurrentLine && !skipAll && (audioSource.isPlaying || subtitleRoutine != null))
        {
            if (!audioSource.isPlaying && subtitleRoutine == null) break;
            yield return null;
        }

        StopCoroutine(inputCheck);

        if (skipCurrentLine || skipAll)
        {
            yield return FadeOutAudioAndSubtitle();
        }
    }

    IEnumerator PlayLocalizedClipAtIndex(int index)
    {
        var init = LocalizationSettings.InitializationOperation;
        yield return init;

        AudioClip[] clips = GetLocalizedClips();
        if (clips == null || index < 0 || index >= clips.Length) yield break;

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
            else
            {
                escHoldTime = 0f;
            }

            yield return null;
        }
    }

    IEnumerator FadeOutAudioAndSubtitle()
    {
        if (isFading) yield break;
        isFading = true;

        float startVolume = audioSource.volume;
        float time = 0f;

        // Prepare subtitle fade
        Color textColor = subtitlesManager.GetComponentInChildren<TextMeshProUGUI>().color;
        Color bgColor = subtitleBackground != null ? subtitleBackground.color : Color.clear;

        while (time < fadeDuration)
        {
            float t = time / fadeDuration;

            if (audioSource.isPlaying)
                audioSource.volume = Mathf.Lerp(startVolume, 0f, t);

            var txt = subtitlesManager.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                Color c = txt.color;
                c.a = Mathf.Lerp(textColor.a, 0f, t);
                txt.color = c;
            }

            if (subtitleBackground != null)
            {
                Color c = subtitleBackground.color;
                c.a = Mathf.Lerp(bgColor.a, 0f, t);
                subtitleBackground.color = c;
            }

            time += Time.unscaledDeltaTime;
            yield return null;
        }

        if (audioSource.isPlaying)
            audioSource.Stop();

        subtitlesManager.EndSubtitles();

        var resetTxt = subtitlesManager.GetComponentInChildren<TextMeshProUGUI>();
        if (resetTxt != null)
        {
            Color c = resetTxt.color;
            c.a = 1f;
            resetTxt.color = c;
        }

        if (subtitleBackground != null)
        {
            Color c = subtitleBackground.color;
            c.a = 1f;
            subtitleBackground.color = c;
        }
    }

    AudioClip[] GetLocalizedClips()
    {
        string code = LocalizationSettings.SelectedLocale.Identifier.Code;

        if (code == "pt" || code == "pt-BR")
            return localizedAudioPlayer.GetType()
                .GetField("portugueseClips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(localizedAudioPlayer) as AudioClip[];

        return localizedAudioPlayer.GetType()
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

            subtitlesManager.EndSubtitles();
            StartCoroutine(LoadNextSceneAfterDelay());
        }
    }

    IEnumerator LoadNextSceneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(sceneChangeDelay);
        LevelLoader.Instance.LoadNextLevel("3DTutorialScene");
    }
}
