// Assets/Scripts/Dialogue/DialogueManager.cs
using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Fade Settings")]
    [SerializeField] private float fadeTime = 0.3f;

    private Coroutine _activeRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// Show a single line for a set duration.
    public void Show(string text, float duration = 3f)
    {
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(ShowRoutine(text, duration));
    }

    /// Show a pre-authored DialogueLine SO.
    public void Show(DialogueLine line) => Show(line.text, line.displayDuration);

    /// Play a full sequence, one line after another.
    public void PlaySequence(DialogueSequence sequence)
    {
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(SequenceRoutine(sequence));
    }

    /// Hide immediately (e.g. on loop reset).
    public void HideImmediate()
    {
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        canvasGroup.alpha = 0f;
    }

    // ── Coroutines ──────────────────────────────────────────────────────────

    private IEnumerator ShowRoutine(string text, float duration)
    {
        dialogueText.text = text;
        yield return Fade(0f, 1f);
        yield return new WaitForSeconds(duration);
        yield return Fade(1f, 0f);
    }

    private IEnumerator SequenceRoutine(DialogueSequence sequence)
    {
        foreach (var line in sequence.lines)
        {
            dialogueText.text = line.text;
            yield return Fade(0f, 1f);
            yield return new WaitForSeconds(line.displayDuration);
            yield return Fade(1f, 0f);
        }
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeTime)
        {
            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeTime);
            t += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}