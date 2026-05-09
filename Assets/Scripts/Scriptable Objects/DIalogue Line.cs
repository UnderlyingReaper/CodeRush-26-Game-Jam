// Assets/Scripts/Dialogue/DialogueLine.cs
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueLine", menuName = "LAYOVER/Dialogue Line")]
public class DialogueLine : ScriptableObject
{
    [TextArea(2, 4)]
    public string text;
    public float displayDuration = 3f;
}