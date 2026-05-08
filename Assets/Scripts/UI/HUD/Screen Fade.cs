// ScreenFade.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFade : MonoBehaviour
{
    public static ScreenFade Instance { get; private set; }

    [Header("UI Reference")]
    [Tooltip("A full-screen Image on your HUD canvas, color set to black.")]
    public Image fadeImage;

    [Header("Timing")]
    public float fadeDuration = 1f;

    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Start fully transparent
        SetAlpha(0f);
    }

    /// <summary>Fade to black then call onComplete.</summary>
    public void FadeOut(System.Action onComplete = null)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, onComplete));
    }

    /// <summary>Fade from black back to clear then call onComplete.</summary>
    public void FadeIn(System.Action onComplete = null)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(1f, 0f, onComplete));
    }

    /// <summary>Instantly snap to black with no fade.</summary>
    public void SnapToBlack() => SetAlpha(1f);

    /// <summary>Instantly snap to clear with no fade.</summary>
    public void SnapToClear() => SetAlpha(0f);

    private IEnumerator FadeRoutine(float from, float to, System.Action onComplete)
    {
        float elapsed = 0f;
        SetAlpha(from);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / fadeDuration));
            yield return null;
        }

        SetAlpha(to);
        _fadeCoroutine = null;
        onComplete?.Invoke();
    }

    private void SetAlpha(float alpha)
    {
        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }
}