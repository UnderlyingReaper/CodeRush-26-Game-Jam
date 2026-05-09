using System;
using System.Collections;
using UnityEngine;

public class Payphone : MonoBehaviour, IInteractable
{
    [Header("Interaction Strings")]
    [SerializeField] private string loop1Prompt = "Answer ringing phone";
    [SerializeField] private string loop2RingingPrompt = "Answer ringing phone";
    [SerializeField] private string pickUpPrompt = "Pick up receiver";
    [SerializeField] private string hangUpPrompt = "Hang up receiver";

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Dialogue")]
    [SerializeField] private DialogueLine loop2AnnouncementDialogue;

    [Header("Volumes")]
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.8f;
    [Range(0f, 1f)][SerializeField] private float voiceVolume = 1.0f;
    [Range(0f, 1f)][SerializeField] private float dialToneVolume = 0.5f;

    [Header("Clips")]
    [SerializeField] private AudioClip loop1Static;
    [SerializeField] private AudioClip loop1NotInService;
    [SerializeField] private AudioClip phoneRingClip;       // Ringing SFX for Loop 2 start
    [SerializeField] private AudioClip loop2Announcement;
    [SerializeField] private AudioClip loop2RouteConfirmedClip;
    [SerializeField] private AudioClip loop2RouteRejectedClip;
    [SerializeField] private AudioClip loop3DialTone;
    [SerializeField] private AudioClip hangUpClick;
    [SerializeField] private AudioClip wrongDigit1Clip;
    [SerializeField] private AudioClip wrongDigit2Clip;
    [SerializeField] private AudioClip wrongDigit3Clip;
    [SerializeField] private AudioClip shuttleConfirmedClip;

    [Header("Loop 2 Ring")]
    [Tooltip("Seconds after Loop 2 starts before the phone begins ringing.")]
    [SerializeField] private float loop2RingDelay = 2f;

    [SerializeField] string correctCode = "247";

    private bool receiverUp = false;
    private bool isBusy = false;
    private bool dialpadOpen = false;
    private bool loop2DialMode = false;
    private bool loop2IsRinging = false;

    // ─── Lifecycle ────────────────────────────────────────────────

    private void OnEnable()
    {
        LoopManager.OnLoopChanged += HandleLoopChanged;
        LoopManager.OnLoopRestarted += HandleLoopRestarted;
    }

    private void OnDisable()
    {
        LoopManager.OnLoopChanged -= HandleLoopChanged;
        LoopManager.OnLoopRestarted -= HandleLoopRestarted;
    }

    private void HandleLoopChanged(LoopStage stage)
    {
        if (stage == LoopStage.Loop2)
            StartCoroutine(Loop2RingSequence());
    }

    private void HandleLoopRestarted(LoopStage stage)
    {
        if (stage == LoopStage.Loop2)
        {
            ResetLoopState();
            StartCoroutine(Loop2RingSequence());
        }
    }

    // ─── IInteractable ────────────────────────────────────────────

    public bool CanInteract => !isBusy;

    public string GetPromptText()
    {
        if (LoopManager.Instance.CurrentLoop == LoopStage.Loop1)
            return isBusy ? "" : loop1Prompt;

        if (LoopManager.Instance.CurrentLoop == LoopStage.Loop2 && loop2IsRinging)
            return loop2RingingPrompt;

        return receiverUp ? hangUpPrompt : pickUpPrompt;
    }

