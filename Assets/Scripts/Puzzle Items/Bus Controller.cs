// BusController.cs
using UnityEngine;
using DG.Tweening;
using Unity.Cinemachine;
using System.Collections.Generic;

public class BusController : MonoBehaviour
{
    public static BusController Instance { get; private set; }

    [Header("Positions")]
    public Transform spawnPoint;
    public Transform stopPoint;
    public Transform exitPoint;

    [Header("Timing")]
    public float driveInDuration = 6f;
    public float waitAtStopDuration = 2f;
    public float driveOutDuration = 8f;

    [Header("Doors")]
    public Transform leftDoor;
    public Transform rightDoor;
    public float doorOpenAngle = 90f;
    public float doorAnimDuration = 0.8f;

    [Header("Extra")]
    public Transform exitSpawnPoint;
    public CinemachineCamera cam1;
    [SerializeField] private List<MeshRenderer> windows;
    [SerializeField] private Material litWindowsMaterial;

    [Header("Audio")]
    [SerializeField] private AudioSource engineSource;   // looping engine hum
    [SerializeField] private AudioSource sfxSource;      // one-shots
    [SerializeField] private AudioClip engineApproachClip;   // looping while driving in
    [SerializeField] private AudioClip engineIdleClip;       // looping while stopped
    [SerializeField] private AudioClip engineDepartClip;     // looping while driving out
    [SerializeField] private AudioClip airBrakeClip;         // on arrival
    [SerializeField] private AudioClip doorOpenClip;
    [SerializeField] private AudioClip doorCloseClip;

    private Tween _moveTween;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        transform.position = spawnPoint.position;
        transform.rotation = Quaternion.identity;

        LoopManager.OnLoopPuzzleComplete += HandlePuzzleComplete;
        LoopManager.OnRealBusArrived += SpawnAndArriveRealBus;

        gameObject.SetActive(false);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    [ContextMenu("Arrive")]
    public void SpawnAndArrive()
    {
        transform.position = spawnPoint.position;
        gameObject.SetActive(true);

        PlayEngineLoop(engineApproachClip);

        _moveTween = transform.DOMove(stopPoint.position, driveInDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(OnArrived);
    }

    public void SpawnAndArriveRealBus()
    {
        transform.position = spawnPoint.position;
        gameObject.SetActive(true);

        foreach (MeshRenderer m in windows)
            m.material = litWindowsMaterial;

        PlayEngineLoop(engineApproachClip);

        _moveTween = transform.DOMove(stopPoint.position, driveInDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(OnArrived);
    }

    [ContextMenu("Depart")]
    public void Depart()
    {
        CloseDoors(() =>
        {
            PlayEngineLoop(engineDepartClip);

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
        if (stage == LoopStage.Loop3) return;
        SpawnAndArrive();
    }

    private void OnArrived()
    {
        PlaySFX(airBrakeClip);
        PlayEngineLoop(engineIdleClip);
        HUDNotification.Instance.Show("A bus has arrived.");
    }

    private void OnExited()
    {
        engineSource.Stop();
        gameObject.SetActive(false);
        transform.position = spawnPoint.position;
    }

    private void OpenDoors(System.Action onComplete = null)
    {
        if (leftDoor == null || rightDoor == null) { onComplete?.Invoke(); return; }

        PlaySFX(doorOpenClip);

        Sequence seq = DOTween.Sequence();
        seq.Join(leftDoor.DOLocalRotate(new Vector3(0f, -doorOpenAngle, 0f), doorAnimDuration).SetEase(Ease.OutCubic));
        seq.Join(rightDoor.DOLocalRotate(new Vector3(0f, doorOpenAngle, 0f), doorAnimDuration).SetEase(Ease.OutCubic));
        seq.OnComplete(() => onComplete?.Invoke());
    }

    private void CloseDoors(System.Action onComplete = null)
    {
        if (leftDoor == null || rightDoor == null) { onComplete?.Invoke(); return; }

        PlaySFX(doorCloseClip);

        Sequence seq = DOTween.Sequence();
        seq.Join(leftDoor.DOLocalRotate(Vector3.zero, doorAnimDuration).SetEase(Ease.InCubic));
        seq.Join(rightDoor.DOLocalRotate(Vector3.zero, doorAnimDuration).SetEase(Ease.InCubic));
        seq.OnComplete(() => onComplete?.Invoke());
    }

    private void PlayEngineLoop(AudioClip clip)
    {
        if (clip == null || engineSource == null) return;
        engineSource.clip = clip;
        engineSource.loop = true;
        engineSource.Play();
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    private void OnDestroy()
    {
        _moveTween?.Kill();
    }
}