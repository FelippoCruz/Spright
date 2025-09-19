using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyScript : MonoBehaviour
{
    enum EnemyState { Patrol, Chase, Attack, Dead }
    EnemyState currentState = EnemyState.Patrol;

    [Header("References")]
    [SerializeField] Transform player;
    [SerializeField] HealthBarScript healthBar;
    [SerializeField] EnemySpawner spawner;
    Animator anim;
    NavMeshAgent agent;
    private Player3DScript playerHealth;

    [Header("Settings")]
    [SerializeField] float detectionRange = 15f;
    [SerializeField] float attackRange = 3f;
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float patrolOffset = 5f;
    [SerializeField] float circlingRadius = 3f;
    [SerializeField] float attackCooldown = 2f;
    [SerializeField] float damageToPlayer = 10f;
    [SerializeField] float attackWindup = 0.8f;

    [Header("Health")]
    [SerializeField] float maxHealth = 100f;
    private float currentHealth;

    [Header("UI")]
    [SerializeField] Vector3 healthBarOffset = new Vector3(0, 2f, 0);

    Vector3[] patrolPoints = new Vector3[2];
    int patrolIndex = 0;
    float lastAttackTime = 0f;
    bool isDead = false;
    bool isCirclingRight = true;
    bool isWindingUp = false;

    public event System.Action OnDeath;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        isCirclingRight = Random.value > 0.5f;

        if (!player)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject pObj in players)
            {
                if (pObj.layer == LayerMask.NameToLayer("P1"))
                {
                    player = pObj.transform;
                    break;
                }
            }
        }

        if (player != null)
        {
            playerHealth = player.GetComponent<Player3DScript>();
        }

        currentHealth = maxHealth;

        Vector3 spawn = transform.position;
        patrolPoints[0] = spawn + Vector3.left * patrolOffset;
        patrolPoints[1] = spawn + Vector3.right * patrolOffset;

        SwitchState(EnemyState.Patrol);
    }

    void Update()
    {
        if (isDead) return;

        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Patrol:
                anim.SetBool("FightState", false);
                if (distToPlayer <= detectionRange) SwitchState(EnemyState.Chase);
                HandlePatrol();
                break;

            case EnemyState.Chase:
                anim.SetBool("FightState", true);
                if (distToPlayer > detectionRange) SwitchState(EnemyState.Patrol);
                else if (distToPlayer <= attackRange) SwitchState(EnemyState.Attack);
                else agent.SetDestination(player.position);
                break;

            case EnemyState.Attack:
                if (distToPlayer > attackRange) SwitchState(EnemyState.Chase);
                HandleAttack();
                break;
        }

        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
            healthBar.transform.position = transform.position + healthBarOffset;
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth; // return whatever variable stores health
    }
    public float GetMaxHealth() => maxHealth;

    public void SetCurrentHealth(float health)
    {
        currentHealth = health;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    void HandlePatrol()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.1f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[patrolIndex]);
        }
    }

    void HandleAttack()
    {
        FaceTarget(player.position);
        CirclingMovement();

        if (!isWindingUp && Time.time >= lastAttackTime + attackCooldown)
        {
            isWindingUp = true;
            lastAttackTime = Time.time;
            StartCoroutine(AttackAfterDelay());
        }
    }

    IEnumerator AttackAfterDelay()
    {
        anim.SetTrigger("IsAttacking");
        yield return new WaitForSeconds(attackWindup);

        if (!isDead && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            playerHealth?.TakeDamage(damageToPlayer);
            Debug.Log($"Enemy dealt {damageToPlayer} damage!");
        }

        isWindingUp = false;
        anim.ResetTrigger("IsAttacking");
    }

    void CirclingMovement()
    {
        Vector3 toPlayer = (transform.position - player.position).normalized;
        Vector3 circleDir = isCirclingRight ? Vector3.Cross(Vector3.up, toPlayer) : Vector3.Cross(toPlayer, Vector3.up);
        Vector3 target = player.position + toPlayer * circlingRadius + circleDir * circlingRadius;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 10f);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (anim != null) { StartCoroutine(TakeDamageAnim()); }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        agent.isStopped = true;
        SwitchState(EnemyState.Dead);
        anim.SetTrigger("IsDead");
        // Tell player to clear lock-on immediately
        Player3DScript playerScript = FindAnyObjectByType<Player3DScript>();
        if (playerScript != null)
        {
            playerScript.ClearLockOnIfTarget(transform);
        }

        if (spawner != null)
        {
            spawner.Notify3DEnemyDied(this.gameObject);
        }

        Debug.Log("Enemy died.");
        OnDeath?.Invoke();
        Destroy(gameObject, 1.5f); // Keep the object for animations before destroying
    }

    void SwitchState(EnemyState state)
    {
        currentState = state;

        switch (state)
        {
            case EnemyState.Patrol:
                agent.isStopped = false;
                agent.speed = moveSpeed;
                agent.SetDestination(patrolPoints[patrolIndex]);
                break;

            case EnemyState.Chase:
                agent.isStopped = false;
                agent.speed = moveSpeed;
                break;

            case EnemyState.Attack:
                agent.isStopped = false;
                break;

            case EnemyState.Dead:
                agent.isStopped = true;
                break;
        }
    }

    public void SetSpawner(EnemySpawner owner)
    {
        spawner = owner;
    }

    IEnumerator TakeDamageAnim()
    {
        anim.SetTrigger("DamageTaken");
        yield return new WaitForSeconds(1f);
        anim.ResetTrigger("DamageTaken");
    }
}
