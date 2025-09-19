using TMPro;
using UnityEngine;

public class Player2DScript : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] CharacterController CC;
    [SerializeField] float Speed2 = 5f;
    [SerializeField] float RotSpeed = 360f;

    [Header("Shooting")]
    [SerializeField] float BulletSpeed = 10f;
    [SerializeField] Transform BulletSpawn;
    [SerializeField] GameObject BulletPrefab;

    [Header("Audio")]
    [SerializeField] AudioClip BulletSound;

    [Header("Health")]
    [SerializeField] GameObject healthBarGO;
    [SerializeField] float maxHealth = 100f;

    [Header("Healing")]
    [SerializeField] int HealingAmount;
    [SerializeField] int MaxHealUses;
    [SerializeField] TextMeshProUGUI HealText;
    int HealUses;

    float currentHealth;
    private HealthBarScript healthBar;

    void Awake()
    {
        currentHealth = maxHealth;
        HealUses = MaxHealUses;

        if (healthBarGO != null)
        {
            healthBar = healthBarGO.GetComponent<HealthBarScript>();
            if (healthBar != null)
            {
                // atualiza UI inicial
                healthBar.UpdateHealth(currentHealth, maxHealth);
            }
            else
            {
                Debug.LogWarning("Player2DScript: healthBarGO does not have HealthBarScript component!");
            }
        }
        else
        {
            Debug.LogWarning("Player2DScript: healthBarGO is not assigned!");
        }
    }

    void Update()
    {
        if (HealText != null) { HealText.text = HealUses.ToString(); }

        HandleMovement();
        HandleShooting();

        if (Input.GetKeyDown(KeyCode.R) && HealUses > 0)
        {
            Heal(HealingAmount);
            HealUses--;
        }
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;

    public void SetCurrentHealth(float health)
    {
        currentHealth = health;
    }

    void HandleMovement()
    {
        // Get input
        float x = Input.GetAxis("Horizontal2") * (OptionsManager.InvertX ? -1f : 1f);
        float z = Input.GetAxis("Vertical2") * (OptionsManager.InvertY ? -1f : 1f);

        // -------- Movement --------
        // Move only forward/back relative to player facing
        if (Mathf.Abs(z) > 0.01f)
        {
            Vector3 forwardMove = transform.forward * z;
            CC.Move(forwardMove.normalized * Speed2 * Time.deltaTime);
        }

        // -------- Rotation --------
        // Rotate in place based on horizontal input (x)
        if (Mathf.Abs(x) > 0.01f)
        {
            float rotationAmount = x * RotSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, rotationAmount);
        }

        // -------- Diagonal movement fix --------
        // If both x and z are pressed, move forward/back normally and rotate based on x only.
        // This prevents "shaking" when going backwards and ensures natural diagonal motion.
        // No extra code needed; the above handles it correctly.
    }

    void HandleShooting()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            GameObject bullet = Instantiate(BulletPrefab, BulletSpawn.position, BulletSpawn.rotation);
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = BulletSpawn.forward * BulletSpeed;
            }

            if (BulletSound != null && GameManager.Instance != null && GameManager.Instance.AudioSource != null)
            {
                GameManager.Instance.AudioSource.PlayOneShot(BulletSound);
            }
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (healthBar != null)
            healthBar.UpdateHealth(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);
    }

    public void SetHealUsesToMax() { HealUses = MaxHealUses; }

    void Die()
    {
        Debug.Log("Player2D: Died!");
        GameManager.Instance.TriggerGameOver();
    }
}
