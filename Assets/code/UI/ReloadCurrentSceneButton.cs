using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a Button to reload current scene.
/// Safe to click while Time.timeScale == 0 (UI uses unscaled time).
/// </summary>
[RequireComponent(typeof(Button))]
public class ReloadCurrentSceneButton : MonoBehaviour
{
    [Header("Auto Wire")]
    [SerializeField] private bool autoWireOnClick = true;

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        if (autoWireOnClick && btn != null)
        {
            btn.onClick.RemoveListener(OnClickReload);
            btn.onClick.AddListener(OnClickReload);
        }
    }

    private void OnDestroy()
    {
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnClickReload);
        }
    }

    /// <summary>
    /// Public method to be hooked from Inspector if preferred.
    /// </summary>
    public void OnClickReload()
    {
        // Không bỏ pause ngay. Chờ fade-in xong ở scene mới rồi mới timeScale = 1
        var scene = SceneManager.GetActiveScene();
        SceneTransition.LoadSceneWithFade(scene.name, -1f, () => { Time.timeScale = 1f; });
    }
}

