// BusController.cs
using UnityEngine;
using DG.Tweening;

public class BusController : MonoBehaviour
{
    public static BusController Instance { get; private set; }

    [Header("Positions")]
    public Transform spawnPoint;      // far -X
    public Transform stopPoint;       // 0 — bus stop
    public Transform exitPoint;       // far +X

    [Header("Timing")]
    [Tooltip("How long the bus takes to drive in from spawn to stop.")]
    public float driveInDuration = 6f;
    [Tooltip("How long the bus waits at the stop before driving off (after boarding).")]
    public float waitAtStopDuration = 2f;
    [Tooltip("How long the bus takes to drive out from stop to exit.")]
    public float driveOutDuration = 8f;

    [Header("Doors")]
    public Transform leftDoor;
    public Transform rightDoor;
    [Tooltip("How many degrees each door rotates open on Y axis.")]
    public float doorOpenAngle = 90f;
    public float doorAnimDuration = 0.8f;

    [Header("Exit")]
    public Transform exitSpawnPoint;

    private Tween _moveTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Park at spawn, hidden
        transform.position = spawnPoint.position;
        transform.rotation = Quaternion.identity;

        LoopManager.OnLoopPuzzleComplete += HandlePuzzleComplete;

        gameObject.SetActive(false);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Trigger this to spawn the bus and drive it to the stop.</summary>
    [ContextMenu("Arrive")]
    public void SpawnAndArrive()
    {
        transform.position = spawnPoint.position;
        gameObject.SetActive(true);

        // Worn-out bus feel:
        // InOutSine = slow start, smooth middle, gentle brake
        _moveTween = transform.DOMove(stopPoint.position, driveInDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(OnArrived);
    }

    /// <summary>Call this after the player boards to close doors and drive off.</summary>
    [ContextMenu("Depart")]
    public void Depart()
    {
        CloseDoors(() =>
        {
            // Brief pause then pull away — OutQuad = quick initial pull, 
            // then settles into speed like a heavy vehicle
            _moveTween = transform.DOMove(exitPoint.position, driveOutDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(OnExited);
        });
    }

    public void OpenDoorsForBoarding()
    {
        OpenDoors();
    }

    // ── Private ────────────────────────────────────────────────────────────

    private void HandlePuzzleComplete(LoopStage stage)
    {
        // Loop3 escape is handled separately via TriggerRealBus
        if (stage == LoopStage.Loop3) return;

        SpawnAndArrive();
    }

    private void OnArrived()
    {
        HUDNotification.Instance.Show("A bus has arrived.");
    }

    private void OnExited()
    {
        gameObject.SetActive(false);
        transform.position = spawnPoint.position;
    }

    private void OpenDoors(System.Action onComplete = null)
    {
        if (leftDoor == null || rightDoor == null)
        {
            onComplete?.Invoke();
            return;
        }

        Sequence seq = DOTween.Sequence();
        seq.Join(leftDoor.DOLocalRotate(
            new Vector3(0f, -doorOpenAngle, 0f), doorAnimDuration)
            .SetEase(Ease.OutCubic));
        seq.Join(rightDoor.DOLocalRotate(
            new Vector3(0f, doorOpenAngle, 0f), doorAnimDuration)
            .SetEase(Ease.OutCubic));
        seq.OnComplete(() => onComplete?.Invoke());
    }

    private void CloseDoors(System.Action onComplete = null)
    {
        if (leftDoor == null || rightDoor == null)
        {
            onComplete?.Invoke();
            return;
        }

        Sequence seq = DOTween.Sequence();
        seq.Join(leftDoor.DOLocalRotate(
            Vector3.zero, doorAnimDuration)
            .SetEase(Ease.InCubic));
        seq.Join(rightDoor.DOLocalRotate(
            Vector3.zero, doorAnimDuration)
            .SetEase(Ease.InCubic));
        seq.OnComplete(() => onComplete?.Invoke());
    }

    private void OnDestroy()
    {
        _moveTween?.Kill();
    }
}