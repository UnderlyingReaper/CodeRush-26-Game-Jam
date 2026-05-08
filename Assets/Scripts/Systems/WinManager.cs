using UnityEngine;

public class WinManager : MonoBehaviour
{
    public static WinManager Instance;
    void Awake() => Instance = this;
    public void TriggerWin() { /* hook up shuttle + door later */ }
}