using System.Collections;
using TMPro;
using UnityEngine;

public class TimeWaiter : MonoBehaviour
{
    [SerializeField] float Time = 20f;   // delay before input field shows
    [SerializeField] float Time2 = 5f;   // delay before scene change
    [SerializeField] float subtitleDelay = 1f; // delay after subtitle before next one

    [SerializeField] SubtitlesManager subtitlesManager;

    [SerializeField] GameObject specialObject;
    [SerializeField] float specialDuration = 2f;

    public TMP_InputField inputField;
    public string PlayerInput;
    LevelLoader LevelLoader;

    void Start()
    {
        inputField.gameObject.SetActive(false);

        // Start subtitles immediately
        StartCoroutine(PlayIntroSubtitles());

        // Start wait for input later
        StartCoroutine(Wait());
    }

    IEnumerator PlayIntroSubtitles()
    {
        // --- First subtitle ---
        subtitlesManager.ResetText();
        subtitlesManager.ShowSubtitle(0);
        yield return subtitlesManager.WaitForTypingComplete();
        yield return new WaitForSecondsRealtime(subtitleDelay);
        subtitlesManager.EndSubtitles();

        if (specialObject != null)
        {
            specialObject.SetActive(true);
            yield return new WaitForSecondsRealtime(specialDuration);
            specialObject.SetActive(false);
        }

        // --- Second subtitle (last one) ---
        subtitlesManager.ShowSubtitle(1);
        yield return subtitlesManager.WaitForTypingComplete();
    }

    IEnumerator Wait()
    {
        yield return new WaitForSeconds(Time);

        // Show input field
        inputField.gameObject.SetActive(true);
        inputField.onSubmit.AddListener(OnInputSubmit);

        // Hide subtitles when input appears
        subtitlesManager.EndSubtitles();
    }

    IEnumerator Wait2()
    {
        yield return new WaitForSeconds(Time2);
        LevelLoader.Instance.LoadNextLevel("GameScene");
    }

    void OnInputSubmit(string input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            PlayerInput = input;
            PlayerPrefs.SetString("playerName", PlayerInput);
            PlayerPrefs.Save();
            inputField.onSubmit.RemoveListener(OnInputSubmit);
            inputField.gameObject.SetActive(false);
            StartCoroutine(Wait2());
        }
    }
}
