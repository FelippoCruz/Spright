using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player2DScript : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] CharacterController CC;
    [SerializeField] float Speed2 = 5f;
    [SerializeField] float RotSpeed = 8f; // smaller = slower rotation

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

    // Input System
    private PlayerControls playerControls;
    private Vector2 moveInput;

    void Awake()
    {
        currentHealth = maxHealth;
        HealUses = MaxHealUses;

        // Instantiate PlayerControls
        playerControls = new PlayerControls();

        // Subscribe to Move2D input
        playerControls.Player.Move2D.performed += ctx =>
        {
            moveInput = ctx.ReadValue<Vector2>();
        };
        playerControls.Player.Move2D.canceled += ctx =>
        {
            moveInput = Vector2.zero;
        };

        // Health setup
        if (healthBarGO != null)
        {
            healthBar = healthBarGO.GetComponent<HealthBarScript>();
            if (healthBar != null)
            {
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

    void OnEnable()
    {
        playerControls.Enable();
    }

    void OnDisable()
    {
        playerControls.Disable();
    }

    void Update()
    {
        if (HealText != null)
            HealText.text = HealUses.ToString();

        HandleMovement();
        HandleShooting();

        if (Keyboard.current.rKey.wasPressedThisFrame && HealUses > 0)
        {
            Heal(HealingAmount);
            HealUses--;
        }
    }

    void HandleMovement()
    {
        // Apply inversion options
        float x = moveInput.x * (OptionsManager.InvertX ? -1f : 1f);
        float z = moveInput.y * (OptionsManager.InvertY ? -1f : 1f);

        // -------- Movement --------
        if (Mathf.Abs(z) > 0.01f)
        {
            Vector3 forwardMove = transform.forward * z;
            CC.Move(forwardMove.normalized * Speed2 * Time.deltaTime);
        }

        // -------- Smooth Rotation --------
        if (Mathf.Abs(x) > 0.01f)
        {
            // Target direction = current forward rotated by input X
            Quaternion targetRotation = Quaternion.Euler(0, x * 90f, 0) * transform.rotation;

            // Smoothly rotate
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * RotSpeed
            );
        }
    }

    void HandleShooting()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
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

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public void SetCurrentHealth(float health) => currentHealth = health;

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
        if (healthBar != null)
            healthBar.UpdateHealth(currentHealth, maxHealth);
    }

    public void SetHealUsesToMax() => HealUses = MaxHealUses;

    void Die()
    {
        Debug.Log("Player2D: Died!");
        GameManager.Instance.TriggerGameOver();
    }
}
