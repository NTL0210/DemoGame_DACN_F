using UnityEngine;
using System.Collections;

/// <summary>
/// Trạng thái Death - chỉ chạy 1 lần, khóa điều khiển và animation khác
/// </summary>
public class PlayerDeathState : MonoBehaviour, IPlayerState
{
    [Header("Animator Parameters")]
    [SerializeField] private string triggerDeath = "Death"; // Trigger trong Animator

    [Header("Skill References")]
    [SerializeField] private FlameAttackManager flameAttackManager;

    private PlayerMoveNew playerMove;
    private Animator animator;
    private Rigidbody2D rb;
    private bool hasPlayed; // đảm bảo chỉ chạy 1 lần
    private bool gameOverShown; // đã hiển thị GameOver/pause chưa

    [Header("Fail-safe")]
    [SerializeField] private float failSafeDelay = 2f; // Nếu sau X giây chưa có Animation Event thì tự hiện GameOver
    private Coroutine failSafeCo;

    private void Awake()
    {
        playerMove = GetComponentInParent<PlayerMoveNew>();
        animator = GetComponentInParent<Animator>();
        rb = GetComponentInParent<Rigidbody2D>();

        // Tự động tìm FlameAttackManager nếu chưa được gán
        if (flameAttackManager == null)
        {
            flameAttackManager = FindObjectOfType<FlameAttackManager>();
        }
    }

    public void Enter()
    {
        if (hasPlayed) return;

        hasPlayed = true;

        if (playerMove != null)
        {
            playerMove.StopMovement();
            playerMove.ApplyFlipForCurrentDirection();
            playerMove.SetWeaponActive(false);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (animator != null && !string.IsNullOrEmpty(triggerDeath))
        {
            animator.SetBool("idle", false);
            animator.SetBool("move", false);
            animator.ResetTrigger(triggerDeath);
            animator.SetTrigger(triggerDeath);
        }

        // Khởi động fail-safe: nếu sau một khoảng thời gian chưa có Animation Event thì tự show GameOver
        if (failSafeCo == null)
        {
            failSafeCo = StartCoroutine(FailSafeRoutine());
        }
    }

    /// <summary>
    /// Hàm này sẽ được gọi bởi Animation Event ở cuối animation chết.
    /// </summary>
    public void OnDeathAnimationFinished()
    {
        if (gameOverShown) return; // tránh chạy lặp
        gameOverShown = true;

        // Ngừng fail-safe nếu đang chạy
        if (failSafeCo != null)
        {
            StopCoroutine(failSafeCo);
            failSafeCo = null;
        }

        // Tắt các hệ thống kỹ năng
        if (flameAttackManager != null)
        {
            flameAttackManager.gameObject.SetActive(false);
        }

        // Dừng game lại ngay sau khi animation kết thúc
        Time.timeScale = 0f;
        Debug.Log("Game Paused. Player has died.");

        // Hiện Game Over UI (dùng unscaled time nên vẫn chạy animation UI bình thường)
        if (GameOverUI.Instance != null)
        {
            GameOverUI.Instance.ShowGameOver();
        }
        else
        {
            Debug.LogWarning("GameOverUI.Instance not found in scene. Please add GameOverUI component to the GameOver object.");
        }
    }

    private IEnumerator FailSafeRoutine()
    {
        float t = 0f;
        while (t < failSafeDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!gameOverShown)
        {
            OnDeathAnimationFinished();
        }
    }

    public void Update()
    {
        // Không làm gì
    }

    public void FixedUpdate()
    {
        // Không làm gì
    }

    public void Exit()
    {
        // Reset lại trạng thái để có thể chết lần nữa nếu game được chơi lại
        hasPlayed = false;
        gameOverShown = false;

        // Hủy fail-safe nếu còn đang chạy
        if (failSafeCo != null)
        {
            StopCoroutine(failSafeCo);
            failSafeCo = null;
        }

        // Khôi phục lại time scale nếu cần (ví dụ: khi nhấn nút chơi lại)
        Time.timeScale = 1f;
    }
}
