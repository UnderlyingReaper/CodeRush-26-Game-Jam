// BusController.cs
using UnityEngine;
using DG.Tweening;
using Unity.Cinemachine;
using System.Collections;
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
    [SerializeField] private AudioSource engineSource;
    [SerializeField] private AudioSource sfxSource;

    [Tooltip("Single looping engine clip used for all states.")]
    [SerializeField] private AudioClip engineLoopClip;

    [Tooltip("Maximum (base) volume of the engine loop. Idle and startup will be quieter.")]
    [Range(0f, 1f)]
    [SerializeField] private float engineBaseVolume = 0.85f;

    [SerializeField] private AudioClip airBrakeClip;
    [SerializeField] private AudioClip doorOpenClip;
    [SerializeField] private AudioClip doorCloseClip;

    // ── Engine state targets (pitch, volume multiplier) ────────────────────
    private static readonly (float pitch, float vol) StateOff = (0.60f, 0.00f);
    private static readonly (float pitch, float vol) StateStartup = (0.70f, 0.45f);
    private static readonly (float pitch, float vol) StateDriving = (1.20f, 1.00f);
    private static readonly (float pitch, float vol) StateSlowing = (0.85f, 0.70f);
    private static readonly (float pitch, float vol) StateIdle = (0.75f, 0.40f);
    private static readonly (float pitch, float vol) StateDepart = (1.35f, 1.00f);

    [Header("Engine Transition Speeds (seconds)")]
    [SerializeField] public float startupRampTime = 1.8f;
    [SerializeField] public float drivingRampTime = 2.5f;
    [SerializeField] public float slowingRampTime = 2.0f;
    [SerializeField] public float idleRampTime = 0.8f;
    [SerializeField] public float departRampTime = 1.6f;
    [SerializeField] public float shutdownRampTime = 1.5f;

    [Tooltip("Small random pitch wobble to make the engine feel organic.")]
    [SerializeField] private float pitchVariance = 0.03f;
    [SerializeField] private float pitchVarianceCycleTime = 0.4f;

    private Tween _moveTween;
    private Coroutine _engineRampCoroutine;
    private Coroutine _pitchVarianceCoroutine;
    private float _currentPitchBase;

    // ── Unity lifecycle ────────────────────────────────────────────────────

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

    private void OnDestroy()
    {
        _moveTween?.Kill();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    [ContextMenu("Arrive")]
    public void SpawnAndArrive()
    {
        transform.position = spawnPoint.position;
        gameObject.SetActive(true);

        StartEngineLoop();
        SetEngineState(StateStartup, startupRampTime, then: () =>
            SetEngineState(StateDriving, drivingRampTime));

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

        StartEngineLoop();
        SetEngineState(StateStartup, startupRampTime, then: () =>
            SetEngineState(StateDriving, drivingRampTime));

        _moveTween = transform.DOMove(stopPoint.position, driveInDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(OnArrived);
    }

    [ContextMenu("Depart")]
    public void Depart()
    {
        // When called after TravelSequence, engine is already idling — just ramp up and go.
        SetEngineState(StateDepart, departRampTime);

        _moveTween = transform.DOMove(exitPoint.position, driveOutDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(OnExited);
    }

    public void OpenDoorsForBoarding()
    {
        OpenDoors();
    }

    /// <summary>
    /// Plays a fake bus journey entirely through audio during the black screen.
    /// Fires onDoorsOpen the moment the destination doors open so BusDoor can
    /// fade the screen back in. Dwell and door-close are handled by BusDoor.
    ///
    /// Sequence: delay -> doors close sfx -> engine drives -> slows ->
    ///           air brake -> idle -> doors open sfx -> (onDoorsOpen fires)
    /// </summary>
    public void PlayTravelSequence(float delayBeforeDepart, float journeyDuration, float doorsOpenDwellTime, System.Action onDoorsOpen = null)
    {
        StartCoroutine(TravelSequence(delayBeforeDepart, journeyDuration, onDoorsOpen));
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
        SetEngineState(StateSlowing, slowingRampTime, then: () =>
            SetEngineState(StateIdle, idleRampTime));

        HUDNotification.Instance.Show("A bus has arrived.");
    }

    private void OnExited()
    {
        SetEngineState(StateOff, shutdownRampTime, then: () =>
        {
            engineSource.Stop();
            StopPitchVariance();
            gameObject.SetActive(false);
            transform.position = spawnPoint.position;
        });
    }

    private IEnumerator TravelSequence(float delayBeforeDepart, float journeyDuration, System.Action onDoorsOpen)
    {
        // 1. Brief pause -- player just stepped on
        yield return new WaitForSeconds(delayBeforeDepart);

        // 2. Doors close
        PlaySFX(doorCloseClip);
        yield return new WaitForSeconds(doorAnimDuration);

        // 3. Engine revs up -- bus pulls away
        SetEngineState(StateDepart, departRampTime);
        yield return new WaitForSeconds(journeyDuration);

        // 4. Slow down -- approaching destination
        SetEngineState(StateSlowing, slowingRampTime);
        yield return new WaitForSeconds(slowingRampTime);

        // 5. Air brake + engine settles to idle
        PlaySFX(airBrakeClip);
        SetEngineState(StateIdle, idleRampTime);
        yield return new WaitForSeconds(idleRampTime);

        // 6. Doors open -- BusDoor fades in from here
        PlaySFX(doorOpenClip);
        onDoorsOpen?.Invoke();
    }

    // ── Engine loop control ────────────────────────────────────────────────

    private void StartEngineLoop()
    {
        if (engineLoopClip == null || engineSource == null) return;

        engineSource.clip = engineLoopClip;
        engineSource.loop = true;
        engineSource.volume = 0f;
        engineSource.pitch = StateOff.pitch;
        engineSource.Play();

        StartPitchVariance();
    }

    private void SetEngineState((float pitch, float vol) state, float duration, System.Action then = null)
    {
        if (_engineRampCoroutine != null)
            StopCoroutine(_engineRampCoroutine);

        _currentPitchBase = state.pitch;
        _engineRampCoroutine = StartCoroutine(RampEngine(state.pitch, state.vol * engineBaseVolume, duration, then));
    }

    private IEnumerator RampEngine(float targetPitch, float targetVolume, float duration, System.Action onDone)
    {
        if (engineSource == null) yield break;

        float startPitch = engineSource.pitch;
        float startVolume = engineSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            engineSource.pitch = Mathf.Lerp(startPitch, targetPitch, t);
            engineSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        engineSource.pitch = targetPitch;
        engineSource.volume = targetVolume;
        _engineRampCoroutine = null;
        onDone?.Invoke();
    }

    // ── Organic pitch variance ─────────────────────────────────────────────

    private void StartPitchVariance()
    {
        StopPitchVariance();
        _pitchVarianceCoroutine = StartCoroutine(PitchVarianceLoop());
    }

    private void StopPitchVariance()
    {
        if (_pitchVarianceCoroutine != null)
        {
            StopCoroutine(_pitchVarianceCoroutine);
            _pitchVarianceCoroutine = null;
        }
    }

    private IEnumerator PitchVarianceLoop()
    {
        float targetOffset = 0f;
        float currentOffset = 0f;

        while (true)
        {
            targetOffset = Random.Range(-pitchVariance, pitchVariance);
            float elapsed = 0f;
            float startOffset = currentOffset;

            while (elapsed < pitchVarianceCycleTime)
            {
                elapsed += Time.deltaTime;
                currentOffset = Mathf.Lerp(startOffset, targetOffset, elapsed / pitchVarianceCycleTime);

                if (engineSource != null && _engineRampCoroutine == null)
                    engineSource.pitch = _currentPitchBase + currentOffset;

                yield return null;
            }

            currentOffset = targetOffset;
        }
    }

    // ── Doors ──────────────────────────────────────────────────────────────

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

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
}