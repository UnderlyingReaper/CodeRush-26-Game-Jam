using System.Collections;
using TMPro; // Required for TextMeshPro
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject controlsPanel;

    [Header("Screen Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Context Text")]
    [SerializeField] private TextMeshProUGUI contextText;
    [SerializeField] private float textFadeInDuration = 1f;
    [SerializeField] private float textDisplayDuration = 3f;
    [SerializeField] private float textFadeOutDuration = 1f;

    private void Start()
    {
        ShowMain();
        InputManager.Instance.DisableGameplay();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ensure text starts invisible
        if (contextText != null)
        {
            contextText.alpha = 0f;
            contextText.gameObject.SetActive(false);
        }
    }

    // --- Button Callbacks ---

    public void OnPlayClicked()
    {
        StartCoroutine(FadeAndLoad("Level 1"));
    }

    public void OnControlsClicked()
    {
        mainPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    public void OnBackClicked()
    {
        controlsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else        
        Application.Quit();
#endif
    }

    // --- Helpers ---

    private void ShowMain()
    {
        mainPanel.SetActive(true);
        controlsPanel.SetActive(false);
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        // Block input during the entire sequence
        fadeCanvasGroup.blocksRaycasts = true;

        // 1. Fade the screen to black
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        // 2. Handle Context Text Sequence
        if (contextText != null)
        {
            contextText.gameObject.SetActive(true);

            // Fade Text IN
            elapsed = 0f;
            while (elapsed < textFadeInDuration)
            {
                elapsed += Time.deltaTime;
                contextText.alpha = Mathf.Clamp01(elapsed / textFadeInDuration);
                yield return null;
            }
            contextText.alpha = 1f;

            // Wait for specified display duration
            yield return new WaitForSeconds(textDisplayDuration);

            // Fade Text OUT
            elapsed = 0f;
            while (elapsed < textFadeOutDuration)
            {
                elapsed += Time.deltaTime;
                contextText.alpha = 1f - Mathf.Clamp01(elapsed / textFadeOutDuration);
                yield return null;
            }
            contextText.alpha = 0f;
        }

        // 3. Load the Game Scene
        SceneManager.LoadScene(sceneName);
    }
}