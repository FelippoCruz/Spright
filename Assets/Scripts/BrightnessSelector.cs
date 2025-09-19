using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Localization.Settings;
using System.Collections;

public class BrightnessSelector : MonoBehaviour
{
    [SerializeField] Slider brightnessSlider;
    [SerializeField] TextMeshProUGUI brightnessValueText;
    [SerializeField] Volume globalVolume;
    [SerializeField] string nextScene = "MechanicAllScene";

    const string KEY_BRIGHTNESS_VAL = "BrightnessValue";
    const string KEY_BRIGHTNESS_DEF = "BrightnessDefault";
    const string KEY_BRIGHTNESS_SET = "HasSetBrightness";

    ColorAdjustments colorAdj;
    LevelLoader LevelLoader;

    void Awake()
    {
        string savedLocale = PlayerPrefs.GetString("SelectedLocaleCode", "en");
        var loc = LocalizationSettings.AvailableLocales.GetLocale(savedLocale);
        if (loc) LocalizationSettings.SelectedLocale = loc;
    }

    void Start()
    {
        //For Resetting Brightness:
        //PlayerPrefs.DeleteKey("BrightnessValue");
        //PlayerPrefs.DeleteKey("HasSetBrightness");
        //PlayerPrefs.Save();

        brightnessSlider.minValue = -9f;
        brightnessSlider.maxValue = 1f;

        if (!PlayerPrefs.HasKey(KEY_BRIGHTNESS_DEF))
        {
            PlayerPrefs.SetFloat(KEY_BRIGHTNESS_DEF, 0f);
            PlayerPrefs.SetFloat(KEY_BRIGHTNESS_VAL, 0f);
            PlayerPrefs.Save();
        }

        if (PlayerPrefs.GetInt(KEY_BRIGHTNESS_SET, 0) == 1)
        {
            float saved = PlayerPrefs.GetFloat(KEY_BRIGHTNESS_VAL, 0f);

            if (globalVolume.profile.TryGet(out colorAdj))
                colorAdj.postExposure.value = saved;

            LevelLoader.Instance.LoadNextLevel(nextScene);
            return;
        }

        brightnessSlider.onValueChanged.AddListener(OnSliderChanged);
        StartCoroutine(SetSliderNextFrame());
    }

    IEnumerator SetSliderNextFrame()
    {
        yield return null;

        float val = PlayerPrefs.GetFloat(KEY_BRIGHTNESS_VAL,
                     PlayerPrefs.GetFloat(KEY_BRIGHTNESS_DEF, 0f));
        brightnessSlider.value = val;
        OnSliderChanged(val);
    }

    void OnSliderChanged(float v)
    {
        if (globalVolume.profile.TryGet(out colorAdj))
            colorAdj.postExposure.value = v;

        if (brightnessValueText)
            brightnessValueText.text = ((v + 9f) * 10f).ToString("F1");
    }

    public void ConfirmBrightness()
    {
        float v = brightnessSlider.value;
        PlayerPrefs.SetFloat(KEY_BRIGHTNESS_VAL, v);
        PlayerPrefs.SetInt(KEY_BRIGHTNESS_SET, 1);
        PlayerPrefs.Save();
        LevelLoader.Instance.LoadNextLevel(nextScene);
    }
}
