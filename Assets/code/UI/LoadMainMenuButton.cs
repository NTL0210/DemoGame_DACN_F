using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a Button to load a target menu scene by name with fallback to build index 0.
/// Reusable across any UI. Safe while Time.timeScale == 0.
/// </summary>
[RequireComponent(typeof(Button))]
public class LoadMainMenuButton : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string targetSceneName = "MainMenu";
    [SerializeField] private bool fallbackToFirstBuildIndex = true;

    [Header("Auto Wire")]
    [SerializeField] private bool autoWireOnClick = true;

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        if (autoWireOnClick && btn != null)
        {
            btn.onClick.RemoveListener(OnClickLoad);
            btn.onClick.AddListener(OnClickLoad);
        }
    }

    private void OnDestroy()
    {
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnClickLoad);
        }
    }

    /// <summary>
    /// Public method to be hooked from Inspector if preferred.
    /// </summary>
    public void OnClickLoad()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(targetSceneName) && Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            SceneTransition.LoadSceneWithFade(targetSceneName, -1f, () => { Time.timeScale = 1f; });
            return;
        }

        if (fallbackToFirstBuildIndex)
        {
            SceneTransition.LoadSceneWithFade(0, -1f, () => { Time.timeScale = 1f; });
        }
    }
}

