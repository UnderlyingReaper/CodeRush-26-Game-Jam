using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScheduleBoard : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI boardTextDisplay;

    [Header("Timing")]
    [Tooltip("How many seconds before the board refreshes its camouflage routes.")]
    public float cycleInterval = 4.0f;

    [Header("Loop 1 Puzzle Data")]
    public int loop1TargetRoute = 7;
    public string loop1Date = "November 14";

    [Header("Display Settings")]
    [Tooltip("How many camouflage routes to show alongside the anomaly.")]
    public int camouflageCount = 5;

    private readonly List<string> normalRoutes = new List<string>
    {
        "Route 1  : 02:41 AM - To [Anavala]",
        "Route 2  : 03:15 AM - To [Oakhaven]",
        "Route 3  : 01:50 AM - To [Pine Ridge]",
        "Route 4  : 04:00 AM - To [Sable]",
        "Route 5  : 02:10 AM - To [Kessler]",
        "Route 6  : 03:30 AM - To [Declan]",
        "Route 8  : 01:15 AM - To [Sienna]",
        "Route 9  : 04:45 AM - To [Marshfield]",
        "Route 10 : 02:55 AM - To [Colton]",
        "Route 11 : 03:00 AM - To [Harwick]",
        "Route 12 : 01:30 AM - To [Vellum]",
        "Route 13 : 04:20 AM - To [Dunmore]",
        "Route 14 : 02:05 AM - To [Ashford]",
        "Route 15 : 03:50 AM - To [Greystone]",
        "Route 16 : 01:00 AM - To [Caldwell]",
    };

    private Coroutine _cycleCoroutine;

    private void OnEnable()
    {
        if (_cycleCoroutine == null)
            _cycleCoroutine = StartCoroutine(CycleRoutine());
    }

    private void OnDisable()
    {
        if (_cycleCoroutine != null)
        {
            StopCoroutine(_cycleCoroutine);
            _cycleCoroutine = null;
        }
    }

    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            GenerateBoardText();
            yield return new WaitForSeconds(cycleInterval);
        }
    }

    private void GenerateBoardText()
    {
        // Pick camouflage routes — no duplicates per cycle
        List<string> pool = new List<string>(normalRoutes);
        List<string> displayed = new List<string>();

        int count = Mathf.Min(camouflageCount, pool.Count);
        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            displayed.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        // Always inject Route 7 anomaly in Loop 1 at a random position
        // It is guaranteed to appear every cycle — player just has to
        // watch long enough to catch it among the shuffling routes
        if (LoopManager.Instance != null && LoopManager.Instance.IsLoop1)
        {
            string anomaly = $"Route {loop1TargetRoute} : 02:41 AM - To [{loop1Date}]";
            int insertAt = Random.Range(0, displayed.Count + 1);
            displayed.Insert(insertAt, anomaly);
        }

        string newText = string.Empty;
        foreach (string line in displayed)
            newText += line + "\n";

        if (boardTextDisplay != null)
            boardTextDisplay.text = newText;
        else
            Debug.LogWarning("ScheduleBoard: boardTextDisplay reference is missing!");
    }
}