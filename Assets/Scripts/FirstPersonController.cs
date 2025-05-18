using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;

    public float jumpHeight = 2f;

    public float gravity = -9.81f;
    public Transform cameraRoot; // Assign this to CameraRoot in the inspector

    private CharacterController controller;
    private float verticalRotation = 0f;

    private bool isGrounded;
    private Vector3 velocity;


    public override void OnNetworkSpawn()
    {
        Debug.Log($"[FPC - OnNetworkSpawn] {gameObject.name} | IsOwner: {IsOwner} | IsLocalPlayer: {IsLocalPlayer}");

        if (!IsOwner)
        {
            cameraRoot.gameObject.SetActive(false); // Disable camera for non-owners
            enabled = false;
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
    {
        Debug.Log($"[FPC] Not owner: {gameObject.name}");
        return;
    }

    if (controller == null)
    {
        Debug.LogError("[FPC] CharacterController is null!");
        return;
    }

    HandleLook();
    HandleMovement();
}


    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate player horizontally
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera vertically
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        cameraRoot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Keeps player grounded
        }

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity manually
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
