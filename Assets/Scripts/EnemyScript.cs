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
    Player3DScript playerHealth;

    [Header("Settings")]
    [SerializeField] float detectionRange = 15f;
    [SerializeField] float attackRange = 3f;
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float patrolOffset = 5f;
    [SerializeField] float circlingRadius = 3f;
    [SerializeField] float attackCooldown = 2f;
    [SerializeField] float damageToPlayer = 10f;
    [SerializeField] float attackWindup = 0.8f;
    [SerializeField] float retreatDistance = 4f; // how far back the enemy retreats
    [SerializeField] float retreatDuration = 1f; // how long the enemy retreats
    bool isRetreating = false;
    [SerializeField] string[] attackPattern = { "Light", "Light", "Heavy" };
    private int currentAttackIndex = 0;
    [SerializeField] float heavyAttackPause = 0.7f; // seconds enemy pauses after heavy attack


    [Header("Circling Settings")]
    [SerializeField] float circlingOffset = 1f; // max random offset around player
    private Vector3 randomOffset; // unique offset for this enemy

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
    [SerializeField] ParticleSystem bloodSplashParticle;
    public event System.Action OnDeath;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        isCirclingRight = Random.value > 0.5f;

        randomOffset = new Vector3(
    Random.Range(-circlingOffset, circlingOffset),
    0,
    Random.Range(-circlingOffset, circlingOffset)
);
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

        if (bloodSplashParticle != null)
        {
            bloodSplashParticle.Stop();
        }
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

        if (!isWindingUp)
        {
            float timeSinceLastAttack = Time.time - lastAttackTime;

            if (timeSinceLastAttack >= attackCooldown)
            {
                // Start attack
                isWindingUp = true;
                lastAttackTime = Time.time;
                StartCoroutine(AttackAfterDelay());
            }
            else if (!isRetreating)
            {
                // Retreat if attack is on cooldown
                StartCoroutine(RetreatFromPlayer());
            }
        }

        CirclingMovement();
    }

    IEnumerator RetreatFromPlayer()
    {
        isRetreating = true;
        Vector3 retreatDir = (transform.position - player.position).normalized;
        Vector3 retreatTarget = transform.position + retreatDir * retreatDistance;

        if (NavMesh.SamplePosition(retreatTarget, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        yield return new WaitForSeconds(retreatDuration);
        isRetreating = false;
    }


    IEnumerator AttackAfterDelay()
    {
        string attackType = attackPattern[currentAttackIndex];
        currentAttackIndex = (currentAttackIndex + 1) % attackPattern.Length;

        //anim.SetTrigger(attackType == "Light" ? "LightAttack" : "HeavyAttack");
        anim.SetTrigger("IsAttacking");

        yield return new WaitForSeconds(attackWindup);

        if (!isDead && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            float damage = attackType == "Light" ? damageToPlayer : damageToPlayer * 1.5f;
            playerHealth?.TakeDamage(damage);
            Debug.Log($"Enemy dealt {damage} damage ({attackType})!");
        }

        if (attackType == "Heavy")
        {
            agent.isStopped = true; // stop movement
            yield return new WaitForSeconds(heavyAttackPause);
            agent.isStopped = false; // resume movement
        }

        isWindingUp = false;
        //anim.ResetTrigger(attackType == "Light" ? "LightAttack" : "HeavyAttack");
        anim.ResetTrigger("IsAttacking");
    }

    void CirclingMovement()
    {
        Vector3 toPlayer = (transform.position - player.position).normalized;
        Vector3 circleDir = isCirclingRight ? Vector3.Cross(Vector3.up, toPlayer) : Vector3.Cross(toPlayer, Vector3.up);
        Vector3 target = player.position + toPlayer * circlingRadius + circleDir * circlingRadius + randomOffset;

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

        if (bloodSplashParticle != null) { bloodSplashParticle.Play(); }
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
