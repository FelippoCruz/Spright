using UnityEngine;

public class Enemy2DScript : MonoBehaviour
{
    enum EnemyState { Idle, Chase, Attack, Dead }
    EnemyState currentState = EnemyState.Idle;

    [Header("References")]
    [SerializeField] Transform player;

    [Header("Settings")]
    [SerializeField] float detectionRange = 15f;
    [SerializeField] float attackRange = 8f;
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float attackCooldown = 1.5f;

    [Header("Health")]
    [SerializeField] float maxHealth = 5;
    float currentHealth;

    [Header("Bullet")]
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform firePoint;

    float lastAttackTime = 0f;
    bool isDead = false;

    public event System.Action OnDeath;

    void Start()
    {
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
            Debug.LogError("Enemy2DScript: No player found in scene with tag Player.");
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

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
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
