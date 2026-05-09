using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class BusDoor : MonoBehaviour, IInteractable
{
    [Header("Journey Timing")]
    [Tooltip("Pause after screen goes black before the doors close and bus pulls away.")]
    public float delayBeforeDepart = 0.4f;

    [Tooltip("How long the bus is 'driving' between stops.")]
    public float journeyDuration = 4f;

    [Tooltip("How long the doors stay open (and screen is visible) at the fake destination.")]
    public float doorsOpenDwellTime = 1.2f;

    [Header("Loop Start Dialogue")]
    public float dialogueDelay = 2.0f;
    [TextArea] public string loop2Text = "I'm back where I started...";
    [TextArea] public string loop3Text = "Not this place again...";

    public bool CanInteract => true;

    public string GetPromptText() => PlayerInventory.Instance.HasTicket
        ? "Board Bus"
        : "You need a ticket";

    public void Interact()
    {
        if (!PlayerInventory.Instance.HasTicket)
        {
            HUDNotification.Instance.Show("You need a ticket to board.");
            return;
        }

        PlayerInventory.Instance.HasTicket = false;
        BusController.Instance.OpenDoorsForBoarding();
        StartCoroutine(BoardingSequence());
    }

    private IEnumerator BoardingSequence()
    {
        // 1. Wait for doors to finish opening, then cut input
        yield return new WaitForSeconds(BusController.Instance.doorAnimDuration);
        InputManager.Instance.DisableGameplay();

        // Switch to bus cam and fade out
        BusController.Instance.cam1.gameObject.SetActive(true);
        bool fadeDone = false;
        ScreenFade.Instance.FadeOut(() => fadeDone = true);
        yield return new WaitUntil(() => fadeDone);

        // ──────────────────────────────────────────────────────────────────
        // ENDING ESCAPE (LOOP 3)
        // ──────────────────────────────────────────────────────────────────
        if (LoopManager.Instance.IsLoop3)
        {
            // 1. Close the bus doors
            BusController.Instance.CloseDoors();
            yield return new WaitForSeconds(BusController.Instance.doorAnimDuration);

            // 2. Tell the bus to physically drive away
            BusController.Instance.Depart();

            // 3. Disable the bus cam so your cutscene camera takes over
            BusController.Instance.cam1.gameObject.SetActive(false);

            // 4. Trigger the cutscene
            LoopManager.Instance.PLayEndCutscene();

            // 5. Unfade the screen so the player can watch the cutscene
            bool fadeInDone = false;
            ScreenFade.Instance.FadeIn(() => fadeInDone = true);
            yield return new WaitUntil(() => fadeInDone);

            // Stop the coroutine here. The PlayableDirector handles the rest of the game.
            yield break;
        }

        // ──────────────────────────────────────────────────────────────────
        // NORMAL LOOP BOARDING (LOOP 1 & 2)
        // ──────────────────────────────────────────────────────────────────

        // PlayTravelSequence fires onDoorsOpen the moment the fake doors open,
        // which is our cue to fade back in mid-sequence.
        bool doorsOpenedAtDestination = false;
        BusController.Instance.PlayTravelSequence(
            delayBeforeDepart,
            journeyDuration,
            doorsOpenDwellTime,
            onDoorsOpen: () => doorsOpenedAtDestination = true
        );

        // Wait until the travel sequence signals doors open
        yield return new WaitUntil(() => doorsOpenedAtDestination);

        // Teleport player while still black, before we reveal anything
        TeleportPlayerToExit();

        // Fade back in — player sees the bus stop through the open doors
        bool dwellFadeInDone = false;
        ScreenFade.Instance.FadeIn(() => dwellFadeInDone = true);
        yield return new WaitUntil(() => dwellFadeInDone);

        // Re-enable input so player can look around during the dwell
        InputManager.Instance.EnableGameplay();
        BusController.Instance.cam1.gameObject.SetActive(false);

        // Dwell: doors are open, player is standing at the stop
        yield return new WaitForSeconds(doorsOpenDwellTime);

        // Fade out again so the door-close and departure happen on black
        fadeDone = false;
        ScreenFade.Instance.FadeOut(() => fadeDone = true);
        yield return new WaitUntil(() => fadeDone);

        // Close the physical doors and wait for the animation
        BusController.Instance.CloseDoors();
        yield return new WaitForSeconds(BusController.Instance.doorAnimDuration);

        // Advance loop state
        LoopManager.Instance.AdvanceLoop();

        // Bus departs physically
        BusController.Instance.Depart();

        // Fade back in to normal gameplay
        bool finalFadeInDone = false;
        ScreenFade.Instance.FadeIn(() => finalFadeInDone = true);
        yield return new WaitUntil(() => finalFadeInDone);

        // Trigger delayed dialogue
        StartCoroutine(PlayDelayedDialogue());
    }

    private IEnumerator PlayDelayedDialogue()
    {
        yield return new WaitForSeconds(dialogueDelay);

        if (LoopManager.Instance.IsLoop2)
        {
            DialogueManager.Instance.Show(loop2Text);
        }
        else if (LoopManager.Instance.IsLoop3)
        {
            DialogueManager.Instance.Show(loop3Text);
        }
    }

    private void TeleportPlayerToExit()
    {
        CharacterController cc = PlayerInventory.Instance.GetComponent<CharacterController>();
        cc.enabled = false;
        PlayerInventory.Instance.transform.position = BusController.Instance.exitSpawnPoint.position;
        cc.enabled = true;

        CinemachineCamera vcam = InputManager.Instance.cameraInput.GetComponent<CinemachineCamera>();
        if (vcam != null)
        {
            var panTilt = vcam.GetComponent<CinemachinePanTilt>();
            if (panTilt != null)
            {
                panTilt.TiltAxis.Value = 0f;
                panTilt.PanAxis.Value = 180f;
            }
        }
    }
}