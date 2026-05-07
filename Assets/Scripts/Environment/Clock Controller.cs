using UnityEngine;

/// <summary>
/// Drives an analogue clock face for LAYOVER.
///
/// t accumulates every frame (asymptotic, never reaches 1).
/// Every [tickInterval] seconds a rotation update is QUEUED.
/// The queued update is only APPLIED when the player is not looking at the clock.
/// If the player is watching, the hand stays frozen until they look away —
/// then it snaps to the current t value instantly.
///
/// Setup:
///   - Assign hourHandTransform, minuteHandTransform, and playerCamera.
///   - visibilityCheckRadius: radius of the clock face in world units used
///     for the viewport check (tune until it feels right for your clock size).
///   - tickInterval: seconds between queued updates.
///   - minuteApproachSpeed: how fast t climbs.
/// </summary>
public class ClockController : MonoBehaviour
{
    [Header("Hand Transforms (pivot at clock centre)")]
    [SerializeField] private Transform hourHandTransform;
    [SerializeField] private Transform minuteHandTransform;

    [Header("Player Camera")]
    [SerializeField] private Camera playerCamera;

    [Tooltip("How large the clock face appears in world space. Used to determine " +
             "if the clock is in the centre of the player's view.")]
    [SerializeField][Range(0.1f, 5f)] private float visibilityCheckRadius = 0.5f;

    [Header("Rotation Direction")]
    [Tooltip("Enable if clock hands need a negative Z angle to rotate clockwise.")]
    [SerializeField] private bool negateRotation = true;

    [Header("Tick Settings")]
    [Tooltip("Seconds between each queued rotation update.")]
    [SerializeField][Range(0.1f, 30f)] private float tickInterval = 5f;

    [Tooltip("How fast t climbs toward 1. Asymptotic — never reaches 1.")]
    [SerializeField][Range(0.01f, 0.5f)] private float minuteApproachSpeed = 0.08f;

    // ── Internal state ─────────────────────────────────────────────────────
    private float t = 0f;
    private float tickTimer = 0f;
    private float hourAngleDeg = 30f;

    // True when a tick fired while the player was watching —
    // we hold the update until they look away.
    private bool updatePending = false;

    // ── Unity lifecycle ────────────────────────────────────────────────────
    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void OnEnable()
    {
        LoopManager.OnLoopChanged += HandleLoopChanged;
        LoopManager.OnLoopRestarted += HandleLoopRestarted;
        LoopManager.OnRealBusArrived += HandleRealBusArrived;
    }

    private void OnDisable()
    {
        LoopManager.OnLoopChanged -= HandleLoopChanged;
        LoopManager.OnLoopRestarted -= HandleLoopRestarted;
        LoopManager.OnRealBusArrived -= HandleRealBusArrived;
    }

    private void Update()
    {
        // t accumulates every frame regardless of visibility
        t += minuteApproachSpeed * (1f - t) * Time.deltaTime;
        t = Mathf.Clamp(t, 0f, 0.9999f);

        // Queue a tick on the fixed interval
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer = 0f;
            updatePending = true; // mark that a snap is due
        }

        // Apply the queued snap only when the player isn't looking at the clock
        if (updatePending && !IsClockVisible())
        {
            SnapMinuteHand();
            updatePending = false;
        }
    }

    // ── Visibility check ───────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the clock face is currently within the player's viewport.
    /// Uses a sphere-in-frustum check so it works regardless of distance.
    /// </summary>
    private bool IsClockVisible()
    {
        if (playerCamera == null) return false;

        // GeometryUtility checks if a bounding sphere overlaps the camera frustum.
        // We build a tiny bounds around the clock's world position.
        Bounds clockBounds = new Bounds(transform.position,
                                        Vector3.one * visibilityCheckRadius * 2f);

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
        return GeometryUtility.TestPlanesAABB(frustumPlanes, clockBounds);
    }

    // ── Snapping ───────────────────────────────────────────────────────────

    private void SnapMinuteHand()
    {
        if (minuteHandTransform == null) return;

        float angle = t * 360f;
        if (negateRotation) angle = -angle;
        minuteHandTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void SnapHourHand()
    {
        if (hourHandTransform == null) return;

        float angle = negateRotation ? -hourAngleDeg : hourAngleDeg;
        hourHandTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ── Reset ──────────────────────────────────────────────────────────────

    private void ResetMinuteHand()
    {
        t = 0f;
        tickTimer = 0f;
        updatePending = false;
        SnapMinuteHand(); // immediately snap to 12 — this is a loop event, not a tick
    }

    // ── Event handlers ─────────────────────────────────────────────────────

    private void HandleRealBusArrived()
    {
        hourAngleDeg = 120f; // 4 AM
        SnapHourHand();
        ResetMinuteHand();
    }

    private void HandleLoopChanged(LoopStage newLoop)
    {
        hourAngleDeg = HourToDegrees(newLoop);
        SnapHourHand();
        ResetMinuteHand();
    }

    private void HandleLoopRestarted(LoopStage sameLoop)
    {
        ResetMinuteHand(); // hour hand stays, minute resets
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static float HourToDegrees(LoopStage loop)
    {
        return loop switch
        {
            LoopStage.Loop1 => 30f,   // 1 AM
            LoopStage.Loop2 => 60f,   // 2 AM
            LoopStage.Loop3 => 90f,   // 3 AM
            _ => 30f
        };
    }
}