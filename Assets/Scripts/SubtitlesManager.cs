using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using System.Collections;

public class SubtitlesManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI subtitlesText;
    [SerializeField] GameObject backgroundImage;

    [SerializeField] float typingSpeed = 0.05f;

    [SerializeField] string tableName = "Dialogue_Text";
    [SerializeField] string[] lineKeys;

    Coroutine typingCoroutine;
    bool typingFinished = false;

    void Awake()
    {
        if (backgroundImage != null)
            backgroundImage.SetActive(false);

        if (subtitlesText != null)
            subtitlesText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        OptionsManager.OnSubtitlesToggleChangedEvent += HandleSubtitlesToggleChanged;
    }

    void OnDisable()
    {
        OptionsManager.OnSubtitlesToggleChangedEvent -= HandleSubtitlesToggleChanged;
    }

    void HandleSubtitlesToggleChanged(bool enabled)
    {
        if (!enabled)
        {
            EndSubtitles();
        }
    }

    public void ShowSubtitle(int index)
    {
        if (!OptionsManager.SubtitlesEnabled)
        {
            EndSubtitles();
            return;
        }

        if (index < 0 || index >= lineKeys.Length) return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (subtitlesText != null)
            subtitlesText.gameObject.SetActive(true);

        if (backgroundImage != null)
            backgroundImage.SetActive(true);

        typingCoroutine = StartCoroutine(TypeLocalizedSubtitle(lineKeys[index]));
    }

    IEnumerator TypeLocalizedSubtitle(string lineKey)
    {
        subtitlesText.text = "";
        typingFinished = false;

        LocalizedString localizedLine = new LocalizedString
        {
            TableReference = tableName,
            TableEntryReference = lineKey
        };

        bool lineReady = false;
        string currentLine = "";

        void OnStringChanged(string value)
        {
            currentLine = value;
            lineReady = true;
        }

        localizedLine.StringChanged += OnStringChanged;
        localizedLine.RefreshString();

        yield return new WaitUntil(() => lineReady);

        // Make sure text is cleared before typing
        subtitlesText.text = "";

        foreach (char c in currentLine)
        {
            subtitlesText.text += c;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        localizedLine.StringChanged -= OnStringChanged;
        typingFinished = true;
    }


    public IEnumerator WaitForTypingComplete()
    {
        yield return new WaitUntil(() => typingFinished);
    }

    public void EndSubtitles()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        typingFinished = false;

        if (subtitlesText != null)
        {
            subtitlesText.text = "";
            subtitlesText.gameObject.SetActive(false);
        }

        if (backgroundImage != null)
            backgroundImage.SetActive(false);

        var uiManager = FindAnyObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.NotifySubtitlesSequenceCompleted();
        }
    }
    public void ResetText()
    {
        subtitlesText.text = string.Empty;
    }
}
