using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class OptionsManager : MonoBehaviour
{
    [SerializeField] TMP_Dropdown GraphicsDropdown, LanguageDropdown, ScreenModeDropdown, FPSDropdown;
    [SerializeField] Slider Master, Music, SFX, BG, Voice, Brightness, Contrast, Saturation;
    [SerializeField] TextMeshProUGUI BrightnessValueText, ContrastValueText, SaturationValueText;
    [SerializeField] AudioMixer MainAudioMixer;
    [SerializeField] Toggle vsyncToggle, invertXToggle, invertYToggle, subtitlesToggle;
    [SerializeField] GameObject Main, KandM, Controller, Graphics, Audio, Language, Video, Accessibility, Extra;

    public static bool InvertX { get; private set; }
    public static bool InvertY { get; private set; }
    public static bool SubtitlesEnabled { get; private set; } = true;
    public static event System.Action<bool> OnSubtitlesToggleChangedEvent;

    const string KEY_MASTER = "MasterVol";
    const string KEY_MUSIC = "MusicVol";
    const string KEY_SFX = "SFXVol";
    const string KEY_BG = "BgVol";
    const string KEY_VOICE = "VoiceVol";

    const string KEY_INVERT_X = "InvertX";
    const string KEY_INVERT_Y = "InvertY";
    const string KEY_VSYNC = "VSyncEnabled";
    const string KEY_SUBS = "Subtitles";
    const string KEY_BRIGHT_VAL = "BrightnessValue";
    const string KEY_CONTRAST = "ContrastValue";
    const string KEY_SAT = "SaturationValue";

    const float DEFAULT_VOLUME_DB = 0f;
    const float DEFAULT_BRIGHTNESS = 0f;

    ColorAdjustments colorAdj;

    int defGraphics, defLanguage, defScreen, defFPS;
    bool defVSync, defInvX, defInvY, defSubs;

    LevelLoader LevelLoader;

    void Awake()
    {
        Volume gv = PersistentVolume.Instance;
        if (gv && gv.profile.TryGet(out colorAdj))
        {
            float defBright = 0f;
            float defContrast = colorAdj.contrast.value;
            float defSat = colorAdj.saturation.value;

            float curBright = PlayerPrefs.GetFloat(KEY_BRIGHT_VAL, defBright);
            float curContrast = PlayerPrefs.GetFloat(KEY_CONTRAST, defContrast);
            float curSat = PlayerPrefs.GetFloat(KEY_SAT, defSat);

            colorAdj.postExposure.value = curBright;
            colorAdj.contrast.value = curContrast;
            colorAdj.saturation.value = curSat;

            Brightness.value = curBright;
            Contrast.value = curContrast;
            Saturation.value = curSat;

            UpdateBrightnessText(curBright);
            ContrastValueText.text = (curContrast / 2f + 50f).ToString("F1");
            SaturationValueText.text = (curSat / 2f + 50f).ToString("F1");

            Brightness.onValueChanged.AddListener(ChangeBrightness);
            Contrast.onValueChanged.AddListener(ChangeContrast);
            Saturation.onValueChanged.AddListener(ChangeSaturation);
        }

        float curMaster = PlayerPrefs.GetFloat(KEY_MASTER, DEFAULT_VOLUME_DB);
        float curMusic = PlayerPrefs.GetFloat(KEY_MUSIC, DEFAULT_VOLUME_DB);
        float curSFX = PlayerPrefs.GetFloat(KEY_SFX, DEFAULT_VOLUME_DB);
        float curBG = PlayerPrefs.GetFloat(KEY_BG, DEFAULT_VOLUME_DB);
        float curVoice = PlayerPrefs.GetFloat(KEY_VOICE, DEFAULT_VOLUME_DB);

        Master.value = curMaster;
        Music.value = curMusic;
        SFX.value = curSFX;
        BG.value = curBG;
        Voice.value = curVoice;

        MainAudioMixer.SetFloat("MasterVol", curMaster);
        MainAudioMixer.SetFloat("MusicVol", curMusic);
        MainAudioMixer.SetFloat("SFXVol", curSFX);
        MainAudioMixer.SetFloat("BgVol", curBG);
        MainAudioMixer.SetFloat("VoiceVol", curVoice);

        Master.onValueChanged.AddListener(v => {
            MainAudioMixer.SetFloat("MasterVol", v);
            PlayerPrefs.SetFloat(KEY_MASTER, v);
        });
        Music.onValueChanged.AddListener(v => {
            MainAudioMixer.SetFloat("MusicVol", v);
            PlayerPrefs.SetFloat(KEY_MUSIC, v);
        });
        SFX.onValueChanged.AddListener(v => {
            MainAudioMixer.SetFloat("SFXVol", v);
            PlayerPrefs.SetFloat(KEY_SFX, v);
        });
        BG.onValueChanged.AddListener(v => {
            MainAudioMixer.SetFloat("BgVol", v);
            PlayerPrefs.SetFloat(KEY_BG, v);
        });
        Voice.onValueChanged.AddListener(v => {
            MainAudioMixer.SetFloat("VoiceVol", v);
            PlayerPrefs.SetFloat(KEY_VOICE, v);
        });

        GraphicsDropdown.value = defGraphics = 2;
        string code = LocalizationSettings.SelectedLocale.Identifier.Code;
        LanguageDropdown.value = defLanguage = code.StartsWith("pt") ? 1 : 0;

        ScreenModeDropdown.value = defScreen = Screen.fullScreenMode switch
        {
            FullScreenMode.ExclusiveFullScreen => 1,
            FullScreenMode.FullScreenWindow => 2,
            _ => 0
        };
        FPSDropdown.value = defFPS = 1;

        defVSync = vsyncToggle.isOn;
        defInvX = invertXToggle.isOn = false;
        defInvY = invertYToggle.isOn = false;
        defSubs = subtitlesToggle.isOn;

        Main.SetActive(true);
        KandM.SetActive(false); Controller.SetActive(false); Graphics.SetActive(false);
        Audio.SetActive(false); Language.SetActive(false); Video.SetActive(false);
        Accessibility.SetActive(false); Extra.SetActive(false);

        LanguageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        InitScreenModeDropdown();
        InitFPSDropdown();

        vsyncToggle.isOn = PlayerPrefs.GetInt(KEY_VSYNC, defVSync ? 1 : 0) == 1;
        vsyncToggle.onValueChanged.AddListener(v =>
        {
            QualitySettings.vSyncCount = v ? 1 : 0;
            PlayerPrefs.SetInt(KEY_VSYNC, v ? 1 : 0);
        });

        invertXToggle.isOn = InvertX = PlayerPrefs.GetInt(KEY_INVERT_X, defInvX ? 1 : 0) == 1;
        invertYToggle.isOn = InvertY = PlayerPrefs.GetInt(KEY_INVERT_Y, defInvY ? 1 : 0) == 1;
        invertXToggle.onValueChanged.AddListener(v => { InvertX = v; PlayerPrefs.SetInt(KEY_INVERT_X, v ? 1 : 0); });
        invertYToggle.onValueChanged.AddListener(v => { InvertY = v; PlayerPrefs.SetInt(KEY_INVERT_Y, v ? 1 : 0); });

        subtitlesToggle.isOn = SubtitlesEnabled = PlayerPrefs.GetInt(KEY_SUBS, defSubs ? 1 : 0) == 1;
        subtitlesToggle.onValueChanged.AddListener(v =>
        {
            SubtitlesEnabled = v;
            PlayerPrefs.SetInt(KEY_SUBS, v ? 1 : 0);
            OnSubtitlesToggleChangedEvent?.Invoke(v);
        });
    }

    public void ResetToDefaults()
    {
        GraphicsDropdown.value = defGraphics;
        LanguageDropdown.value = defLanguage;
        ScreenModeDropdown.value = defScreen;
        FPSDropdown.value = defFPS;
        OnLanguageChanged(defLanguage);
        ScreenModeDropdown.onValueChanged.Invoke(defScreen);
        FPSDropdown.onValueChanged.Invoke(defFPS);

        Master.value = DEFAULT_VOLUME_DB; MainAudioMixer.SetFloat("MasterVol", DEFAULT_VOLUME_DB);
        Music.value = DEFAULT_VOLUME_DB; MainAudioMixer.SetFloat("MusicVol", DEFAULT_VOLUME_DB);
        SFX.value = DEFAULT_VOLUME_DB; MainAudioMixer.SetFloat("SFXVol", DEFAULT_VOLUME_DB);
        BG.value = DEFAULT_VOLUME_DB; MainAudioMixer.SetFloat("BgVol", DEFAULT_VOLUME_DB);
        Voice.value = DEFAULT_VOLUME_DB; MainAudioMixer.SetFloat("VoiceVol", DEFAULT_VOLUME_DB);

        PlayerPrefs.SetFloat(KEY_MASTER, DEFAULT_VOLUME_DB);
        PlayerPrefs.SetFloat(KEY_MUSIC, DEFAULT_VOLUME_DB);
        PlayerPrefs.SetFloat(KEY_SFX, DEFAULT_VOLUME_DB);
        PlayerPrefs.SetFloat(KEY_BG, DEFAULT_VOLUME_DB);
        PlayerPrefs.SetFloat(KEY_VOICE, DEFAULT_VOLUME_DB);

        // --- Brightness reset ---
        Brightness.value = DEFAULT_BRIGHTNESS;
        if (colorAdj) colorAdj.postExposure.value = DEFAULT_BRIGHTNESS;
        UpdateBrightnessText(DEFAULT_BRIGHTNESS);
        PlayerPrefs.SetFloat(KEY_BRIGHT_VAL, DEFAULT_BRIGHTNESS);

        // --- Contrast reset to 0 ---
        Contrast.value = 0f;
        if (colorAdj) colorAdj.contrast.value = 0f;
        ContrastValueText.text = (0f / 2f + 50f).ToString("F1");
        PlayerPrefs.SetFloat(KEY_CONTRAST, 0f);

        // --- Saturation reset to 0 ---
        Saturation.value = 0f;
        if (colorAdj) colorAdj.saturation.value = 0f;
        SaturationValueText.text = (0f / 2f + 50f).ToString("F1");
        PlayerPrefs.SetFloat(KEY_SAT, 0f);

        vsyncToggle.isOn = defVSync;
        QualitySettings.vSyncCount = defVSync ? 1 : 0;
        PlayerPrefs.SetInt(KEY_VSYNC, defVSync ? 1 : 0);

        invertXToggle.isOn = defInvX;
        InvertX = defInvX;
        PlayerPrefs.SetInt(KEY_INVERT_X, defInvX ? 1 : 0);

        invertYToggle.isOn = defInvY;
        InvertY = defInvY;
        PlayerPrefs.SetInt(KEY_INVERT_Y, defInvY ? 1 : 0);

        subtitlesToggle.isOn = defSubs;
        SubtitlesEnabled = defSubs;
        PlayerPrefs.SetInt(KEY_SUBS, defSubs ? 1 : 0);
        OnSubtitlesToggleChangedEvent?.Invoke(defSubs);

        PlayerPrefs.Save();
    }

    void OnLanguageChanged(int idx)
    {
        string c = idx == 1 ? "pt-BR" : "en";
        var loc = LocalizationSettings.AvailableLocales.GetLocale(c);
        if (loc) LocalizationSettings.SelectedLocale = loc;
    }

    void InitScreenModeDropdown()
    {
        ScreenModeDropdown.onValueChanged.AddListener(i =>
        {
            Screen.fullScreenMode = i switch
            {
                1 => FullScreenMode.ExclusiveFullScreen,
                2 => FullScreenMode.FullScreenWindow,
                _ => FullScreenMode.Windowed
            };
        });
    }

    void InitFPSDropdown()
    {
        FPSDropdown.onValueChanged.AddListener(i =>
        {
            Application.targetFrameRate = i switch
            {
                0 => 30,
                1 => 60,
                2 => 120,
                3 => 144,
                _ => -1
            };
        });
    }

    void ChangeBrightness(float v)
    {
        if (colorAdj) colorAdj.postExposure.value = v;
        UpdateBrightnessText(v);
        PlayerPrefs.SetFloat(KEY_BRIGHT_VAL, v);
    }

    void ChangeContrast(float v)
    {
        if (colorAdj) colorAdj.contrast.value = v;
        ContrastValueText.text = (v / 2f + 50f).ToString("F1");
        PlayerPrefs.SetFloat(KEY_CONTRAST, v);
    }

    void ChangeSaturation(float v)
    {
        if (colorAdj) colorAdj.saturation.value = v;
        SaturationValueText.text = (v / 2f + 50f).ToString("F1");
        PlayerPrefs.SetFloat(KEY_SAT, v);
    }

    void UpdateBrightnessText(float v)
    {
        if (BrightnessValueText) BrightnessValueText.text = ((v + 9f) * 10f).ToString("F1");
    }

    public void KeyboardAndMouseCall() { Main.SetActive(false); KandM.SetActive(true); }
    public void ControllerCall() { Main.SetActive(false); Controller.SetActive(true); }
    public void GraphicsCall() { Main.SetActive(false); Graphics.SetActive(true); }
    public void AudioCall() { Main.SetActive(false); Audio.SetActive(true); }
    public void LanguageCall() { Main.SetActive(false); Language.SetActive(true); }
    public void VideoCall() { Main.SetActive(false); Video.SetActive(true); }
    public void AccessibilityCall() { Main.SetActive(false); Accessibility.SetActive(true); }
    public void ExtraCall() { Main.SetActive(false); Extra.SetActive(true); }

    public void CreditsCall()
    {
        PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
        StartCoroutine(Credits());
    }

    IEnumerator Credits()
    {
        yield return new WaitForSecondsRealtime(2f);
        LevelLoader.Instance.LoadNextLevel("CreditsScene");
    }
}
