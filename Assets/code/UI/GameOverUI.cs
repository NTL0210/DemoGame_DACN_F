using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controls the Game Over UI and its unscaled-time animations.
/// Expected hierarchy (can auto-find):
/// GameOver
/// └── Panel
///     ├── GameOverText (TMP_Text)
///     ├── PlayAgainButton (Button)
///     └── BackMainMenuButton (Button)
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Root References")]
    [SerializeField] private GameObject gameOverRoot; // parent object "GameOver"
    [SerializeField] private RectTransform panel;     // child "Panel"
    [SerializeField] private TMP_Text gameOverText;   // child "GameOverText"
    [SerializeField] private Button playAgainButton;  // child "PlayAgainButton"
    [SerializeField] private Button backMainMenuButton; // child "BackMainMenuButton"

    [Header("Animation Settings")]
    [SerializeField] private float textFadeDuration = 0.35f;
    [SerializeField] private float textPopStartScale = 0.85f;
    [SerializeField] private float textPopEndScale = 1.0f;
    [SerializeField] private float buttonFadeDuration = 0.25f;
    [SerializeField] private float button1Delay = 0.3f; // seconds after text shown
    [SerializeField] private float button2Delay = 0.6f; // seconds after text shown

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // fallback to buildIndex 0 if not found

    private CanvasGroup textCg;
    private CanvasGroup btn1Cg;
    private CanvasGroup btn2Cg;

    private bool isShowing = false;

    public static GameOverUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        AutoFindHierarchy();
        EnsureCanvasGroups();
        WireButtons();
        InstantHide();
    }

    private void AutoFindHierarchy()
    {
        // Try to infer references if not set in inspector
        if (gameOverRoot == null)
        {
            Transform t = transform;
            if (name == "GameOver") gameOverRoot = gameObject;
            else
            {
                var found = GameObject.Find("GameOver");
                if (found != null) gameOverRoot = found;
                else gameOverRoot = gameObject; // fallback to current
            }
        }

        if (panel == null)
        {
            var panelTf = gameOverRoot.transform.Find("Panel");
            if (panelTf != null) panel = panelTf as RectTransform;
        }

        if (gameOverText == null && panel != null)
        {
            var textTf = panel.Find("GameOverText");
            if (textTf != null) gameOverText = textTf.GetComponent<TMP_Text>();
        }

        if (playAgainButton == null && panel != null)
        {
            var b1 = panel.Find("PlayAgainButton");
            if (b1 != null) playAgainButton = b1.GetComponent<Button>();
        }

        if (backMainMenuButton == null && panel != null)
        {
            var b2 = panel.Find("BackMainMenuButton");
            if (b2 != null) backMainMenuButton = b2.GetComponent<Button>();
        }
    }

    private void EnsureCanvasGroups()
    {
        if (gameOverText != null)
        {
            textCg = gameOverText.GetComponent<CanvasGroup>();
            if (textCg == null) textCg = gameOverText.gameObject.AddComponent<CanvasGroup>();
        }
        if (playAgainButton != null)
        {
            btn1Cg = playAgainButton.GetComponent<CanvasGroup>();
            if (btn1Cg == null) btn1Cg = playAgainButton.gameObject.AddComponent<CanvasGroup>();
        }
        if (backMainMenuButton != null)
        {
            btn2Cg = backMainMenuButton.GetComponent<CanvasGroup>();
            if (btn2Cg == null) btn2Cg = backMainMenuButton.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void WireButtons()
    {
        if (playAgainButton != null)
        {
            // Nếu button đã có script riêng thì không wire handler nội bộ để tránh double-load
            if (playAgainButton.GetComponent<ReloadCurrentSceneButton>() == null)
            {
                playAgainButton.onClick.RemoveListener(OnClickPlayAgain);
                playAgainButton.onClick.AddListener(OnClickPlayAgain);
            }
        }
        if (backMainMenuButton != null)
        {
            // Nếu button đã có script riêng thì không wire handler nội bộ để tránh double-load
            if (backMainMenuButton.GetComponent<LoadMainMenuButton>() == null)
            {
                backMainMenuButton.onClick.RemoveListener(OnClickMainMenu);
                backMainMenuButton.onClick.AddListener(OnClickMainMenu);
            }
        }
    }

    private void InstantHide()
    {
        if (gameOverRoot != null) gameOverRoot.SetActive(false);
        if (textCg != null) textCg.alpha = 0f;
        if (btn1Cg != null)
        {
            btn1Cg.alpha = 0f;
            if (playAgainButton != null) playAgainButton.interactable = false;
        }
        if (btn2Cg != null)
        {
            btn2Cg.alpha = 0f;
            if (backMainMenuButton != null) backMainMenuButton.interactable = false;
        }
        if (gameOverText != null)
        {
            gameOverText.rectTransform.localScale = Vector3.one * textPopStartScale;
        }
    }

    public void ShowGameOver()
    {
        if (isShowing) return;
        if (!EndGameUIState.TrySet("GameOver")) return; // Đã có Victory/End khác → không hiển thị
        isShowing = true;
        // Bật root và Panel để đảm bảo hiển thị ngay cả khi Panel bị set inactive sẵn trong scene
        if (gameOverRoot != null) gameOverRoot.SetActive(true);
        if (panel != null) panel.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(ShowSequence());
    }

    private IEnumerator ShowSequence()
    {
        // Text fade + pop using unscaled time
        yield return StartCoroutine(FadeCanvasGroup(textCg, 1f, textFadeDuration));
        yield return StartCoroutine(ScaleRect(gameOverText.rectTransform, textPopStartScale, textPopEndScale, textFadeDuration));

        // Buttons appear with staggered delays
        yield return new WaitForSecondsRealtime(button1Delay);
        if (btn1Cg != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(btn1Cg, 1f, buttonFadeDuration));
            if (playAgainButton != null) playAgainButton.interactable = true;
        }

        float remainingDelay = Mathf.Max(0f, button2Delay - button1Delay);
        yield return new WaitForSecondsRealtime(remainingDelay);
        if (btn2Cg != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(btn2Cg, 1f, buttonFadeDuration));
            if (backMainMenuButton != null) backMainMenuButton.interactable = true;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float target, float duration)
    {
        if (cg == null) yield break;
        float start = cg.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            cg.alpha = Mathf.Lerp(start, target, k);
            yield return null;
        }
        cg.alpha = target;
    }

    private IEnumerator ScaleRect(RectTransform rt, float from, float to, float duration)
    {
        if (rt == null) yield break;
        float t = 0f;
        Vector3 a = Vector3.one * from;
        Vector3 b = Vector3.one * to;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            // Ease-out slightly
            float e = 1f - Mathf.Pow(1f - k, 2f);
            rt.localScale = Vector3.LerpUnclamped(a, b, e);
            yield return null;
        }
        rt.localScale = b;
    }

    // Button hooks
    public void OnClickPlayAgain()
    {
        EndGameUIState.Reset(); // resume timeScale
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    public void OnClickMainMenu()
    {
        EndGameUIState.Reset(); // resume timeScale
        if (!string.IsNullOrEmpty(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }
        // Fallback to first scene
        SceneManager.LoadScene(0);
    }
}

