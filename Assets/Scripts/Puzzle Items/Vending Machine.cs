// VendingMachine.cs
using UnityEngine;

public class VendingMachine : MonoBehaviour, IInteractable
{
    public static VendingMachine Instance { get; private set; }

    [Header("Props")]
    [Tooltip("The receipt GameObject to enable on correct input.")]
    public GameObject receiptProp;

    private bool _hasDispensed = false;

    public bool CanInteract => !_hasDispensed;

    public string GetPromptText() => PlayerInventory.Instance.HasCoin
        ? "Use Vending Machine"
        : "Need a coin";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (receiptProp != null)
            receiptProp.SetActive(false);
    }

    private void OnEnable()
    {
        LoopManager.OnLoopRestarted += HandleLoopRestarted;
        LoopManager.OnLoopChanged += HandleLoopChanged;
    }

    private void OnDisable()
    {
        LoopManager.OnLoopRestarted -= HandleLoopRestarted;
        LoopManager.OnLoopChanged -= HandleLoopChanged;
    }

    public void Interact()
    {
        if (!PlayerInventory.Instance.HasCoin)
        {
            return;
        }

        VendingMachineUI.Instance.Open();
    }

    public void OnCorrectInput()
    {
        _hasDispensed = true;
        PlayerInventory.Instance.HasCoin = false;

        if (receiptProp != null)
            receiptProp.SetActive(true);

        HUDNotification.Instance.Show("Something dispensed.");
        LoopManager.Instance.CompleteCurrentLoopPuzzle();
    }

    public void OnWrongInput()
    {
        PlayerInventory.Instance.HasCoin = false;
        HUDNotification.Instance.Show("Nothing dispensed.");

        // Stay on Loop1, restart the same loop
        LoopManager.Instance.RestartCurrentLoop();
    }

    private void HandleLoopRestarted(LoopStage stage)
    {
        if (stage == LoopStage.Loop1)
            ResetForLoop();
    }

    private void HandleLoopChanged(LoopStage stage)
    {
        // Moving past Loop1 — keep receipt visible, just lock interaction
        if (stage != LoopStage.Loop1)
            _hasDispensed = true;
    }

    private void ResetForLoop()
    {
        _hasDispensed = false;
        PlayerInventory.Instance.ResetForLoop();

        if (receiptProp != null)
            receiptProp.SetActive(false);
    }
}