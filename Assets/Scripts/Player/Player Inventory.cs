// PlayerInventory.cs
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    public bool HasCoin { get; set; } = false;
    public bool HasTicket { get; set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ResetForLoop()
    {
        HasCoin = false;
        HasTicket = false;
    }
}