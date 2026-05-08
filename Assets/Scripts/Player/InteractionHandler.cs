using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private LayerMask interactableLayer;

    private IInteractable currentTarget;

    private void Start()
    {
        InputManager.Instance.OnInteract += HandleInput;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnInteract -= HandleInput;
    }

    void Update()
    {
        HandleRaycast();
    }

    void HandleRaycast()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            // Case 1: We found a valid, interactable target
            if (interactable != null && interactable.CanInteract)
            {
                // Update UI if the target is new OR if the prompt text needs to refresh
                if (interactable != currentTarget)
                {
                    currentTarget = interactable;
                }

                // Always refresh the prompt text in case the state changed (Pick up vs Hang up)
                InteractionUI.Instance.Show(currentTarget.GetPromptText());
            }
            // Case 2: Found something on the layer, but it's currently "Busy"
            else
            {
                ClearTarget();
            }
        }
        // Case 3: Hit nothing
        else
        {
            ClearTarget();
        }
    }

    void HandleInput()
    {
        // Double check CanInteract here to prevent input lag/misfires
        if (currentTarget != null && currentTarget.CanInteract)
        {
            currentTarget.Interact();

            // Immediately check if it became busy so the UI hides instantly
            if (!currentTarget.CanInteract)
            {
                ClearTarget();
            }
        }
    }

    private void ClearTarget()
    {
        if (currentTarget != null)
        {
            currentTarget = null;
            InteractionUI.Instance.Hide();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(Camera.main.transform.position, Camera.main.transform.position + (Camera.main.transform.forward * interactRange));
    }
}