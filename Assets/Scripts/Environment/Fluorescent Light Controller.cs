using UnityEngine;
using System.Collections.Generic;
using DG.Tweening; // Required for DOTween

public class FluorescentController : MonoBehaviour
{
    [Header("Light Components")]
    public List<Light> childLights;
    public List<MeshRenderer> bulbRenderers;

    [Header("Materials")]
    [Tooltip("The 'On' state material using Hex #E8F4E8 for a cold white-green glow.")]
    public Material litMaterial;
    [Tooltip("The 'Off' state material. Should look like dull, unlit glass.")]
    public Material offMaterial;

    [Header("Flicker Settings")]
    [Tooltip("The length of one full cycle in seconds.")]
    public float cycleDuration = 4.0f;
    [Tooltip("Probability of the light staying 'On'. Lower values cause more frequent/longer flickering.")]
    [Range(0, 1)] public float flickerThreshold = 0.85f;
    [Tooltip("Speed of the noise sampling. Higher values create faster, more 'staccato' flickering.")]
    public float flickerSpeed = 15f;

    [Tooltip("How long it takes to smoothly fade between the on and off states.")]
    public float smoothDuration = 0.1f;

    [Header("Audio")]
    public AudioSource humSource;
    [Tooltip("The constant 60Hz hum volume when the light is fully 'On'.")]
    [Range(0, 1)] public float baseVolume = 0.15f;
    [Tooltip("The starting pitch for Loop 1.")]
    public float basePitch = 1.0f;
    [Tooltip("GDD Source 133: How much the pitch increases per loop to create unease.")]
    public float pitchIncreasePerLoop = 0.05f;

    private float timer;
    private bool wasOn = true;

    private void OnEnable()
    {
        LoopManager.OnLoopChanged += HandleLoopChanged;
    }

    private void OnDisable()
    {
        LoopManager.OnLoopChanged -= HandleLoopChanged;

        // Safety: Kill tweens when disabled to prevent errors if the object is destroyed
        foreach (Light l in childLights)
        {
            if (l != null) l.DOKill();
        }
        if (humSource != null) humSource.DOKill();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // Flicker Logic (GDD 4-second cycle integration)
        // We use PerlinNoise for a more organic, 'unstable' electrical feel
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0);
        bool isOn = noise < flickerThreshold;

        if (isOn != wasOn)
        {
            UpdateSystemState(isOn);
            wasOn = isOn;
        }
    }

    private void UpdateSystemState(bool state)
    {
        // 1. Swap Materials for physical bulb feedback 
        // (Note: Material swaps are instant. If you want smooth material transitions later, 
        // you would need to tween the Emission Color property of a single Material instance).
        Material targetMat = state ? litMaterial : offMaterial;
        foreach (MeshRenderer renderer in bulbRenderers)
        {
            renderer.sharedMaterial = targetMat;
        }

        // 2. Smoothly Toggle Light Sources using DOTween
        float targetIntensity = state ? 1.0f : 0.0f;
        foreach (Light l in childLights)
        {
            // Always kill the active tween on this object before starting a new one
            l.DOKill();
            l.DOIntensity(targetIntensity, smoothDuration).SetEase(Ease.InOutSine);
        }

        // 3. Smoothly Sync Audio Hum with proportional volume dip
        if (humSource != null)
        {
            humSource.DOKill();
            float targetVolume = state ? baseVolume : baseVolume * 0.2f;
            humSource.DOFade(targetVolume, smoothDuration).SetEase(Ease.InOutSine);
        }
    }

    private void HandleLoopChanged(LoopStage stage)
    {
        if (humSource != null)
        {
            // Converts LoopStage enum (0, 1, 2) to pitch modifiers
            int loopIndex = (int)stage;
            humSource.pitch = basePitch + (loopIndex * pitchIncreasePerLoop);
        }
    }
}