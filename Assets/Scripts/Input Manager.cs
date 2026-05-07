using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    [SerializeField] private CinemachineInputAxisController cameraInput;

    private InputSystem_Actions inputActions;

    // Exposed values other systems read
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public event Action OnInteract;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        inputActions = new InputSystem_Actions();

        Initialize();
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
    }
    void OnDisable() => inputActions.Player.Disable();

    // Call this when you want to kill all input (cutscene, menu, loop reset)
    public void DisableGameplay()
    {
        inputActions.Player.Disable();
        if (cameraInput != null) cameraInput.enabled = false;
    }
    public void EnableGameplay()
    {
        inputActions.Player.Enable();
        if (cameraInput != null) cameraInput.enabled = true;
    }

    private void Initialize()
    {
        inputActions.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => MoveInput = Vector2.zero;

        inputActions.Player.Interact.started += ctx => OnInteract?.Invoke();
    }
}