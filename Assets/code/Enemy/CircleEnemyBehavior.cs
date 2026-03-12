using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý hành vi của enemy trong Event Loại 2 (Circle Surround Event)
/// 
/// Hành vi:
/// 1. Di chuyển từ vị trí spawn đến vị trí trên vòng tròn
/// 2. Khi vào đúng vòng tròn, đứng yên vĩnh viễn để tạo "vòng giam"
/// 3. KHÔNG truy đuổi/tấn công player; chỉ đóng vai trò chướng ngại vật
/// 4. Giữ nguyên hình tròn cố định quanh tâm đã chụp khi event bắt đầu
/// </summary>
public class CircleEnemyBehavior : MonoBehaviour
{
    private Vector2 targetPosition; // Vị trí đích trên vòng tròn
    private float angleRad; // Góc trên vòng tròn (để tính toán vị trí)
    private float movementSpeed;
    private EnemyCircleSurroundEvent eventManager;

    // State
    private bool hasReachedCircle = false;

    // Components
    private Rigidbody2D rb;
    private EnemyMove enemyMove;
    private EnemyController controller;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    public void Initialize(Vector2 target, float angle, float speed, EnemyCircleSurroundEvent manager)
    {
        targetPosition = target;
        angleRad = angle;
        movementSpeed = speed;
        eventManager = manager;

        rb = GetComponent<Rigidbody2D>();
        enemyMove = GetComponent<EnemyMove>();
        controller = GetComponent<EnemyController>();

        if (showDebugInfo)
        {
            Debug.Log($"[CircleEnemyBehavior] Enemy initialized. Target: {target}, Speed: {speed}");
        }
    }

    private void FixedUpdate()
    {
        if (!controller.IsAlive) return;
        MoveTowardTarget();
    }

    /// <summary>
    /// Di chuyển enemy đến/giữ vị trí trên vòng tròn (cập nhật liên tục nếu tâm/radius thay đổi)
    /// </summary>
    private void MoveTowardTarget()
    {
        if (rb == null) return;

        Vector2 currentPos = rb.position;
        Vector2 toTarget = (targetPosition - currentPos);
        float distance = toTarget.magnitude;

        if (distance < 0.1f)
        {
            hasReachedCircle = true;
            rb.linearVelocity = Vector2.zero;

            // Thông báo cho event manager chỉ 1 lần khi enemy đã vào vòng
            if (eventManager != null && !_notifiedReached)
            {
                _notifiedReached = true;
                eventManager.NotifyMemberReachedCircle();
            }
        }
        else
        {
            Vector2 direction = toTarget.normalized;
            rb.linearVelocity = direction * movementSpeed;

            if (enemyMove != null)
            {
                enemyMove.SetFacingDirection(direction.x > 0 ? 1 : -1);
            }
        }
    }

    // Chặn thông báo lặp
    private bool _notifiedReached = false;

    /// <summary>
    /// Cập nhật lại điểm đích khi camera/center thay đổi, giữ nguyên góc angleRad
    /// </summary>
    public void UpdateLockedTarget(Vector2 center, float radius)
    {
        targetPosition = center + new Vector2(Mathf.Cos(angleRad) * radius, Mathf.Sin(angleRad) * radius);
    }

    private void OnDestroy()
    {
        // Thông báo event manager khi enemy bị destroy
        if (eventManager != null)
        {
            eventManager.RemoveCircleEnemy(gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        // Vẽ vị trí đích
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(targetPosition, 0.3f);

        // Vẽ đường từ vị trí hiện tại đến đích
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, targetPosition);
    }
}

