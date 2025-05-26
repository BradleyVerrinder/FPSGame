using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : NetworkBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 6f;
    public float gravity = -9.81f;
    public float mouseSensitivity = 2f;
    public float cameraPitchLimit = 85f;

    [Header("References")]
    public Transform cameraRoot;
    public Transform playerBody;
    public GameObject firstPersonGun;
    public GameObject thirdPersonGun;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float cameraPitch;

    private Animator animator;

    // Input cache
    private Vector2 moveInput;
    private bool jumpPressed;

    // Server-side buffered jump input
    private bool jumpBufferedOnServer;

    // Animation values
    private NetworkVariable<float> netMoveX = new(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> netMoveY = new(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Server sync
    private NetworkVariable<Vector3> serverPosition = new(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Quaternion> serverRotation = new(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private const float reconciliationThreshold = 0.3f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            firstPersonGun.SetActive(true);
            thirdPersonGun.SetActive(false);
        }
        else
        {
            cameraRoot.gameObject.SetActive(false);
            firstPersonGun.SetActive(false);
            thirdPersonGun.SetActive(true);
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            HandleInput();
            HandleLook();

            if (!IsServer)
            {
                ClientPredictMovement();

                // 🔍 Debugging position drift
                float dist = Vector3.Distance(transform.position, serverPosition.Value);
                if (dist > 0.1f)
                    Debug.Log($"[Drift] Local vs Server Position Drift: {dist:F3}");
            }
            // Update local animations
            Vector3 localMove = playerBody.InverseTransformDirection(
                playerBody.right * moveInput.x + playerBody.forward * moveInput.y);
            animator.SetFloat("MoveX", localMove.x);
            animator.SetFloat("MoveY", localMove.z);

            SendInputToServerRpc(moveInput, jumpPressed, playerBody.rotation.eulerAngles.y);
            jumpPressed = false;
        }
        else
        {
            // Smooth remote players only
            transform.position = Vector3.Lerp(transform.position, serverPosition.Value, Time.deltaTime * 5f);

            // Only update rotation for remote clients
            if (!IsOwner)
            {
                Quaternion targetRotation = Quaternion.Euler(0f, serverRotation.Value.eulerAngles.y, 0f);
                playerBody.rotation = Quaternion.Slerp(playerBody.rotation, targetRotation, Time.deltaTime * 5f);
            }

            animator.SetFloat("MoveX", netMoveX.Value);
            animator.SetFloat("MoveY", netMoveY.Value);
        }
    }

    private void FixedUpdate()
    {
        if (IsServer)
        {
            ApplyMovement(moveInput, jumpBufferedOnServer);
            jumpBufferedOnServer = false;

            serverPosition.Value = transform.position;
            serverRotation.Value = playerBody.rotation;
        }
    }

    private void HandleInput()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (Input.GetButtonDown("Jump"))
            jumpPressed = true;
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Horizontal rotation (Yaw)
        playerBody.Rotate(Vector3.up * mouseX);

        // Vertical rotation (Pitch)
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -cameraPitchLimit, cameraPitchLimit);
        cameraRoot.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
    }

    private void ClientPredictMovement()
    {
        Vector3 move = playerBody.right * moveInput.x + playerBody.forward * moveInput.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        if (jumpPressed && controller.isGrounded)
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (Vector3.Distance(transform.position, serverPosition.Value) > reconciliationThreshold)
            transform.position = serverPosition.Value;
    }

    [ServerRpc]
    private void SendInputToServerRpc(Vector2 move, bool jump, float yaw)
    {
        moveInput = move;
        playerBody.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (jump)
            jumpBufferedOnServer = true;
    }

    private void ApplyMovement(Vector2 move, bool jump)
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        Vector3 moveDir = playerBody.right * move.x + playerBody.forward * move.y;
        controller.Move(moveDir * moveSpeed * Time.fixedDeltaTime);

        if (jump && isGrounded)
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

        velocity.y += gravity * Time.fixedDeltaTime;
        controller.Move(velocity * Time.fixedDeltaTime);

        Vector3 localMove = playerBody.InverseTransformDirection(moveDir);
        netMoveX.Value = localMove.x;
        netMoveY.Value = localMove.z;
    }
}
