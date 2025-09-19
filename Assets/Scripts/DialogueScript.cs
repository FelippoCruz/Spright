using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueScript : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI dialogueText;
    [SerializeField] GameObject portraitImage;
    public GameObject Target;

    [Header("Typing")]
    [SerializeField] float textSpeed = 0.03f;

    [Header("Audio")]
    [SerializeField] AudioSource voiceSource;

    [SerializeField] AudioClip[] voiceClipsEnglishOphelia;
    [SerializeField] AudioClip[] voiceClipsPortugueseOphelia;

    [SerializeField] AudioClip[] voiceClipsEnglishSlade;
    [SerializeField] AudioClip[] voiceClipsPortugueseSlade;

    [Header("Localization")]
    [SerializeField] string tableName = "Dialogue_Text";
    [SerializeField] string[] lineKeys;

    string playerName;
    int index;
    bool showingNameLine = true;
    bool isTyping = false;
    string currentFullLine = "";
    LocalizedString currentLine = new LocalizedString();
    AudioClip[] currentVoiceClips;
    string lastDialogueID = "";

    // Events instead of bool flags
    public static event System.Action OnPlayerNameCalled;
    public static event System.Action OnTypeCalled;
    public static event System.Action OnEndDialogueCalled;

    [SerializeField] GameObject NPC;

    IEnumerator Start()
    {
        playerName = PlayerPrefs.GetString("playerName", "Player");
        dialogueText.text = string.Empty;

        yield return LocalizationSettings.InitializationOperation;

        if (LocalizationSettings.Instance == null)
        {
            Debug.LogError("LocalizationSettings.Instance is null. Please assign a Localization Settings asset in Project Settings > Localization.");
            yield break;
        }

        if (LocalizationSettings.SelectedLocale == null)
        {
            Debug.LogWarning("SelectedLocale is null. Defaulting to first available locale.");
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[0];
        }

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        currentVoiceClips = GetCurrentVoiceClips();
        StartDialogue();
    }

    void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    void OnLocaleChanged(Locale locale)
    {
        currentVoiceClips = GetCurrentVoiceClips();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (IsClickOnUIButton()) return;
        if (!IsClickValid()) return;

        if (showingNameLine)
        {
            if (dialogueText.text == playerName + "?")
            {
                showingNameLine = false;
                index = 0;
                StartCoroutine(TypeLocalizedLine());
            }
            else
            {
                StopAllCoroutines();
                dialogueText.text = playerName + "?";
            }
            return;
        }

        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = currentFullLine;
            isTyping = false;
            if (voiceSource.isPlaying) voiceSource.Stop();
        }
        else
        {
            if (voiceSource.isPlaying) voiceSource.Stop();
            NextLine();
        }
    }

    bool IsClickOnUIButton()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            if (result.gameObject.GetComponent<Button>() != null)
                return true;
        }
        return false;
    }

    bool IsClickValid()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        int uiLayer = LayerMask.NameToLayer("UI");

        if (results.Count == 0)
            return true;

        foreach (var result in results)
        {
            GameObject hitObj = result.gameObject;

            if (hitObj.layer != uiLayer)
            {
                return true;
            }
            else
            {
                if (IsGameObjectOrChild(hitObj, portraitImage))
                    return true;

                if (IsGameObjectOrChild(hitObj, dialogueText.gameObject))
                    return true;
            }
        }
        return false;
    }

    bool IsGameObjectOrChild(GameObject obj, GameObject parentObj)
    {
        if (obj == parentObj) return true;

        Transform t = obj.transform;
        while (t != null)
        {
            if (t.gameObject == parentObj)
                return true;
            t = t.parent;
        }
        return false;
    }

    void StartDialogue()
    {
        string currentDialogueID = (lineKeys != null && lineKeys.Length > 0) ? lineKeys[0] : "";
        Debug.Log($"[DialogueScript] Starting DialogueID: {currentDialogueID}");

        if (currentDialogueID != lastDialogueID)
        {
            lastDialogueID = currentDialogueID;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.IncrementTimesTalkedToMainNPC();
            }
        }

        if (Target) DestroyTarget();
        if (portraitImage) portraitImage.SetActive(true);
        Time.timeScale = 0f;
        StartCoroutine(TypePlayerName());
    }

    public void DestroyTarget() { Destroy(Target); }

    IEnumerator TypePlayerName()
    {
        OnPlayerNameCalled?.Invoke(); // Trigger event
        dialogueText.text = string.Empty;
        string line = playerName + "?";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(textSpeed);
        }
    }

    IEnumerator TypeLocalizedLine()
    {
        OnTypeCalled?.Invoke(); // Trigger event
        isTyping = true;
        currentFullLine = "";
        bool lineReady = false;

        currentLine.TableReference = tableName;
        currentLine.TableEntryReference = lineKeys[index];
        currentLine.StringChanged += (value) =>
        {
            currentFullLine = value;
            lineReady = true;
        };
        currentLine.RefreshString();

        yield return new WaitUntil(() => lineReady);

        dialogueText.text = string.Empty;
        PlayCurrentAudio();

        foreach (char c in currentFullLine)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(textSpeed);
        }

        isTyping = false;
        currentLine.StringChanged -= delegate { };
    }

    void NextLine()
    {
        if (++index < lineKeys.Length)
        {
            StartCoroutine(TypeLocalizedLine());
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        OnEndDialogueCalled?.Invoke(); // Trigger event
        if (portraitImage) portraitImage.SetActive(false);
        Time.timeScale = 1f;
        NPC.SetActive(false);
        gameObject.SetActive(false);
    }

    void PlayCurrentAudio()
    {
        if (index < currentVoiceClips.Length && currentVoiceClips[index] != null)
        {
            voiceSource.clip = currentVoiceClips[index];
            voiceSource.Play();
        }
    }

    AudioClip[] GetCurrentVoiceClips()
    {
        int characterChosen = PlayerPrefs.GetInt("CharacterChosen", 0);
        string lang = LocalizationSettings.SelectedLocale.Identifier.Code;

        if (characterChosen == 0)
        {
            if (lang == "pt" || lang == "pt-BR")
                return voiceClipsPortugueseOphelia;
            else
                return voiceClipsEnglishOphelia;
        }
        else
        {
            if (lang == "pt" || lang == "pt-BR")
                return voiceClipsPortugueseSlade;
            else
                return voiceClipsEnglishSlade;
        }
    }
}
