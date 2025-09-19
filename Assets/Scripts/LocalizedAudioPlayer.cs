using UnityEngine;
using System.Collections;
using UnityEngine.Localization.Settings;

public class LocalizedAudioPlayer : MonoBehaviour
{
    [Header("Audio Source")]
    [Tooltip("AudioSource to play clips on.")]
    [SerializeField] AudioSource audioSource;

    [Header("Audio Clips")]
    [SerializeField] AudioClip[] englishClips;
    [SerializeField] AudioClip[] portugueseClips;

    [Header("Options")]
    [Tooltip("If true, automatically play when enabled.")]
    [SerializeField] bool playOnEnable = false;

    void Awake()
    {
        if (audioSource == null)
        {
            Debug.LogWarning($"[{nameof(LocalizedAudioPlayer)}] No AudioSource assigned on '{gameObject.name}'. Please assign one in the Inspector.");
        }
    }

    void OnEnable()
    {
        if (playOnEnable)
            PlaySequence();
    }

    public void PlaySequence()
    {
        if (audioSource == null)
        {
            Debug.LogError($"[{nameof(LocalizedAudioPlayer)}] Cannot play sequence: no AudioSource assigned.");
            return;
        }

        StartCoroutine(PlayLocalizedSequence());
    }

    private IEnumerator PlayLocalizedSequence()
    {
        yield return LocalizationSettings.InitializationOperation;

        AudioClip[] clips = GetLocalizedClips();

        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[i];
            if (clip == null) continue;

            audioSource.clip = clip;
            audioSource.Play();

            float timer = 0f;
            while (timer < clip.length)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    private AudioClip[] GetLocalizedClips()
    {
        string code = LocalizationSettings.SelectedLocale.Identifier.Code;

        if (code == "pt" || code == "pt-BR")
            return portugueseClips;

        return englishClips;
    }
}
