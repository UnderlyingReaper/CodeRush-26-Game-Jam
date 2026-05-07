// TicketPickup.cs
using UnityEngine;

public class TicketPickup : MonoBehaviour, IInteractable
{
    public bool CanInteract => true;

    public string GetPromptText() => "Pick up Ticket";

    public void Interact()
    {
        PlayerInventory.Instance.HasTicket = true;
        HUDNotification.Instance.Show("Picked up: Bus Ticket");
        gameObject.SetActive(false);
    }
}