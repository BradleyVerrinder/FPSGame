using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public Transform playerBody; 

    public Transform cameraRoot; // Assign this in inspector

    private CharacterController controller;
    private float verticalRotation = 0f;

    private bool isGrounded;
    private Vector3 velocity;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            cameraRoot.gameObject.SetActive(false); // disable camera for other players
            enabled = false;  // disable script for other players (no input for them)
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (!IsOwner)
            return;

        Debug.Log($"Update running on owner for {gameObject.name}");

        HandleLook();

        // Gather input every frame and send to server
        Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        bool jumpPressed = Input.GetButtonDown("Jump");

        // Move locally immediately for responsiveness
        HandleMovementLocal(moveInput, jumpPressed);

        SendMovementInputServerRpc(moveInput, jumpPressed);
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        Debug.Log($"Mouse Input: X={mouseX}, Y={mouseY}");

        // Rotate player horizontally locally for immediate feedback
        playerBody.Rotate(Vector3.up * mouseX);

        SendLookInputServerRpc(mouseX);

        // Rotate camera vertically locally
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        cameraRoot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
    
    [ServerRpc]
    void SendLookInputServerRpc(float mouseX)
    {
        if (!IsServer) return;
        playerBody.Rotate(Vector3.up * mouseX);
    }

    void HandleMovementLocal(Vector2 input, bool jump)
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        Vector3 move = playerBody.right * input.x + playerBody.forward * input.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        if (jump && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    [ServerRpc]
    void SendMovementInputServerRpc(Vector2 moveInput, bool jumpPressed)
    {
        if (!IsServer) return;

        // Process movement on server
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f; // keep grounded

        Vector3 move = playerBody.right * moveInput.x + playerBody.forward * moveInput.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        if (jumpPressed && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
