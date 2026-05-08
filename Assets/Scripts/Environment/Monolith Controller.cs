using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Combined Controller for the LAYOVER monolith.
/// Handles physical movement (when not looked at) and text updates per loop.
/// </summary>
public class MonolithController : MonoBehaviour
{
    [System.Serializable]
    public struct LoopTransform
    {
        public Vector3 position;
        public Vector3 rotation; // Euler angles
    }

    [Header("UI References")]
    [SerializeField] private GameObject normalText;
    [SerializeField] private GameObject anomalyText;

    [Header("Monolith Transform Per Loop")]
    [SerializeField] private LoopTransform loop1Transform;
    [SerializeField] private LoopTransform loop2Transform;
    [SerializeField] private LoopTransform loop3Transform;

    [Header("Visibility Settings")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("World-space radius of the monolith used for frustum visibility check.")]
    [SerializeField][Range(0.5f, 20f)] private float visibilityCheckRadius = 3f;

    // Internal state
    private LoopTransform pendingTransform;
    private bool transformPending = false;

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

    private void Start()
    {
        // Sync to current state on start
        if (LoopManager.Instance != null)
        {
            UpdateText(LoopManager.Instance.CurrentLoop);
            // Initial position snap
            ApplyInstantTransform(LoopManager.Instance.CurrentLoop);
        }
    }

    private void Update()
    {
        // If a movement is queued, only move when the player looks away
        if (transformPending && !IsMonolithVisible())
            ApplyPendingTransform();
    }

    // ── Event Handlers ─────────────────────────────────────────────────────

    private void HandleLoopChanged(LoopStage newLoop)
    {
        UpdateText(newLoop);

        LoopTransform target = GetTransformForLoop(newLoop);
        QueueTransform(target);
    }

    private void HandleLoopRestarted(LoopStage sameLoop)
    {
        // Ensure text is correct if a restart triggers specific logic
        UpdateText(sameLoop);
    }

    private void HandleRealBusArrived()
    {
        anomalyText.SetActive(false);
        normalText.SetActive(true);
    }

    // ── Logic Helpers ──────────────────────────────────────────────────────

    private void UpdateText(LoopStage loop)
    {
        switch(loop)
        {
            case LoopStage.Cutscene:
            case LoopStage.Loop1:
            case LoopStage.Loop2:
            default:
                normalText.SetActive(true);
                anomalyText.SetActive(false);
                break;

            case LoopStage.Loop3:
                anomalyText.SetActive(true);
                normalText.SetActive(false);
                break;
        }
    }

    private LoopTransform GetTransformForLoop(LoopStage loop)
    {
        return loop switch
        {
            LoopStage.Loop1 => loop1Transform,
            LoopStage.Loop2 => loop2Transform,
            LoopStage.Loop3 => loop3Transform,
            _ => loop1Transform
        };
    }

    private void QueueTransform(LoopTransform target)
    {
        pendingTransform = target;
        transformPending = true;

        // Apply immediately if player isn't watching right now
        if (!IsMonolithVisible())
            ApplyPendingTransform();
    }

    private void ApplyPendingTransform()
    {
        transform.position = pendingTransform.position;
        transform.rotation = Quaternion.Euler(pendingTransform.rotation);
        transformPending = false;
    }

    private void ApplyInstantTransform(LoopStage loop)
    {
        LoopTransform target = GetTransformForLoop(loop);
        transform.position = target.position;
        transform.rotation = Quaternion.Euler(target.rotation);
        transformPending = false;
    }

    private bool IsMonolithVisible()
    {
        if (playerCamera == null) return false;
        Bounds bounds = new Bounds(transform.position, Vector3.one * visibilityCheckRadius * 2f);
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }
}