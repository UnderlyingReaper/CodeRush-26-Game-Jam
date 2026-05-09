using UnityEngine;

public class VendingMachine : MonoBehaviour, IInteractable
{
    public static VendingMachine Instance { get; private set; }

    [Header("Props")]
    public GameObject receiptProp;
    public GameObject ticketProp; // The bus ticket prop — enable this in Loop 2

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip dispenseSound;
    public AudioClip errorSound; // Optional: for wrong inputs

    private bool _hasDispensed = false;

    public bool CanInteract => !_hasDispensed;

    public string GetPromptText() => PlayerInventory.Instance.HasCoin
        ? "Use Vending Machine"
        : "Need a coin";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (receiptProp != null) receiptProp.SetActive(false);
        if (ticketProp != null) ticketProp.SetActive(false);
    }

    private void OnEnable()
    {
        LoopManager.OnLoopRestarted += HandleLoopRestarted;
        LoopManager.OnLoopChanged += HandleLoopChanged;
        LoopManager.OnLoopPuzzleComplete += HandlePuzzleComplete;
    }

    private void OnDisable()
    {
        LoopManager.OnLoopRestarted -= HandleLoopRestarted;
        LoopManager.OnLoopChanged -= HandleLoopChanged;
        LoopManager.OnLoopPuzzleComplete -= HandlePuzzleComplete;
    }

    // ─── Loop 2 free dispense ─────────────────────────────────────

    private void HandlePuzzleComplete(LoopStage stage)
    {
        if (stage != LoopStage.Loop2) return;

        // Payphone puzzle solved — ticket appears in machine slot, no coin needed
        _hasDispensed = false; // Re-enable interaction temporarily
        DispenseTicket();
    }

    private void DispenseTicket()
    {
        _hasDispensed = true;
        PlayerInventory.Instance.HasTicket = true;

        if (ticketProp != null) ticketProp.SetActive(true);

        PlaySound(dispenseSound);
        HUDNotification.Instance.Show("A ticket appears in the machine slot.");
    }

    // ─── Loop 1 normal interaction ────────────────────────────────

    public void Interact()
    {
        if (!PlayerInventory.Instance.HasCoin) return;
        VendingMachineUI.Instance.Open();
    }

    public void OnCorrectInput()
    {
        _hasDispensed = true;
        PlayerInventory.Instance.HasCoin = false;

        if (receiptProp != null) receiptProp.SetActive(true);

        PlaySound(dispenseSound);
        HUDNotification.Instance.Show("Something dispensed.");
        LoopManager.Instance.CompleteCurrentLoopPuzzle();
    }

    public void OnWrongInput()
    {
        PlayerInventory.Instance.HasCoin = false;

        PlaySound(errorSound);
        HUDNotification.Instance.Show("Nothing dispensed.");
        LoopManager.Instance.RestartCurrentLoop();
    }

    // ─── Audio Helper ─────────────────────────────────────────────

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            // PlayOneShot allows multiple sounds to overlap without cutting each other off
            audioSource.PlayOneShot(clip);
        }
    }

    // ─── Loop state handling ──────────────────────────────────────

    private void HandleLoopRestarted(LoopStage stage)
    {
        if (stage == LoopStage.Loop1) ResetForLoop1();
        if (stage == LoopStage.Loop2) ResetForLoop2();
    }

    private void HandleLoopChanged(LoopStage stage)
    {
        if (stage == LoopStage.Loop2) ResetForLoop2();
        if (stage == LoopStage.Loop3) LockPermanently();
    }

    private void ResetForLoop1()
    {
        _hasDispensed = false;
        PlayerInventory.Instance.ResetForLoop();
        if (receiptProp != null) receiptProp.SetActive(false);
        if (ticketProp != null) ticketProp.SetActive(false);
    }

    private void ResetForLoop2()
    {
        // Machine is locked until payphone puzzle is solved
        _hasDispensed = true;
        if (ticketProp != null) ticketProp.SetActive(false);
    }

    private void LockPermanently()
    {
        _hasDispensed = true;
    }
}