// Assets/Scripts/Dialogue/DialogueSequence.cs
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueSequence", menuName = "LAYOVER/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    public DialogueLine[] lines;
}