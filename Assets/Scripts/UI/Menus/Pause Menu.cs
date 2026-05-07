using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject mainOptionsPanel;
    [SerializeField] private GameObject controlsPanel;

    private bool isPaused = false;

    private void Awake()
    {
        Instance = this;
        pausePanel.SetActive(false);
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
        Time.timeScale = 0f; // Freezes physics and animations 

        pausePanel.SetActive(true);
        ShowMainPause();

        // Use your InputManager to freeze camera/movement
        InputManager.Instance.DisableGameplay();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f; // Resumes game world

        pausePanel.SetActive(false);

        // Restore gameplay state
        InputManager.Instance.EnableGameplay();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        // Ensure Main Menu is Index 0 in Build Settings 
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}