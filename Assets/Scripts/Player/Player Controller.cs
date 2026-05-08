using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity = -15f;

    [Header("References")]
    public Transform cameraPivot;

    [Header("Footsteps")]
    public AudioSource footstepSource;
    public AudioClip footstepClip;

    [Range(0f, 1f)] public float footstepVolumeMin = 0.6f;
    [Range(0f, 1f)] public float footstepVolumeMax = 1.0f;
    [Range(0.8f, 1.2f)] public float footstepPitchMin = 0.9f;
    [Range(0.8f, 1.4f)] public float footstepPitchMax = 1.2f;

    [Tooltip("Time in seconds between each footstep.")]
    public float footstepInterval = 0.45f;


    private float _footstepTimer = 0f;
    private CharacterController _cc;
    private CinemachinePanTilt _panTilt;
    private float _verticalVelocity;

    void Start()
    {
        _cc = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleRotation();
        HandleMovement();
    }

    void HandleRotation()
    {
        Vector2 look = InputManager.Instance.LookInput;
    }

    void HandleMovement()
    {
        Vector2 input = InputManager.Instance.MoveInput;

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        // Flatten vectors
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        // Create the movement vector relative to the camera
        Vector3 moveDirection = (camForward * input.y) + (camRight * input.x);

        // Gravity
        if (_cc.isGrounded)
            _verticalVelocity = -2f;
        else
            _verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalMove = moveDirection * moveSpeed;
        finalMove.y = _verticalVelocity;

        _cc.Move(finalMove * Time.deltaTime);

        if (moveDirection.magnitude > 0.1f)
            transform.rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);

        // Footsteps
        bool isMoving = moveDirection.magnitude > 0.1f && _cc.isGrounded;

        if (isMoving)
        {
            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                footstepSource.volume = Random.Range(footstepVolumeMin, footstepVolumeMax);
                footstepSource.pitch = Random.Range(footstepPitchMin, footstepPitchMax);
                footstepSource.PlayOneShot(footstepClip);
                _footstepTimer = footstepInterval;
            }
        }
        else
        {
            _footstepTimer = 0f; // Reset so first step plays immediately on move
        }
    }

    public void SetPosition(Vector3 pos)
    {
        _cc.enabled = false;
        transform.position = pos;
        _cc.enabled = true;
    }
}