    public void Interact()
    {
        if (!CanInteract) return;

        switch (LoopManager.Instance.CurrentLoop)
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

    // ─── Loop 2 Ring ──────────────────────────────────────────────

    private IEnumerator Loop2RingSequence()
    {
        yield return new WaitForSeconds(loop2RingDelay);

        // Start ringing — loops until player answers
        loop2IsRinging = true;
        audioSource.clip = phoneRingClip;
        audioSource.volume = sfxVolume;
        audioSource.loop = true;
        audioSource.Play();
    }

    // ─── Loop Sequences ───────────────────────────────────────────

    public void ResetLoopState()
    {
        StopAllCoroutines();
        loop2DialMode = false;
        loop2IsRinging = false;
        receiverUp = false;
        dialpadOpen = false;
        isBusy = false;
        audioSource.Stop();
        audioSource.loop = false;
    }

    private IEnumerator Loop1Sequence()
    {
        isBusy = true;
        receiverUp = true;

        PlayClip(loop1Static, sfxVolume);
        yield return new WaitForSeconds(2f);

        PlayClip(loop1NotInService, voiceVolume);
        yield return new WaitForSeconds(loop1NotInService ? loop1NotInService.length : 3f);

        ForceHangUp();
        isBusy = false;
    }

    private void ToggleLoop2()
    {
        if (loop2IsRinging)
        {
            // Player answers the ringing phone
            loop2IsRinging = false;
            audioSource.Stop();
            audioSource.loop = false;

            receiverUp = true;
            audioSource.clip = loop2Announcement;
            audioSource.volume = voiceVolume;
            audioSource.loop = true;
            audioSource.Play();

            if (loop2AnnouncementDialogue != null)
                DialogueManager.Instance.Show(loop2AnnouncementDialogue);
            return;
        }

        if (receiverUp && !loop2DialMode)
        {
            // Hang up after hearing announcement — enters dial mode
            ForceHangUp();
            loop2DialMode = true;
            return;
        }

        if (loop2DialMode && !receiverUp)
        {
            // Second pickup — open dialpad
            OpenLoop2Dialpad();
        }
    }

    private void OpenLoop2Dialpad()
    {
        receiverUp = true;
        PlayClip(loop3DialTone, dialToneVolume);

        if (DialpadUI.Instance != null)
            DialpadUI.Instance.OpenWithDigits(2, OnLoop2CodeSubmitted);
    }

    public void OnLoop2CodeSubmitted(string input)
    {
        isBusy = true;
        audioSource.Stop();
        audioSource.loop = false;

        string correctRoute = ScheduleBoard.Instance.loop2TargetRoute.ToString();

        if (input == correctRoute)
            StartCoroutine(Loop2WinSequence());
        else
            StartCoroutine(Loop2WrongSequence());
    }

    private IEnumerator Loop2WinSequence()
    {
        PlayClip(loop2RouteConfirmedClip, voiceVolume); // "Route confirmed. Stand by."
        yield return new WaitForSeconds(loop2RouteConfirmedClip ? loop2RouteConfirmedClip.length : 3f);

        ForceHangUp();
        isBusy = false;
        loop2DialMode = false;

        // Signal bus spawn — vending machine will dispense ticket via OnLoopPuzzleComplete
        LoopManager.Instance.CompleteCurrentLoopPuzzle();
    }

    private IEnumerator Loop2WrongSequence()
    {
        PlayClip(loop2RouteRejectedClip, voiceVolume); // "Route service unavailable."
        yield return new WaitForSeconds(loop2RouteRejectedClip ? loop2RouteRejectedClip.length : 3f);

        ForceHangUp();
        isBusy = false;
        loop2DialMode = false;

        LoopManager.Instance.RestartCurrentLoop();
    }

    // ─── Loop 3 ───────────────────────────────────────────────────

    private void OpenDialpad()
    {
        dialpadOpen = true;
        receiverUp = true;
        audioSource.loop = true;
        PlayClip(loop3DialTone, dialToneVolume);

        if (DialpadUI.Instance != null)
            DialpadUI.Instance.Open(OnCodeSubmitted);
    }

    private void CloseDialpad()
    {
        if (DialpadUI.Instance != null)
            DialpadUI.Instance.Close();
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

    // ─── Code Logic ───────────────────────────────────────────────

    public void OnCodeSubmitted(string input)
    {
        isBusy = true;
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

        if (input.Length > 0 && input[0] != correctCode[0]) response = wrongDigit1Clip;
        else if (input.Length > 1 && input[1] != correctCode[1]) response = wrongDigit2Clip;

        PlayClip(response, voiceVolume);
        yield return new WaitForSeconds(response ? response.length : 2f);

        ForceHangUp();
        isBusy = false;
        LoopManager.Instance.RestartCurrentLoop();
    }

    private IEnumerator WinSequence()
    {
        PlayClip(shuttleConfirmedClip, voiceVolume);
        yield return new WaitForSeconds(shuttleConfirmedClip ? shuttleConfirmedClip.length : 3f);

        LoopManager.Instance.TriggerRealBus();
        PlayerInventory.Instance.HasTicket = true;
    }

    private void PlayClip(AudioClip clip, float volume)
    {
        if (clip == null) return;
        audioSource.volume = volume;
        audioSource.clip = clip;
        audioSource.Play();
    }
}