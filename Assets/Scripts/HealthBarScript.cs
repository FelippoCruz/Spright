using UnityEngine;
using UnityEngine.UI;

public class HealthBarScript : MonoBehaviour
{
    [SerializeField] Slider HealthSlider;
    [SerializeField] Slider EaseHealthSlider;
    [SerializeField] float LerpSpeed = 0.05f;

    private float targetHealth = 0f;
    private float maxHealth = 100f;

    void Awake()
    {
        if (HealthSlider != null)
        {
            HealthSlider.maxValue = maxHealth;
            HealthSlider.value = maxHealth;
        }

        if (EaseHealthSlider != null)
        {
            EaseHealthSlider.maxValue = maxHealth;
            EaseHealthSlider.value = maxHealth;
        }

        targetHealth = maxHealth;
    }

    void Update()
    {
        if (EaseHealthSlider != null)
        {
            EaseHealthSlider.value = Mathf.Lerp(EaseHealthSlider.value, targetHealth, LerpSpeed);
        }
    }

    public void UpdateHealth(float current, float max)
    {
        maxHealth = max;
        targetHealth = current;

        if (HealthSlider != null)
        {
            HealthSlider.maxValue = max;
            HealthSlider.value = current;
        }

        if (EaseHealthSlider != null)
        {
            EaseHealthSlider.maxValue = max;
        }
    }
}
