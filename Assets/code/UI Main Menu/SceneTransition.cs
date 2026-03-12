using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiệu ứng chuyển cảnh Fade In/Out hoàn toàn bằng code (không bắt buộc tạo UI trước)
/// - Tự tạo Canvas + Image đen + CanvasGroup nếu chưa có
/// - Singleton và DontDestroyOnLoad để dùng xuyên suốt
/// </summary>
public class SceneTransition : MonoBehaviour
{
    [Header("Cài đặt")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;

    private static SceneTransition instance;

    private void Awake()
    {
        // Singleton
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Bảo đảm có overlay để fade
        BuildOverlayIfNeeded();
    }

    private void Start()
    {
        // Fade in khi vừa vào scene
        if (fadeCanvasGroup != null)
        {
            StartCoroutine(FadeIn());
        }
    }

    // Tạo overlay nếu chưa có (Canvas + Image đen + CanvasGroup)
    private void BuildOverlayIfNeeded()
    {
        if (fadeCanvasGroup != null) return;

        // Tạo Canvas cha
        var canvasGO = new GameObject("SceneTransition_Canvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000; // Luôn trên cùng
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Tạo Image đen full màn hình
        var imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        var rect = imgGO.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = imgGO.AddComponent<Image>();
        image.color = Color.black;

        // CanvasGroup để điều chỉnh alpha và chặn input
        fadeCanvasGroup = imgGO.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 1f; // Bắt đầu đen, rồi FadeIn ở Start
        fadeCanvasGroup.blocksRaycasts = true; // chặn input khi đang fade
    }

    // Tạo instance khi được gọi tĩnh lần đầu (không cần đặt sẵn trong scene)
    private static SceneTransition CreateRuntimeInstance()
    {
        var go = new GameObject("SceneTransition");
        var comp = go.AddComponent<SceneTransition>();
        DontDestroyOnLoad(go);
        comp.BuildOverlayIfNeeded();
        return comp;
    }

    /// <summary>
    /// Load scene với hiệu ứng fade (theo tên)
    /// </summary>
    public static void LoadSceneWithFade(string sceneName, float duration = -1f)
    {
        if (instance == null)
        {
            instance = CreateRuntimeInstance();
        }

        float finalDuration = duration > 0 ? duration : instance.fadeDuration;
        instance.StartCoroutine(instance.TransitionToScene(sceneName, finalDuration, null));
    }

    /// <summary>
    /// Load scene với fade (theo tên) và gọi callback sau khi fade-in hoàn tất
    /// </summary>
    public static void LoadSceneWithFade(string sceneName, float duration, System.Action onFadeInComplete)
    {
        if (instance == null)
        {
            instance = CreateRuntimeInstance();
        }

        float finalDuration = duration > 0 ? duration : instance.fadeDuration;
        instance.StartCoroutine(instance.TransitionToScene(sceneName, finalDuration, onFadeInComplete));
    }

    /// <summary>
    /// Load scene với hiệu ứng fade (theo build index)
    /// </summary>
    public static void LoadSceneWithFade(int buildIndex, float duration = -1f)
    {
        if (instance == null)
        {
            instance = CreateRuntimeInstance();
        }

        float finalDuration = duration > 0 ? duration : instance.fadeDuration;
        instance.StartCoroutine(instance.TransitionToScene(buildIndex, finalDuration));
    }

    /// <summary>
    /// Load scene với fade (theo build index) và gọi callback sau fade-in
    /// </summary>
    public static void LoadSceneWithFade(int buildIndex, float duration, System.Action onFadeInComplete)
    {
        if (instance == null)
        {
            instance = CreateRuntimeInstance();
        }

        float finalDuration = duration > 0 ? duration : instance.fadeDuration;
        instance.StartCoroutine(instance.TransitionToScene(buildIndex, finalDuration, onFadeInComplete));
    }

    /// <summary>
    /// Coroutine chuyển scene với fade out -> load -> fade in (theo tên)
    /// </summary>
    private IEnumerator TransitionToScene(string sceneName, float duration)
    {
        // Backward compatible path (no callback)
        yield return StartCoroutine(TransitionToScene(sceneName, duration, null));
    }

    private IEnumerator TransitionToScene(string sceneName, float duration, System.Action onFadeInComplete)
    {
        // Fade out (sync music)
        if (MusicManager.Instance != null) MusicManager.Instance.FadeOut(duration);
        yield return StartCoroutine(FadeOut(duration));

        // Load scene thực (màn hình đang đen hoàn toàn)
        SceneManager.LoadScene(sceneName);

        // RESET NHẠC NGAY Ở ĐIỂM GIỮA (màn hình đen, trước khi fade-in)
        if (MusicManager.Instance != null) MusicManager.Instance.ResetToStart(true);

        // Chờ một frame để đảm bảo scene đã lên, rồi fade in
        yield return null;
        StartCoroutine(FadeIn(duration, onFadeInComplete));
    }

    /// <summary>
    /// Coroutine chuyển scene với fade out -> load -> fade in (theo build index)
    /// </summary>
    private IEnumerator TransitionToScene(int buildIndex, float duration)
    {
        // Backward compatible path (no callback)
        yield return StartCoroutine(TransitionToScene(buildIndex, duration, null));
    }

    private IEnumerator TransitionToScene(int buildIndex, float duration, System.Action onFadeInComplete)
    {
        // Fade out (sync music)
        if (MusicManager.Instance != null) MusicManager.Instance.FadeOut(duration);
        yield return StartCoroutine(FadeOut(duration));

        // Load scene thực (màn hình đang đen hoàn toàn)
        SceneManager.LoadScene(buildIndex);

        // RESET NHẠC NGAY Ở ĐIỂM GIỮA (màn hình đen, trước khi fade-in)
        if (MusicManager.Instance != null) MusicManager.Instance.ResetToStart(true);

        // Chờ một frame để đảm bảo scene đã lên, rồi fade in
        yield return null;
        StartCoroutine(FadeIn(duration, onFadeInComplete));
    }

    /// <summary>
    /// Fade từ trong suốt -> đen
    /// </summary>
    private IEnumerator FadeOut(float duration = -1f)
    {
        if (fadeCanvasGroup == null) yield break;

        float finalDuration = duration > 0 ? duration : fadeDuration;
        float elapsed = 0f;

        fadeCanvasGroup.blocksRaycasts = true;

        while (elapsed < finalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / finalDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Fade từ đen -> trong suốt
    /// </summary>
    private IEnumerator FadeIn(float duration = -1f)
    {
        yield return StartCoroutine(FadeIn(duration, null));
    }

    private IEnumerator FadeIn(float duration, System.Action onComplete)
    {
        if (fadeCanvasGroup == null) yield break;

        float finalDuration = duration > 0 ? duration : fadeDuration;
        float elapsed = 0f;

        // Sync music fade-in with screen fade-in
        if (MusicManager.Instance != null) MusicManager.Instance.FadeIn(finalDuration);

        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = true;

        while (elapsed < finalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / finalDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Thoát game với hiệu ứng fade
    /// </summary>
    public static void QuitGameWithFade(float duration = -1f)
    {
        if (instance == null)
        {
            instance = CreateRuntimeInstance();
        }

        float finalDuration = duration > 0 ? duration : instance.fadeDuration;
        instance.StartCoroutine(instance.QuitAfterFade(finalDuration));
    }

    private IEnumerator QuitAfterFade(float duration)
    {
        if (MusicManager.Instance != null) MusicManager.Instance.FadeOut(duration);
        yield return StartCoroutine(FadeOut(duration));
        QuitGame();
    }

    private static void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
