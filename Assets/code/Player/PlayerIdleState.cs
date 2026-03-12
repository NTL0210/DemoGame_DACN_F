using UnityEngine;

/// <summary>
/// Trạng thái Idle - Player đứng yên
/// </summary>
public class PlayerIdleState : MonoBehaviour, IPlayerState
{
    private PlayerMoveNew playerMove;
    private Animator animator;
    
    private void Awake()
    {
        // Lấy tham chiếu khi được thêm như một Component
        playerMove = GetComponentInParent<PlayerMoveNew>();
        if (playerMove != null)
        {
            animator = playerMove.GetComponent<Animator>();
        }
    }
    
    public void Enter()
    {
        // Đặt animation idle và dừng di chuyển
        if (animator != null)
        {
            animator.SetBool("idle", true);
            animator.SetBool("move", false);
        }
        
        // Dừng di chuyển hoàn toàn
        if (playerMove != null)
        {
            playerMove.StopMovement();
        }
    }
    
    public void Update()
    {
        // Idle không cần xử lý gì thêm
    }
    
    public void FixedUpdate()
    {
        // Không cần xử lý physics trong idle
    }
    
    public void Exit()
    {
        // Không cần xử lý gì khi thoát Idle
    }
}
