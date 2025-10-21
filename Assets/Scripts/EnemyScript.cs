using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] float repositionInterval = 3f; // how often they pick a new position
    [SerializeField] float repositionJitter = 1f;   // small random offset time variation
    private float nextRepositionTime = 0f;
    [SerializeField] float minFlankAngle = 60f;  // minimum angle away from player's forward to be considered a flank
    [SerializeField] float crowdingDistance = 2f; // if too close, they try to move elsewhere
    [SerializeField] float flankCheckInterval = 1.5f; // how often to check and possibly reposition
    private float nextFlankCheck = 0f;

    [Header("Health")]
    [SerializeField] float maxHealth = 100f;
    private float currentHealth;

    [Header("UI")]
    [SerializeField] Vector3 healthBarOffset = new Vector3(0, 2f, 0);

    [Header("Audio")]
    [SerializeField] AudioClip hitSound;
    [SerializeField] AudioClip deathSound;
    [SerializeField] AudioSource audioSource;

    Vector3[] patrolPoints = new Vector3[2];
    int patrolIndex = 0;
    float lastAttackTime = 0f;
    bool isDead = false;
    bool isCirclingRight = true;
    bool isWindingUp = false;
    [SerializeField] ParticleSystem bloodSplashParticle;
    public event System.Action OnDeath;

    private static List<EnemyScript> activeEnemies = new List<EnemyScript>();
    // Group coordination / attack queue
    private static List<EnemyScript> currentAttackers = new List<EnemyScript>();
    [SerializeField] int maxSimultaneousAttackers = 2;  // how many can attack at once
    [SerializeField] float swapAttackDelay = 1.2f;       // small delay after an attacker finishes
    [SerializeField] float waitBeforeAttack = 0.5f;      // how long queued enemy waits before trying to step up
    private bool isQueuedToAttack = false;


    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
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

        activeEnemies.Add(this);
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
                else if (distToPlayer <= attackRange)
                {
                    SwitchState(EnemyState.Attack);
                }
                else
                {
                    // If we're still far from the player, approach directly (so enemies don't politely walk to their spread point)
                    // Once near the circling radius, switch to spread/circle positioning.
                    float approachThreshold = circlingRadius * 1.2f; // how close before we stop charging and begin to spread/circle
                    if (distToPlayer > approachThreshold)
                    {
                        // go straight for the player to close distance fast
                        if (NavMesh.SamplePosition(player.position, out NavMeshHit phit, 1f, NavMesh.AllAreas))
                            agent.SetDestination(phit.position);
                        else
                            agent.SetDestination(player.position);
                    }
                    else
                    {
                        // we're close enough — use spread/circle positioning
                        SetSpreadDestination();
                    }
                }
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
        if (player == null || isDead || isWindingUp) return;

        FaceTarget(player.position);

        // If currently retreating, just continue circling/retreating
        if (isRetreating)
        {
            CirclingMovement();
            return;
        }

        // If not already among current attackers, check slot/queue
        if (!currentAttackers.Contains(this))
        {
            if (currentAttackers.Count >= maxSimultaneousAttackers)
            {
                // Too many attacking -> queue and circle
                if (!isQueuedToAttack)
                {
                    isQueuedToAttack = true;
                    StartCoroutine(WaitForTurn());
                }
                CirclingMovement();
                return;
            }
            else
            {
                // Take an attack slot
                currentAttackers.Add(this);
                isQueuedToAttack = false;
            }
        }

        // If ready to attack
        float timeSinceLastAttack = Time.time - lastAttackTime;
        if (timeSinceLastAttack >= attackCooldown)
        {
            isWindingUp = true;
            lastAttackTime = Time.time;
            StartCoroutine(AttackAfterDelay());
        }
        else
        {
            // Not ready — retreat a bit while cooling down
            if (!isRetreating)
            {
                StartCoroutine(RetreatFromPlayer());
            }
            CirclingMovement();
        }
    }

    IEnumerator WaitForTurn()
    {
        // Keep circling until a slot opens or we die
        while (currentAttackers.Count >= maxSimultaneousAttackers && !isDead)
        {
            CirclingMovement();
            yield return new WaitForSeconds(waitBeforeAttack + Random.Range(0f, 0.6f));
        }

        if (!isDead && !isWindingUp)
        {
            // take slot after a brief jitter
            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
            if (!currentAttackers.Contains(this))
                currentAttackers.Add(this);
            isQueuedToAttack = false;
        }
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

        anim.SetTrigger("IsAttacking");

        yield return new WaitForSeconds(attackWindup);

        if (!isDead && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            float damage = attackType == "Light" ? damageToPlayer : damageToPlayer * 1.5f;
            playerHealth?.TakeDamage(damage);
            Debug.Log($"Enemy dealt {damage} damage ({attackType})!");
        }

        // Heavy attack pause (breathing room)
        if (attackType == "Heavy")
        {
            agent.isStopped = true;
            anim.SetBool("IsIdle", true); // optional: uses an idle flag if your animator supports it
            yield return new WaitForSeconds(heavyAttackPause);
            anim.SetBool("IsIdle", false);
            agent.isStopped = false;
        }

        // Finished attack: release slot for other enemies
        isWindingUp = false;
        anim.ResetTrigger("IsAttacking");

        if (currentAttackers.Contains(this))
            currentAttackers.Remove(this);

        // small swap delay to let other queued enemies take over smoothly
        yield return new WaitForSeconds(swapAttackDelay);
    }

    void CirclingMovement()
    {
        if (player == null || agent == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (distToPlayer < crowdingDistance)
        {
            // Move slightly backward to make space
            Vector3 retreatDir = (transform.position - player.position).normalized;
            Vector3 retreatTarget = transform.position + retreatDir * 1.5f;

            if (NavMesh.SamplePosition(retreatTarget, out NavMeshHit retreatHit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(retreatHit.position);
            }
            return; // skip rest this frame
        }

        // Index for even spacing
        int index = activeEnemies.IndexOf(this);
        if (index < 0) index = 0;

        float angleOffset = (360f / Mathf.Max(activeEnemies.Count, 1)) * index;
        Vector3 offsetCircle = Quaternion.Euler(0, angleOffset, 0) * Vector3.forward * circlingRadius;

        // Reposition logic: occasionally shift to a new random offset
        if (Time.time >= nextRepositionTime)
        {
            randomOffset = new Vector3(
                Random.Range(-circlingOffset, circlingOffset),
                0,
                Random.Range(-circlingOffset, circlingOffset)
            );

            // Schedule next reposition
            nextRepositionTime = Time.time + repositionInterval + Random.Range(-repositionJitter, repositionJitter);
        }

        // Occasionally check for crowding and flank reposition
        if (Time.time >= nextFlankCheck)
        {
            Vector3 flankTarget = GetSmartFlankPosition();
            if (NavMesh.SamplePosition(flankTarget, out NavMeshHit flankHit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(flankHit.position);
                FaceTarget(player.position);
            }

            nextFlankCheck = Time.time + flankCheckInterval + Random.Range(-0.3f, 0.3f);
        }
        else
        {
            // keep normal circling movement between checks
            Vector3 target = player.position + offsetCircle + randomOffset;
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                FaceTarget(player.position);
            }
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

    void SetSpreadDestination()
    {
        if (player == null || agent == null) return;

        // Index in the active enemy list
        int index = activeEnemies.IndexOf(this);
        if (index < 0) index = 0;

        // Distribute enemies evenly in a circle
        float angleOffset = (360f / Mathf.Max(activeEnemies.Count, 1)) * index;
        float spreadRadius = Mathf.Max(circlingRadius, 2f); // how far each stands from player

        // Calculate target position in a circle around player
        Vector3 offsetCircle = Quaternion.Euler(0, angleOffset, 0) * Vector3.forward * spreadRadius;

        // Add a small personal random offset to avoid robotic symmetry
        Vector3 noise = new Vector3(
            Random.Range(-0.5f, 0.5f),
            0,
            Random.Range(-0.5f, 0.5f)
        );

        Vector3 target = player.position + offsetCircle + noise;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    Vector3 GetSmartFlankPosition()
    {
        if (player == null) return transform.position;

        // Calculate the direction from player to this enemy
        Vector3 toEnemy = (transform.position - player.position).normalized;

        // Calculate if too many enemies are in front of the player
        int frontCount = 0;
        foreach (var enemy in activeEnemies)
        {
            if (enemy == null || enemy == this) continue;

            Vector3 toOther = (enemy.transform.position - player.position).normalized;
            float angleToForward = Vector3.Angle(player.forward, toOther);

            // If within a forward cone, count as "in front"
            if (angleToForward < minFlankAngle) frontCount++;
        }

        // If too many in front, this one should move to flank
        if (frontCount >= Mathf.Max(2, activeEnemies.Count / 3))
        {
            // pick a flank angle (left or right randomly)
            float flankAngle = Random.value > 0.5f ? 90f : -90f;
            Vector3 flankDir = Quaternion.Euler(0, flankAngle, 0) * player.forward;
            Vector3 flankPos = player.position + flankDir * circlingRadius * 1.2f;

            if (NavMesh.SamplePosition(flankPos, out NavMeshHit flankHit, 2f, NavMesh.AllAreas))
            {
                return flankHit.position;
            }
        }

        // Default: normal circle position
        int index = activeEnemies.IndexOf(this);
        if (index < 0) index = 0;
        float angleOffset = (360f / Mathf.Max(activeEnemies.Count, 1)) * index;
        Vector3 offsetCircle = Quaternion.Euler(0, angleOffset, 0) * Vector3.forward * circlingRadius;
        Vector3 defaultPos = player.position + offsetCircle + randomOffset;

        return defaultPos;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        if (bloodSplashParticle != null) { bloodSplashParticle.Play(); }
        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);
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
        activeEnemies.Remove(this);
        currentAttackers.Remove(this);

        // Stop movement and set death state
        agent.isStopped = true;
        SwitchState(EnemyState.Dead);
        anim.SetTrigger("IsDead");

        // Play death sound
        float deathSoundDuration = 0f;
        if (deathSound != null && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(deathSound);
            deathSoundDuration = deathSound.length;
        }

        // Tell player to clear lock-on immediately
        Player3DScript playerScript = FindAnyObjectByType<Player3DScript>();
        if (playerScript != null)
        {
            playerScript.ClearLockOnIfTarget(transform);
        }

        // Notify spawner
        if (spawner != null)
        {
            spawner.Notify3DEnemyDied(this.gameObject);
        }

        Debug.Log("Enemy died.");
        OnDeath?.Invoke();

        // Wait for both animation and sound before destroying
        float destroyDelay = Mathf.Max(1.5f, deathSoundDuration);
        Destroy(gameObject, destroyDelay);
    }

    void OnDisable()
    {
        activeEnemies.Remove(this);
        currentAttackers.Remove(this);
    }

    void OnDestroy()
    {
        activeEnemies.Remove(this);
        currentAttackers.Remove(this);
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
                // Allow this enemy to attempt an immediate attack when entering the Attack state
                // (sets lastAttackTime so timeSinceLastAttack >= attackCooldown)
                lastAttackTime = Time.time - attackCooldown;
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
