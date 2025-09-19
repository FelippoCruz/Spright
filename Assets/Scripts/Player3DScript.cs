using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class Player3DScript : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] CharacterController controller;
    [SerializeField] float speed = 5f;
    [SerializeField] float jumpHeight = 2f;
    [SerializeField] float rotationSpeed = 10f;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float sensorRadius = 0.4f;
    [SerializeField] LayerMask groundMask;

    [Header("Combat")]
    [SerializeField] float damageAmount = 1f;
    [SerializeField] float attackDuration = 0.7f;
    [SerializeField] float attackRange = 2f;
    [SerializeField] float attackAngle = 120f;
    [SerializeField] Transform attackOrigin;
    [SerializeField] bool sweepRightToLeft = false;
    [SerializeField, Range(0f, 1f)] float attackMoveMultiplier = 0.3f;

    [Header("Lock-On")]
    [SerializeField] float lockOnRange = 15f;
    [SerializeField] CinemachineCamera virtualCamera;
    [SerializeField] CinemachineTargetGroup targetGroup;
    [SerializeField] float shoulderHeight = 1.5f;
    [SerializeField] float shoulderSideOffset = -0.3f;
    [SerializeField] float playerRadius = 2f;
    [SerializeField] float enemyRadius = 1.5f;

    [Header("Lock-On Symbol")]
    [SerializeField] GameObject lockOnSymbolPrefab;
    GameObject activeLockOnSymbol;

    [Header("Health")]
    [SerializeField] GameObject healthBarGO;
    [SerializeField] float maxHealth = 100f;

    [Header("Roll Settings")]
    [SerializeField] float rollSpeed = 8f;
    [SerializeField] float rollDuration = 0.4f;
    [SerializeField] float rollCooldown = 0.8f;
    [SerializeField] CinemachineImpulseSource rollImpulse;

    [Header("Crouch Settings")]
    [SerializeField, Range(0.1f, 1f)] float crouchSpeedMultiplier = 0.5f;
    [SerializeField] float crouchHeight = 1f;
    [SerializeField] float standHeight = 2f;
    [SerializeField] float crouchTransitionSpeed = 6f;
    [SerializeField] float ceilingCheckRadius = 0.3f;
    [SerializeField] float ceilingCheckDistance = 0.5f;
    [SerializeField] LayerMask ceilingMask;
    [SerializeField] float crouchCenterY = 0.5f;
    [SerializeField] float standCenterY = 1f;

    [Header("Run Settings")]
    [SerializeField] float runSpeedMultiplier = 1.8f;
    [SerializeField] float runDuration = 10f;
    [SerializeField] float runCooldown = 3f;
    [SerializeField] float runHoldThreshold = 1f;

    [Header("Ladder")]
    [SerializeField] float ladderDetectDistance = 0.4f;
    [SerializeField] LayerMask ladderMask;
    [SerializeField] float ClimbingSpeed = 3f;
    GameObject CurrentLadderPrompt;
    bool onLadder = false;
    Vector3 lastGrabLadderDirection = Vector3.zero;
    int ladderRegrabFrames = 0;
    int ladderDropCooldownFrames = 0;
    int chestMissFrames = 0;
    const int chestMissTolerance = 3; // In frames

    [Header("Slide")]
    [SerializeField] LayerMask slideMask; // layer do escorregador
    [SerializeField] float slideSpeed = 3f; // velocidade do deslize
    [SerializeField] float slideTriggerHeight = 13f; // altura máxima para começar a deslizar
    bool onSlide = false;
    int slideMissFrames = 0;
    const int maxSlideMissFrames = 10; // how many frames to tolerate before exiting

    [Header("Fall Damage")]
    [SerializeField] float fallDamageThreshold = 5f;     // Minimum fall distance to take damage
    [SerializeField] float maxFallDamage = 100f;         // Max possible damage
    [SerializeField] float maxFallDistance = 20f;        // Fall distance that causes max damage
    [SerializeField] CinemachineImpulseSource fallDamageImpulse;
    float highestYWhileFalling;
    bool isFalling = false;
    float lastYPosition;

    [Header("Healing")]
    [SerializeField] int HealingAmount;
    [SerializeField] int MaxHealUses;
    [SerializeField] TextMeshProUGUI HealText;
    int HealUses;

    // Internals
    float currentHealth;
    HealthBarScript healthBar;
    Vector3 velocity;
    bool isAttacking = false;
    float attackTimer = 0f;
    HashSet<Collider> alreadyHit = new HashSet<Collider>();
    float currentSweepAngle = 0f;
    Transform lockOnTarget;
    Transform shoulderProxy;
    bool isDead = false;

    // Rolling
    bool isRolling = false;
    float rollTimer = 0f;
    float rollCooldownTimer = 0f;
    Vector3 rollDirection;
    bool isInvincible = false;

    // Crouch
    bool isCrouching = false;
    float targetHeight;
    float targetCenterY;

    // Running
    bool isRunning = false;
    float runTimer = 0f;
    float runCooldownRemaining = 0f;

    // Shift input tracking
    bool shiftPressed = false;
    float shiftPressTimer = 0f;
    bool shiftUsedForRun = false;

    // Custom grounded flag
    bool myIsGrounded = false;

    public Animator currentAnimator;

    [SerializeField] GameObject Chatbox;

    void Awake()
    {
        Animator[] animators = GetComponentsInChildren<Animator>(true); // true = include inactive

        foreach (Animator anim in animators)
        {
            if (anim.gameObject.activeInHierarchy) // only pick the active one
            {
                currentAnimator = anim;
                anim.updateMode = AnimatorUpdateMode.UnscaledTime;
                Debug.Log("Active animator found: " + anim.gameObject.name);
                break;
            }
        }
        Time.timeScale = 1f;
        currentHealth = maxHealth;
        HealUses = MaxHealUses;

        if (healthBarGO != null)
            healthBar = healthBarGO.GetComponent<HealthBarScript>();
        if (healthBar != null)
            healthBar.UpdateHealth(currentHealth, maxHealth);

        GameObject proxy = new GameObject("ShoulderProxy");
        proxy.transform.SetParent(transform);
        proxy.transform.localPosition = new Vector3(shoulderSideOffset, shoulderHeight, 0f);
        shoulderProxy = proxy.transform;

        targetHeight = standHeight;
        targetCenterY = standCenterY;
        controller.height = standHeight;
        controller.center = new Vector3(0f, standCenterY, 0f);

        UpdateTargetGroup(null);
        if (currentAnimator != null)
        {
            currentAnimator.ResetTrigger("IsDead");
        }
    }

    void Update()
    {
        if (isDead) return;
        if (HealText != null) { HealText.text = HealUses.ToString(); }
        // Countdown ladder regrab cooldown frames at the start of Update
        if (ladderRegrabFrames > 0)
            ladderRegrabFrames = Mathf.Max(0, ladderRegrabFrames - 1);
        if (Time.timeScale == 0f)
        {
            // Force idle and skip fall logic
            if (currentAnimator != null)
                currentAnimator.Play("Idle");

            return;
        }

        DetectLadder();
        DetectSlide();
        if (!onLadder)
        {
            if (!onSlide)
            {
                HandleShiftInput();
            }
            HandleRoll();
            HandleCrouch();
            HandleFallTracking();
        }
        if (!isRolling)
        {
            if (Input.GetKeyDown(KeyCode.Q))
                ToggleLockOn();

            MoveCharacter();

            if (Input.GetMouseButtonDown(0) && !isAttacking && !onLadder && !onSlide)
                StartAttack();
        }
        else
        {
            RollMovement();
        }

        if (Input.GetKeyDown(KeyCode.R) && HealUses > 0)
        {
            Heal(HealingAmount);
            HealUses--;
        }

        if (isAttacking)
        {
            attackTimer -= Time.deltaTime;
            float progress = 1f - (attackTimer / attackDuration);
            PerformSweepingAttack(progress);
            if (attackTimer <= 0f)
            {
                isAttacking = false;
                if (currentAnimator != null) { currentAnimator.ResetTrigger("IsAttacking"); }
                alreadyHit.Clear();
            }
        }

        if (isRunning)
        {
            runTimer -= Time.deltaTime;
            if (runTimer <= 0f)
            {
                isRunning = false;
                if (currentAnimator != null) { currentAnimator.SetBool("IsRunning", false); }
                runCooldownRemaining = runCooldown;
            }
        }
        if (runCooldownRemaining > 0f)
            runCooldownRemaining -= Time.deltaTime;

        UpdateLockOnCamera();

        if (currentAnimator != null)
        {
            // Get state info from Base Layer (layer index 0)
            AnimatorStateInfo stateInfo = currentAnimator.GetCurrentAnimatorStateInfo(0);

            // Check if we are in the Idle state
            if (stateInfo.IsName("Idle"))
            {
                // Get the current float parameter
                float currentValue = currentAnimator.GetFloat("IdleTimer");

                // Add to it based on time
                currentValue += Time.deltaTime;

                // Update animator parameter
                currentAnimator.SetFloat("IdleTimer", currentValue);
            }
            else
            {
                // Optionally reset when leaving Idle
                currentAnimator.SetFloat("IdleTimer", 0f);
            }
        }

        if (Chatbox != null && Chatbox.activeSelf && Time.timeScale == 0)
        {
            currentAnimator.Play("Idle");
        }
        if (onLadder && !currentAnimator.GetBool("IsWalking"))
        {
            currentAnimator.speed = 0f;
        }
        else if (onLadder && currentAnimator.GetBool("IsWalking"))
        {
            currentAnimator.speed = 1f;
        }
    }

    public void SetCurrentHealth(float health)
    {
        currentHealth = health;
    }

    void HandleShiftInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            shiftPressed = true;
            shiftPressTimer = 0f;
            shiftUsedForRun = false;
        }

        if (shiftPressed)
        {
            shiftPressTimer += Time.deltaTime;
            if (shiftPressTimer >= runHoldThreshold && !shiftUsedForRun)
            {
                if (runCooldownRemaining <= 0f)
                {
                    isRunning = true;
                    if (currentAnimator != null) { currentAnimator.SetBool("IsRunning", true); }
                    runTimer = runDuration;
                }
                shiftUsedForRun = true;
            }
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            if (!shiftUsedForRun && shiftPressTimer < runHoldThreshold)
            {
                TryRoll();
            }

            if (isRunning)
            {
                isRunning = false;
                if (currentAnimator != null) { currentAnimator.SetBool("IsRunning", false); }
                runCooldownRemaining = runCooldown;
            }

            shiftPressed = false;
        }
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (isCrouching)
            {
                if (CanStandUp())
                {
                    isCrouching = false;
                    if (currentAnimator != null)
                    {
                        currentAnimator.ResetTrigger("IsCrouching");
                        currentAnimator.SetBool("IsCrouching 1", false);
                    }
                    targetHeight = standHeight;
                    targetCenterY = standCenterY;
                }
            }
            else
            {
                isCrouching = true;
                if (currentAnimator != null)
                {
                    currentAnimator.SetTrigger("IsCrouching");
                    currentAnimator.SetBool("IsCrouching 1", true);
                }
                targetHeight = crouchHeight;
                targetCenterY = crouchCenterY;
            }
        }

        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        Vector3 c = controller.center;
        c.y = Mathf.Lerp(c.y, targetCenterY, crouchTransitionSpeed * Time.deltaTime);
        controller.center = c;
    }

    bool CanStandUp()
    {
        Vector3 checkPos = transform.position + Vector3.up * (controller.height / 2f);
        return !Physics.SphereCast(checkPos, ceilingCheckRadius, Vector3.up, out RaycastHit hit, ceilingCheckDistance, ceilingMask);
    }

    void DetectLadder()
    {
        if (ladderRegrabFrames > 0)
        {
            if (CurrentLadderPrompt != null)
            {
                CurrentLadderPrompt.SetActive(false);
                CurrentLadderPrompt = null;
            }
            ladderRegrabFrames--;
            return;
        }

        Vector3 rayOrigin = controller.bounds.center;
        rayOrigin.y = controller.bounds.min.y + 0.1f; // feet level
        Debug.DrawRay(rayOrigin, transform.forward * ladderDetectDistance, Color.green);

        if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit hit, ladderDetectDistance, ladderMask))
        {
            if (hit.collider.CompareTag("Ladder"))
            {
                // Only show prompt if NOT already climbing
                if (!onLadder)
                {
                    Transform promptCanvas = hit.collider.transform.Find("LadderCanvas");
                    if (promptCanvas != null)
                    {
                        GameObject promptObj = promptCanvas.gameObject;

                        if (CurrentLadderPrompt != promptObj)
                        {
                            if (CurrentLadderPrompt != null)
                                CurrentLadderPrompt.SetActive(false);

                            promptObj.SetActive(true);
                            CurrentLadderPrompt = promptObj;
                        }
                    }
                }

                if (Input.GetKeyDown(KeyCode.E) && !onLadder)
                {
                    Debug.Log("[DetectLadder] GrabLadder called.");
                    GrabLadder();
                    lastGrabLadderDirection = -hit.normal;
                    return;
                }
            }
        }
        else
        {
            if (CurrentLadderPrompt != null)
            {
                CurrentLadderPrompt.SetActive(false);
                CurrentLadderPrompt = null;
            }

            // Only drop if on ladder and cooldown is over
            if (onLadder)
            {
                if (ladderDropCooldownFrames > 0)
                {
                    ladderDropCooldownFrames--;
                }
                else
                {
                    // Check if player is pressing down + below threshold
                    bool wantsDown = Input.GetKey(KeyCode.S);
                    if (wantsDown && transform.position.y <= 1.75f)
                    {
                        Debug.Log("[DetectLadder] Dropping ladder: going down at safe height.");
                        DropLadder(false); // exiting downward: no forward push
                    }
                    else
                    {
                        Debug.Log("[DetectLadder] Lost ladder forward raycast, dropping ladder normally.");
                        DropLadder(true); // exiting forward/top: push
                    }

                    ladderDropCooldownFrames = 5; // buffer before dropping again
                }
            }
        }
    }


    void GrabLadder()
    {
        if (!onLadder)
            Debug.Log("[GrabLadder] Entered ladder state");
        ShowLadderPrompt(false);
        lastGrabLadderDirection = transform.forward;
        onLadder = true;
        if (currentAnimator != null)
        {
            currentAnimator.SetTrigger("GrabbedLadder");
            currentAnimator.SetBool("IsClimbingLadder", true);
            currentAnimator.Play("Climbing Ladder");
        }
        velocity.y = 0f;
    }

    void DropLadder(bool pushForwardOnExit)
    {
        if (onLadder)
            Debug.Log("[DropLadder] Exiting ladder climb.");

        onLadder = false;
        if (currentAnimator != null)
        {
            currentAnimator.SetBool("IsClimbingLadder", false);
            currentAnimator.ResetTrigger("GrabbedLadder");
        }

        // Frame cooldown to prevent immediate regrab
        ladderRegrabFrames = 3;

        if (pushForwardOnExit)
        {
            // Push player up and forward a bit for smooth ladder exit
            Vector3 pushDir = lastGrabLadderDirection.normalized;
            pushDir.y = 0f; // horizontal push only
            float pushDistance = 1.3f;
            float pushUpwards = 1.2f;

            Vector3 pushVector = pushDir * pushDistance + Vector3.up * pushUpwards;
            controller.Move(pushVector);
        }
        else
        {
            float bottomThreshold = controller.bounds.min.y + 0.1f;
            if (transform.position.y <= bottomThreshold + 0.5f) // within 0.5 units of bottom
            {
                Debug.Log("[DropLadder] Exited ladder downward safely.");
                onLadder = false;
            }
            else
            {
                Debug.Log("[DropLadder] Tried to exit down, but not at bottom - ignoring.");
                return; // cancel exit, stay climbing
            }
        }
    }

    void ShowLadderPrompt(bool show)
    {
        if (CurrentLadderPrompt != null)
            CurrentLadderPrompt.SetActive(show);
    }

    void DetectSlide()
    {
        // se já está deslizando, não faz nada
        if (onSlide) return;

        // origem do raycast: próximo dos pés
        Vector3 feetPosition = controller.bounds.center;
        feetPosition.y = controller.bounds.min.y; // um pouco acima dos pés

        // Raycast para baixo para detectar escorregador
        if (Physics.Raycast(feetPosition, Vector3.down, out RaycastHit hit, 0.5f, slideMask))
        {
            // Se está abaixo da altura limite
            if (transform.position.y <= slideTriggerHeight)
            {
                Debug.Log("[SLIDE] Entrando no escorregador!");
                onSlide = true;
            }
        }
    }

    void MoveCharacter()
    {
        myIsGrounded = false;

        float x = Input.GetAxis("Horizontal1") * (OptionsManager.InvertX ? -1f : 1f);
        float z = Input.GetAxis("Vertical1") * (OptionsManager.InvertY ? -1f : 1f);

        Vector3 moveInput = new Vector3(x, 0f, z);
        Vector3 moveDirection = ConvertToCameraSpace(moveInput);
        if (currentAnimator != null)
        {
            if (moveInput != Vector3.zero)
            {
                currentAnimator.SetBool("IsWalking", true);
            }
            else
            {
                currentAnimator.SetBool("IsWalking", false);
            }
        }
        if (onLadder)
        {
            if (ladderDropCooldownFrames > 0)
                ladderDropCooldownFrames--;

            float vertical = Input.GetAxis("Vertical1");
            Debug.Log($"[LADDER] vertical={vertical}, moveDir={moveDirection}, lastGrabDir={lastGrabLadderDirection}");

            velocity.y = 0f;

            // Ladder drop check using player space input
            Vector3 localInputDir = (transform.forward * z) + (transform.right * x);
            localInputDir.y = 0f;

            if (localInputDir.sqrMagnitude > 0.001f)
            {
                localInputDir.Normalize();
                float dot = Vector3.Dot(localInputDir, lastGrabLadderDirection.normalized);
                Debug.Log($"[LADDER] Dot product (player space) = {dot}");

                // EXIT DOWNWARD
                if (dot < 0f && vertical < -0.01f && ladderDropCooldownFrames == 0)
                {
                    Vector3 feetPosition = controller.bounds.center;
                    feetPosition.y = controller.bounds.min.y; // just above feet
                    float ladderFloorDropDistance = 0.6f;

                    if (Physics.Raycast(feetPosition, Vector3.down, out RaycastHit floorHit, ladderFloorDropDistance, groundMask) && transform.position.y <= 3)
                    {
                        Debug.Log($"[LADDER] Floor detected below at {floorHit.point} on {floorHit.collider.name}, dropping ladder.");
                        DropLadder(false); // exiting downward: no forward push
                        ladderDropCooldownFrames = 5;
                        return;
                    }
                    else
                    {
                        Debug.Log($"[LADDER] Pressing down but no floor detected below. Origin={feetPosition}, dist={ladderFloorDropDistance}");
                    }
                }
            }

            // Ladder movement
            Vector3 ladderMove = new Vector3(moveDirection.x * 0.1f, vertical * ClimbingSpeed, moveDirection.z * 0.1f);
            controller.Move(ladderMove * Time.deltaTime);

            // Ladder presence check at chest level
            Vector3 checkOriginChest = controller.bounds.center;
            checkOriginChest.y = ((controller.bounds.min.y + controller.bounds.max.y) / 2f); // chest height

            bool goingDown = Input.GetAxis("Vertical1") < -0.01f;

            if (goingDown)
            {
                // Ignore chest ray when pressing down; rely only on feet logic
                chestMissFrames = 0; // reset since we don't care about chest
            }
            else
            {
                if (Physics.Raycast(checkOriginChest, transform.forward, out RaycastHit ladderHitChest, ladderDetectDistance, ladderMask))
                {
                    // Reset miss counter and rotate toward ladder
                    chestMissFrames = 0;

                    Vector3 lookDir = -ladderHitChest.normal;
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(lookDir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                    }
                }
                else
                {
                    // Count consecutive misses
                    chestMissFrames++;

                    if (chestMissFrames > chestMissTolerance && ladderDropCooldownFrames == 0)
                    {
                        Debug.Log("[LADDER] Chest lost ladder for several frames, dropping.");
                        DropLadder(true); // exiting forward/top
                        ladderDropCooldownFrames = 5;
                        chestMissFrames = 0; // reset
                    }
                }
            }

            return;
        }

        // Se estamos no escorregador, mover automaticamente e ignorar input
        if (onSlide)
        {
            currentAnimator.SetBool("IsOnSlide", true);
            // always push a bit down so we stick to the slide
            Vector3 slideWorldDir = (Vector3.right + Vector3.down * 3f).normalized;
            controller.Move(slideWorldDir * slideSpeed * Time.deltaTime);

            // Raycast from feet downward with a generous distance
            Vector3 feetPosition = controller.bounds.center;
            feetPosition.y = controller.bounds.min.y + 0.1f;
            bool stillOnSlide = Physics.Raycast(feetPosition, Vector3.down, out RaycastHit slideHit, 0.5f, slideMask);

            if (stillOnSlide)
            {
                slideMissFrames = 0; // reset grace period
            }
            else
            {
                slideMissFrames++;
                if (slideMissFrames >= maxSlideMissFrames)
                {
                    Debug.Log("[SLIDE] Saindo do escorregador após grace period.");
                    onSlide = false;
                    currentAnimator.SetBool("IsOnSlide", false);
                    slideMissFrames = 0;
                }
            }

            return; // ignore player input while sliding
        }

        // NORMAL MOVEMENT

        if (IsGrounded() && velocity.y < 0f)
            velocity.y = -2f;

        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            if (currentAnimator != null) { currentAnimator.SetTrigger("IsJumping"); }
        }

        velocity.y += Physics.gravity.y * Time.deltaTime;

        float moveSpeedMultiplier = isAttacking ? attackMoveMultiplier : 1f;
        if (isCrouching) moveSpeedMultiplier *= crouchSpeedMultiplier;
        if (isRunning) moveSpeedMultiplier *= runSpeedMultiplier;

        Vector3 finalMove = (moveDirection * speed * moveSpeedMultiplier) + new Vector3(0f, velocity.y, 0f);
        controller.Move(finalMove * Time.deltaTime);

        if (moveInput.magnitude > 0.1f && lockOnTarget == null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void HandleRoll()
    {
        if (rollCooldownTimer > 0f)
            rollCooldownTimer -= Time.deltaTime;
    }

    void TryRoll()
    {
        if (rollCooldownTimer > 0f || isRolling || !IsGrounded()) return;

        float x = Input.GetAxis("Horizontal1") * (OptionsManager.InvertX ? -1f : 1f);
        float z = Input.GetAxis("Vertical1") * (OptionsManager.InvertY ? -1f : 1f);
        Vector3 inputDir = new Vector3(x, 0, z);

        if (inputDir.sqrMagnitude <= 0.1f)
            inputDir = transform.forward;

        rollDirection = ConvertToCameraSpace(inputDir).normalized;
        isRolling = true;
        if (currentAnimator != null) { currentAnimator.SetTrigger("IsRolling"); }
        isInvincible = true;
        rollTimer = rollDuration;
        rollCooldownTimer = rollCooldown;
        if (rollImpulse != null)
            rollImpulse.GenerateImpulse();
    }

    void RollMovement()
    {
        rollTimer -= Time.deltaTime;

        // Horizontal roll movement
        Vector3 move = rollDirection * rollSpeed;

        // Keep grounded by applying gravity manually
        if (!controller.isGrounded)
        {
            move.y += Physics.gravity.y * Time.deltaTime;
        }
        else
        {
            move.y = -2f; // small downward force to "stick" to the ground
        }

        controller.Move(move * Time.deltaTime);

        // Rotate player toward roll direction
        if (rollDirection.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(rollDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // End roll
        if (rollTimer <= 0f)
        {
            isRolling = false;
            if (currentAnimator != null) { currentAnimator.ResetTrigger("IsRolling"); }
            isInvincible = false;
        }
    }

    bool IsGrounded()
    {
        return controller.isGrounded || myIsGrounded || Physics.CheckSphere(groundCheck.position, sensorRadius, groundMask);
    }

    Vector3 ConvertToCameraSpace(Vector3 input)
    {
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();
        return camForward * input.z + camRight * input.x;
    }

    void StartAttack()
    {
        isAttacking = true;
        if (currentAnimator != null) { currentAnimator.SetTrigger("IsAttacking"); }
        attackTimer = attackDuration;
        alreadyHit.Clear();
    }

    void PerformSweepingAttack(float progress)
    {
        currentSweepAngle = sweepRightToLeft
            ? Mathf.Lerp(attackAngle / 2f, -attackAngle / 2f, progress)
            : Mathf.Lerp(-attackAngle / 2f, attackAngle / 2f, progress);

        Vector3 direction = Quaternion.Euler(0, currentSweepAngle, 0) * transform.forward;
        Debug.DrawRay(attackOrigin.position, direction * attackRange, Color.red, 0.1f);

        Ray ray = new Ray(attackOrigin.position, direction);
        if (Physics.SphereCast(ray, 0.5f, out RaycastHit hit, attackRange))
        {
            Debug.Log("Attacc");
            if (!alreadyHit.Contains(hit.collider))
            {
                Debug.Log("Found somth");
                if (hit.transform.CompareTag("Enemy"))
                {
                    var enemy = hit.transform.GetComponent<EnemyScript>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(damageAmount);
                        alreadyHit.Add(hit.collider);
                    }
                }
                else if (hit.transform.CompareTag("EnemySpawner"))
                {
                    var spawner = hit.transform.GetComponent<EnemySpawner>();
                    if (spawner != null)
                    {
                        spawner.TakeDamage(Mathf.RoundToInt(damageAmount));
                        alreadyHit.Add(hit.collider);
                    }
                }
                else if (hit.transform.CompareTag("AttackWall"))
                {
                    Debug.Log("AttaccWall Identifid");
                    Destroy(hit.collider.gameObject);
                    alreadyHit.Add(hit.collider);
                }
            }
        }
    }

    void HandleFallTracking()
    {
        if (!controller.isGrounded || myIsGrounded)
        {
            if (!isFalling && !onLadder)
            {
                isFalling = true;
                if (currentAnimator != null) { currentAnimator.SetBool("IsFalling", true); }
                highestYWhileFalling = transform.position.y;
            }
            else
            {
                if (transform.position.y > highestYWhileFalling)
                    highestYWhileFalling = transform.position.y;
            }
        }
        else if (isFalling)
        {
            float fallDistance = highestYWhileFalling - transform.position.y;

            if (fallDistance > fallDamageThreshold)
            {
                float damageRatio = Mathf.Clamp01((fallDistance - fallDamageThreshold) / (maxFallDistance - fallDamageThreshold));
                float damage = damageRatio * maxFallDamage;
                if (fallDamageImpulse != null)
                {
                    float intensity = Mathf.Clamp01((fallDistance - fallDamageThreshold) / (maxFallDistance - fallDamageThreshold));

                    Vector3 impulseVelocity = Vector3.down * Mathf.Lerp(1f, 5f, intensity);
                    fallDamageImpulse.GenerateImpulse(impulseVelocity);
                }
                TakeDamage(damage);
            }

            isFalling = false;
            if (currentAnimator != null) { currentAnimator.SetBool("IsFalling", false); }
        }
    }

    public void TakeDamage(float dmg)
    {
        if (isDead) return;
        if (isInvincible) return;
        currentHealth -= dmg;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);
        if (currentHealth <= 0f) Die();
        else if (currentAnimator != null) { StartCoroutine(TakeDamageAnim()); }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);
    }

    public void SetHealUsesToMax() { HealUses = MaxHealUses; }
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;

    void Die()
    {
        if (isDead) return;
        isDead = true;
        if (currentAnimator != null)
        {
            currentAnimator.SetBool("IsFalling", false);
            currentAnimator.SetTrigger("IsDead");
        }
        Debug.Log("Player morreu!");
        GameManager.Instance.TriggerGameOver();
    }

    IEnumerator TakeDamageAnim()
    {
        currentAnimator.SetTrigger("DamageTaken");
        yield return new WaitForSeconds(1f);
        currentAnimator.ResetTrigger("DamageTaken");
    }
    void ToggleLockOn()
    {
        if (lockOnTarget != null)
        {
            lockOnTarget = null;
            UpdateTargetGroup(null);
            if (activeLockOnSymbol != null)
            {
                Destroy(activeLockOnSymbol);
                activeLockOnSymbol = null;
            }
            return;
        }

        Collider[] potentialTargets = Physics.OverlapSphere(transform.position, lockOnRange);
        Transform closest = null;
        float closestScreenDist = float.MaxValue;

        foreach (var col in potentialTargets)
        {
            if (!col.CompareTag("Enemy") && !col.CompareTag("EnemySpawner") && !col.CompareTag("AttackWall"))
                continue;

            Vector3 screenPoint = Camera.main.WorldToScreenPoint(col.transform.position);
            if (screenPoint.z < 0) continue;

            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            float dist = Vector2.Distance(screenCenter, new Vector2(screenPoint.x, screenPoint.y));

            if (dist < closestScreenDist)
            {
                closestScreenDist = dist;
                closest = col.transform;
            }
        }

        if (closest != null)
        {
            lockOnTarget = closest;
            UpdateTargetGroup(lockOnTarget);

            if (lockOnSymbolPrefab != null)
            {
                if (activeLockOnSymbol == null)
                    activeLockOnSymbol = Instantiate(lockOnSymbolPrefab);
                activeLockOnSymbol.SetActive(true);
            }
        }
    }

    void UpdateLockOnCamera()
    {
        if (lockOnTarget != null)
        {
            float distance = Vector3.Distance(transform.position, lockOnTarget.position);
            if (distance > lockOnRange || !lockOnTarget.gameObject.activeInHierarchy)
            {
                lockOnTarget = null;
                UpdateTargetGroup(null);
                if (activeLockOnSymbol != null)
                {
                    Destroy(activeLockOnSymbol);
                    activeLockOnSymbol = null;
                }
                return;
            }

            Vector3 toTarget = lockOnTarget.position - transform.position;
            toTarget.y = 0f;
            Quaternion targetRot = Quaternion.LookRotation(toTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            if (activeLockOnSymbol != null)
            {
                Vector3 mid = lockOnTarget.position + Vector3.up * enemyRadius;
                activeLockOnSymbol.transform.position = mid;
                activeLockOnSymbol.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    void UpdateTargetGroup(Transform enemy)
    {
        if (targetGroup == null) return;
        var newTargets = new List<CinemachineTargetGroup.Target>();
        newTargets.Add(new CinemachineTargetGroup.Target { Object = shoulderProxy, Weight = 1f, Radius = playerRadius });
        if (enemy != null)
        {
            newTargets.Add(new CinemachineTargetGroup.Target { Object = enemy, Weight = 1f, Radius = enemyRadius });
        }
        targetGroup.Targets = newTargets;
    }

    public void ClearLockOnIfTarget(Transform target)
    {
        if (lockOnTarget == target)
        {
            lockOnTarget = null;
            UpdateTargetGroup(null);
            if (activeLockOnSymbol != null)
            {
                Destroy(activeLockOnSymbol);
                activeLockOnSymbol = null;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (controller != null)
        {
            Gizmos.color = Color.blue;
            Vector3 worldCenter = transform.position + controller.center;
            Gizmos.DrawSphere(worldCenter, 0.05f);

            float halfHeight = Mathf.Max(controller.height * 0.5f, controller.radius);
            Vector3 top = worldCenter + Vector3.up * (halfHeight - controller.radius);
            Vector3 bottom = worldCenter - Vector3.up * (halfHeight - controller.radius);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(top, bottom);
            Gizmos.DrawWireSphere(top, controller.radius);
            Gizmos.DrawWireSphere(bottom, controller.radius);
        }

        if (attackOrigin != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackOrigin.position, attackRange);

            Vector3 forward = transform.forward;
            Vector3 leftLimit = Quaternion.Euler(0, -attackAngle / 2f, 0) * forward;
            Vector3 rightLimit = Quaternion.Euler(0, attackAngle / 2f, 0) * forward;
            Gizmos.DrawRay(attackOrigin.position, leftLimit * attackRange);
            Gizmos.DrawRay(attackOrigin.position, rightLimit * attackRange);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, sensorRadius);
        }
    }
}
