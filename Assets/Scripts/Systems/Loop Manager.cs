using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Which loop the player is currently in.
/// Loop1 = 1AM  |  Loop2 = 2AM  |  Loop3 = 3AM
/// Escape is triggered externally by the real bus's arrival.
/// </summary>
public enum LoopStage
{
    Cutscene,
    Loop1,
    Loop2,
    Loop3
}

/// <summary>
/// Central singleton that owns loop state for LAYOVER.
/// Place on an empty GameObject called "LoopManager" in your scene.
/// </summary>
public class LoopManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static LoopManager Instance { get; private set; }

    // ── State ──────────────────────────────────────────────────────────────
    [Header("Current State (read-only in Inspector)")]
    [SerializeField] private LoopStage currentLoop = LoopStage.Loop1;

    [Header("Loop 1")]
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject coinObj;

    public LoopStage CurrentLoop => currentLoop;

    // ── Convenience flags ──────────────────────────────────────────────────
    public bool IsLoop1 => currentLoop == LoopStage.Loop1;
    public bool IsLoop2 => currentLoop == LoopStage.Loop2;
    public bool IsLoop3 => currentLoop == LoopStage.Loop3;

    // ── Events ─────────────────────────────────────────────────────────────
    /// <summary>Fired when the loop advances to the next stage (bus departs).</summary>
    public static event UnityAction<LoopStage> OnLoopChanged;

    /// <summary>
    /// Fired when the current loop RESTARTS (wrong code, failure, etc.).
    /// Stage does not change — same hour stays on the clock.
    /// </summary>
    public static event UnityAction<LoopStage> OnLoopRestarted;

    // Add this event at the top with your other events
    /// <summary>
    /// Fired when the current loop's puzzle is solved and the bus should spawn.
    /// </summary>
    public static event UnityAction<LoopStage> OnLoopPuzzleComplete;

    /// <summary>
    /// Fired when the REAL bus arrives at the end of Loop3.
    /// This is the escape trigger — handled externally by your bus/scene logic.
    /// </summary>
    public static event UnityAction OnRealBusArrived;

    // ── Unity lifecycle ────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        OnLoopChanged?.Invoke(currentLoop);
    }

    // ── Public API ─────────────────────────────────────────────────────────


    public void StartGame()
    {
        player.SetActive(true);
        SetLoop(LoopStage.Loop1);
    }

    public void StartCutscene()
    {
        player.SetActive(false);
        SetLoop(LoopStage.Cutscene);
    }

    /// <summary>
    /// Player boards the bus — advances Loop1 → Loop2 → Loop3.
    /// On Loop3, call TriggerRealBus() instead to escape.
    /// </summary>
    public void AdvanceLoop()
    {
        if (currentLoop == LoopStage.Loop3)
        {
            Debug.LogWarning("LoopManager: Already on Loop3 — call TriggerRealBus() to escape.");
            return;
        }

        currentLoop = currentLoop switch
        {
            LoopStage.Loop1 => LoopStage.Loop2,
            LoopStage.Loop2 => LoopStage.Loop3,
            _ => LoopStage.Loop3
        };

        Debug.Log($"[LoopManager] Bus departed — now in {currentLoop}");
        OnLoopChanged?.Invoke(currentLoop);
    }

    /// <summary>
    /// Called when the player has solved the current loop's puzzle.
    /// Fires OnLoopPuzzleComplete so the bus system knows to spawn.
    /// Loop does NOT advance here — it advances when the player boards.
    /// </summary>
    public void CompleteCurrentLoopPuzzle()
    {
        Debug.Log($"[LoopManager] Puzzle complete for {currentLoop} — spawning bus.");
        OnLoopPuzzleComplete?.Invoke(currentLoop);
    }

    /// <summary>
    /// The REAL bus has arrived. Fires the escape event.
    /// Only valid during Loop3 — ignored otherwise.
    /// </summary>
    public void TriggerRealBus()
    {
        if (currentLoop != LoopStage.Loop3)
        {
            Debug.LogWarning("LoopManager: Real bus can only arrive during Loop3.");
            return;
        }

        Debug.Log("[LoopManager] Real bus arrived — escape triggered.");
        OnRealBusArrived?.Invoke();
    }

    /// <summary>
    /// Player failed — restart the SAME loop. Clock stays at the same hour.
    /// </summary>
    public void RestartCurrentLoop()
    {
        Debug.Log($"[LoopManager] Restarting {currentLoop}");

        if (currentLoop == LoopStage.Loop1)
        {
            coinObj.SetActive(true);
        }

        PlayerInventory.Instance.ResetForLoop();
        OnLoopRestarted?.Invoke(currentLoop);
    }

    /// <summary>Debug / cutscene use only — jump directly to a stage.</summary>
    public void SetLoop(LoopStage stage)
    {
        currentLoop = stage;
        Debug.Log($"[LoopManager] Forced to {currentLoop}");
        OnLoopChanged?.Invoke(currentLoop);
    }

    [ContextMenu("Debug: Force Loop 1")] private void ForceLoop1() => SetLoop(LoopStage.Loop1);
    [ContextMenu("Debug: Force Loop 2")] private void ForceLoop2() => SetLoop(LoopStage.Loop2);
    [ContextMenu("Debug: Force Loop 3")] private void ForceLoop3() => SetLoop(LoopStage.Loop3);
    [ContextMenu("Debug: Trigger Real Bus")] private void DebugRealBus() => TriggerRealBus();
}