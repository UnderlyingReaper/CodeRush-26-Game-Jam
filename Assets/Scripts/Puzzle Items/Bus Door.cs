// BusDoor.cs
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class BusDoor : MonoBehaviour, IInteractable
{
    [Header("Boarding")]
    [Tooltip("How long the player is frozen on the black screen before bus departs.")]
    public float boardingDelay = 2f;

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
        BusController.Instance.cam1.gameObject.SetActive(true);
        yield return new WaitForSeconds(BusController.Instance.doorAnimDuration);

        InputManager.Instance.DisableGameplay();

        bool fadeDone = false;
        ScreenFade.Instance.FadeOut(() => fadeDone = true);
        yield return new WaitUntil(() => fadeDone);

        bool isEscape = LoopManager.Instance.IsLoop3;

        LoopManager.Instance.AdvanceLoop();

        yield return new WaitForSeconds(boardingDelay);

        if (isEscape)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Credits");
            yield break;
        }

        TeleportPlayerToExit();
        InputManager.Instance.EnableGameplay();
        BusController.Instance.cam1.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.5f);

        BusController.Instance.Depart();

        bool fadeInDone = false;
        ScreenFade.Instance.FadeIn(() => fadeInDone = true);
        yield return new WaitUntil(() => fadeInDone);
    }

    private void TeleportPlayerToExit()
    {
        // Teleport the CharacterController
        CharacterController cc = PlayerInventory.Instance.GetComponent<CharacterController>();
        cc.enabled = false;
        PlayerInventory.Instance.transform.position = BusController.Instance.exitSpawnPoint.position;
        cc.enabled = true;

        // Force CinemachinePanTilt Y axis to 180 (facing away from bus)
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