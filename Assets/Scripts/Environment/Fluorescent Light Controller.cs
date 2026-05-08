using UnityEngine;
using System.Collections.Generic;

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
        Material targetMat = state ? litMaterial : offMaterial;
        foreach (MeshRenderer renderer in bulbRenderers)
        {
            renderer.sharedMaterial = targetMat;
        }

        // 2. Toggle Light Sources
        float intensity = state ? 1.0f : 0.0f;
        foreach (Light l in childLights)
        {
            l.intensity = intensity;
        }

        // 3. Sync Audio Hum with proportional volume dip
        if (humSource != null)
        {
            // Dimming the audio to 20% of base when flickering off 
            // simulates the power drop without cutting audio completely.
            humSource.volume = state ? baseVolume : baseVolume * 0.2f;
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