using UnityEngine;
using System.Collections;
using UnityEngine.Localization.Settings;

public class EnemyAreaTrigger : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] AudioSource voiceSource;
    [SerializeField] AudioSource musicSource;

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

    public bool Triggered { get; private set; } = false;

    private void Start()
    {
        if (messageObject != null)
            messageObject.SetActive(false);
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

        musicSource.clip = fightMusic;
        musicSource.loop = true;
        musicSource.Play();

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
}
