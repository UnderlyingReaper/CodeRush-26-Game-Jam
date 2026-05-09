// BusDoor.cs
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

    public bool CanInteract => true;

    public string GetPromptText() => PlayerInventory.Instance.HasTicket
        ? "Board Bus [E]"
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
        // Wait for doors to finish opening, then cut input
        yield return new WaitForSeconds(BusController.Instance.doorAnimDuration);
        InputManager.Instance.DisableGameplay();

        // Switch to bus cam and fade out
        BusController.Instance.cam1.gameObject.SetActive(true);
        bool fadeDone = false;
        ScreenFade.Instance.FadeOut(() => fadeDone = true);
        yield return new WaitUntil(() => fadeDone);

        // ── Black screen: fake travel soundscape ──────────────────────────

        bool isEscape = LoopManager.Instance.IsLoop3;

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
        bool fadeInDone = false;
        ScreenFade.Instance.FadeIn(() => fadeInDone = true);
        yield return new WaitUntil(() => fadeInDone);

        // Re-enable input so player can look around during the dwell
        InputManager.Instance.EnableGameplay();
        BusController.Instance.cam1.gameObject.SetActive(false);

        // Dwell: doors are open, player is standing at the stop
        yield return new WaitForSeconds(doorsOpenDwellTime);

        // Fade out again so the door-close and departure happen on black
        fadeDone = false;
        ScreenFade.Instance.FadeOut(() => fadeDone = true);
        yield return new WaitUntil(() => fadeDone);

        // ── Advance loop state ────────────────────────────────────────────

        LoopManager.Instance.AdvanceLoop();

        if (isEscape)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Credits");
            yield break;
        }

        // Bus departs, then fade back in to normal gameplay
        BusController.Instance.Depart();

        bool fadeBackDone = false;
        ScreenFade.Instance.FadeIn(() => fadeBackDone = true);
        yield return new WaitUntil(() => fadeBackDone);
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