using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Hiển thị màn Victory với hiệu ứng tương tự GameOverUI.
/// Kỳ vọng hierarchy:
/// Victory
/// └── Panel
///     ├── VictoryText (TMP_Text)  (fallback: GameOverText)
///     ├── PlayAgainButton (Button)
///     └── BackMainMenuButton (Button)
/// </summary>
public class VictoryUI : MonoBehaviour
{
    [Header("Root References")]
    [SerializeField] private GameObject victoryRoot;          // parent object "Victory"
    [SerializeField] private RectTransform panel;             // child "Panel"
    [SerializeField] private TMP_Text victoryText;            // child "VictoryText" (hoặc GameOverText nếu tái dùng prefab)
    [SerializeField] private Button playAgainButton;          // child "PlayAgainButton"
    [SerializeField] private Button backMainMenuButton;       // child "BackMainMenuButton"

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

    public static VictoryUI Instance { get; private set; }

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
        if (victoryRoot == null)
        {
            Transform t = transform;
            if (name == "Victory") victoryRoot = gameObject;
            else
            {
                var found = GameObject.Find("Victory");
                if (found != null) victoryRoot = found;
                else victoryRoot = gameObject; // fallback
            }
        }

        if (panel == null && victoryRoot != null)
        {
            var panelTf = victoryRoot.transform.Find("Panel");
            if (panelTf != null) panel = panelTf as RectTransform;
        }

        if (victoryText == null && panel != null)
        {
            var textTf = panel.Find("VictoryText");
            if (textTf == null) textTf = panel.Find("GameOverText"); // fallback nếu tái dùng UI cũ
            if (textTf != null) victoryText = textTf.GetComponent<TMP_Text>();
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
        if (victoryText != null)
        {
            textCg = victoryText.GetComponent<CanvasGroup>();
            if (textCg == null) textCg = victoryText.gameObject.AddComponent<CanvasGroup>();
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
            if (playAgainButton.GetComponent<ReloadCurrentSceneButton>() == null)
            {
                playAgainButton.onClick.RemoveListener(OnClickPlayAgain);
                playAgainButton.onClick.AddListener(OnClickPlayAgain);
            }
        }
        if (backMainMenuButton != null)
        {
            if (backMainMenuButton.GetComponent<LoadMainMenuButton>() == null)
            {
                backMainMenuButton.onClick.RemoveListener(OnClickMainMenu);
                backMainMenuButton.onClick.AddListener(OnClickMainMenu);
            }
        }
    }

    private void InstantHide()
    {
        if (victoryRoot != null) victoryRoot.SetActive(false);
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
        if (victoryText != null)
        {
            victoryText.rectTransform.localScale = Vector3.one * textPopStartScale;
        }
    }

    public void ShowVictory()
    {
        if (isShowing) return;
        if (!EndGameUIState.TrySet("Victory")) return; // Nếu đã có GameOver/Victory khác thì không hiển thị
        isShowing = true;
        if (victoryRoot != null) victoryRoot.SetActive(true);
        if (panel != null) panel.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(ShowSequence());
    }

    private IEnumerator ShowSequence()
    {
        // Text fade + pop using unscaled time
        yield return StartCoroutine(FadeCanvasGroup(textCg, 1f, textFadeDuration));
        if (victoryText != null)
            yield return StartCoroutine(ScaleRect(victoryText.rectTransform, textPopStartScale, textPopEndScale, textFadeDuration));

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
            float e = 1f - Mathf.Pow(1f - k, 2f); // ease-out nhẹ
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
        SceneManager.LoadScene(0);
    }
}

