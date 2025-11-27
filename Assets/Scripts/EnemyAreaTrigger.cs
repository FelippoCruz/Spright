using UnityEngine;
using System.Collections;
using UnityEngine.Localization.Settings;

public class EnemyAreaTrigger : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] AudioSource voiceSource;
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource backgroundMusicSource;

    [SerializeField] AudioClip[] audioClipsEnglishOphelia;
    [SerializeField] AudioClip[] audioClipsPortugueseOphelia;

    [SerializeField] AudioClip[] audioClipsEnglishSlade;
    [SerializeField] AudioClip[] audioClipsPortugueseSlade;

    [SerializeField] AudioClip fightMusic;

    [Header("Subtitles")]
    [SerializeField] SubtitlesManager subtitlesManager;

    [Header("Trigger Message")]
    [SerializeField] GameObject messageObject;
    [SerializeField] float messageDuration = 3f;

    bool messageShown = false;

    bool sequenceStarted = false;

    [Header("Music Fading")]
    [SerializeField] float fadeDuration = 1.5f; // seconds

    public bool Triggered { get; private set; } = false;

    private void Start()
    {
        if (messageObject != null)
            messageObject.SetActive(false);

        if (backgroundMusicSource != null && !backgroundMusicSource.isPlaying)
            backgroundMusicSource.Play();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !sequenceStarted)
        {
            Triggered = true;
            sequenceStarted = true;

            if (!messageShown)
                StartCoroutine(ShowTriggerMessageOnce());

            StartCoroutine(PlaySequenceThenMusic());
        }
    }

    IEnumerator PlaySequenceThenMusic()
    {
        yield return LocalizationSettings.InitializationOperation;
        int steps = audioClipsEnglishOphelia.Length;

        for (int i = 0; i < steps; i++)
        {
            AudioClip[] currentVoiceClips = GetCurrentVoiceClips();

            AudioClip clip = currentVoiceClips[i];
            if (clip == null) continue;

            if (OptionsManager.SubtitlesEnabled && subtitlesManager != null)
                subtitlesManager.ShowSubtitle(i);

            voiceSource.clip = clip;
            voiceSource.Play();

            float timer = 0f;
            while (timer < clip.length)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.2f);
        }

        yield return new WaitForSecondsRealtime(0.4f);

        if (OptionsManager.SubtitlesEnabled && subtitlesManager != null)
            subtitlesManager.EndSubtitles();

        StartCoroutine(MusicTransition());

        gameObject.SetActive(false);
    }

    AudioClip[] GetCurrentVoiceClips()
    {
        int characterChosen = PlayerPrefs.GetInt("CharacterChosen", 0);
        string code = LocalizationSettings.SelectedLocale.Identifier.Code;

        if (characterChosen == 0)
        {
            if (code == "pt" || code == "pt-BR")
                return audioClipsPortugueseOphelia;
            else
                return audioClipsEnglishOphelia;
        }
        else
        {
            if (code == "pt" || code == "pt-BR")
                return audioClipsPortugueseSlade;
            else
                return audioClipsEnglishSlade;
        }
    }

    IEnumerator ShowTriggerMessageOnce()
    {
        messageShown = true;

        if (messageObject != null)
            messageObject.SetActive(true);

        yield return new WaitForSecondsRealtime(messageDuration);

        if (messageObject != null)
            messageObject.SetActive(false);
    }

    private IEnumerator MusicTransition()
    {
        // 1. Fade out background music
        if (backgroundMusicSource != null)
        {
            float startVolume = backgroundMusicSource.volume;
            yield return StartCoroutine(FadeAudioSource(backgroundMusicSource, startVolume, 0f, fadeDuration));
            backgroundMusicSource.Stop();
            backgroundMusicSource.volume = startVolume;
        }

        // 2. Fade in fight music
        if (musicSource != null && fightMusic != null)
        {
            musicSource.clip = fightMusic;
            musicSource.volume = 0f;
            musicSource.loop = true;
            musicSource.Play();

            yield return StartCoroutine(FadeAudioSource(musicSource, 0f, 1f, fadeDuration));
        }
    }

    private IEnumerator FadeAudioSource(AudioSource source, float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            source.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }

        source.volume = to;
    }
}
