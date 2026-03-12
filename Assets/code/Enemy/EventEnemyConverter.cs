using UnityEngine;

/// <summary>
/// Chuyển đổi enemy thường thành event enemy và ngược lại bằng cách thay đổi các thuộc tính
/// và kích hoạt chế độ event của script EnemyMove.
/// </summary>
public class EventEnemyConverter : MonoBehaviour
{
    // Lưu trữ trạng thái ban đầu của enemy
    public struct OriginalEnemyState
    {
        public float originalHealth;
        public bool collisionAvoidanceEnabled;
    }

    private OriginalEnemyState savedState;
    private bool isConverted = false;

    // Components
    private EnemyController controller;
    private EnemyMove normalMove;
    private EnemyCollisionAvoidance avoidance;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private void Awake()
    {
        CacheComponents();
    }

    private void CacheComponents()
    {
        controller = GetComponent<EnemyController>();
        normalMove = GetComponent<EnemyMove>();
        avoidance = GetComponent<EnemyCollisionAvoidance>();
    }

    /// <summary>
    /// Chuyển enemy thường thành event enemy.
    /// </summary>
    public void ConvertToEventEnemy(Vector2 moveDirection, EventEnemySpawner spawner, float healthMultiplier, float eventSpeed)
    {
        if (isConverted) return;

        SaveOriginalState();

        // Tăng HP
        if (controller != null)
        {
            controller.ApplyHealthMultiplier(healthMultiplier);
        }

        // Kích hoạt chế độ event trên EnemyMove
        if (normalMove != null)
        {
            normalMove.SetSpeedOverride(eventSpeed); // Ghi đè tốc độ
            normalMove.EnableEventMode(moveDirection);
        }

        // Vô hiệu hóa các script không cần thiết
        if (avoidance != null)
        {
            avoidance.enabled = false;
        }

        // Thêm component EventEnemy để quản lý vòng đời
        EventEnemy eventEnemy = GetComponent<EventEnemy>() ?? gameObject.AddComponent<EventEnemy>();
        eventEnemy.Initialize(moveDirection, spawner);

        isConverted = true;

        if (showDebugInfo)
            Debug.Log($"[EventEnemyConverter] {gameObject.name} đã được chuyển thành event enemy!");
    }

    /// <summary>
    /// Chuyển event enemy trở lại thành enemy thường.
    /// </summary>
    public void RevertToNormalEnemy()
    {
        if (!isConverted) return;

        // Tắt chế độ event trên EnemyMove
        if (normalMove != null)
        {
            normalMove.DisableEventMode();
            // Tốc độ gốc sẽ được tự động khôi phục từ EnemyData trong EnemyController
        }

        // Restore HP về ban đầu
        if (controller != null && controller.IsAlive)
        {
            float currentHealth = controller.CurrentHealth;
            if (currentHealth > savedState.originalHealth)
            {
                controller.TakeDamage(currentHealth - savedState.originalHealth);
            }
        }

        // Bật lại các script đã tắt
        if (avoidance != null)
        {
            avoidance.enabled = savedState.collisionAvoidanceEnabled;
        }

        // Vô hiệu hóa EventEnemy
        EventEnemy eventEnemy = GetComponent<EventEnemy>();
        if (eventEnemy != null) eventEnemy.enabled = false;

        isConverted = false;

        if (showDebugInfo)
            Debug.Log($"[EventEnemyConverter] {gameObject.name} đã được chuyển về enemy thường!");
    }

    private void SaveOriginalState()
    {
        savedState = new OriginalEnemyState
        {
            originalHealth = controller != null ? controller.CurrentHealth : 0f,
            collisionAvoidanceEnabled = avoidance != null && avoidance.enabled,
        };
    }

    /// <summary>
    /// Destroy enemy này khi event kết thúc (không drop exp).
    /// </summary>
    public void DestroyAsEventEnemy()
    {
        EventEnemy eventEnemy = GetComponent<EventEnemy>();
        if (eventEnemy != null)
        {
            eventEnemy.DestroyWithoutDrop();
        }
        else
        {
            if (controller != null) controller.enabled = false;
            Destroy(gameObject);
        }
    }

    public bool IsEventEnemy()
    {
        return isConverted;
    }
}
