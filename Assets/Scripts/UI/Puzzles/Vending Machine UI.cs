// VendingMachineUI.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VendingMachineUI : MonoBehaviour
{
    public static VendingMachineUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject panel;

    [Header("Display")]
    public TextMeshProUGUI selectionDisplay;
    public TextMeshProUGUI feedbackText;

    [Header("Buttons — drag each in order A1-A5, B1-B5, C1-C5")]
    public Button[] keypadButtons;

    [Header("Confirm / Cancel")]
    public Button confirmButton;
    public Button cancelButton;

    [Header("Puzzle Config")]
    [Tooltip("The correct slot e.g. B1")]
    public string correctSlot = "B1";

    private string _currentSelection = string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        panel.SetActive(false);
    }

    private void Start()
    {
        // Wire up each button using its own label text as the slot name
        foreach (Button btn in keypadButtons)
        {
            string slot = btn.GetComponentInChildren<TextMeshProUGUI>().text;
            btn.onClick.AddListener(() => OnSlotPressed(slot));
        }

        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(Close);

        feedbackText.gameObject.SetActive(false);
    }

    public void Open()
    {
        _currentSelection = string.Empty;
        UpdateDisplay();
        ClearFeedback();
        panel.SetActive(true);
        InputManager.Instance.DisableGameplay();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        panel.SetActive(false);
        InputManager.Instance.EnableGameplay();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnSlotPressed(string slot)
    {
        _currentSelection = slot;
        UpdateDisplay();
        ClearFeedback();
    }

    private void OnConfirm()
    {
        if (string.IsNullOrEmpty(_currentSelection))
        {
            ShowFeedback("SELECT A SLOT FIRST", Color.yellow);
            return;
        }

        if (_currentSelection == correctSlot)
            StartCoroutine(CorrectInputRoutine());
        else
            StartCoroutine(WrongInputRoutine());
    }

    private IEnumerator CorrectInputRoutine()
    {
        ShowFeedback("DISPENSING...", Color.green);
        yield return new WaitForSeconds(1.5f);
        Close();
        VendingMachine.Instance.OnCorrectInput();
    }

    private IEnumerator WrongInputRoutine()
    {
        ShowFeedback("INVALID SELECTION", Color.red);
        yield return new WaitForSeconds(1.5f);
        Close();
        VendingMachine.Instance.OnWrongInput();
    }

    private void UpdateDisplay()
    {
        selectionDisplay.text = string.IsNullOrEmpty(_currentSelection)
            ? "SELECT SLOT"
            : _currentSelection;
    }

    private void ShowFeedback(string message, Color color)
    {
        feedbackText.text = message;
        feedbackText.color = color;
        feedbackText.gameObject.SetActive(true);
    }

    private void ClearFeedback()
    {
        feedbackText.gameObject.SetActive(false);
    }
}