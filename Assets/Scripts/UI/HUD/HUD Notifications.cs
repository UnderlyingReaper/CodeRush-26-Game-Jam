// HUDNotification.cs
using System.Collections;
using UnityEngine;
using TMPro;

public class HUDNotification : MonoBehaviour
{
    public static HUDNotification Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI notificationText;

    [Header("Timing")]
    public float displayDuration = 2.5f;
    public float fadeDuration = 0.5f;

    private Coroutine _activeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetAlpha(0f);
    }

    public void Show(string message)
    {
        if (notificationText == null)
        {
            Debug.LogWarning("HUDNotification: notificationText reference is missing!");
            return;
        }

        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        notificationText.text = message;
        _activeCoroutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        SetAlpha(1f);
        yield return new WaitForSeconds(displayDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(1f - (elapsed / fadeDuration));
            yield return null;
        }

        SetAlpha(0f);
        _activeCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        Color c = notificationText.color;
        c.a = alpha;
        notificationText.color = c;
    }
}