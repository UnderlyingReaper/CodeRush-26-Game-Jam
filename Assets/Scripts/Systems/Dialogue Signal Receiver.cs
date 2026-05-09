// Assets/Scripts/Dialogue/DialogueSignalEmitter.cs
using UnityEngine;
using UnityEngine.Timeline;  // required for INotificationReceiver
using UnityEngine.Playables;

// ── The notification payload ─────────────────────────────────────────────

/// Drop one of these on a Signal Track marker in the Timeline.
[System.Serializable]
public class DialogueSignal : UnityEngine.Object, INotification
{
    public PropertyName id => new PropertyName("DialogueSignal");

    [TextArea(2, 4)]
    public string inlineText;       // optional: type text directly in the Timeline
    public float inlineDuration = 3f;

    public DialogueLine line;       // optional: use a SO instead
    public DialogueSequence sequence; // optional: play a full sequence
}

// ── The receiver ─────────────────────────────────────────────────────────

/// Attach this to the same GameObject as your PlayableDirector.
public class DialogueSignalReceiver : MonoBehaviour, INotificationReceiver
{
    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (notification is not DialogueSignal signal) return;

        if (signal.sequence != null)
        {
            DialogueManager.Instance.PlaySequence(signal.sequence);
        }
        else if (signal.line != null)
        {
            DialogueManager.Instance.Show(signal.line);
        }
        else if (!string.IsNullOrEmpty(signal.inlineText))
        {
            DialogueManager.Instance.Show(signal.inlineText, signal.inlineDuration);
        }
    }
}