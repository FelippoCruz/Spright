using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

public class Player3DScript : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] CharacterController controller;
    [SerializeField] float speed = 5f;
    [SerializeField] float jumpHeight = 2f;
    [SerializeField] float rotationSpeed = 10f;
    float rotationSmoothVelocity;


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

    [Header("Attack Rate")]
    [SerializeField] float attackCooldown = 0.8f;
    private float attackCooldownTimer = 0f;
    [SerializeField] float comboCooldown = 0.3f; // cooldown between combo hits in seconds
    private float comboCooldownTimer = 0f;

    [Header("Lock-On")]
    [SerializeField] float lockOnRange = 15f;
    [SerializeField] CinemachineCamera virtualCamera;
    [SerializeField] CinemachineTargetGroup targetGroup;
    [SerializeField] float shoulderHeight = 1.5f;
    [SerializeField] float shoulderSideOffset = -0.3f;
    [SerializeField] float playerRadius = 2f;
    [SerializeField] float enemyRadius = 1.5f;
    [SerializeField] float switchSensitivity = 0.6f; // How much mouse input to trigger a switch
    [SerializeField] float switchCooldown = 0.4f;    // Delay before another switch allowed
    private float lastSwitchTime;
    [SerializeField] float orbitSpeedMultiplier = 1f; // tweak this: 1 = normal, 0.8 = slower orbit, 1.2 = faster

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

    [Header("Footstep SFX")]
    [SerializeField] private AudioSource walkAudio;
    [SerializeField] private AudioSource runAudio;

    [SerializeField] private float footstepIntervalWalk = 0.45f;
    [SerializeField] private float footstepIntervalRun = 0.32f;

    private float footstepTimer = 0f;

    [Header("Attack SFX Settings")]
    [SerializeField] private AudioSource attackAudioSource;

    // Slade sound list
    [SerializeField] private List<AudioClip> sladeAttackSounds = new();

    // Ophelia sound list
    [SerializeField] private List<AudioClip> opheliaAttackSounds = new();

    // The list that will actually be used
    private List<AudioClip> activeAttackSounds = new();

    [Header("Jump SFX")]
    [SerializeField] private AudioSource jumpAudioSource;
    [SerializeField] private AudioClip sladeJumpSound;
    [SerializeField] private AudioClip opheliaJumpSound;

    // Runtime-active jump sound depending on character
    private AudioClip activeJumpSound;

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
    float smoothVelocity = 0f;
    float velocitySmoothTime = 0.25f; // adjust for how snappy/gradual the blend should be
    float velocitySmoothSpeed = 0f;  // required ref param for SmoothDamp
    Vector3 currentMove = Vector3.zero; // smoothed movement vector
    bool JumpAnimation = false;
    // --- Combo System ---
    int currentComboStep = 0;
    float comboResetTime = 1.0f; // time to reset combo if player stops attacking
    float comboTimer = 0f;
    bool canReceiveNextInput = true;
    bool attackInputQueued = false; // <- New: stores queued attack input
    [SerializeField] float[] comboDamage; // damage per step
    [SerializeField] string[] comboAnimations; // animation triggers per step
    [SerializeField] private int maxComboSteps = 3; // how many attacks in combo
    [SerializeField] float[] comboSweepAngles; // optional

    // Rolling
    bool isRolling = false;
    float rollTimer = 0f;
    float rollCooldownTimer = 0f;
    Vector3 rollDirection;
    bool isInvincible = false;
    bool rollBuffered = false;       // Was roll pressed during attack
    bool attackBufferedDuringRoll = false; // Was attack pressed while rolling

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

    // Input System
    PlayerControls PlayerControls;
    Vector2 CurrentMovement;
    float VerticalInput;
    float HorizontalInput;
    bool MovementPressed;
    bool RunPressed;

    public Animator currentAnimator;

    [SerializeField] GameObject Chatbox;
    [SerializeField] ParticleSystem BloodSplashParticle;
    [SerializeField] ParticleSystem HealParticle;
    [SerializeField] ParticleSystem WalkParticle;

    private void OnEnable()
    {
        PlayerControls.Player.Enable();
    }

    private void OnDisable()
    {
        PlayerControls.Player.Disable();
    }
    void Awake()
    {
        Time.timeScale = 1f;
        currentHealth = maxHealth;
        HealUses = MaxHealUses;
        PlayerControls = new PlayerControls();
        PlayerControls.Player.Move3D.performed += ctx =>
        {
            CurrentMovement = ctx.ReadValue<Vector2>();
            MovementPressed = CurrentMovement.x != 0 || CurrentMovement.y != 0;
        };
        PlayerControls.Player.Move3D.canceled += ctx =>
        {
            CurrentMovement = Vector2.zero;
        };
        PlayerControls.Player.Sprint.performed += ctx =>
        {
            if (runCooldownRemaining <= 0f)
            {
                StartRun();
            }
        };

        PlayerControls.Player.Sprint.canceled += ctx =>
        {
            StopRun();
        };

        // --- Roll ---
        PlayerControls.Player.Roll.performed += ctx =>
        {
            TryRoll();
        };
        PlayerControls.Player.Jump.performed += ctx =>
        {
            if (IsGrounded())
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);

                // Play jump SFX
                if (jumpAudioSource != null && activeJumpSound != null)
                    jumpAudioSource.PlayOneShot(activeJumpSound);

                if (currentAnimator != null)
                    currentAnimator.SetTrigger("IsJumping");
            }
        };
        PlayerControls.Player.Crouch.performed += ctx => { /* No code needed, we use .triggered in Update */ };

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
        if (BloodSplashParticle != null)
        {
            BloodSplashParticle.Stop();
        }
        if (HealParticle != null)
        {
            HealParticle.Stop();
        }
        if (WalkParticle != null)
        {
            WalkParticle.Stop();
        }
    }

    void Start()
    {
        if (healthBar != null)
            healthBar.UpdateHealth(currentHealth, maxHealth);

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

        string playerName = PlayerPrefs.GetString("PlayerName", "Slade Sullivan");
        if (playerName == "Ophelia Sullivan")
        {
            activeAttackSounds = sladeAttackSounds;
            activeJumpSound = sladeJumpSound;
        }
        else
        {
            activeAttackSounds = opheliaAttackSounds;
            activeJumpSound = opheliaJumpSound;
        }

        // Safety fallback
        if (activeAttackSounds == null || activeAttackSounds.Count == 0)
            Debug.LogWarning("No attack sounds assigned for: " + playerName);

        if (activeJumpSound == null)
            Debug.LogWarning("No jump sound assigned for: " + playerName);
    }
    void Update()
    {
        if (isDead) return;
        if (HealText != null) { HealText.text = HealUses.ToString(); }
        if (healthBar != null)
            healthBar.UpdateHealth(currentHealth, maxHealth);
        // Countdown ladder regrab cooldown frames at the start of Update
        if (ladderRegrabFrames > 0)
            ladderRegrabFrames = Mathf.Max(0, ladderRegrabFrames - 1);
        if (Time.timeScale == 0f)
        {
            // Force idle and skip fall logic
            if (currentAnimator != null)
                currentAnimator.SetFloat("Velocity", 0f);
                currentAnimator.Play("Locomotion Blend Tree");

            return;
        }

        DetectLadder();
        DetectSlide();
        if (!onLadder)
        {
            //if (!onSlide)
            //{
            //    HandleShiftInput();
            //}
            HandleRoll();
            HandleCrouch();
            HandleFallTracking();
            HandleRunning();
        }
        if (!isRolling)
        {
            if (Input.GetKeyDown(KeyCode.Q))
                ToggleLockOn();

            MoveCharacter();

            if (Input.GetMouseButtonDown(0))
            {
                if (!isAttacking && !onLadder && !onSlide)
                {
                    // Prevent starting new attack if still in cooldown
                    if (attackCooldownTimer <= 0f)
                    {
                        StartAttack();
                        attackCooldownTimer = attackCooldown; // global cooldown
                    }
                }
                else if (isAttacking && comboCooldownTimer <= 0f)
                {
                    // Queue next combo if inside combo window
                    attackInputQueued = true;
                    comboCooldownTimer = comboCooldown; // prevent instant spam inside combo
                }
            }

            // Decrease cooldown timer
            if (attackCooldownTimer > 0f)
                attackCooldownTimer -= Time.deltaTime;

            if (comboCooldownTimer > 0f)
                comboCooldownTimer -= Time.deltaTime;
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
                if (currentAnimator != null)
                {
                    currentAnimator.ResetTrigger("IsAttacking");
                    // Reset all combo animation triggers
                    foreach (string anim in comboAnimations)
                    {
                        currentAnimator.ResetTrigger(anim);
                    }
                }
                alreadyHit.Clear();

                // --- Execute buffered roll ---
                if (rollBuffered)
                {
                    rollBuffered = false;
                    TryRoll();
                }

                // --- Process queued input ---
                if (attackInputQueued)
                {
                    attackInputQueued = false;
                    // force next attack immediately (bypass canReceiveNextInput guard)
                    StartAttack(true);
                }
            }
        }

        // --- Combo input reset ---
        // Decrease combo reset timer when we are gating inputs
        if (!canReceiveNextInput)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                currentComboStep = 0;
                canReceiveNextInput = true;
            }
        }

        // Always allow the next input as soon as the attack finished (avoid 'else if' trap)
        if (isAttacking && attackTimer <= 0f)
        {
            canReceiveNextInput = true;
        }

        if (isRunning)
        {
            runTimer -= Time.deltaTime;
            if (runTimer <= 0f)
            {
                isRunning = false;
                if (currentAnimator != null) currentAnimator.SetBool("IsRunning", false);
                runCooldownRemaining = runCooldown;
            }
        }

        if (runCooldownRemaining > 0f)
            runCooldownRemaining -= Time.deltaTime;

        UpdateLockOnCamera();

        if (currentAnimator != null)
        {
            if (!controller.isGrounded && !JumpAnimation)
            {
                currentAnimator.SetTrigger("StartJumpFall");
                JumpAnimation = true;
            }
            else if (controller.isGrounded)
            {
                JumpAnimation = false;
            }

            float velocityIdle = currentAnimator.GetFloat("Velocity");

            // Consider idle if velocity is very close to 0
            if (velocityIdle < 0.1f) // tolerance so tiny jitters don’t break idle
            {
                float currentValue = currentAnimator.GetFloat("IdleTimer");
                currentValue += Time.deltaTime;
                currentAnimator.SetFloat("IdleTimer", currentValue);
            }
            else
            {
                // Reset timer if not idle
                currentAnimator.SetFloat("IdleTimer", 0f);
            }
        }

        AnimatorStateInfo stateInfo = currentAnimator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("WaitingIdleAnimation")) // name of your waiting animation state
        {
            if (stateInfo.normalizedTime >= 1f) // finished once
            {
                currentAnimator.SetFloat("IdleTimer", 0f);
            }
        }

        if (Chatbox != null && Chatbox.activeSelf && Time.timeScale == 0)
        {
            currentAnimator.SetFloat("Velocity", 0f);
            currentAnimator.Play("Locomotion Blend Tree");
        }
        if (onLadder && !currentAnimator.GetBool("IsWalking"))
        {
            currentAnimator.speed = 0f;
        }
        else if (onLadder && currentAnimator.GetBool("IsWalking"))
        {
            currentAnimator.speed = 1f;
        }
        if(lockOnTarget != null)
        {
            CheckMouseDirectionForTargetSwitch();
        }
        HandleFootsteps();
    }

    public void SetCurrentHealth(float health)
    {
        currentHealth = health;
    }

    private void HandleRunning()
    {
        if (isRunning)
        {
            runTimer -= Time.deltaTime;
            if (runTimer <= 0f)
            {
                StopRun();
            }
        }

        if (runCooldownRemaining > 0f)
            runCooldownRemaining -= Time.deltaTime;
    }

    private void StartRun()
    {
        isRunning = true;
        runTimer = runDuration;
        if (currentAnimator != null) currentAnimator.SetBool("IsRunning", true);
    }

    private void StopRun()
    {
        if (isRunning)
        {
            isRunning = false;
            if (currentAnimator != null) currentAnimator.SetBool("IsRunning", false);
            runCooldownRemaining = runCooldown;
        }
    }

    //void HandleShiftInput()
    //{
    //    if (Input.GetKeyDown(KeyCode.LeftShift))
    //    {
    //        shiftPressed = true;
    //        shiftPressTimer = 0f;
    //        shiftUsedForRun = false;
    //    }

    //    if (shiftPressed)
    //    {
    //        shiftPressTimer += Time.deltaTime;
    //        if (shiftPressTimer >= runHoldThreshold && !shiftUsedForRun)
    //        {
    //            if (runCooldownRemaining <= 0f)
    //            {
    //                isRunning = true;
    //                if (currentAnimator != null) { currentAnimator.SetBool("IsRunning", true); }
    //                runTimer = runDuration;
    //            }
    //            shiftUsedForRun = true;
    //        }
    //    }

    //    if (Input.GetKeyUp(KeyCode.LeftShift))
    //    {
    //        if (!shiftUsedForRun && shiftPressTimer < runHoldThreshold)
    //        {
    //            TryRoll();
    //        }

    //        if (isRunning)
    //        {
    //            isRunning = false;
    //            if (currentAnimator != null) { currentAnimator.SetBool("IsRunning", false); }
    //            runCooldownRemaining = runCooldown;
    //        }

    //        shiftPressed = false;
    //    }
    //}

    void HandleCrouch()
    {
        if (PlayerControls.Player.Crouch.triggered)
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

    private void HandleFootsteps()
    {
        // Do not play footsteps if player is dead, attacking, rolling, sliding, on ladder, etc.
        if (isDead || isRolling || onSlide || onLadder)
        {
            StopAllFootsteps();
            return;
        }

        // Player must be grounded
        if (!controller.isGrounded)
        {
            StopAllFootsteps();
            return;
        }

        bool isWalking = currentAnimator != null && currentAnimator.GetBool("IsWalking");
        bool isRunningNow = isRunning;

        // If player is not walking nor running -> stop sounds
        if (!isWalking && !isRunningNow)
        {
            StopAllFootsteps();
            return;
        }

        footstepTimer -= Time.deltaTime;

        // --- RUNNING FOOTSTEPS ---
        if (isRunningNow)
        {
            // Stop walk sound if it was playing
            if (walkAudio != null && walkAudio.isPlaying)
                walkAudio.Stop();

            if (footstepTimer <= 0f)
            {
                if (runAudio != null)
                {
                    runAudio.Stop(); // ensure no overlap
                    runAudio.Play();
                }

                footstepTimer = footstepIntervalRun;
            }

            return;
        }

        // --- WALKING FOOTSTEPS ---
        if (isWalking)
        {
            // Stop run sound if it was playing
            if (runAudio != null && runAudio.isPlaying)
                runAudio.Stop();

            if (footstepTimer <= 0f)
            {
                if (walkAudio != null)
                {
                    walkAudio.Stop(); // ensure no overlap
                    walkAudio.Play();
                }

                footstepTimer = footstepIntervalWalk;
            }
        }
    }

    private void StopAllFootsteps()
    {
        footstepTimer = 0f;

        if (walkAudio != null && walkAudio.isPlaying)
            walkAudio.Stop();

        if (runAudio != null && runAudio.isPlaying)
            runAudio.Stop();
    }

    void MoveCharacter()
    {
        myIsGrounded = false;

        // Read movement input from New Input System
        float x = CurrentMovement.x * (OptionsManager.InvertX ? -1f : 1f);
        float z = CurrentMovement.y * (OptionsManager.InvertY ? -1f : 1f);

        Vector3 moveInput = new Vector3(x, 0f, z);
        Vector3 moveDirection = ConvertToCameraSpace(moveInput);

        // When locked on, adjust movement to orbit around enemy
        if (lockOnTarget != null)
        {
            Vector3 toEnemy = (lockOnTarget.position - transform.position).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, toEnemy);

            // Build orbit movement vector: forward/back moves toward/away from enemy, strafe moves perpendicular
            Vector3 orbitMove = (right * CurrentMovement.x + toEnemy * CurrentMovement.y);

            // Keep smooth magnitude for diagonal movement
            if (orbitMove.sqrMagnitude > 1f)
                orbitMove.Normalize();

            moveDirection = orbitMove;
            moveDirection *= orbitSpeedMultiplier;

        }

        if (currentAnimator != null)
            currentAnimator.SetBool("IsWalking", moveInput.magnitude > 0.1f);

        // ---------------- LADDER ----------------
        if (onLadder)
        {
            if (ladderDropCooldownFrames > 0)
                ladderDropCooldownFrames--;

            float vertical = CurrentMovement.y; // new input system vertical

            velocity.y = 0f;

            // Ladder drop check using player space input
            Vector3 localInputDir = (transform.forward * z) + (transform.right * x);
            localInputDir.y = 0f;

            if (localInputDir.sqrMagnitude > 0.001f)
            {
                localInputDir.Normalize();
                float dot = Vector3.Dot(localInputDir, lastGrabLadderDirection.normalized);

                if (dot < 0f && vertical < -0.01f && ladderDropCooldownFrames == 0)
                {
                    Vector3 feetPosition = controller.bounds.center;
                    feetPosition.y = controller.bounds.min.y;
                    float ladderFloorDropDistance = 0.6f;

                    if (Physics.Raycast(feetPosition, Vector3.down, out RaycastHit floorHit, ladderFloorDropDistance, groundMask) && transform.position.y <= 3)
                    {
                        DropLadder(false);
                        ladderDropCooldownFrames = 5;
                        return;
                    }
                }
            }

            // Ladder movement
            Vector3 ladderMove = new Vector3(moveDirection.x * 0.1f, vertical * ClimbingSpeed, moveDirection.z * 0.1f);
            controller.Move(ladderMove * Time.deltaTime);

            // Ladder chest detection
            Vector3 checkOriginChest = controller.bounds.center;
            checkOriginChest.y = ((controller.bounds.min.y + controller.bounds.max.y) / 2f);

            bool goingDown = vertical < -0.01f;

            if (!goingDown)
            {
                if (Physics.Raycast(checkOriginChest, transform.forward, out RaycastHit ladderHitChest, ladderDetectDistance, ladderMask))
                {
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
                    chestMissFrames++;
                    if (chestMissFrames > chestMissTolerance && ladderDropCooldownFrames == 0)
                    {
                        DropLadder(true);
                        ladderDropCooldownFrames = 5;
                        chestMissFrames = 0;
                    }
                }
            }
            else
            {
                chestMissFrames = 0;
            }

            return;
        }

        // ---------------- SLIDE ----------------
        if (onSlide)
        {
            currentAnimator.SetBool("IsOnSlide", true);
            Vector3 slideWorldDir = (Vector3.right + Vector3.down * 3f).normalized;
            controller.Move(slideWorldDir * slideSpeed * Time.deltaTime);

            Vector3 feetPosition = controller.bounds.center;
            feetPosition.y = controller.bounds.min.y + 0.1f;
            bool stillOnSlide = Physics.Raycast(feetPosition, Vector3.down, out RaycastHit slideHit, 0.5f, slideMask);

            if (stillOnSlide)
            {
                slideMissFrames = 0;
            }
            else
            {
                slideMissFrames++;
                if (slideMissFrames >= maxSlideMissFrames)
                {
                    onSlide = false;
                    currentAnimator.SetBool("IsOnSlide", false);
                    slideMissFrames = 0;
                }
            }

            return;
        }

        // ---------------- NORMAL MOVEMENT ----------------
        if (IsGrounded() && velocity.y < 0f)
            velocity.y = -2f;

        // Jump now handled in input callback -> no GetButtonDown here

        velocity.y += Physics.gravity.y * Time.deltaTime;

        float moveSpeedMultiplier = isAttacking ? attackMoveMultiplier : 1f;
        if (isCrouching) moveSpeedMultiplier *= crouchSpeedMultiplier;
        if (isRunning) moveSpeedMultiplier *= runSpeedMultiplier;

        Vector3 targetMove = moveDirection * speed * moveSpeedMultiplier;
        currentMove = Vector3.Lerp(currentMove, targetMove, Time.deltaTime * 3f);

        if (WalkParticle != null && currentAnimator != null) { if (currentAnimator.GetBool("IsWalking")) WalkParticle.Play(); else WalkParticle.Stop(); }
        Vector3 finalMove = currentMove + new Vector3(0f, velocity.y, 0f);
        controller.Move(finalMove * Time.deltaTime);

        float verticalVelocity = controller.velocity.y;
        if (currentAnimator != null)
            currentAnimator.SetFloat("VerticalVelocity", verticalVelocity);

        float rawVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
        smoothVelocity = Mathf.SmoothDamp(smoothVelocity, rawVelocity, ref velocitySmoothSpeed, velocitySmoothTime);

        if (currentAnimator != null)
            currentAnimator.SetFloat("Velocity", smoothVelocity);

        // ---------------- ADVANCED SMOOTH ROTATION ----------------
        Vector3 targetDirection;

        if (lockOnTarget != null)
        {
            // Face lock-on target only
            targetDirection = (lockOnTarget.position - transform.position).normalized;
            targetDirection.y = 0f; // ignore vertical
        }
        else
        {
            // Normal movement-based rotation
            targetDirection = moveDirection;
        }

        if (targetDirection.sqrMagnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref rotationSmoothVelocity,
                0.15f // rotation smooth time
            );

            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
        }
    }

    void HandleRoll()
    {
        if (rollCooldownTimer > 0f)
            rollCooldownTimer -= Time.deltaTime;
    }

    void TryRoll()
    {
        // If attacking, buffer the roll instead of canceling
        if (isAttacking)
        {
            rollBuffered = true;
            return;
        }

        // Otherwise, do normal roll
        if (rollCooldownTimer > 0f || isRolling || !IsGrounded()) return;

        // --- CANCEL ATTACK ---
        CancelAttack();

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

            // --- Execute buffered attack ---
            if (attackBufferedDuringRoll)
            {
                attackBufferedDuringRoll = false;
                StartAttack();
            }

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

    private void PlayRandomAttackSound()
    {
        if (attackAudioSource == null) return;
        if (activeAttackSounds == null || activeAttackSounds.Count == 0) return;

        int index = UnityEngine.Random.Range(0, activeAttackSounds.Count);
        AudioClip chosen = activeAttackSounds[index];

        attackAudioSource.clip = chosen;
        attackAudioSource.Play();
    }

    // Allow forcing the start (used when we process a queued input immediately after an attack)
    void StartAttack(bool force = false)
    {
        // If we are not allowed to receive the next input and this is not a forced start, bail out
        if (!canReceiveNextInput && !force) return;

        // --- Prevent starting new attack if cooldown not ready (unless forced) ---
        if (attackCooldownTimer > 0f && !force)
            return;

        isAttacking = true;
        attackTimer = attackDuration;
        alreadyHit.Clear();
        PlayRandomAttackSound();

        // --- Play combo animation ---
        if (currentAnimator != null && comboAnimations != null && comboAnimations.Length > 0)
        {
            int animIndex = Mathf.Clamp(currentComboStep, 0, comboAnimations.Length - 1);
            string animTrigger = comboAnimations[animIndex];
            currentAnimator.SetTrigger(animTrigger);
        }

        // --- Set damage for this step ---
        if (comboDamage != null && comboDamage.Length > 0)
        {
            int dmgIndex = Mathf.Clamp(currentComboStep, 0, comboDamage.Length - 1);
            damageAmount = comboDamage[dmgIndex];
        }

        // --- Handle combo progression ---
        canReceiveNextInput = false;
        comboTimer = comboResetTime;

        // --- Apply cooldown (only if this is not a forced combo continuation) ---
        if (!force)
            attackCooldownTimer = attackCooldown;
    }

    void PerformSweepingAttack(float progress)
    {
        if (comboDamage == null || comboDamage.Length == 0) return;

        // --- Interrupt combo if rolling ---
        if (isRolling)
        {
            isAttacking = false;
            canReceiveNextInput = true; // allow next attack after roll
            alreadyHit.Clear();
            currentComboStep = 0;
            attackBufferedDuringRoll = true; // Buffer attack to happen after roll
            return;
        }

        // Sweep angle for the attack
        currentSweepAngle = sweepRightToLeft
            ? Mathf.Lerp(attackAngle / 2f, -attackAngle / 2f, progress)
            : Mathf.Lerp(-attackAngle / 2f, attackAngle / 2f, progress);

        Vector3 direction = Quaternion.Euler(0, currentSweepAngle, 0) * transform.forward;
        Debug.DrawRay(attackOrigin.position, direction * attackRange, Color.red, 0.1f);

        Ray ray = new Ray(attackOrigin.position, direction);
        if (Physics.SphereCast(ray, 0.5f, out RaycastHit hit, attackRange))
        {
            if (!alreadyHit.Contains(hit.collider))
            {
                float dmg = comboDamage[Mathf.Clamp(currentComboStep, 0, comboDamage.Length - 1)];

                if (hit.transform.CompareTag("Enemy"))
                {
                    var enemy = hit.transform.GetComponent<EnemyScript>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(dmg);
                        alreadyHit.Add(hit.collider);
                    }
                }
                else if (hit.transform.CompareTag("EnemySpawner"))
                {
                    var spawner = hit.transform.GetComponent<EnemySpawner>();
                    if (spawner != null)
                    {
                        spawner.TakeDamage(Mathf.RoundToInt(dmg));
                        alreadyHit.Add(hit.collider);
                    }
                }
                else if (hit.transform.CompareTag("AttackWall"))
                {
                    Destroy(hit.collider.gameObject);
                    alreadyHit.Add(hit.collider);
                }
            }
        }

        // Move to next combo step if attack completed
        if (progress >= 1f)
        {
            currentComboStep++;
            if (currentComboStep >= maxComboSteps)
            {
                currentComboStep = 0; // reset combo
            }
            isAttacking = false; // allow next attack input
        }
    }

    void CancelAttack()
    {
        if (!isAttacking) return;

        isAttacking = false;
        alreadyHit.Clear();
        currentComboStep = 0;
        comboTimer = 0f;
        canReceiveNextInput = true;
        rollBuffered = false; // clear roll buffer if attack canceled

        if (currentAnimator != null)
        {
            currentAnimator.ResetTrigger("IsAttacking");

            // Reset all combo animation triggers
            foreach (string anim in comboAnimations)
            {
                currentAnimator.ResetTrigger(anim);
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
        if (BloodSplashParticle != null) { BloodSplashParticle.Play(); }
        currentHealth -= dmg;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);
        if (currentHealth <= 0f) Die();
        else if (currentAnimator != null) { StartCoroutine(TakeDamageAnim()); }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        if (HealParticle != null) { HealParticle.Play(); }
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
        // If already locked on, clear it
        if (lockOnTarget != null)
        {
            ClearLockOn();
            return;
        }

        // Try to find nearest valid enemy
        Transform newTarget = FindNearestLockOnTarget();

        if (newTarget != null)
        {
            lockOnTarget = newTarget;
            UpdateTargetGroup(lockOnTarget);
            StartCoroutine(SmoothLockSwitch(newTarget));

            // Show the lock-on symbol
            if (lockOnSymbolPrefab != null)
            {
                if (activeLockOnSymbol == null)
                {
                    activeLockOnSymbol = Instantiate(lockOnSymbolPrefab, newTarget.position + Vector3.up * 2f, Quaternion.identity);
                    activeLockOnSymbol.transform.SetParent(newTarget);
                }
                else
                    activeLockOnSymbol.transform.position = lockOnTarget.position + Vector3.up * 2f;

                activeLockOnSymbol.transform.SetParent(lockOnTarget);
            }
        }
        else
        {
            ClearLockOn();
        }
    }

    Transform FindNearestLockOnTarget()
    {
        Camera cam = Camera.main;
        if (cam == null) return null;

        Collider[] hits = Physics.OverlapSphere(transform.position, lockOnRange);
        Transform bestTarget = null;

        float bestScore = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy") && !hit.CompareTag("EnemySpawner")) continue;

            if (hit.CompareTag("Enemy"))
            {
                EnemyScript enemy = hit.GetComponent<EnemyScript>();
                if (enemy == null || enemy.IsDead()) continue;
            }
            else if (hit.CompareTag("EnemySpawner"))
            {
                EnemySpawner Spawner = hit.GetComponent<EnemySpawner>();
                if (Spawner == null || !Spawner.isActive) continue;
            }
            else continue;

            Vector3 dirToEnemy = (hit.transform.position - cam.transform.position).normalized;
            float angleFromCamera = Vector3.Angle(cam.transform.forward, dirToEnemy);
            float distance = Vector3.Distance(transform.position, hit.transform.position);

            // Weighted score: prioritize small angle, then distance
            float score = angleFromCamera * 1.5f + distance * 0.5f;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = hit.transform;
            }
        }

        return bestTarget;
    }

    void CheckMouseDirectionForTargetSwitch()
    {
        // Use your existing lockOnTarget as the guard
        if (lockOnTarget == null) return;
        if (Time.time < lastSwitchTime + switchCooldown) return;

        float mouseX = Input.GetAxis("Mouse X");
        if (Mathf.Abs(mouseX) < switchSensitivity) return;

        int direction = mouseX > 0 ? 1 : -1; // right = +1, left = -1

        // Collect potential targets
        Collider[] hits = Physics.OverlapSphere(transform.position, lockOnRange);
        Transform bestTarget = null;
        float bestAngle = 360f;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            if (hit.transform == lockOnTarget) continue;

            EnemyScript enemy = hit.GetComponent<EnemyScript>();
            if (enemy == null || enemy.IsDead()) continue;

            Vector3 toEnemy = (hit.transform.position - transform.position).normalized;
            Vector3 toCurrent = (lockOnTarget.position - transform.position).normalized;

            // Flatten
            toEnemy.y = 0f;
            toCurrent.y = 0f;

            // Signed angle from current to candidate around up axis
            float signedAngle = Vector3.SignedAngle(toCurrent, toEnemy, Vector3.up);

            // Candidate must be roughly in the flick direction and within a reasonable cone
            if ((direction > 0 && signedAngle > 10f && signedAngle < 120f) ||
                (direction < 0 && signedAngle < -10f && signedAngle > -120f))
            {
                float absAngle = Mathf.Abs(signedAngle);
                if (absAngle < bestAngle)
                {
                    bestAngle = absAngle;
                    bestTarget = hit.transform;
                }
            }
        }

        if (bestTarget != null)
        {
            // Switch lock-on
            lockOnTarget = bestTarget;
            UpdateTargetGroup(lockOnTarget);

            // Reparent / move the symbol to the new target if present
            if (activeLockOnSymbol != null)
            {
                activeLockOnSymbol.transform.SetParent(lockOnTarget, false);
                activeLockOnSymbol.transform.localPosition = new Vector3(0f, enemyRadius + 1.0f, 0f);
                activeLockOnSymbol.SetActive(true);
            }

            lastSwitchTime = Time.time;
        }
    }

    void UpdateLockOnCamera()
    {
        if (lockOnTarget == null)
        {
            if (activeLockOnSymbol != null)
            {
                Destroy(activeLockOnSymbol);
                activeLockOnSymbol = null;
            }
            UpdateTargetGroup(null);
            return;
        }

        // Check if current target is dead or destroyed
        EnemyScript enemy = lockOnTarget.GetComponent<EnemyScript>();
        if (enemy == null || enemy.IsDead())
        {
            // Try to find a replacement target automatically
            Transform newTarget = FindNearestLockOnTarget();

            if (newTarget != null && newTarget != lockOnTarget)
            {
                StartCoroutine(SmoothLockSwitch(newTarget));
                UpdateTargetGroup(lockOnTarget);

                // Update or recreate the lock-on symbol
                if (lockOnSymbolPrefab != null)
                {
                    if (activeLockOnSymbol == null)
                        activeLockOnSymbol = Instantiate(lockOnSymbolPrefab, lockOnTarget);
                    activeLockOnSymbol.transform.localPosition = new Vector3(0, 2.5f, 0);
                }
            }
            else
            {
                // No other enemy nearby -> unlock
                ClearLockOn();
                return;
            }
        }

        // Keep the camera stable on both player and target
        if (targetGroup != null)
        {
            targetGroup.Targets[0].Object = shoulderProxy;
            targetGroup.Targets[0].Radius = playerRadius;
            targetGroup.Targets[1].Object = lockOnTarget;
            targetGroup.Targets[1].Radius = enemyRadius;
        }

        // Make player face enemy smoothly
        Vector3 lookDir = (lockOnTarget.position - transform.position);
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
        }
    }

    IEnumerator SmoothLockSwitch(Transform newTarget)
    {
        yield return new WaitForSeconds(0.2f);
        lockOnTarget = newTarget;
        UpdateTargetGroup(lockOnTarget);

        if (lockOnSymbolPrefab != null)
        {
            if (activeLockOnSymbol == null)
                activeLockOnSymbol = Instantiate(lockOnSymbolPrefab, lockOnTarget);
            activeLockOnSymbol.transform.localPosition = new Vector3(0, 2.5f, 0);
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

    void ClearLockOn()
    {
        lockOnTarget = null;
        if (activeLockOnSymbol != null)
        {
            Destroy(activeLockOnSymbol);
            activeLockOnSymbol = null;
        }
        UpdateTargetGroup(null);
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
