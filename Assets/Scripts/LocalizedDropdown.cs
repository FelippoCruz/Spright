using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizedDropdown : MonoBehaviour
{
    [SerializeField] TMP_Dropdown dropdown;

    [Tooltip("One LocalizedString per option in the dropdown.")]
    public List<LocalizedString> localizedOptions = new List<LocalizedString>();

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += UpdateOptions;
        UpdateOptions(LocalizationSettings.SelectedLocale);
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= UpdateOptions;
    }

    void UpdateOptions(Locale locale)
    {
        dropdown.options.Clear();

        for (int i = 0; i < localizedOptions.Count; i++)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData("Loading…"));

            int index = i;

            localizedOptions[i].StringChanged += (localizedText) =>
            {
                if (index < dropdown.options.Count)
                {
                    dropdown.options[index].text = localizedText;
                    dropdown.RefreshShownValue();
                }
            };
            localizedOptions[i].RefreshString();
        }
    }

}
