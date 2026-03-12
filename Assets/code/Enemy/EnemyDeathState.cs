using UnityEngine;

/// <summary>
/// Trạng thái Enemy đã chết - Không có animation, chỉ destroy ngay lập tức
/// </summary>
public class EnemyDeathState : MonoBehaviour, IEnemyState
{
    private EnemyMove enemyMove;
    private EnemyCollisionAvoidance collisionAvoidance;
    private EnemyController enemyController;
    private Rigidbody2D rb;
    
    private void Awake()
    {
        enemyMove = GetComponentInParent<EnemyMove>();
        collisionAvoidance = GetComponentInParent<EnemyCollisionAvoidance>();
        enemyController = GetComponentInParent<EnemyController>();
        rb = GetComponentInParent<Rigidbody2D>();
    }
    
    public void Enter()
    {
        // Dừng tất cả di chuyển
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        if (enemyMove != null)
        {
            enemyMove.StopMoving();
        }
        
        // Tắt collision avoidance
        if (collisionAvoidance != null)
        {
            collisionAvoidance.enabled = false;
        }
        
        // Không có animation chết, chỉ destroy ngay lập tức
        if (enemyController != null)
        {
            // Destroy enemy ngay lập tức
            Destroy(enemyController.gameObject);
        }
    }
    
    public void Update()
    {
        // Không làm gì khi đã chết
    }
    
    public void FixedUpdate()
    {
        // Không xử lý physics khi đã chết
    }
    
    public void Exit()
    {
        // Không thể thoát khỏi trạng thái chết
    }
}
