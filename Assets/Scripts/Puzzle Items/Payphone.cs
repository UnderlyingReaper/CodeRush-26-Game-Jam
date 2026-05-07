using System;
using System.Collections;
using UnityEngine;

public class Payphone : MonoBehaviour, IInteractable
{
    [Header("Interaction Strings")]
    [SerializeField] private string loop1Prompt = "Answer ringing phone";
    [SerializeField] private string pickUpPrompt = "Pick up receiver";
    [SerializeField] private string hangUpPrompt = "Hang up receiver";

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Volumes")]
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.8f;
    [Range(0f, 1f)][SerializeField] private float voiceVolume = 1.0f;
    [Range(0f, 1f)][SerializeField] private float dialToneVolume = 0.5f;

    [Header("Clips")]
    [SerializeField] private AudioClip loop1Static;
    [SerializeField] private AudioClip loop1NotInService;
    [SerializeField] private AudioClip loop2Announcement;
    [SerializeField] private AudioClip loop3DialTone;
    [SerializeField] private AudioClip hangUpClick;
    [SerializeField] private AudioClip wrongDigit1Clip;
    [SerializeField] private AudioClip wrongDigit2Clip;
    [SerializeField] private AudioClip wrongDigit3Clip;
    [SerializeField] private AudioClip shuttleConfirmedClip;

    [SerializeField] string correctCode = "247";
    private bool receiverUp = false;
    private bool isBusy = false; // Internal flag for lock-out
    private bool dialpadOpen = false;

    // ─── IInteractable Implementation ──────────────────────────

    public bool CanInteract => !isBusy;

    public string GetPromptText()
    {
        if (LoopManager.Instance.CurrentLoop == LoopStage.Loop1)
        {
            return isBusy ? "" : loop1Prompt;
        }

        return receiverUp ? hangUpPrompt : pickUpPrompt;
    }

    public void Interact()
    {
        if (!CanInteract) return;

        LoopStage currentLoop = LoopManager.Instance.CurrentLoop;

        switch (currentLoop)
        {
            case LoopStage.Loop1:
                StartCoroutine(Loop1Sequence());
                break;
            case LoopStage.Loop2:
                ToggleLoop2();
                break;
            case LoopStage.Loop3:
                if (!dialpadOpen) OpenDialpad();
                else CloseDialpad();
                break;
        }
    }

    // ─── Loop Sequences ──────────────────────────────────────────

    private IEnumerator Loop1Sequence()
    {
        isBusy = true; // Lock interaction
        receiverUp = true;

        PlayClip(loop1Static, sfxVolume);
        yield return new WaitForSeconds(2f);

        PlayClip(loop1NotInService, voiceVolume); // "This line is not in service" [cite: 86]
        yield return new WaitForSeconds(loop1NotInService ? loop1NotInService.length : 3f);

        ForceHangUp();
        isBusy = false; // Unlock
    }

    private void ToggleLoop2()
    {
        if (!receiverUp)
        {
            receiverUp = true;
            audioSource.clip = loop2Announcement; // Automated transit announcement [cite: 86]
            audioSource.volume = voiceVolume;
            audioSource.loop = true;
            audioSource.Play();
        }
        else
        {
            ForceHangUp();
        }
    }

    private void OpenDialpad()
    {
        dialpadOpen = true;
        receiverUp = true;
        audioSource.loop = true;
        PlayClip(loop3DialTone, dialToneVolume); // 

        if (DialpadUI.Instance != null)
            DialpadUI.Instance.Open(OnCodeSubmitted); // Opens UI and disables movement
    }

    private void CloseDialpad()
    {
        if (DialpadUI.Instance != null)
            DialpadUI.Instance.Close(); // Closes UI and enables movement
        ForceHangUp();
    }

    private void ForceHangUp()
    {
        audioSource.Stop();
        audioSource.loop = false;
        PlayClip(hangUpClick, sfxVolume);
        receiverUp = false;
        dialpadOpen = false;
    }

    // ─── Code Logic ──────────────────────────────────────────────

    public void OnCodeSubmitted(string input)
    {
        isBusy = true; // Prevent player from hanging up mid-response
        audioSource.Stop();
        audioSource.loop = false;

        if (input == correctCode)
            StartCoroutine(WinSequence());
        else
            StartCoroutine(WrongCodeSequence(input));
    }

    private IEnumerator WrongCodeSequence(string input)
    {
        AudioClip response = wrongDigit3Clip;

        // GDD Informative Failure Logic [cite: 97]
        if (input.Length > 0 && input[0] != correctCode[0]) response = wrongDigit1Clip;
        else if (input.Length > 1 && input[1] != correctCode[1]) response = wrongDigit2Clip;

        PlayClip(response, voiceVolume);
        yield return new WaitForSeconds(response ? response.length : 2f);

        ForceHangUp();
        isBusy = false;
        LoopManager.Instance.RestartCurrentLoop(); // [cite: 99]
    }

    private IEnumerator WinSequence()
    {
        PlayClip(shuttleConfirmedClip, voiceVolume); // "Your shuttle is two minutes away" [cite: 90]
        yield return new WaitForSeconds(shuttleConfirmedClip ? shuttleConfirmedClip.length : 3f);

        LoopManager.Instance.TriggerRealBus(); // [cite: 91]
    }

    private void PlayClip(AudioClip clip, float volume)
    {
        if (clip == null) return;
        audioSource.volume = volume;
        audioSource.clip = clip;
        audioSource.Play();
    }
}