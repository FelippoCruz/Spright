using UnityEngine;
using UnityEngine.Localization.Settings;
using TMPro;
using UnityEngine.SceneManagement;

public class LanguageSelector : MonoBehaviour
{
    [SerializeField] TMP_Dropdown languageDropdown;
    [SerializeField] string nextScene = "BrightnessSelectorScene";

    const string KEY_LANG_SELECTED = "HasSelectedLanguage";
    const string KEY_LOCALE_CODE = "SelectedLocaleCode";

    bool hasConfirmed = false;

    void Start()
    {
        //For Resetting Language: 
        //PlayerPrefs.DeleteKey("SelectedLanguage"); 
        //PlayerPrefs.DeleteKey("SelectedLocaleCode"); 
        //PlayerPrefs.DeleteKey("HasSelectedLanguage"); 
        //PlayerPrefs.Save();

        // Check if it's the very first launch
        if (!PlayerPrefs.HasKey(KEY_LANG_SELECTED))
        {
            // First time: force dropdown default, but DON'T assign PlayerPrefs yet
            languageDropdown.value = 0;
            ApplyLanguage(1); // Default = English (or change if you want another)
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }
        else
        {
            // Not the first time: load saved locale and skip to next scene
            string savedLocaleCode = PlayerPrefs.GetString(KEY_LOCALE_CODE, "en");
            LoadSceneWithLocale(savedLocaleCode);
        }
    }

    public void OnLanguageChanged(int index)
    {
        if (index == 0 || hasConfirmed) return;

        // Map dropdown index to locale codes
        string localeCode = index == 2 ? "pt-BR" : "en";

        // Save PlayerPrefs permanently starting from this moment
        PlayerPrefs.SetInt(KEY_LANG_SELECTED, 1);
        PlayerPrefs.SetInt("SelectedLanguage", index);
        PlayerPrefs.SetString(KEY_LOCALE_CODE, localeCode);
        PlayerPrefs.Save();

        hasConfirmed = true;
        LoadSceneWithLocale(localeCode);
    }

    async void LoadSceneWithLocale(string code)
    {
        var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;
            await LocalizationSettings.InitializationOperation.Task;
        }

        LevelLoader.Instance.LoadNextLevel(nextScene);
    }

    void ApplyLanguage(int index)
    {
        if (index == 0) return;

        string code = index == 2 ? "pt-BR" : "en";
        var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;
        }
    }
}