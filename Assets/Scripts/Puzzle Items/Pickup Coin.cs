// CoinPickup.cs
using UnityEngine;

public class CoinPickup : MonoBehaviour, IInteractable
{
    public bool CanInteract => !PlayerInventory.Instance.HasCoin;

    public string GetPromptText() => "Pick up Coin ";

    public void Interact()
    {
        PlayerInventory.Instance.HasCoin = true;
        HUDNotification.Instance.Show("Picked up: Coin");
        gameObject.SetActive(false);
    }
}