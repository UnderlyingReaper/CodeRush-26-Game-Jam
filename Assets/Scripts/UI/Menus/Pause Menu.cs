using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering; // Required for Volume
using UnityEngine.Rendering.Universal; // Required for URP effects

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject mainOptionsPanel;
    [SerializeField] private GameObject controlsPanel;

    [Header("Blur Settings")]
    [SerializeField] private Volume globalVolume;

    private DepthOfField _blurEffect;
    private bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        pausePanel.SetActive(false);

        // Find the Depth of Field effect in your volume profile
        if (globalVolume != null && globalVolume.profile.TryGet(out DepthOfField blur))
        {
            _blurEffect = blur;
            _blurEffect.focusDistance.value = 10f; // Ensure it starts clear
        }
    }

    private void Start()
    {
        InputManager.Instance.OnPause += HandleInput;
    }

    void HandleInput()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        pausePanel.SetActive(true);
        ShowMainPause();

        // Instantly set Focus Distance to 0 (Blurred)
        SetBlur(0f);

        InputManager.Instance.DisableGameplay();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        pausePanel.SetActive(false);

        // Instantly set Focus Distance to 10 (Clear)
        SetBlur(10f);

        InputManager.Instance.EnableGameplay();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetBlur(float targetValue)
    {
        if (_blurEffect != null)
        {
            _blurEffect.focusDistance.value = targetValue;
        }
    }

    // ─── Navigation ─────────────────────────────────────────────

    public void ShowControls()
    {
        mainOptionsPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    public void ShowMainPause()
    {
        mainOptionsPanel.SetActive(true);
        controlsPanel.SetActive(false);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}