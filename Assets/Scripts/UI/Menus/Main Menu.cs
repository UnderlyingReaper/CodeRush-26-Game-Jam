using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject controlsPanel;

    private void Start()
    {
        ShowMain();
    }

    // --- Button Callbacks ---

    public void OnPlayClicked()
    {
        SceneManager.LoadScene("Level 1");
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
}