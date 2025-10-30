using UnityEngine;
using System.Collections; // Required for using Coroutines

public class Enemy2DScript : MonoBehaviour
{
    enum EnemyState { Idle, Chase, Attack, Dead }
    EnemyState currentState = EnemyState.Idle;

    [Header("References")]
    [SerializeField] Transform player;
    // New: Reference to the enemy's renderer (MeshRenderer or SpriteRenderer)
    private Renderer enemyRenderer;
    private Color originalColor;

    [Header("Settings")]
    [SerializeField] float detectionRange = 15f;
    [SerializeField] float attackRange = 8f;
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float attackCooldown = 1.5f;

    [Header("Health & Damage Flash")]
    [SerializeField] float maxHealth = 5;
    float currentHealth;
    // New: Color to flash to when taking damage
    [SerializeField] Color damageFlashColor = Color.red;
    // New: How long the damage color flash lasts (in seconds)
    [SerializeField] float flashDuration = 0.15f;
    private Coroutine flashRoutine;


    [Header("Bullet")]
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform firePoint;

    float lastAttackTime = 0f;
    bool isDead = false;

    public event System.Action OnDeath;

    void Start()
    {
        // Get the Renderer from the first child only (skip parent)
        if (transform.childCount > 0)
        {
            enemyRenderer = transform.GetChild(0).GetComponent<Renderer>();
        }

        if (enemyRenderer == null)
        {
            Debug.LogError($"{name}: Enemy2DScript -> No Renderer found in first child!");
        }
        else
        {
            // Make it a unique instance material
            enemyRenderer.material = new Material(enemyRenderer.material);

            Material mat = enemyRenderer.material;

            if (mat.HasProperty("_BaseColor"))
            {
                originalColor = mat.GetColor("_BaseColor");
                Debug.Log($"{name}: Using _BaseColor for flash. Original={originalColor}");
            }
            else if (mat.HasProperty("_Color"))
            {
                originalColor = mat.GetColor("_Color");
                Debug.Log($"{name}: Using _Color for flash. Original={originalColor}");
            }
            else
            {
                originalColor = mat.color;
                Debug.LogWarning($"{name}: Material has no _BaseColor or _Color; using mat.color instead.");
            }
        }

        // Player auto-find
        if (!player)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject pObj in players)
            {
                if (pObj.layer == LayerMask.NameToLayer("P2"))
                {
                    player = pObj.transform;
                    break;
                }
            }
        }

        currentHealth = maxHealth;

        if (!player)
        {
            Debug.LogError("Enemy2DScript: No player found with tag Player.");
            enabled = false;
        }
    }

    void Update()
    {
        if (isDead) return;
        if (player == null) return;

        Vector3 enemyXZ = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 playerXZ = new Vector3(player.position.x, 0f, player.position.z);

        float distToPlayer = Vector3.Distance(enemyXZ, playerXZ);

        switch (currentState)
        {
            case EnemyState.Idle:
                if (distToPlayer <= detectionRange) SwitchState(EnemyState.Chase);
                break;

            case EnemyState.Chase:
                if (distToPlayer > detectionRange) SwitchState(EnemyState.Idle);
                else if (distToPlayer <= attackRange) SwitchState(EnemyState.Attack);
                else MoveTowardsPlayer(enemyXZ, playerXZ);
                break;

            case EnemyState.Attack:
                if (distToPlayer > attackRange)
                {
                    SwitchState(EnemyState.Chase);
                }
                else
                {
                    HandleAttack(enemyXZ, playerXZ);
                }
                break;
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth; // return whatever variable stores health
    }
    public void SetCurrentHealth(float health)
    {
        currentHealth = health;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    // Original TakeDamage method has been modified
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        // --- Damage Logic ---
        currentHealth -= amount;

        // --- Visual Feedback Logic (New) ---
        if (enemyRenderer != null)
        {
            // Stop any existing flash routine to prevent flickering issues
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }
            // Start the new flash routine
            flashRoutine = StartCoroutine(DamageFlash());
        }

        // --- Death Logic ---
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Coroutine to handle the damage flash effect.
    /// </summary>
    IEnumerator DamageFlash()
    {
        if (enemyRenderer == null) yield break;

        Material mat = enemyRenderer.material;

        // Detect which property to use
        bool hasBaseColor = mat.HasProperty("_BaseColor");
        bool hasColor = mat.HasProperty("_Color");

        // Flash
        if (hasBaseColor)
        {
            mat.SetColor("_BaseColor", damageFlashColor);
        }
        else if (hasColor)
        {
            mat.SetColor("_Color", damageFlashColor);
        }

        yield return new WaitForSeconds(flashDuration);

        // Revert
        if (hasBaseColor)
        {
            mat.SetColor("_BaseColor", originalColor);
        }
        else if (hasColor)
        {
            mat.SetColor("_Color", originalColor);
        }

        flashRoutine = null;
    }

    void MoveTowardsPlayer(Vector3 enemyXZ, Vector3 playerXZ)
    {
        Vector3 dir = (playerXZ - enemyXZ).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
        FacePlayer(enemyXZ, playerXZ);
    }

    void HandleAttack(Vector3 enemyXZ, Vector3 playerXZ)
    {
        FacePlayer(enemyXZ, playerXZ);

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            ShootBullet(playerXZ);
            lastAttackTime = Time.time;
        }
    }

    void ShootBullet(Vector3 playerXZ)
    {
        if (!bulletPrefab || !firePoint) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();

        if (bulletScript != null)
        {
            Vector3 dir3D = (playerXZ - new Vector3(firePoint.position.x, 0f, firePoint.position.z)).normalized;
            bulletScript.Initialize(dir3D);
        }
        else
        {
            Debug.LogWarning("Enemy2DScript: Bullet prefab has no EnemyBullet script!");
        }

        Debug.Log("Enemy2D: Fired a bullet!");
    }

    void FacePlayer(Vector3 enemyXZ, Vector3 playerXZ)
    {
        Vector3 dir = (playerXZ - enemyXZ).normalized;
        if (dir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        SwitchState(EnemyState.Dead);
        Debug.Log("Enemy2D died.");
        OnDeath?.Invoke();
        Destroy(gameObject, 0.1f);
    }

    void SwitchState(EnemyState state)
    {
        currentState = state;
    }
}
