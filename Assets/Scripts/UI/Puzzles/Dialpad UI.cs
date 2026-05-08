using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class DialpadUI : MonoBehaviour
{
    public static DialpadUI Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject dialpadPanel;
    [SerializeField] private TextMeshProUGUI inputDisplayText;

    [Header("Settings")]
    [SerializeField] private int maxDigits = 3;

    private string currentInput = "";
    private Action<string> onCompleteCallback;

    private void Awake()
    {
        Instance = this;
        dialpadPanel.SetActive(false);
    }

    public void Open(Action<string> onComplete)
    {
        maxDigits = 3; // reset to default
        onCompleteCallback = onComplete;
        currentInput = "";
        UpdateDisplay();

        dialpadPanel.SetActive(true);
        InputManager.Instance.DisableGameplay();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenWithDigits(int digits, Action<string> onComplete)
    {
        maxDigits = digits;
        onCompleteCallback = onComplete;
        currentInput = "";
        UpdateDisplay();

        dialpadPanel.SetActive(true);
        InputManager.Instance.DisableGameplay();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        dialpadPanel.SetActive(false);

        // Re-enable movement and hide cursor
        InputManager.Instance.EnableGameplay();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void PressKey(string key)
    {
        if (currentInput.Length < maxDigits)
        {
            currentInput += key;
            UpdateDisplay();

            if (currentInput.Length == maxDigits)
            {
                Submit();
            }
        }
    }

    public void Backspace()
    {
        if (currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        string display = "";
        for (int i = 0; i < maxDigits; i++)
        {
            display += (i < currentInput.Length) ? currentInput[i].ToString() : "_";
        }
        inputDisplayText.text = display;
    }

    public void Submit()
    {
        onCompleteCallback?.Invoke(currentInput);
        Close();
    }
}