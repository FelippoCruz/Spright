using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    [SerializeField] GameObject SprightText;
    [SerializeField] GameObject MainMenu;
    [SerializeField] TriggerNPC NPCScript;
    [SerializeField] EnemyAreaTrigger EATScript;
    public GameObject Chatbox;
    [SerializeField] GameObject SubtitlesBackground;
    public GameObject PausePanel;
    public GameObject OptionsPanel;
    [SerializeField] GameObject Main, KandM, Controller, Graphics, Audio, Language, Video, Accessibility, Extra;

    bool hasShownSubtitlesUI = false;
    bool subtitlesSequenceCompleted = false;
    LevelLoader LevelLoader;

    [SerializeField] Button OptionsButton;

    void Awake()
    {
        if (SprightText != null)
        {
            SprightText.SetActive(true);
        }
        if (MainMenu != null)
        {
            MainMenu.SetActive(true);
        }
        if (PausePanel != null)
        {
            PausePanel.SetActive(false);
        }
        if (Chatbox != null)
        {
            Chatbox.SetActive(false);
        }
        if (SubtitlesBackground != null)
        {
            SubtitlesBackground.SetActive(false);
        }
        OptionsPanel.SetActive(false);
    }

    void Update()
    {
        if (EATScript != null && EATScript.Triggered && !hasShownSubtitlesUI && !subtitlesSequenceCompleted && OptionsManager.SubtitlesEnabled)
        {
            ShowSubtitlesBackground();
        }

        if (NPCScript != null && Chatbox != null && NPCScript.PlayerOnRange && Input.GetKeyDown(KeyCode.E))
        {
            Chatbox.SetActive(true);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (KandM.activeSelf || Controller.activeSelf || Graphics.activeSelf ||
                Audio.activeSelf || Language.activeSelf || Video.activeSelf ||
                Accessibility.activeSelf || Extra.activeSelf)
            {
                KandM.SetActive(false);
                Controller.SetActive(false);
                Graphics.SetActive(false);
                Audio.SetActive(false);
                Language.SetActive(false);
                Video.SetActive(false);
                Accessibility.SetActive(false);
                Extra.SetActive(false);
                Main.SetActive(true);
            }
            else if (OptionsPanel.activeSelf)
            {
                LeaveOptions();
            }
            else if (PausePanel != null && PausePanel.activeSelf)
            {
                Resume();
            }
            else if (PausePanel != null && !PausePanel.activeSelf)
            {
                PausePanel.SetActive(true);
                Time.timeScale = 0;
            }
            else {
                Options();
            }
        }
        if (PausePanel != null && OptionsPanel != null && Chatbox != null)
        {
            if (!PausePanel.activeSelf && !OptionsPanel.activeSelf && !Chatbox.activeSelf)
            {
                Time.timeScale = 1;
            }
        }
    }

    public void ShowSubtitlesBackground()
    {
        SubtitlesBackground.SetActive(true);
        hasShownSubtitlesUI = true;
    }

    public void HideSubtitlesBackground()
    {
        SubtitlesBackground.SetActive(false);
        hasShownSubtitlesUI = false;
    }

    public void NotifySubtitlesSequenceCompleted()
    {
        subtitlesSequenceCompleted = true;
        HideSubtitlesBackground();
    }

    public void Resume()
    {
        PausePanel.SetActive(false);
        if ((Chatbox != null && !Chatbox.activeSelf) || Chatbox == null)
        {
            Time.timeScale = 1;
        }
    }

    public void Options()
    {
        if (PausePanel != null)
        {
            PausePanel.SetActive(false);
        }
        OptionsPanel.SetActive(true);
    }

    public void LeaveOptions()
    {
        if (EventSystem.current != null && OptionsButton != null && EventSystem.current.currentSelectedGameObject == OptionsButton.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        OptionsPanel.SetActive(false);
        if (PausePanel != null)
        {
            PausePanel.SetActive(true);
        }
        else
        {
            SprightText.SetActive(true);
            MainMenu.SetActive(true);
        }
    }

    public void Quit()
    {
        PausePanel.SetActive(false);
        Time.timeScale = 1;
        Invoke(nameof(Quitted), 2f);
    }

    void Quitted()
    {
        LevelLoader.Instance.LoadNextLevel("StartScene");
    }
}
