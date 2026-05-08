using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI promptText;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(string text)
    {
        promptText.text = text;
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}