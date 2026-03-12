using System.Collections;
using UnityEngine;

/// <summary>
/// Lắng nghe TimerManager.OnTimerComplete: nếu player còn sống, sau 0.3s hiển thị VictoryUI.
/// Gắn script này vào một GameObject trong scene (ví dụ UI root).
/// </summary>
public class TimerVictoryListener : MonoBehaviour
{
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private float showDelay = 0.3f;

    private bool victoryShown = false;

    private void Awake()
    {
        if (timerManager == null) timerManager = FindFirstObjectByType<TimerManager>();
        if (healthSystem == null) healthSystem = FindFirstObjectByType<HealthSystem>();
    }

    private void OnEnable()
    {
        if (timerManager != null)
            timerManager.OnTimerComplete += HandleTimerComplete;
    }

    private void OnDisable()
    {
        if (timerManager != null)
            timerManager.OnTimerComplete -= HandleTimerComplete;
    }

    private void HandleTimerComplete()
    {
        if (victoryShown) return;
        if (healthSystem != null && !healthSystem.IsAlive()) return; // player đã chết → không show victory
        StartCoroutine(ShowVictoryDelayed());
    }

    private IEnumerator ShowVictoryDelayed()
    {
        victoryShown = true;
        yield return new WaitForSecondsRealtime(showDelay);

        if (healthSystem != null && !healthSystem.IsAlive())
        {
            victoryShown = false; // nếu sau delay player chết thì không show
            yield break;
        }

        VictoryUI.Instance?.ShowVictory();
    }
}

