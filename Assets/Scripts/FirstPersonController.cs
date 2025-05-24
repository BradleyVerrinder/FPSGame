using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : NetworkBehaviour
{
    public NetworkVariable<float> moveSpeed = new NetworkVariable<float>(
        2f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> gravity = new NetworkVariable<float>(
        -9.81f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float mouseSensitivity = 2f;
    public float jumpHeight = 2f;

    private Animator animator;
    public Transform playerBody;
    public Transform cameraRoot;

    private CharacterController controller;
    private float verticalRotation = 0f;
    private bool isGrounded;
    private Vector3 velocity;

    private NetworkVariable<float> netMoveX = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> netMoveY = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Vector2 cachedMoveInput;
    private bool cachedJump;
    private bool jumpConsumed = true;

    private bool jumpQueued = false;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            moveSpeed.Value = 4f;
            gravity.Value = -9.81f;
        }

        if (!IsOwner)
        {
            cameraRoot.gameObject.SetActive(false);
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
            // Update camera rotation
            // No need to overwrite players rotation with outdated network value - causes jittery rotation on client
            //playerBody.rotation = networkRotation.Value;

            HandleLook();

            cachedMoveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            
            if (Input.GetButtonDown("Jump"))
            {
               jumpQueued = true;
            }

            SendMovementInputServerRpc(cachedMoveInput, jumpQueued);
            jumpQueued = false;

            // Display locally for responsiveness
            Vector3 localMove = playerBody.InverseTransformDirection(
                playerBody.right * cachedMoveInput.x + playerBody.forward * cachedMoveInput.y);
            animator.SetFloat("MoveX", localMove.x);
            animator.SetFloat("MoveY", localMove.z);
        }
        else
        {
            playerBody.rotation = Quaternion.Slerp(playerBody.rotation, networkRotation.Value, Time.deltaTime * 10f);
            animator.SetFloat("MoveX", netMoveX.Value);
            animator.SetFloat("MoveY", netMoveY.Value);
        }
    }

    void FixedUpdate()
    {
        if (IsServer)
        {
            ApplyMovement(cachedMoveInput, cachedJump);
            cachedJump = false; // always clear after use
        }
    }

    private Quaternion lastSentRotation;

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate locally immediately for smooth client response
        playerBody.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        cameraRoot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // Send the *final* rotation quaternion to server (not just mouseX)
        if (IsOwner)
        {
            if (Quaternion.Angle(lastSentRotation, playerBody.rotation) > 0.1f)
            {
                SendRotationServerRpc(playerBody.rotation);
                lastSentRotation = playerBody.rotation;
            }
        }
    }


    [ServerRpc]
    void SendRotationServerRpc(Quaternion rotation)
    {
        networkRotation.Value = rotation;
    }

    [ServerRpc]
    void SendMovementInputServerRpc(Vector2 moveInput, bool jumpPressed)
    {
        cachedMoveInput = moveInput;
        if (jumpPressed)
        {
            cachedJump = true;
        }
    }

    void ApplyMovement(Vector2 moveInput, bool jumpPressed)
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        Vector3 move = playerBody.right * moveInput.x + playerBody.forward * moveInput.y;
        controller.Move(move * moveSpeed.Value * Time.fixedDeltaTime);

        if (jumpPressed && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity.Value);

        velocity.y += gravity.Value * Time.fixedDeltaTime;
        controller.Move(velocity * Time.fixedDeltaTime);

        Vector3 localMove = playerBody.InverseTransformDirection(move);
        netMoveX.Value = localMove.x;
        netMoveY.Value = localMove.z;

    }
}
