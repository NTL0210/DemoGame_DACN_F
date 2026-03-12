using UnityEngine;

/// <summary>
/// Trạng thái Enemy đang di chuyển theo player
/// </summary>
public class EnemyMovingState : MonoBehaviour, IEnemyState
{
    private EnemyMove enemyMove;
    private EnemyCollisionAvoidance collisionAvoidance;
    private EnemyController enemyController;
    
    private void Awake()
    {
        enemyMove = GetComponentInParent<EnemyMove>();
        collisionAvoidance = GetComponentInParent<EnemyCollisionAvoidance>();
        enemyController = GetComponentInParent<EnemyController>();
    }
    
    public void Enter()
    {
        // Bắt đầu di chuyển
        if (enemyMove != null)
        {
            enemyMove.StartMoving();
        }
    }
    
    public void Update()
    {
        // Kiểm tra có còn sống không
        if (enemyController != null && !enemyController.IsAlive)
        {
            // Chuyển sang trạng thái chết
            var stateMachine = GetComponentInParent<EnemyStateMachine>();
            if (stateMachine != null)
            {
                var deathState = GetComponentInParent<EnemyDeathState>();
                if (deathState != null)
                {
                    stateMachine.ChangeState(deathState);
                }
            }
            return;
        }
        
        // Logic di chuyển được xử lý bởi EnemyCollisionAvoidance
        // Chỉ cần đảm bảo animation được cập nhật
    }
    
    public void FixedUpdate()
    {
        // Physics được xử lý bởi EnemyCollisionAvoidance
    }
    
    public void Exit()
    {
        // Dừng di chuyển
        if (enemyMove != null)
        {
            enemyMove.StopMoving();
        }
    }
}
