using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    bool triggered = false;

    [Header("Movement Settings")]
    public float speed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    // Input System
    private PlayerControls inputSystem;
    private InputAction moveAction;
    private InputAction jumpAction;

    LevelLoader LevelLoader;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Create new instance of your generated InputActions class
        inputSystem = new PlayerControls();

        // Assign actions from your Input Map
        moveAction = inputSystem.Player.Move3D;
        jumpAction = inputSystem.Player.Jump;
    }

    private void OnEnable()
    {
        inputSystem.Enable();
    }

    private void OnDisable()
    {
        inputSystem.Disable();
    }

    private void Update()
    {
        // --- Ground check ---
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f; // Keeps grounded nicely

        // --- Movement ---
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * speed * Time.deltaTime);

        // --- Jump ---
        if (jumpAction.WasPressedThisFrame() && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // --- Gravity ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.F10))
        {
            LevelLoader.LoadNextLevel("StartScene");
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Portal"))
        {
            triggered = true;

            int chosenIndex = (other.gameObject.name == "PortalSlade") ? 0 :
                              (other.gameObject.name == "PortalOphelia") ? 1 : -1;

            if (chosenIndex != -1)
            {
                CharacterChosen(chosenIndex, controller);
            }
        }
    }
    void CharacterChosen(int v, CharacterController controller)
    {
        PlayerPrefs.SetInt("CharacterChosen", v);
        PlayerPrefs.Save();
        Debug.Log(v);
    }
}
