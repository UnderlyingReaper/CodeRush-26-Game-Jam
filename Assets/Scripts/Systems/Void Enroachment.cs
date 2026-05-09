using UnityEngine;
using System.Collections;
using System;

public class VoidEncroachment : MonoBehaviour
{
    public static VoidEncroachment Instance { get; private set; }

    [Header("Fog Targets (End Distance)")]
    [Tooltip("Loop 1 default open state")]
    public float loop1Fog = 50f;
    [Tooltip("Loop 2 intermediate state")]
    public float loop2Fog = 35f;
    [Tooltip("Loop 3 maximum encroachment")]
    public float loop3Fog = 20f;

    [Tooltip("How long the fog takes to close in when the loop changes")]
    public float transitionDuration = 3f;

    [Header("Physical Boundaries")]
    [Tooltip("The wall on the left road that seals in Loop 2")]
    public GameObject leftBoundaryWall;
    [Tooltip("The wall on the right road that seals in Loop 3")]
    public GameObject rightBoundaryWall;

    public static event Action OnLoop2WallCreated;
    public static event Action OnLoop3WallsCreated;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(this);
    }

    void Start()
    {
        // Ensure we start with Loop 1 settings natively
        RenderSettings.fogMode = FogMode.Linear;
        ApplyLoopState(LoopStage.Loop1);

        LoopManager.OnLoopChanged += ApplyLoopState;
    }

    /// <summary>
    /// Call this from your Loop Manager when the player resets.
    /// </summary>
    /// <param name="loopNumber">The current loop (1, 2, or 3)</param>
    /// <param name="instant">If true, snaps the fog immediately (useful for initialization)</param>
    public void ApplyLoopState(LoopStage stage)
    {
        float targetFogEnd = loop1Fog;

        // Handle physical walls based on the loop count
        switch (stage)
        {
            case LoopStage.Loop1:
                targetFogEnd = loop1Fog;
                leftBoundaryWall.SetActive(false);
                rightBoundaryWall.SetActive(false);
                break;

            case LoopStage.Loop2:
                targetFogEnd = loop2Fog;
                // The left road fades into solid black fog and becomes impassable 
                leftBoundaryWall.SetActive(true);
                rightBoundaryWall.SetActive(false);

                OnLoop2WallCreated?.Invoke();
                break;

            case LoopStage.Loop3:
            default: // Handle loop 3 and the final escape sequence
                targetFogEnd = loop3Fog;
                // Both directions seal 
                leftBoundaryWall.SetActive(true);
                rightBoundaryWall.SetActive(true);
                OnLoop3WallsCreated?.Invoke();

                break;
        }

        RenderSettings.fogEndDistance = targetFogEnd;
    }
}