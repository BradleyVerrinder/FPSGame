using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    private Animator animator;
    public Transform playerBody; 

    public Transform cameraRoot; // Assign this in inspector

    private CharacterController controller;
    private float verticalRotation = 0f;

    private bool isGrounded;
    private Vector3 velocity;

    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            cameraRoot.gameObject.SetActive(false); // disable camera for other players
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (IsOwner)
        {
            // Update playerBody rotation from network variable for yaw
            playerBody.rotation = networkRotation.Value;

            HandleLook();

            Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            bool jumpPressed = Input.GetButtonDown("Jump");

            // Move locally immediately for responsiveness (optional prediction)
            HandleMovementLocal(moveInput, jumpPressed);

            // Send inputs to server for authoritative processing
            SendMovementInputServerRpc(moveInput, jumpPressed);
        }
        else
        {
            // Non-owners: smoothly lerp rotation to the authoritative rotation from server
            playerBody.rotation = Quaternion.Slerp(playerBody.rotation, networkRotation.Value, Time.deltaTime * 10f);
        }
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // IMPORTANT: Do NOT rotate playerBody locally here to avoid desync!
        // Instead, send input to server for authoritative rotation update:
        if (!IsServer)
        {
            SendLookInputServerRpc(mouseX);
        }
        else
        {
            // If on server and owner, apply rotation directly (because server processes input)
            playerBody.Rotate(Vector3.up * mouseX);
            networkRotation.Value = playerBody.rotation; // Update networked rotation for clients
        }

        // Vertical camera tilt is local only
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        cameraRoot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    [ServerRpc]
    void SendLookInputServerRpc(float mouseX)
    {
        // Server receives rotation input and applies rotation authoritatively
        playerBody.Rotate(Vector3.up * mouseX);

        // Update network variable to sync rotation with clients
        networkRotation.Value = playerBody.rotation;
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

        float speed = new Vector2(input.x, input.y).magnitude;
        animator.SetFloat("Speed", speed);
    }

    [ServerRpc]
    void SendMovementInputServerRpc(Vector2 moveInput, bool jumpPressed)
    {
        if (!IsServer) return;

        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        Vector3 move = playerBody.right * moveInput.x + playerBody.forward * moveInput.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        if (jumpPressed && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
